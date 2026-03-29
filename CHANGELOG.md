# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased] - YYYY-MM-DD

### Added
- Added Circuit Breaker pattern (`Conversion/Implementations/CircuitBreaker.cs`)
  - Prevents cascading failures when FFmpeg operations fail repeatedly
  - States: Closed (normal), Open (blocked), Half-Open (testing recovery)
  - Configurable failure threshold, timeout, and success rate

- Added MediaOrchestratorMetrics API (`Analytics/MediaOrchestratorMetrics.cs`)
  - `MediaOrchestratorMetrics.Instance` - singleton for runtime metrics
  - `GetSnapshot()` - returns snapshot with circuit breaker state, cache counts, learning/compression status
  - `MediaOrchestratorHealth.Check()` - health check for ffmpeg/ffprobe availability

- Added StreamingOutput class (`Media/StreamingOutput.cs`)
  - Supports piped output to stdout, file, or memory stream
  - Enables progress-only output mode for real-time progress updates

- Added Conversion.Stop() method for cancellation
  - Allows stopping ongoing conversions without full process termination
  - Uses CancellationToken integration

- Added hardware acceleration detection caching (`HardwareAcceleration/HardwareAccelerationAutoDetector.cs`)
  - Caches detected hardware acceleration profile
  - Avoids repeated ffprobe calls for -hwaccels detection

- Added adaptive CancellationToken timeout system with LRU cache (`OperationDurationLruCache.cs`)
  - `GetAdaptiveTimeout(operationKey, defaultTimeout)` - returns adaptive timeout based on historical data
  - `CreateAdaptiveCancellationTokenSource(...)` - creates CTS with adaptive timeout
  - `RecordOperationDuration(...)` - records actual operation duration after execution
  - `BuildOperationKey(...)` - builds operation key from input path, scenario, strategy, codecs, duration
  - Integrated with `MediaProcessingAnalytics.ReportExecutionAsync` for automatic duration recording
  - Uses weighted average with safety factor (default 2.0), adjusts for success rate < 80%
  - Timeout bounds: min 30 seconds, max 30 minutes

- Added BufferPool for I/O operations (`Media/BufferPool.cs`)
  - Uses `ArrayPool<byte>` for memory-efficient file operations
  - Methods: `RentDefault()`, `RentLarge()`, `ReturnDefault()`, `ReturnLarge()`

- Added StringInternPool for string optimization (`Extensions/StringInternPool.cs`)
  - Concurrent dictionary-based string interning
  - Available for future optimization of codec/format string caching

### Performance & Stability Improvements
- Enhanced ConversionProgressEventArgs with ETA, speed, and bitrate reporting
  - Added `ETA`, `Speed`, `Bitrate` properties to progress events
  - Real-time calculation of estimated time remaining
  - Speed in FPS and bitrate in Mbps

- Added `IDisposable` to `ProcessResourceTelemetryCollector` with proper resource cleanup
  - Proper disposal of `CancellationTokenSource`
  - Added `volatile` for `_isDisposed` flag
  - Exception-handled `Complete()` method

- Replaced `lock()` with atomic `Interlocked` operations (`InterlockedOperations.cs`)
  - `UpdateIfGreater(ref long)` - atomic max update for long values
  - `UpdateIfGreater(ref double)` - atomic max update for double values
  - `BeginUpdate(ref double, ref int, ...)` - atomic sum/count update
  - Eliminates lock contention in high-frequency telemetry collection

- Added defensive null-checks and exception handling to extension methods:
  - `StringExtensions.Escape/Unescape` - null/empty string handling, safe array indexing
  - `TimeExtensions.ParseFFmpegTime` - comprehensive try-catch with TimeSpan.Zero fallback
  - `DoubleExtensions.ToFFmpegFormat` - NaN/Infinity handling and decimalPlaces clamping (0-10)

- Fixed `SemaphoreSlim` using pattern in `CachedMediaAnalysisStore.FlushDirtyEntriesAsync`
  - Proper disposal on cancellation via `using` block

### Fixed
- Fixed OperationDurationLruCache thread-safety: MoveToHead now uses write lock
- Fixed MediaOrchestratorMetrics health check: use else if to prevent null path File.Exists
- Fixed HardwareAccelerationAutoDetector: removed null caching (ConcurrentDictionary doesn't allow null values)
- Fixed FileMediaAnalysisStore: GetAllAsync now includes .json.gz files when compression enabled
- Added Trace logging to empty catch blocks for visibility
- Fixed LruCache thread-safety: `TryGet` now properly upgrades to write lock before modifying linked list
- Fixed SemaphoreSlim disposal in CachedMediaAnalysisStore: proper try/finally to ensure release on any exit path
- Fixed memory leak in MediaIoBridge: `_cleanupErrors` dictionary now has max size and periodic cleanup
- Fixed exception visibility: cleanup errors now store exception type and message instead of just boolean flag

- Fixed MediaOrchestratorMetrics compilation errors:
  - Added `Count` property to `CachedMediaAnalysisStore`
  - Added public static `FFmpegExecutablePath` and `FFprobeExecutablePath` properties to `MediaOrchestrator`
  - Updated metrics to use new property names

- Fixed FileMediaAnalysisStore old compressed file cleanup logic
  - Now correctly checks for alternative format (if writing .json.gz, check for .json; if writing .json, check for .json.gz)
  - Fixed bug where path + ".gz" would look for .gz.gz when compression already enabled

- Fixed FileMediaAnalysisStore duplicate catch blocks
  - Combined IOException and UnauthorizedAccessException handlers using `when` clause

- Fixed extension methods namespace from `System` to `MediaOrchestrator.Extensions`
  - `StringExtensions`, `TimeExtensions`, `DoubleExtensions`
- Fixed `Streams.Collections` namespace to match directory structure
  - Updated `ParametersList.cs` namespace
  - Added required using statements in dependent files
- Fixed `ParseFFmpegTime` using `CultureInfo.InvariantCulture` instead of system culture

### I/O Reliability Improvements
- Added atomic write with temp file + rename pattern in `FileMediaAnalysisStore` and `MediaIoBridge`
- Added retry logic (3 attempts, 100ms delay) for file write operations
- Added isolated temp directories per operation (`media-orchestrator-io/pid{PID}_{GUID}`)
- Enforced minimum buffer size (81920 bytes) for all I/O operations
- Added post-write validation (file exists, size > 0, readable header)
- Added robust cleanup with directory removal
- Added compression support (GZIP) for analytics storage
- Fixed temp file extension preservation for output destinations

### Changed
- Improved LRU cache thread-safety test (`LruCacheTests.cs`)
  - Fixed capacity from 100 to 1100 to match test key count to avoid eviction during test

## [1.0.3] - 2026-03-28

### Added
- Added a unified library exception base type: `MediaOrchestratorException`.
- Added typed exception families for:
  - locked, empty, unstable, unreadable input files
  - access-denied scenarios for input, output, executable, and network paths
  - missing audio, video, and subtitle streams
  - stream mapping failures and codec/container incompatibility
  - inaccessible `ffmpeg` / `ffprobe` executable locations
- Added localized NuGet XML documentation support:
  - `ru` and `en` XML templates
  - `buildTransitive` default set to English with optional explicit locale override
  - one final `MediaOrchestrator.xml` file consumed by the IDE
- Added explicit packaging scripts:
  - `scripts/pack.sh`
  - `scripts/pack.ps1`
- Added regression tests for:
  - localization initialization
  - executable resolution
  - safe file reading
  - `MediaInfo` caching
  - exception contract coverage
  - stream/snippet scenarios

### Changed
- Reorganized the project into semantic directories:
  - `Interfaces`
  - `Implementations`
  - `Arguments`
  - `Filters`
  - `Inputs`
  - `Settings`
  - `Wrappers`
  - `Messages`
  - `Localization`
  - `Media`
  - `HardwareAcceleration`
- Replaced a large portion of stringly-typed FFmpeg command construction with typed helper layers and fluent APIs.
- Narrowed the public API surface:
  - moved internal-only types to `internal`
  - sealed safe leaf classes
- Unified exception handling around a library-level exception contract suitable for external service integration.
- Localization defaults to English for exceptions, while explicit API language selection still takes precedence.
- Hardware acceleration auto-detection now uses a one-time cached initialization flow after executable discovery.
- Updated README to reflect the new architecture, exception model, and IntelliSense packaging behavior.

### Fixed
- Removed redundant `ffprobe` calls: a single `GetMediaInfo(...)` cache miss now uses one process execution.
- Fixed localization defaults so exception messages are English when language is not explicitly set.
- Replaced raw system exceptions during executable resolution with library-specific typed exceptions.
- Localized and normalized file, stream, and access-denied error messages.
- Cleaned up broken and mixed-language blocks in the English IntelliSense XML.

### Documentation
- Updated build, packaging, and demo-project documentation.
- Documented the unified exception contract for consuming services.
- Documented the localized IntelliSense flow that still produces a single XML file for IDE consumption.
- Expanded English XML documentation coverage for a significant part of the public API.

### Tests
- Expanded automated coverage to `72/72` passing tests.
- Added verification for:
  - localization initialization
  - `ffmpeg` / `ffprobe` resolution from configured directories, environment variables, and `ffmpeg-binaries/<os>`
  - one-time hardware acceleration detection
  - safe media signature reading
  - `MediaInfo` caching behavior
  - typed exception contract behavior