using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics;
using MediaOrchestrator.Analytics.Models;
using MediaOrchestrator.Analytics.Reports;
using MediaOrchestrator.Analytics.Stores;
using MediaOrchestrator.Streams.SubtitleStream;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class MediaAnalysisLearningTests : IDisposable
    {
        private readonly string _storeDirectory = Path.Combine(Path.GetTempPath(), "media-orchestrator-analysis-" + Guid.NewGuid().ToString("N"));

        public MediaAnalysisLearningTests()
        {
            MediaOrchestrator.SetMediaAnalysisStoreDirectory(_storeDirectory);
            MediaOrchestrator.ClearMediaAnalysisStore();
            MediaOrchestrator.MediaAnalysisLearningEnabled = true;
        }

        public void Dispose()
        {
            MediaOrchestrator.ClearMediaAnalysisStore();
            MediaOrchestrator.SetMediaAnalysisStoreDirectory();
            MediaOrchestrator.MediaAnalysisLearningEnabled = true;
        }

        [Fact]
        public async Task ReportExecutionAsync_PersistsStatistics()
        {
            var snapshot = new MediaProbeSnapshot
            {
                ContainerHint = "mp4",
                HasAudio = true,
                HasVideo = true,
                PrimaryAudioCodec = "aac",
                PrimaryVideoCodec = "h264",
                DurationSeconds = 120
            };
            var constraints = new ProcessingConstraints
            {
                AllowRemux = true,
                AllowTranscode = true,
                AllowHardwareAcceleration = true,
                PreferredContainer = Format.mp4
            };
            var capabilities = new EnvironmentCapabilities
            {
                IsHardwareAccelerationDetected = true,
                DetectedHardwareAccelerator = "cuda"
            };
            var key = MediaProcessingAnalytics.BuildAnalysisKey(snapshot, ProcessingScenario.BrowserPlayback, constraints, capabilities);
            var session = new MediaAnalysisSession
            {
                AnalysisKey = key,
                Scenario = ProcessingScenario.BrowserPlayback,
                Constraints = constraints,
                Capabilities = capabilities,
                ProbeSnapshot = snapshot,
                Plan = new MediaProcessingPlan(
                    ProcessingScenario.BrowserPlayback,
                    MediaProcessingStrategy.Remux,
                    Format.mp4,
                    false,
                    new[] { ProcessingDecisionReason.BrowserCodecsCompatible })
            };

            await MediaOrchestrator.Analytics.ReportExecutionAsync(
                session,
                DateTime.UtcNow.AddSeconds(-4),
                DateTime.UtcNow,
                "-i input.mp4 -c copy output.mp4",
                succeeded: true,
                failureType: null,
                resourceMetrics: new ExecutionResourceMetrics
                {
                    PeakWorkingSetBytes = 128 * 1024 * 1024,
                    AverageCpuUsagePercent = 52.5,
                    PeakCpuUsagePercent = 88.2,
                    LogicalCoreCount = 12,
                    AverageAcceleratorUsagePercent = 63.4,
                    PeakAcceleratorUsagePercent = 91.7
                }).ConfigureAwait(false);

            var record = await MediaOrchestrator.MediaAnalysisStore.GetAsync(key).ConfigureAwait(false);

            Assert.NotNull(record);
            Assert.NotNull(record.LastPlan);
            Assert.Single(record.RecentExecutions);
            Assert.Equal(MediaProcessingStrategy.Remux, record.StrategyStatistics.Single().Strategy);
            Assert.Equal(1, record.StrategyStatistics.Single().Attempts);
            Assert.Equal(1, record.StrategyStatistics.Single().Successes);
            Assert.True(record.StrategyStatistics.Single().AverageSpeedFactor > 0);
            Assert.Equal(128 * 1024 * 1024, record.RecentExecutions[0].PeakWorkingSetBytes);
            Assert.Equal(52.5, record.RecentExecutions[0].AverageCpuUsagePercent);
            Assert.Equal(12, record.RecentExecutions[0].LogicalCoreCount);
            Assert.Equal(91.7, record.RecentExecutions[0].PeakAcceleratorUsagePercent);
        }

        [Fact]
        public void DecideProcessingPlan_UsesHistoricalStatsToPreferTranscode()
        {
            var analytics = new MediaProcessingAnalytics();
            var mediaInfo = new FakeMediaInfo(
                path: "sample.mp4",
                audioCodecs: new[] { "aac" },
                videoCodecs: new[] { "h264" });
            var constraints = new ProcessingConstraints
            {
                AllowRemux = true,
                AllowTranscode = true,
                PreferredContainer = Format.mp4
            };
            var capabilities = new EnvironmentCapabilities();
            var snapshot = MediaProbeSnapshot.Create(mediaInfo);
            var record = MediaAnalysisRecord.Create(
                MediaProcessingAnalytics.BuildAnalysisKey(snapshot, ProcessingScenario.BrowserPlayback, constraints, capabilities),
                ProcessingScenario.BrowserPlayback,
                snapshot,
                constraints,
                capabilities);

            var remuxStats = record.GetStrategyStatistics(MediaProcessingStrategy.Remux);
            remuxStats.Attempts = 4;
            remuxStats.Successes = 0;
            remuxStats.Failures = 4;
            remuxStats.TotalSpeedFactor = 1;
            remuxStats.SpeedFactorSamples = 1;

            var transcodeStats = record.GetStrategyStatistics(MediaProcessingStrategy.FullTranscode);
            transcodeStats.Attempts = 3;
            transcodeStats.Successes = 3;
            transcodeStats.Failures = 0;
            transcodeStats.TotalSpeedFactor = 2.4;
            transcodeStats.SpeedFactorSamples = 3;

            var plan = analytics.DecideProcessingPlan(
                mediaInfo,
                ProcessingScenario.BrowserPlayback,
                constraints,
                capabilities,
                record);

            Assert.Equal(MediaProcessingStrategy.FullTranscode, plan.Strategy);
            Assert.Contains(ProcessingDecisionReason.HistoricalPreferenceForTranscode, plan.Reasons);
        }

        [Fact]
        public async Task CachedStore_UsesMemoryImmediately_AndFlushesPersistentStoreLazily()
        {
            var persistentDirectory = Path.Combine(Path.GetTempPath(), "media-orchestrator-analysis-persistent-" + Guid.NewGuid().ToString("N"));
            var persistentStore = new FileMediaAnalysisStore(persistentDirectory);
            var cachedStore = new CachedMediaAnalysisStore(persistentStore, TimeSpan.FromMinutes(5));
            var record = MediaAnalysisRecord.Create(
                "analysis-key",
                ProcessingScenario.BrowserPlayback,
                new MediaProbeSnapshot
                {
                    ContainerHint = "mp4",
                    PrimaryVideoCodec = "h264",
                    PrimaryAudioCodec = "aac",
                    HasVideo = true,
                    HasAudio = true,
                    DurationSeconds = 10
                },
                new ProcessingConstraints { PreferredContainer = Format.mp4 },
                new EnvironmentCapabilities());

            record.LastPlan = MediaProcessingPlanSnapshot.Create(new MediaProcessingPlan(
                ProcessingScenario.BrowserPlayback,
                MediaProcessingStrategy.Remux,
                Format.mp4,
                false,
                new[] { ProcessingDecisionReason.RemuxAllowed }));

            await cachedStore.SaveAsync(record).ConfigureAwait(false);

            var immediate = await cachedStore.GetAsync("analysis-key").ConfigureAwait(false);
            var beforeFlush = await persistentStore.GetAsync("analysis-key").ConfigureAwait(false);

            Assert.NotNull(immediate);
            Assert.Null(beforeFlush);

            await cachedStore.FlushPendingAsync().ConfigureAwait(false);

            var afterFlush = await persistentStore.GetAsync("analysis-key").ConfigureAwait(false);
            Assert.NotNull(afterFlush);
            Assert.Equal(MediaProcessingStrategy.Remux, afterFlush.LastPlan.Strategy);
        }

        [Fact]
        public async Task FlushMediaAnalysisStoreAsync_ForcesPersistentWrite()
        {
            var snapshot = new MediaProbeSnapshot
            {
                ContainerHint = "mp4",
                HasAudio = true,
                HasVideo = true,
                PrimaryAudioCodec = "aac",
                PrimaryVideoCodec = "h264",
                DurationSeconds = 60
            };
            var constraints = new ProcessingConstraints
            {
                AllowRemux = true,
                AllowTranscode = true,
                PreferredContainer = Format.mp4
            };
            var capabilities = new EnvironmentCapabilities();
            var key = MediaProcessingAnalytics.BuildAnalysisKey(snapshot, ProcessingScenario.BrowserPlayback, constraints, capabilities);

            await MediaOrchestrator.Analytics.ReportExecutionAsync(
                new MediaAnalysisSession
                {
                    AnalysisKey = key,
                    Scenario = ProcessingScenario.BrowserPlayback,
                    Constraints = constraints,
                    Capabilities = capabilities,
                    ProbeSnapshot = snapshot,
                    Plan = new MediaProcessingPlan(
                        ProcessingScenario.BrowserPlayback,
                        MediaProcessingStrategy.Remux,
                        Format.mp4,
                        false,
                        new[] { ProcessingDecisionReason.RemuxAllowed })
                },
                DateTime.UtcNow.AddSeconds(-2),
                DateTime.UtcNow,
                "-i sample.mp4 -c copy out.mp4",
                succeeded: true,
                failureType: null).ConfigureAwait(false);

            await MediaOrchestrator.FlushMediaAnalysisStoreAsync().ConfigureAwait(false);

            var fileStore = new FileMediaAnalysisStore(MediaOrchestrator.MediaAnalysisStoreDirectory);
            var persisted = await fileStore.GetAsync(key).ConfigureAwait(false);

            Assert.NotNull(persisted);
            Assert.Single(persisted.RecentExecutions);
        }

        [Fact]
        public async Task FileStore_UsesShardedDirectories()
        {
            var persistentDirectory = Path.Combine(Path.GetTempPath(), "media-orchestrator-analysis-sharded-" + Guid.NewGuid().ToString("N"));
            var fileStore = new FileMediaAnalysisStore(persistentDirectory);
            var record = MediaAnalysisRecord.Create(
                "sharded-key",
                ProcessingScenario.BrowserPlayback,
                new MediaProbeSnapshot
                {
                    ContainerHint = "mp4",
                    HasVideo = true,
                    HasAudio = true,
                    PrimaryVideoCodec = "h264",
                    PrimaryAudioCodec = "aac",
                    DurationSeconds = 10
                },
                new ProcessingConstraints(),
                new EnvironmentCapabilities());

            await fileStore.SaveAsync(record).ConfigureAwait(false);

            var jsonFiles = Directory.GetFiles(persistentDirectory, "*.json", SearchOption.AllDirectories);

            Assert.Single(jsonFiles);
            Assert.NotEqual(persistentDirectory, Path.GetDirectoryName(jsonFiles[0]));
        }

        [Fact]
        public async Task ReportExecutionAsync_TruncatesLongArguments_AndLimitsRecentHistory()
        {
            var snapshot = new MediaProbeSnapshot
            {
                ContainerHint = "mp4",
                HasAudio = true,
                HasVideo = true,
                PrimaryAudioCodec = "aac",
                PrimaryVideoCodec = "h264",
                DurationSeconds = 45
            };
            var constraints = new ProcessingConstraints
            {
                AllowRemux = true,
                AllowTranscode = true,
                PreferredContainer = Format.mp4
            };
            var capabilities = new EnvironmentCapabilities();
            var key = MediaProcessingAnalytics.BuildAnalysisKey(snapshot, ProcessingScenario.BrowserPlayback, constraints, capabilities);
            var longArguments = new string('x', MediaProcessingAnalytics.MaxStoredArgumentsLength + 200);

            for (int i = 0; i < MediaProcessingAnalytics.MaxRecentExecutions + 5; i++)
            {
                await MediaOrchestrator.Analytics.ReportExecutionAsync(
                    new MediaAnalysisSession
                    {
                        AnalysisKey = key,
                        Scenario = ProcessingScenario.BrowserPlayback,
                        Constraints = constraints,
                        Capabilities = capabilities,
                        ProbeSnapshot = snapshot,
                        Plan = new MediaProcessingPlan(
                            ProcessingScenario.BrowserPlayback,
                            MediaProcessingStrategy.Remux,
                            Format.mp4,
                            false,
                            new[] { ProcessingDecisionReason.RemuxAllowed })
                    },
                    DateTime.UtcNow.AddSeconds(-2),
                    DateTime.UtcNow,
                    longArguments,
                    succeeded: true,
                    failureType: null).ConfigureAwait(false);
            }

            var record = await MediaOrchestrator.MediaAnalysisStore.GetAsync(key).ConfigureAwait(false);

            Assert.Equal(MediaProcessingAnalytics.MaxRecentExecutions, record.RecentExecutions.Count);
            Assert.All(record.RecentExecutions, sample => Assert.True(sample.Arguments.Length <= MediaProcessingAnalytics.MaxStoredArgumentsLength));
        }

        [Fact]
        public async Task GetMediaAnalyticsReportAsync_ReturnsPublicAggregates_WithDateFilter()
        {
            var now = DateTimeOffset.UtcNow;
            var recentMp4Key = MediaProcessingAnalytics.BuildAnalysisKey(
                new MediaProbeSnapshot
                {
                    ContainerHint = "mp4",
                    HasVideo = true,
                    HasAudio = true,
                    PrimaryVideoCodec = "h264",
                    PrimaryAudioCodec = "aac",
                    DurationSeconds = 90,
                    InputSizeBytes = 8 * 1024 * 1024
                },
                ProcessingScenario.BrowserPlayback,
                new ProcessingConstraints { PreferredContainer = Format.mp4 },
                new EnvironmentCapabilities());

            var oldWebmKey = MediaProcessingAnalytics.BuildAnalysisKey(
                new MediaProbeSnapshot
                {
                    ContainerHint = "webm",
                    HasVideo = true,
                    HasAudio = true,
                    PrimaryVideoCodec = "vp9",
                    PrimaryAudioCodec = "opus",
                    DurationSeconds = 600,
                    InputSizeBytes = 200 * 1024 * 1024
                },
                ProcessingScenario.Custom,
                new ProcessingConstraints { PreferredContainer = Format.webm },
                new EnvironmentCapabilities());

            await MediaOrchestrator.Analytics.ReportExecutionAsync(
                new MediaAnalysisSession
                {
                    AnalysisKey = recentMp4Key,
                    Scenario = ProcessingScenario.BrowserPlayback,
                    Constraints = new ProcessingConstraints { PreferredContainer = Format.mp4 },
                    Capabilities = new EnvironmentCapabilities(),
                    ProbeSnapshot = new MediaProbeSnapshot
                    {
                        ContainerHint = "mp4",
                        HasVideo = true,
                        HasAudio = true,
                        PrimaryVideoCodec = "h264",
                        PrimaryAudioCodec = "aac",
                        DurationSeconds = 90,
                        InputSizeBytes = 8 * 1024 * 1024
                    },
                    Plan = new MediaProcessingPlan(
                        ProcessingScenario.BrowserPlayback,
                        MediaProcessingStrategy.Remux,
                        Format.mp4,
                        false,
                        new[] { ProcessingDecisionReason.RemuxAllowed })
                },
                now.AddMinutes(-5).UtcDateTime,
                now.UtcDateTime,
                "-i recent.mp4 -c copy out.mp4",
                succeeded: true,
                failureType: null,
                resourceMetrics: new ExecutionResourceMetrics
                {
                    PeakWorkingSetBytes = 64 * 1024 * 1024,
                    AverageCpuUsagePercent = 40,
                    PeakCpuUsagePercent = 75,
                    LogicalCoreCount = 8,
                    AverageAcceleratorUsagePercent = 0,
                    PeakAcceleratorUsagePercent = 0
                }).ConfigureAwait(false);

            await MediaOrchestrator.Analytics.ReportExecutionAsync(
                new MediaAnalysisSession
                {
                    AnalysisKey = oldWebmKey,
                    Scenario = ProcessingScenario.Custom,
                    Constraints = new ProcessingConstraints { PreferredContainer = Format.webm },
                    Capabilities = new EnvironmentCapabilities(),
                    ProbeSnapshot = new MediaProbeSnapshot
                    {
                        ContainerHint = "webm",
                        HasVideo = true,
                        HasAudio = true,
                        PrimaryVideoCodec = "vp9",
                        PrimaryAudioCodec = "opus",
                        DurationSeconds = 600,
                        InputSizeBytes = 200 * 1024 * 1024
                    },
                    Plan = new MediaProcessingPlan(
                        ProcessingScenario.Custom,
                        MediaProcessingStrategy.FullTranscode,
                        Format.mp4,
                        false,
                        new[] { ProcessingDecisionReason.TranscodeAllowed })
                },
                now.AddDays(-10).UtcDateTime,
                now.AddDays(-10).AddMinutes(2).UtcDateTime,
                "-i old.webm -c:v libx264 out.mp4",
                succeeded: false,
                failureType: "TestFailure",
                resourceMetrics: new ExecutionResourceMetrics
                {
                    PeakWorkingSetBytes = 256 * 1024 * 1024,
                    AverageCpuUsagePercent = 90,
                    PeakCpuUsagePercent = 99,
                    LogicalCoreCount = 16,
                    AverageAcceleratorUsagePercent = 71,
                    PeakAcceleratorUsagePercent = 83
                }).ConfigureAwait(false);

            var report = await MediaOrchestrator.GetMediaAnalyticsReportAsync(new MediaAnalyticsQuery
            {
                FromUtc = now.AddDays(-1),
                ToUtc = now.AddDays(1),
                TimelineBucket = MediaAnalyticsTimeBucket.Day
            }).ConfigureAwait(false);

            Assert.Equal(1, report.Attempts);
            Assert.Equal(1, report.Successes);
            Assert.Equal(0, report.Failures);
            Assert.Equal("mp4", report.ByFileType.Single().Key);
            Assert.Equal("<10MB", report.BySizeBucket.Single().Key);
            Assert.Equal("BrowserPlayback", report.ByScenario.Single().Key);
            Assert.Equal("Remux", report.ByStrategy.Single().Key);
            Assert.Equal(64 * 1024 * 1024, report.PeakWorkingSetBytes);
            Assert.Equal(40, report.AverageCpuUsagePercent);
            Assert.Equal(75, report.PeakCpuUsagePercent);
            Assert.Equal(8, report.AverageLogicalCoreCount);
            Assert.Empty(report.ByFailureType);
            Assert.Empty(report.ByFailureCategory);
            Assert.Single(report.Timeline);
        }

        [Fact]
        public async Task GetMediaAnalyticsReportAsync_ReturnsFailureBreakdowns()
        {
            var now = DateTimeOffset.UtcNow;
            var key = MediaProcessingAnalytics.BuildAnalysisKey(
                new MediaProbeSnapshot
                {
                    ContainerHint = "webm",
                    HasVideo = true,
                    HasAudio = true,
                    PrimaryVideoCodec = "vp9",
                    PrimaryAudioCodec = "opus",
                    DurationSeconds = 120,
                    InputSizeBytes = 20 * 1024 * 1024
                },
                ProcessingScenario.Custom,
                new ProcessingConstraints { PreferredContainer = Format.mp4 },
                new EnvironmentCapabilities());

            await MediaOrchestrator.Analytics.ReportExecutionAsync(
                new MediaAnalysisSession
                {
                    AnalysisKey = key,
                    Scenario = ProcessingScenario.Custom,
                    Constraints = new ProcessingConstraints { PreferredContainer = Format.mp4 },
                    Capabilities = new EnvironmentCapabilities(),
                    ProbeSnapshot = new MediaProbeSnapshot
                    {
                        ContainerHint = "webm",
                        HasVideo = true,
                        HasAudio = true,
                        PrimaryVideoCodec = "vp9",
                        PrimaryAudioCodec = "opus",
                        DurationSeconds = 120,
                        InputSizeBytes = 20 * 1024 * 1024
                    },
                    Plan = new MediaProcessingPlan(
                        ProcessingScenario.Custom,
                        MediaProcessingStrategy.FullTranscode,
                        Format.mp4,
                        false,
                        new[] { ProcessingDecisionReason.TranscodeAllowed })
                },
                now.AddMinutes(-3).UtcDateTime,
                now.UtcDateTime,
                "-i broken.webm -c:v libx264 out.mp4",
                succeeded: false,
                failureType: "MediaOrchestrator.Exceptions.UnknownDecoderException").ConfigureAwait(false);

            var report = await MediaOrchestrator.GetMediaAnalyticsReportAsync(new MediaAnalyticsQuery
            {
                FromUtc = now.AddDays(-1),
                ToUtc = now.AddDays(1),
                TimelineBucket = MediaAnalyticsTimeBucket.Day
            }).ConfigureAwait(false);

            Assert.Single(report.ByFailureType);
            Assert.Equal("UnknownDecoderException", report.ByFailureType.Single().Key);
            Assert.Single(report.ByErrorCode);
            Assert.Equal("MOR-CV-1001", report.ByErrorCode.Single().Key);
            Assert.Single(report.ByFailureCategory);
            Assert.Equal("codec", report.ByFailureCategory.Single().Key);
            Assert.Equal(1, report.ByFailureCategory.Single().Failures);
        }

        private sealed class FakeMediaInfo : IMediaInfo
        {
            public FakeMediaInfo(string path, IEnumerable<string> audioCodecs, IEnumerable<string> videoCodecs)
            {
                Path = path;
                AudioStreams = (audioCodecs ?? Enumerable.Empty<string>())
                    .Select((codec, i) => new AudioStream { Codec = codec, Index = i, SampleRate = 48000, Channels = 2 })
                    .Cast<IAudioStream>()
                    .ToArray();
                VideoStreams = (videoCodecs ?? Enumerable.Empty<string>())
                    .Select((codec, i) => new VideoStream { Codec = codec, Index = i, Width = 1920, Height = 1080, Framerate = 30 })
                    .Cast<IVideoStream>()
                    .ToArray();
                SubtitleStreams = Array.Empty<ISubtitleStream>();
            }

            public IEnumerable<IStream> Streams => VideoStreams.Cast<IStream>().Concat(AudioStreams).Concat(SubtitleStreams);

            public string Path { get; }

            public TimeSpan Duration => TimeSpan.FromMinutes(2);

            public DateTime? CreationTime => null;

            public long Size => 1024;

            public string FormatName => "mp4";

            public long Bitrate => 1024;

            public IReadOnlyDictionary<string, string> Metadata => new Dictionary<string, string>();

            public IEnumerable<IVideoStream> VideoStreams { get; }

            public IEnumerable<IAudioStream> AudioStreams { get; }

            public IEnumerable<ISubtitleStream> SubtitleStreams { get; }
        }
    }
}
