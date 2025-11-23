using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ThumbsUpApi.Configuration;
using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Services;

public class OpenAiThemeService : IImageThemeService
{
    private readonly AiOptions _options;
    private readonly IOpenAiClient _client;
    private readonly ILogger<OpenAiThemeService> _logger;

    public OpenAiThemeService(Microsoft.Extensions.Options.IOptions<AiOptions> options, IOpenAiClient client, ILogger<OpenAiThemeService> logger)
    {
        _options = options.Value;
        _client = client;
        _logger = logger;
    }

    private const string StructuredPrompt = "You are a creative director describing marketing visuals. Return STRICT JSON with keys subjects, vibes, notableElements, colors, and keywords (each an array of <=5 short lowercase phrases). No commentary.";
    private const string FallbackPrompt = "List 5-10 concise lowercase style/theme keywords for this image. Return a comma-separated list only.";

    public async Task<ThemeInsights> ExtractThemesAsync(string physicalPath, CancellationToken ct = default)
    {
        var model = _options.VisionModel ?? _options.TextModel ?? "gpt-4o-mini";
        byte[] imageBytes = Array.Empty<byte>();
        string? fileId = null;

        try
        {
            imageBytes = await File.ReadAllBytesAsync(physicalPath, ct);

            await using var stream = new MemoryStream(imageBytes, writable: false);
            var upload = await _client.UploadFileAsync(stream, Path.GetFileName(physicalPath), "vision", ct)
                         ?? throw new InvalidOperationException("OpenAI returned no payload for file upload");
            fileId = upload.Id;

            var structuredText = await SendVisionRequestAsync(model, StructuredPrompt, fileId, ct);
            var structuredInsights = ThemeInsights.FromModelResponse(structuredText);
            if (structuredInsights.HasAnyData)
            {
                return structuredInsights;
            }

            _logger.LogWarning("Structured theme extraction returned empty for {Path}; retrying with fallback prompt", physicalPath);
            var fallbackText = await SendVisionRequestAsync(model, FallbackPrompt, fileId, ct);
            var fallbackTags = ParseFallbackTags(fallbackText);

            var heuristicInsights = BuildFallbackInsights(imageBytes, physicalPath);

            if (fallbackTags.Count > 0)
            {
                heuristicInsights.Keywords.AddRange(fallbackTags);
                return ThemeInsights.Combine(new[] { heuristicInsights });
            }

            if (heuristicInsights.HasAnyData)
            {
                return heuristicInsights;
            }

            _logger.LogWarning("Fallback theme extraction also returned empty for {Path}. Raw response: {ResponseSnippet}", physicalPath, Truncate(fallbackText, 200));
            return ThemeInsights.Empty;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI theme extraction exception for {Path}", physicalPath);
            if (imageBytes.Length > 0)
            {
                var heuristics = BuildFallbackInsights(imageBytes, physicalPath);
                if (heuristics.HasAnyData)
                {
                    _logger.LogInformation("Fell back to heuristic theme insights for {Path}", physicalPath);
                    return heuristics;
                }
            }

            return ThemeInsights.Empty;
        }
        finally
        {
            await DeleteUploadedFileAsync(fileId);
        }
    }

    private async Task<string> SendVisionRequestAsync(string model, string prompt, string? fileId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            throw new InvalidOperationException("Vision file reference missing for responses call.");
        }

        var request = new OpenAiResponseRequest
        {
            Model = model,
            Input = new[]
            {
                new OpenAiResponseMessage
                {
                    Role = "user",
                    Content = new[]
                    {
                        new OpenAiResponseContent { Type = "input_text", Text = prompt },
                        new OpenAiResponseContent { Type = "input_image", ImageFile = new OpenAiResponseImageFile { FileId = fileId } }
                    }
                }
            }
        };

        var payload = await _client.PostResponsesAsync<OpenAiResponseRequest, OpenAiResponsePayload>(request, ct);
        return payload?.GetFirstTextOutput() ?? string.Empty;
    }

    private async Task DeleteUploadedFileAsync(string? fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return;
        }

        try
        {
            await _client.DeleteFileAsync(fileId, CancellationToken.None);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogWarning(cleanupEx, "Failed to delete temporary OpenAI file {FileId}", fileId);
        }
    }

    private static List<string> ParseFallbackTags(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        return text
            .Split(new[] { ',', '\n', '\r', ';', '|', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Where(tag => tag.Length > 1 && tag.Length <= 50)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private ThemeInsights BuildFallbackInsights(byte[] bytes, string physicalPath)
    {
        try
        {
            using var image = Image.Load<Rgba32>(bytes);
            var samples = SamplePixels(image);
            if (samples.Count == 0)
            {
                return ThemeInsights.Empty;
            }

            var stats = ComputeImageStats(samples, image.Width * image.Height);

            var insights = new ThemeInsights();
            var aspectRatio = (double)image.Width / image.Height;

            insights.Subjects.Add(aspectRatio switch
            {
                > 1.2 => "landscape",
                < 0.8 => "portrait",
                _ => "square"
            });

            insights.Vibes.Add(stats.Brightness switch
            {
                >= 0.65 => "bright",
                <= 0.35 => "moody",
                _ => "balanced"
            });

            insights.Vibes.Add(stats.Saturation >= 0.35 ? "vibrant" : "muted");

            foreach (var color in stats.DominantColors)
            {
                insights.Colors.Add(color);
            }

            if (stats.Contrast >= 0.35)
            {
                insights.NotableElements.Add("high-contrast");
            }

            if (stats.Noise >= 0.3)
            {
                insights.NotableElements.Add("textured-background");
            }

            insights.Keywords.Add(stats.Resolution >= 3_000_000 ? "high-res" : "low-res");
            insights.Keywords.Add(aspectRatio >= 1 ? "wide-framing" : "tall-framing");

            _logger.LogInformation("Generated heuristic theme insights for {Path}", physicalPath);
            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate heuristic theme insights for {Path}", physicalPath);
            return ThemeInsights.Empty;
        }
    }

    private static List<Rgba32> SamplePixels(Image<Rgba32> image)
    {
        var samples = new List<Rgba32>();
        var stepX = Math.Max(1, image.Width / 64);
        var stepY = Math.Max(1, image.Height / 64);

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y += stepY)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x += stepX)
                {
                    samples.Add(row[x]);
                }
            }
        });

        return samples;
    }

    private static ImageStats ComputeImageStats(List<Rgba32> samples, double resolution)
    {
        var brightnessValues = new List<double>(samples.Count);
        var saturationValues = new List<double>(samples.Count);
        var colorBuckets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var sample in samples)
        {
            var (h, s, l) = ToHsl(sample);
            brightnessValues.Add(l);
            saturationValues.Add(s);

            if (s >= 0.12)
            {
                var color = CategorizeColor(h, l);
                if (!string.IsNullOrEmpty(color))
                {
                    colorBuckets.TryGetValue(color, out var count);
                    colorBuckets[color] = count + 1;
                }
            }
            else
            {
                colorBuckets.TryGetValue("neutral", out var neutralCount);
                colorBuckets["neutral"] = neutralCount + 1;
            }
        }

        var dominantColors = colorBuckets
            .OrderByDescending(kv => kv.Value)
            .Take(2)
            .Select(kv => kv.Key)
            .ToList();

        return new ImageStats
        {
            Brightness = brightnessValues.Average(),
            Saturation = saturationValues.Average(),
            Contrast = StdDev(brightnessValues),
            Noise = StdDev(saturationValues),
            DominantColors = dominantColors,
            Resolution = resolution
        };
    }

    private static (double h, double s, double l) ToHsl(Rgba32 color)
    {
        var r = color.R / 255d;
        var g = color.G / 255d;
        var b = color.B / 255d;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        var l = (max + min) / 2d;
        double h = 0;
        double s = 0;

        if (delta > 0.0001)
        {
            s = l > 0.5 ? delta / (2 - max - min) : delta / (max + min);

            if (Math.Abs(max - r) < double.Epsilon)
            {
                h = ((g - b) / delta + (g < b ? 6 : 0)) * 60;
            }
            else if (Math.Abs(max - g) < double.Epsilon)
            {
                h = ((b - r) / delta + 2) * 60;
            }
            else
            {
                h = ((r - g) / delta + 4) * 60;
            }
        }

        return (h, s, l);
    }

    private static string CategorizeColor(double hue, double lightness)
    {
        if (lightness <= 0.2)
        {
            return "deep";
        }

        if (lightness >= 0.85)
        {
            return "airy";
        }

        return hue switch
        {
            < 15 or >= 345 => "red",
            < 45 => "orange",
            < 75 => "yellow",
            < 150 => "green",
            < 200 => "teal",
            < 255 => "blue",
            < 300 => "purple",
            < 345 => "pink",
            _ => "neutral"
        };
    }

    private static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
        return Math.Sqrt(variance);
    }

    private sealed class ImageStats
    {
        public double Brightness { get; init; }
        public double Saturation { get; init; }
        public double Contrast { get; init; }
        public double Noise { get; init; }
        public IReadOnlyList<string> DominantColors { get; init; } = Array.Empty<string>();
        public double Resolution { get; init; }
    }
}