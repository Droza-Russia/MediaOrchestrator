# MediaOrchestrator Architecture

## Overview

MediaOrchestrator is an enterprise-grade .NET SDK for media processing, adaptive execution, analytics, and observability. It provides a unified interface for media conversion, probing, and intelligent strategy selection based on historical execution data.

---

## 1. Program Architecture

### 1.1 Layer Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                     Public API Layer                            │
│  MediaOrchestrator (Facade), MediaSource, MediaDestination     │
├─────────────────────────────────────────────────────────────────┤
│                  High-Level Operations Layer                     │
│  Conversion Snippets, Analytics, Reporting                      │
├─────────────────────────────────────────────────────────────────┤
│                   Core Processing Layer                          │
│  Conversion, Probe, MediaInfo, Streams                          │
├─────────────────────────────────────────────────────────────────┤
│                    Infrastructure Layer                          │
│  Caching, Stores, I/O, Hardware Detection                       │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 Key Modules

| Module | Responsibility |
|--------|---------------|
| **MediaOrchestrator** | Static facade, entry point, configuration |
| **Conversion** | FFmpeg execution, progress, snippets |
| **Probe** | MediaInfo extraction and caching |
| **Analytics** | Adaptive learning, reporting, storage |
| **Media** | Universal I/O (files, streams, bytes) |
| **HardwareAcceleration** | Auto-detection, profiles |
| **Streams** | Video/Audio/Subtitle stream models |
| **Localization** | Error messages, multi-language support |

### 1.3 Target Framework

- **netstandard2.0** with C# 7.3 compatibility
- Supports .NET Framework, .NET Core, .NET 5+

---

## 2. Functional Architecture

### 2.1 Core Functional Areas

#### 2.1.1 Toolchain Resolution
```
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│ Explicit Path   │     │ Environment      │     │ Common Paths     │
│ Configuration   │     │ Variables        │     │ Discovery        │
└────────┬─────────┘     └────────┬─────────┘     └────────┬─────────┘
         │                        │                        │
         └────────────────────────┼────────────────────────┘
                                  │
                         ┌────────▼────────┐
                         │ Executable      │
                         │ Validator       │
                         │ (per OS)        │
                         └────────┬────────┘
                                  │
                         ┌────────▼────────┐
                         │ Cached Result   │
                         └─────────────────┘
```

#### 2.1.2 Universal Media I/O
```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ MediaSource │    │ MediaDest   │    │ MediaDir    │
│             │    │             │    │ Destination │
└──────┬──────┘    └──────┬──────┘    └──────┬──────┘
       │                  │                  │
       └──────────────────┼──────────────────┘
                          │
                 ┌────────▼────────┐
                 │ MediaIoBridge   │
                 │ - BufferPool    │
                 │ - Atomic Write  │
                 │ - Retry Logic   │
                 └─────────────────┘
```

#### 2.1.3 Media Conversion Pipeline
```
Input Media ──► Probe ──► Decision ──► Build Command ──► Execute ──► Output
                 │         │              │               │
                 │         │              │               │
            [Cache]   [Analytics]    [FFmpeg Args]   [Process]
```

#### 2.1.4 Adaptive Analytics System
```
┌──────────────────────────────────────────────────────────────┐
│                    Analytics Pipeline                         │
│                                                               │
│  ┌─────────┐    ┌──────────┐    ┌────────────┐              │
│  │ Scenario│───►│ Decision │───►│ Processing │              │
│  │ Input   │    │ Engine   │    │ Plan       │              │
│  └─────────┘    └──────────┘    └─────┬──────┘              │
│                                         │                     │
│  ┌──────────────────────────────────────▼──────────────────┐ │
│  │              Execution Feedback Loop                    │ │
│  │                                                          │ │
│  │  ┌──────────┐   ┌──────────┐   ┌──────────┐           │ │
│  │  │ Execution │──►│ Statistics│──►│ Strategy  │           │ │
│  │  │ Sample    │   │ Aggregation│  │ Preference│           │ │
│  │  └──────────┘   └──────────┘   └──────────┘           │ │
│  └──────────────────────────────────────────────────────────┘ │
│                              │                                │
│  ┌───────────────────────────▼────────────────────────────┐   │
│  │              Storage Layer (Two-Level)                 │   │
│  │   ┌─────────────────┐    ┌─────────────────────┐        │   │
│  │   │ LRU Cache       │    │ File Store         │        │   │
│  │   │ (In-Memory)     │    │ (Persistent)       │        │   │
│  │   └─────────────────┘    └─────────────────────┘        │   │
│  └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 Key Data Models

#### MediaProcessingPlan
- **Scenario**: BrowserPlayback, AiTranscription, Custom
- **Strategy**: Remux, NormalizeAudio, FullTranscode
- **Constraints**: max duration, max size, codecs, hardware acceleration
- **Reason**: Decision explanation

#### MediaAnalysisRecord
- **ProbeSnapshot**: container, codecs, duration, resolution
- **ProcessingPlan**: selected strategy and parameters
- **ExecutionSamples**: recent executions with success/failure/speed
- **AggregatedStats**: per-strategy success rate, average speed

#### ExecutionResourceMetrics
- Peak working set (memory)
- CPU usage
- Hardware accelerator load (GPU)
- Logical cores

---

## 3. Business Architecture

### 3.1 Problem Domain

| Problem | Solution |
|---------|----------|
| Media processing requires FFmpeg expertise | High-level snippets (ToMp4, ExtractAudio, etc.) |
| Inefficient repeated probing | In-memory LRU cache with deduplication |
| Suboptimal strategy selection | Adaptive analytics with historical learning |
| No visibility into processing | Public analytics reports with breakdowns |
| Brittle operations | Circuit Breaker pattern |
| Unpredictable timeouts | Adaptive CancellationToken based on history |
| I/O reliability | Atomic writes, retry logic, buffer pooling |

### 3.2 Use Cases

#### Primary Use Cases
1. **Media Conversion** - Convert between formats (MP4, WebM, MKV, etc.)
2. **Audio Extraction** - Extract audio tracks for AI transcription
3. **Transcoding** - Transcode with hardware acceleration
4. **Media Probing** - Get metadata without full conversion
5. **Adaptive Processing** - Let the system choose optimal strategy

#### Analytics Use Cases
1. **Historical Analysis** - Query processing history by scenario/strategy
2. **Performance Monitoring** - Track success rates, processing times
3. **Resource Monitoring** - CPU, memory, GPU usage
4. **Failure Analysis** - Breakdown by error code, failure type

### 3.3 Value Proposition

| Stakeholder | Value |
|-------------|-------|
| **Developers** | Simple API, no FFmpeg knowledge required |
| **DevOps** | Health checks, metrics, predictable behavior |
| **Operations** | Analytics dashboards, failure reports |
| **Business** | Adaptive optimization, cost reduction |

### 3.4 Error Model

All exceptions inherit from `MediaOrchestratorException` with:
- **ErrorCode**: human-readable identifier (e.g., `AudioStreamNotFound`)
- **ErrorCodeId**: stable ID (e.g., `MOR-IN-3007`)

Error domains:
- `MOR-IN-*` - Input errors
- `MOR-CV-*` - Conversion errors
- `MOR-IO-*` - I/O errors
- `MOR-HW-*` - Hardware acceleration errors
- `MOR-TL-*` - Toolchain errors
- `MOR-HD-*` - Hosted download errors
- `MOR-OP-*` - Operation lifecycle errors
- `MOR-GN-*` - Generic errors

---

## 4. Resilience Patterns

### 4.1 Circuit Breaker
```
        ┌─────────┐
        │ Closed  │ ───► Normal operation, failures counted
        └────┬────┘
             │ failure threshold exceeded
             ▼
        ┌─────────┐
        │  Open   │ ───► Fast-fail, no operations
        └────┬────┘
             │ timeout elapsed
             ▼
    ┌────────┴────────┐
    │   Half-Open    │ ───► Test recovery
    └────────┬────────┘
             │ success
             ▼
        ┌─────────┐
        │ Closed  │
        └─────────┘
```

### 4.2 Adaptive Timeout
- Uses historical operation duration data
- Calculates weighted average with safety factor (2.0x)
- Adjusts for low success rate (<80%)
- Bounds: min 30s, max 30min

### 4.3 Retry Logic
- 3 attempts for file operations
- 100ms delay between attempts
- Atomic writes with temp file + rename

---

## 5. Performance Optimizations

| Optimization | Description |
|--------------|-------------|
| **LRU Cache** | Probe result caching with TTL |
| **Concurrent Probe Deduplication** | SemaphoreSlim prevents duplicate ffprobe calls |
| **Buffer Pool** | Reusable byte buffers for I/O |
| **String Intern Pool** | Optimize repeated codec/format strings |
| **Interlocked Operations** | Lock-free telemetry collection |
| **Hardware Detection Cache** | Single ffprobe call for -hwaccels |
| **Lazy Write-Behind** | Async persistence, not blocking |

---

## 6. Project Structure

```
MediaOrchestrator/
├── Analytics/
│   ├── Models/          # Domain models
│   ├── Reports/         # Reporting APIs
│   └── Stores/         # Caching & persistence
├── Conversion/
│   ├── Arguments/       # FFmpeg argument builders
│   ├── Events/          # Progress events
│   ├── Exceptions/      # Typed exceptions
│   ├── Filters/         # Filter graph builders
│   ├── Implementations/ # Core conversion logic
│   ├── Interfaces/      # Contracts
│   ├── Settings/        # Conversion settings
│   └── Snippets/        # High-level operations
├── Examples/            # Sample code
├── HardwareAcceleration/ # HW detection & profiles
├── Localization/        # Multi-language support
├── Media/               # Universal I/O
├── Probe/               # MediaInfo extraction
├── Streams/             # Video/Audio/Subtitle models
└── Extensions/          # Utility extensions

MediaOrchestrator.Test/
└── *Tests.cs           # Unit & integration tests
```

---

## 7. Public API Summary

### Initialization
```csharp
MediaOrchestrator.SetExecutablesPath("/path/to/ffmpeg", tryDetectHardwareAcceleration: true);
```

### Universal I/O
```csharp
MediaSource.FromFile(path);
MediaSource.FromStream(stream, ".mp4");
MediaSource.FromBytes(bytes, ".mp4");

MediaDestination.ToFile(path);
MediaDestination.ToStream(stream);
MediaDestination.ToBytes(".mp4");

MediaDirectoryDestination.ToDirectory(path);
MediaDirectoryDestination.ToMemory();
```

### Conversion
```csharp
var conversion = await MediaOrchestrator.Conversions.FromSnippet.ToMp4(input, output);
await conversion.Start();
```

### Analytics
```csharp
var report = await MediaOrchestrator.GetMediaAnalyticsReportAsync(query);
var metrics = MediaOrchestratorMetrics.Instance.GetSnapshot();
var health = MediaOrchestratorHealth.Check();
```

### Flush
```csharp
await MediaOrchestrator.FlushMediaAnalysisStoreAsync();
```
