# Changelog

All notable changes to this project will be documented in this file.

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
