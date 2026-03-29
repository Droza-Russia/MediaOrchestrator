# MediaOrchestrator

Enterprise-grade .NET SDK for media processing, adaptive execution, analytics, observability and universal I/O.

`MediaOrchestrator` started as a media conversion library and now behaves more like a media runtime:
- it resolves and validates the media toolchain on the host;
- probes media with caching and file-stability checks;
- builds conversions through a high-level API;
- adapts strategy selection using persisted execution statistics;
- exposes public analytics reports for web dashboards and Grafana;
- supports file paths, `Stream` and `byte[]` as both input and output contracts.

## What It Does

`MediaOrchestrator` provides four major capabilities.

1. Media execution and conversion
- high-level conversion snippets for remux, transcode, audio extraction, subtitles, overlays, stream remux, HLS capture and hosted-video download;
- global output limits for frame rate, audio sample rate and audio channels;
- optional auto hardware acceleration profile detection and runtime application when transcoding makes sense;
- cancellation support, progress reporting and cleanup of partial outputs on failure.

2. Media probing and readiness
- `GetMediaInfo(...)` for local files, streams and byte arrays;
- in-memory metadata cache with deduplication of concurrent probe requests;
- file readiness and stabilization checks before probing partially written files;
- input signature validation for safer local-file probing.

3. Adaptive analytics and learning
- rule-based plan selection for scenarios like browser playback and AI transcription;
- persisted execution history per analysis key;
- in-memory analytics cache with lazy write-behind persistence;
- adaptive strategy preference using historical success rate, failure rate and speed factor;
- explicit flush API for graceful shutdown.

4. Reporting and observability
- public analytics reports for totals, breakdowns and time series;
- breakdowns by scenario, strategy, file type, size bucket, duration bucket, hardware accelerator, failure type, failure category and error code;
- runtime resource metrics such as peak working set, CPU usage, logical cores and accelerator load when available;
- stable public error codes with a documented catalog.

## Main Functional Areas

### 1. Toolchain Resolution

The SDK can work with an explicitly configured executables directory or resolve the media toolchain automatically.

Available behavior:
- explicit configuration through `MediaOrchestrator.SetExecutablesPath(...)`;
- automatic lookup through environment variables, common directories and `PATH`;
- executable signature validation per OS;
- cached resolution to avoid repeated expensive lookup;
- optional hardware acceleration profile detection during initialization.

Related public API:
- `MediaOrchestrator.SetExecutablesPath(...)`
- `MediaOrchestrator.EnsureExecutablesLocated(...)`
- `MediaOrchestrator.SetGlobalOutputLimits(...)`
- `MediaOrchestrator.SetLocalizationLanguage(...)`

### 2. Universal Media I/O

Most public processing APIs now support:
- file path input/output;
- `Stream` input/output;
- `byte[]` input/output.

Abstractions:
- `MediaSource`
- `MediaDestination`
- `MediaDirectoryDestination`

This allows service code to work without exposing temporary files in its own contract. The SDK handles buffering and temporary materialization internally when the underlying media toolchain still needs filesystem paths.

Examples:
- `MediaSource.FromFile(...)`
- `MediaSource.FromStream(...)`
- `MediaSource.FromBytes(...)`
- `MediaDestination.ToFile(...)`
- `MediaDestination.ToStream(...)`
- `MediaDestination.ToBytes(...)`
- `MediaDirectoryDestination.ToDirectory(...)`
- `MediaDirectoryDestination.ToMemory(...)`

### 3. High-Level Conversion API

The main entry points remain:
- `MediaOrchestrator.Conversions`
- `MediaOrchestrator.Conversions.FromSnippet`

Representative operations:
- `ToMp4`, `ToWebM`, `ToOgv`, `ToTs`
- `Convert`, `Transcode`, `ConvertWithHardware`
- `ExtractAudio`, `ExtractVideo`, `Snapshot`, `Split`
- `NormalizeAudioForTranscription`
- `AddAudio`, `AddSubtitle`, `BurnSubtitle`
- `SetWatermark`
- `RemuxStream`, `SaveAudioStream`, `SaveM3U8Stream`
- `StreamFromStdin`, `StreamAudioFromStdin`
- `Concatenate`
- `SplitAudioByTimecodes`

The snippet API is intended to cover most service-level use cases without hand-assembling command-line arguments.

### 4. Adaptive Processing Analytics

The analytics layer is exposed through:
- `MediaOrchestrator.Analytics`

Core behavior:
- chooses a `MediaProcessingPlan` for a scenario;
- persists decisions and execution outcomes;
- learns from historical execution data;
- prefers strategies that are historically faster and more reliable for similar inputs.

Current scenarios and strategies:
- scenarios such as `BrowserPlayback`, `AiTranscription`, `Custom`;
- strategies such as `Remux`, `NormalizeAudio`, `FullTranscode`.

What is stored:
- probe snapshot of the media;
- last processing plan;
- recent execution samples;
- aggregated per-strategy statistics;
- resource metrics and failure metadata.

### 5. Analytics Store and Runtime Cache

Analytics data uses a two-level model:
- in-memory operational cache for immediate decisions during the current process;
- persistent store for reuse after the next startup.

Implemented behavior:
- lazy write-behind persistence;
- explicit flush on demand;
- sharded file store layout;
- bounded recent execution history;
- argument truncation to avoid oversized records.

Public operational APIs:
- `MediaOrchestrator.FlushMediaAnalysisStore()`
- `MediaOrchestrator.FlushMediaAnalysisStoreAsync()`
- `MediaOrchestrator.ClearMediaAnalysisStore()`
- `MediaOrchestrator.SetMediaAnalysisStoreDirectory(...)`

### 6. Public Analytics Reports

Reports are designed for:
- web admin panels;
- Grafana;
- export to external monitoring or BI layers.

Entry points:
- `MediaOrchestrator.GetMediaAnalyticsReport(...)`
- `MediaOrchestrator.GetMediaAnalyticsReportAsync(...)`

Filtering:
- by `FromUtc`
- by `ToUtc`
- by `TimelineBucket`

Report totals include:
- attempts;
- successes;
- failures;
- hardware accelerated runs;
- average processing time;
- average speed factor;
- input duration and input size totals;
- resource metrics such as memory, CPU and accelerator usage.

Breakdowns include:
- `ByScenario`
- `ByStrategy`
- `ByFileType`
- `BySizeBucket`
- `ByDurationBucket`
- `ByHardwareAccelerator`
- `ByErrorCode`
- `ByFailureType`
- `ByFailureCategory`

Timeline:
- hour;
- day;
- month.

### 7. Error Model

The public base exception is:
- `MediaOrchestrator.Exceptions.MediaOrchestratorException`

Every public library exception exposes:
- `ErrorCode`
- `ErrorCodeId`

Error codes are documented in [ERROR_CODES.md](ERROR_CODES.md).

Current error-code domains:
- `MOR-IN-*` input errors;
- `MOR-CV-*` conversion errors;
- `MOR-IO-*` output and disk errors;
- `MOR-HW-*` hardware acceleration errors;
- `MOR-TL-*` toolchain errors;
- `MOR-HD-*` hosted download errors;
- `MOR-OP-*` operation lifecycle errors;
- `MOR-GN-*` generic fallback.

## Quick Start

### Initialization

```csharp
using MediaOrchestrator;

MediaOrchestrator.SetExecutablesPath(
    "/usr/local/bin",
    language: LocalizationLanguage.English,
    maxOutputVideoFrameRate: 30,
    maxOutputAudioSampleRate: 48000,
    maxOutputAudioChannels: 2,
    tryDetectHardwareAcceleration: true);
```

### Media Info From Bytes

```csharp
using MediaOrchestrator;

IMediaInfo info = await MediaOrchestrator.GetMediaInfo(
    MediaSource.FromBytes(fileBytes, ".mp4"),
    cancellationToken);
```

### Convert From Bytes To Bytes

```csharp
using MediaOrchestrator;

var input = MediaSource.FromBytes(sourceBytes, ".mov");
var output = MediaDestination.ToBytes(".mp4");

var conversion = await MediaOrchestrator.Conversions.FromSnippet.ToMp4(input, output, cancellationToken);
await conversion.Start(cancellationToken);

byte[] resultBytes = output.GetBytes();
```

### Split Audio To Memory Package

```csharp
using MediaOrchestrator;

var input = MediaSource.FromBytes(sourceBytes, ".mp4");
var output = MediaDirectoryDestination.ToMemory();

var conversions = await MediaOrchestrator.Conversions.FromSnippet.SplitAudioByTimecodes(
    input,
    output,
    new[] { TimeSpan.Zero, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1) },
    cancellationToken: cancellationToken);

foreach (var conversion in conversions)
{
    await conversion.Start(cancellationToken);
}

var files = output.GetFiles();
```

### Analytics Report

```csharp
using MediaOrchestrator;
using MediaOrchestrator.Analytics.Reports;

var report = await MediaOrchestrator.GetMediaAnalyticsReportAsync(new MediaAnalyticsQuery
{
    FromUtc = DateTimeOffset.UtcNow.AddDays(-7),
    ToUtc = DateTimeOffset.UtcNow,
    TimelineBucket = MediaAnalyticsTimeBucket.Day
});
```

## Error-Code Example

```csharp
using MediaOrchestrator.Exceptions;

try
{
    await conversion.Start(cancellationToken);
}
catch (MediaOrchestratorException ex)
{
    Console.WriteLine(ex.ErrorCode);   // AudioStreamNotFound
    Console.WriteLine(ex.ErrorCodeId); // MOR-IN-3007
}
```

## Current Public Surface Added In This Revision

This revision adds or significantly extends:
- `MediaOrchestrator` as the primary product-facing namespace and assembly identity;
- universal I/O via `MediaSource`, `MediaDestination` and `MediaDirectoryDestination`;
- analytics persistence, lazy cached store and explicit flush;
- adaptive learning from execution history;
- public analytics reports with breakdowns and timeline;
- resource metrics in analytics;
- failure reports and domain-classified error codes;
- public error catalog for documentation and UI;
- renamed public exception surface under `MediaOrchestrator.Exceptions`.

## Build And Test

```bash
dotnet build ./MediaOrchestrator/MediaOrchestrator.csproj
dotnet test ./MediaOrchestrator/MediaOrchestrator.sln
```

## PR Review With Gemini

The repository includes a GitHub Actions workflow at [.github/workflows/gemini-code-review.yml](.github/workflows/gemini-code-review.yml) that runs Gemini Code Assist review for pull requests.

Behavior:
- automatic review on `pull_request` open, reopen, ready-for-review and new commits;
- manual rerun from a pull request comment: `@gemini-cli /review`;
- manual review comments are accepted only from `OWNER`, `MEMBER` or `COLLABORATOR`;
- automatic review is skipped for forked pull requests for safety.

If Gemini Code Assist is already installed for this repository in GitHub, the workflow uses it directly via `use_gemini_code_assist: true`.

## Notes

- The repository folder names are still in transition in a few places, but the product-facing API and package identity are `MediaOrchestrator`.
- The SDK still relies on an external media-processing toolchain being available on the host machine.
- Localized XML documentation packaging is supported through `buildTransitive/MediaOrchestrator.props` and `buildTransitive/MediaOrchestrator.targets`.

## License

See [LICENSE.md](LICENSE.md).
