# Content Summary Service - Hybrid Approach

## Overview

The content summary service provides natural language summaries of submission content based on extracted media features. It uses a **hybrid architecture** combining rule-based logic with optional AI enhancement.

## Architecture

### Components

1. **`IContentSummaryService`** - Interface defining the summary generation contract
2. **`RuleBasedContentSummaryService`** - Deterministic, fast baseline implementation
3. **`AiContentSummaryService`** - AI-enhanced implementation with intelligent fallback

### Service Selection

The active implementation is selected at startup based on configuration:

```json
{
  "Submission": {
    "EnableAiSummary": true,
    "AiSummaryTimeoutMs": 2000
  }
}
```

- `EnableAiSummary: true` → Uses `AiContentSummaryService`
- `EnableAiSummary: false` → Uses `RuleBasedContentSummaryService`

## Rule-Based Service

### Characteristics
- **Predictable**: Same inputs always produce the same output
- **Fast**: Zero latency, synchronous execution
- **Reliable**: No external dependencies or failure modes
- **Safe**: No risk of hallucination or sensitive data leakage

### Algorithm
1. Check media file count and type
2. Extract feature metadata (subjects, vibes, colors, tags, etc.)
3. Build structured description with top N elements from each category
4. Calculate message alignment via token intersection
5. Provide actionable feedback if alignment is low

### Use Cases
- Analysis still pending
- Partial feature data
- High-throughput list endpoints
- Audit/compliance scenarios requiring deterministic output

## AI-Enhanced Service

### Characteristics
- **Natural**: Produces fluid, professional prose
- **Adaptive**: Adjusts tone and emphasis based on content
- **Intelligent**: Better semantic bridging between message and features
- **Fallback-Protected**: Degrades gracefully to rule-based on errors

### Fallback Strategy

The AI service automatically falls back to rule-based when:

1. **Analysis incomplete** - ContentFeatureStatus is Pending
2. **Minimal metadata** - Single image with < 3 tags and < 2 subjects
3. **Timeout exceeded** - AI call exceeds configured timeout (default: 2000ms)
4. **API failure** - Network error, quota exceeded, or service unavailable
5. **Invalid output** - Response validation fails (< 20 or > 1000 characters)

### AI Prompt Design

**System Prompt:**
- Constrains model to ONLY use provided JSON fields
- Prohibits introducing new concepts beyond supplied data
- Enforces 2-3 sentence natural summary format
- Requires alignment assessment and optimization suggestions

**User Prompt:**
- Structured JSON with normalized, deduplicated lists
- Includes pre-calculated alignment tokens
- Provides message context when available

**Constraints:**
- Max 5 subjects, 5 vibes, 5 notable elements
- Max 7 colors, 5 keywords, 5 tags
- Top 10 alignment tokens
- 100-300 word target length

### Validation

Generated summaries are validated:
- Length must be 20-1000 characters
- Non-empty and trimmed
- Logged with submission ID for observability

## Integration

### Dependency Injection

```csharp
// Program.cs
builder.Services.AddScoped<RuleBasedContentSummaryService>();
if (builder.Configuration.GetValue<bool>("Submission:EnableAiSummary", true))
{
    builder.Services.AddScoped<IContentSummaryService, AiContentSummaryService>();
}
else
{
    builder.Services.AddScoped<IContentSummaryService, RuleBasedContentSummaryService>();
}
```

**Note**: `RuleBasedContentSummaryService` is registered in both cases because `AiContentSummaryService` depends on it for fallback.

### Controller Usage

```csharp
public class SubmissionController : ControllerBase
{
    private readonly IContentSummaryService _contentSummaryService;
    
    [HttpPost]
    public async Task<IActionResult> CreateSubmission(...)
    {
        // ... create submission ...
        var response = _mapper.ToResponse(submission);
        response.ContentSummary = await _contentSummaryService.GenerateAsync(response);
        return Ok(response);
    }
}
```

## Configuration Reference

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Submission:EnableAiSummary` | bool | `true` | Enable AI-enhanced summaries |
| `Submission:AiSummaryTimeoutMs` | int | `2000` | AI generation timeout in milliseconds |

## Performance Considerations

### Rule-Based
- **Latency**: < 1ms (synchronous in-memory)
- **Throughput**: Limited only by CPU
- **Cost**: Zero per-request cost

### AI-Enhanced
- **Latency**: 500-2000ms (depends on model, load, network)
- **Throughput**: Limited by OpenAI rate limits and concurrency
- **Cost**: ~$0.0001-0.0005 per summary (using gpt-4o-mini)
- **Timeout Protection**: Automatically falls back if timeout exceeded

### Recommendations
- Use AI summaries for detail views and single-submission endpoints
- Consider caching AI summaries with feature vector hash as cache key
- Use rule-based summaries for bulk list operations
- Monitor AI service latency and fallback rates via logging

## Testing Strategy

### Unit Tests
- **Rule-Based**: Test edge cases (no media, pending analysis, empty features, alignment scenarios)
- **AI-Enhanced**: Mock `ITextGenerationService` to test fallback logic without live API calls

### Integration Tests
- Test configuration-based service selection
- Verify fallback on timeout/error
- Validate output format and length constraints

### Example Test Cases
```csharp
// No media files
// Pending analysis
// Single image, minimal metadata → fallback
// AI timeout → fallback
// AI returns invalid length → fallback
// Full feature set → AI enhanced
// Message alignment with strong overlap
// Message alignment with no overlap
```

## Monitoring & Observability

### Logging Events

**`RuleBasedContentSummaryService`**: (None - silent success)

**`AiContentSummaryService`**:
- Info: "AI summary generated successfully for submission {SubmissionId}"
- Warning: "AI summary validation failed (length: {Length}), using fallback"
- Warning: "AI summary generation failed for submission {SubmissionId}, using fallback" (includes exception)

### Metrics to Track
- AI summary generation success rate
- Fallback rate by reason (timeout, error, validation)
- P50/P95/P99 latency for AI generation
- OpenAI API error rate
- Token usage and cost per summary

## Future Enhancements

### Potential Improvements
1. **Caching**: Store AI summaries keyed by feature hash + message hash
2. **A/B Testing**: Log AI vs rule-based outputs for quality comparison
3. **User Preferences**: Per-user or per-client toggle for AI summaries
4. **Style Customization**: Prompt templates for different tones (professional, casual, marketing)
5. **Multi-Language**: Detect user locale and generate summaries in target language
6. **Semantic Validation**: Use embeddings to verify AI output stays within feature scope
7. **Progressive Enhancement**: Start with rule-based, upgrade to AI in background, cache result

## Migration Notes

### Breaking Changes
- `SubmissionController` now requires `IContentSummaryService` in constructor
- `BuildContentSummary` private method removed from controller

### Backward Compatibility
- Output format unchanged for rule-based implementation
- AI summaries may be more natural but convey the same information
- Configuration defaults to AI enabled (`EnableAiSummary: true`)

### Rollback Plan
If issues arise, immediately set `EnableAiSummary: false` in configuration and restart the service. No code changes required.
