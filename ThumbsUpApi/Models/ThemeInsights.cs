using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThumbsUpApi.Models;

public sealed class ThemeInsights
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public List<string> Subjects { get; set; } = new();
    public List<string> Vibes { get; set; } = new();
    public List<string> NotableElements { get; set; } = new();
    public List<string> Colors { get; set; } = new();
    public List<string> Keywords { get; set; } = new();

    [JsonIgnore]
    public bool HasAnyData => Subjects.Count + Vibes.Count + NotableElements.Count + Colors.Count + Keywords.Count > 0;

    public static ThemeInsights Empty => new();

    public static ThemeInsights Combine(IEnumerable<ThemeInsights> source)
    {
        var combined = new ThemeInsights();
        foreach (var insights in source)
        {
            if (insights == null) continue;
            combined.Subjects.AddRange(insights.Subjects);
            combined.Vibes.AddRange(insights.Vibes);
            combined.NotableElements.AddRange(insights.NotableElements);
            combined.Colors.AddRange(insights.Colors);
            combined.Keywords.AddRange(insights.Keywords);
        }

        combined.Normalize();
        return combined;
    }

    public static ThemeInsights FromKeywords(IEnumerable<string> keywords)
    {
        var insights = new ThemeInsights
        {
            Keywords = NormalizeList(keywords)
        };
        return insights;
    }

    public IEnumerable<string> FlattenTags()
    {
        return Subjects
            .Concat(Vibes)
            .Concat(NotableElements)
            .Concat(Colors)
            .Concat(Keywords)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0);
    }

    public string ToJson()
    {
        Normalize();
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static ThemeInsights FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Empty;
        }

        var cleaned = StripCodeFences(json);
        try
        {
            var insights = JsonSerializer.Deserialize<ThemeInsights>(cleaned, JsonOptions);
            if (insights != null)
            {
                insights.Normalize();
                if (insights.HasAnyData)
                {
                    return insights;
                }
            }
        }
        catch
        {
            // Ignore and try legacy parsing below
        }

        return FromLegacyArray(cleaned);
    }

    public static ThemeInsights FromModelResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Empty;
        }

        var cleaned = StripCodeFences(raw.Trim());
        var insights = FromJson(cleaned);
        if (insights.HasAnyData)
        {
            return insights;
        }

        return FromLegacyArray(cleaned);
    }

    private static ThemeInsights FromLegacyArray(string raw)
    {
        try
        {
            var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var tags = doc.RootElement
                    .EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToList();
                return FromFlatList(tags);
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var keywords = ExtractList(doc.RootElement, "tags");
                if (keywords.Count > 0)
                {
                    return new ThemeInsights { Keywords = keywords };
                }
            }
        }
        catch
        {
            // Could not parse JSON; fall back to splitting text
        }

        var split = raw
            .Split(new[] { ',', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0);

        return FromFlatList(split);
    }

    private static ThemeInsights FromFlatList(IEnumerable<string> values)
    {
        var normalized = values
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(v => v.ToLowerInvariant())
            .ToList();

        return normalized.Count == 0
            ? Empty
            : new ThemeInsights { Keywords = normalized };
    }

    private static List<string> ExtractList(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return new List<string>();
        }

        return element
            .EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();
    }

    private static string StripCodeFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var newlineIndex = trimmed.IndexOf('\n');
            if (newlineIndex >= 0)
            {
                trimmed = trimmed[(newlineIndex + 1)..];
            }
            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed[..^3];
            }
        }
        return trimmed.Trim();
    }

    private void Normalize()
    {
        Subjects = NormalizeList(Subjects);
        Vibes = NormalizeList(Vibes);
        NotableElements = NormalizeList(NotableElements);
        Colors = NormalizeList(Colors);
        Keywords = NormalizeList(Keywords);
    }

    private static List<string> NormalizeList(IEnumerable<string> values)
    {
        return values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
