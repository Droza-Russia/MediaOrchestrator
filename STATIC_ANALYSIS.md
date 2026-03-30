# Static Analysis Report (FEATURE.md execution)

## Scope
- Decision layer implementation (`MediaOrchestrator/Analytics/**`).
- Integration point in facade (`MediaOrchestrator/MediaOrchestratorFacade.cs`).
- Added tests (`MediaOrchestrator.Test/MediaProcessingAnalyticsTests.cs`).

## Bottlenecks (hot paths)
1. `DecideProcessingPlanAsync` always calls `FFmpeg.GetMediaInfo(...)`, which can trigger `ffprobe` process startup.
   - Impact: process spawn + I/O overhead on every cold request.
   - Mitigation: keep MediaInfo cache enabled; next iteration should add persistent analysis store described in `FEATURE.md`.

2. `IsBrowserCompatible(...)` uses first audio/video stream only.
   - Impact: can choose suboptimal strategy for multi-stream media (e.g. second stream is browser-safe).
   - Mitigation: score all candidate streams in next phase.

## Race-condition review
1. Global mutable state in tests (`FFprobeWrapper.ProbeCommandExecutor`, `FFmpeg.SetExecutablesPath(...)`).
   - Risk: cross-test interference when tests run in parallel.
   - Mitigation applied: restore probe executor and FFmpeg global state in `Dispose()` for `MediaProcessingAnalyticsTests`.

2. Production side uses existing lock strategy in `FFmpeg` static class for executable resolution.
   - New analytics code introduces no additional shared mutable static state.

## Debuggability improvements
1. Decision output now carries structured `Reasons` (`ProcessingDecisionReason`) in `MediaProcessingPlan`.
   - Helps explain why remux/transcode was selected.

2. Plan object has explicit flags (`IsRemux`, `RequiresTranscode`, `RequiresAudioNormalization`) for fast runtime diagnostics.

## Known limitations / next step
- No persistent analysis cache (`IMediaAnalysisStore`) yet.
- Browser compatibility heuristic is intentionally minimal (first iteration, rule-based baseline).
- Need CI `dotnet test` and Roslyn analyzers in environment to automate static diagnostics.
