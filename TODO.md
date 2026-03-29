# TODO - MediaOrchestrator

## High Priority

### Architecture Improvements
- [ ] Split into modular NuGet packages
  - MediaOrchestrator.Core - main SDK
  - MediaOrchestrator.HandBrake - HandBrake CLI wrapper
  - MediaOrchestrator.Data - database analytics stores (SQL Server, PostgreSQL, MySQL)
  - MediaOrchestrator.Cloud - cloud storage adapters (S3, GCS, Azure Blob)
- [ ] Replace static configuration with DI container (Microsoft.Extensions.DependencyInjection)
- [ ] Add configuration interfaces for testability
- [ ] Implement options pattern (IOptions<T>) for all settings

### Security
- [ ] Add path traversal protection (prevent ../../../etc/passwd)
- [ ] Add file size limits for input/output (prevent disk exhaustion)
- [ ] Add maximum execution time limits per conversion
- [ ] Add sandbox mode (restricted filesystem access)
- [ ] Validate output directory is writable before starting
- [ ] Add permission system for operations (admin/user modes)
- [ ] Add operation audit logging
- [ ] Add rate limiting for conversions per tenant/user
- [ ] Add malware scanning for uploaded files (optional integration)
- [ ] Sanitize all user-provided file paths and commands

### Configuration
- [ ] Add configuration file support (JSON/XML/YAML)
- [ ] Add environment variable configuration override
- [ ] Add configuration validation on startup
- [ ] Support multiple FFmpeg versions side-by-side
- [ ] Add configuration change observers (reload without restart)
- [ ] Add configuration profiles (development, production)

### Input/Output Validation
- [ ] Validate input file format matches declared extension
- [ ] Add media integrity check before conversion
- [ ] Validate output disk space before starting
- [ ] Add output filename sanitization

### Performance
- [ ] Benchmark LRU cache performance under high concurrency
- [ ] Add connection pooling for persistent ffprobe connections
- [ ] Optimize memory allocation in hot paths (conversion progress)

### Testing
- [ ] Add integration tests for Circuit Breaker
- [ ] Add integration tests for adaptive timeout
- [ ] Add stress tests for concurrent media operations
- [ ] Increase code coverage to 80%+
- [ ] Add mocking infrastructure for FFmpeg binary (test without real binaries)
- [ ] Add property-based testing for analytics calculations
- [ ] Add end-to-end tests for full conversion pipelines
- [ ] Add performance regression tests

### Error Handling
- [ ] Add retry with exponential backoff for transient FFmpeg failures
- [ ] Add dead letter queue for failed conversions
- [ ] Implement graceful degradation when hardware acceleration unavailable

---

## Medium Priority

### Analytics
- [ ] Add SQLite-based media analysis store (currently file-based only)
- [ ] Add MS SQL Server support for analytics storage
  - Connection pooling
  - Entity Framework / Dapper integration
  - Migration scripts
- [ ] Add PostgreSQL support for analytics storage
  - Full-text search for media metadata
  - JSONB support for flexible schemas
  - Connection pooling
- [ ] Add MySQL/MariaDB support for analytics storage
  - Connection pooling
  - UTF8MB4 support for metadata
- [ ] Add database connection string validation
- [ ] Add analytics database schema migrations
- [ ] Add read replica support for analytics queries
- [ ] Add aggregation pipeline for long-term analytics
- [ ] Add Grafana dashboard template for metrics
- [ ] Implement time-series forecasting for processing duration

### Features
- [ ] Add batch conversion support (multiple files)
- [ ] Add video thumbnail generation snippet
- [ ] Add watermark template system
- [ ] Add preset profiles (social media, streaming, archival)

### HandBrake Integration
- [ ] Add HandBrake CLI wrapper (MediaOrchestrator.HandBrake)
- [ ] Add HandBrake preset profiles
  - Android, iOS, TV, Web, Production
  - Quality-based (RF 0-28)
  - Speed-based (ultrafast to veryfast)
  - Hardware-accelerated (QSV, NVEnc, VCE)
- [ ] Add HandBrake-specific options
  - Picture settings (width, height, crop, anamorphic)
  - Filtering (deinterlace, denoise, detelecine, deblock)
  - Audio tracks and codec selection
  - Subtitles (burn-in, soft, external)
- [ ] Add HandBrake scan support (title detection, chapter list)
- [ ] Add HandBrake to FFmpeg converter (interchangeable usage)
- [ ] Add dual-engine support: FFmpeg or HandBrake (auto-select based on preset)
- [ ] Add HandBrake binary auto-download
- [ ] Add HandBrake version detection

### Observability
- [ ] Add OpenTelemetry integration
- [ ] Add structured logging with correlation IDs
- [ ] Add metrics export to Prometheus

### Documentation
- [ ] Add API reference documentation
- [ ] Add migration guide from Xabe.FFmpeg
- [ ] Add deployment guide for containerized environments

### Database Support
- [ ] Design unified database schema for all supported databases
- [ ] Add database abstraction layer (Repository pattern)
- [ ] Implement connection string builders for each database
- [ ] Add health check for database connectivity
- [ ] Add query optimization hints for each database
- [ ] Implement analytics aggregation stored procedures (optional)
- [ ] Add database migration tool (Flyway-style)
- [ ] Add database backup/restore utilities

---

## Low Priority

### Extensions
- [ ] Add WebP encoding support
- [ ] Add AV1 hardware acceleration profiles
- [ ] Add HDR metadata handling

### Platform
- [ ] Add Linux ARM64 build target
- [ ] Add macOS ARM64 (Apple Silicon) build target
- [ ] Add native AOT compilation support

### Toolchain
- [ ] Auto-download FFmpeg binaries when not found locally
  - Priority 1: Check local executable path
  - Priority 2: Check NuGet package dependency (e.g., ffmpeg-win, ffmpeg-latest)
  - Priority 3: Download via HTTP from official mirrors (ffmpeg.org, GitHub releases)
  - Priority 4: Extract from NuGet package on-demand
- [ ] Add FFmpeg version compatibility checker
- [ ] Add bundled FFmpeg downloader with progress
- [ ] Implement binary cache with version-aware cleanup
- [ ] Support custom download URLs (corporate proxies)
- [ ] Add signature verification for downloaded binaries

### Quality Control
- [ ] Add output quality verification (compare input/output quality metrics)
- [ ] Add media integrity validation after conversion
- [ ] Implement output file playable check (probe output)
- [ ] Add comparison report (size reduction, quality loss)

### Streaming & Real-time
- [ ] Add streaming input support (stdin, HTTP stream)
- [ ] Add streaming output support (stdout, RTMP/HLS)
- [ ] Add real-time preview generation
- [ ] Add low-latency encoding profiles

### Conversion Management
- [ ] Add job queue with priority system
- [ ] Add conversion cancellation with cleanup
- [ ] Add pause/resume conversion support
- [ ] Implement conversion templates
- [ ] Add batch operations with progress aggregation

---

## Technical Debt

- [ ] Replace remaining `dynamic` with typed interfaces
- [ ] Add XML documentation to all public APIs
- [ ] Run static analyzer (SonarQube/roslynator) and fix warnings
- [ ] Review and seal internal classes where applicable
- [ ] Add FxCop/Analyzer rules for project-specific checks

---

## Ideas / Backlog

- [ ] ML-based adaptive encoding profiles
- [ ] Cloud storage integration (S3, GCS, Azure Blob)
- [ ] Distributed processing with message queue (RabbitMQ, Kafka)
- [ ] Video editing timeline (cut, trim, concat visual editor)
- [ ] Audio normalization with loudness standards (LUFS)
- [ ] Video overlay editor (text, images, animations)
- [ ] Multi-language subtitle generation
- [ ] Automatic video tagging/metadata extraction
- [ ] Content-aware encoding (detect scene changes)
- [ ] Integration with video hosting platforms (YouTube, Vimeo API)
- [ ] Webhook notifications for conversion completion
- [ ] Conversion history with re-run capability
- [ ] Tenant isolation for multi-tenant deployments
- [ ] Conversion scheduling (cron-like)
- [ ] Priority queue with SLA guarantees
- [ ] Video comparison tool (side-by-side diff)
- [ ] Integration with video CMS platforms
