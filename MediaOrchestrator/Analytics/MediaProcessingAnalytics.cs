using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Analytics.Models;
using MediaOrchestrator.Exceptions;

namespace MediaOrchestrator.Analytics
{
    /// <summary>
    ///     Decision layer для выбора сценария обработки.
    /// </summary>
    public sealed class MediaProcessingAnalytics
    {
        internal const int ModelVersion = 1;
        internal const int MaxRecentExecutions = 12;
        internal const int MaxStoredArgumentsLength = 512;

        public async Task<MediaProcessingPlan> DecideProcessingPlanAsync(
            string inputPath,
            ProcessingScenario scenario,
            ProcessingConstraints constraints = null,
            EnvironmentCapabilities capabilities = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException(ErrorMessages.InputPathMustBeProvided, nameof(inputPath));
            }

            constraints = constraints ?? ProcessingConstraints.Default;
            capabilities = capabilities ?? EnvironmentCapabilities.DetectFromCurrentProcess();

            IMediaInfo mediaInfo = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken).ConfigureAwait(false);
            var analysisRecord = await TryLoadAnalysisRecordAsync(mediaInfo, scenario, constraints, capabilities, cancellationToken).ConfigureAwait(false);
            return DecideProcessingPlan(mediaInfo, scenario, constraints, capabilities, analysisRecord);
        }

        internal MediaProcessingPlan DecideProcessingPlan(
            IMediaInfo mediaInfo,
            ProcessingScenario scenario,
            ProcessingConstraints constraints,
            EnvironmentCapabilities capabilities)
        {
            return DecideProcessingPlan(mediaInfo, scenario, constraints, capabilities, null);
        }

        internal MediaProcessingPlan DecideProcessingPlan(
            IMediaInfo mediaInfo,
            ProcessingScenario scenario,
            ProcessingConstraints constraints,
            EnvironmentCapabilities capabilities,
            MediaAnalysisRecord analysisRecord)
        {
            if (mediaInfo == null)
            {
                throw new ArgumentNullException(nameof(mediaInfo));
            }

            constraints = constraints ?? ProcessingConstraints.Default;
            capabilities = capabilities ?? EnvironmentCapabilities.DetectFromCurrentProcess();

            var reasons = new List<ProcessingDecisionReason>();
            var useHardwareAcceleration = constraints.AllowHardwareAcceleration && capabilities.IsHardwareAccelerationDetected;
            reasons.Add(useHardwareAcceleration
                ? ProcessingDecisionReason.HardwareAccelerationAllowed
                : ProcessingDecisionReason.HardwareAccelerationNotAllowed);

            if (analysisRecord != null)
            {
                reasons.Add(ProcessingDecisionReason.HistoricalStatisticsAvailable);
            }

            switch (scenario)
            {
                case ProcessingScenario.AiTranscription:
                    reasons.Add(ProcessingDecisionReason.ScenarioAiTranscriptionContract);
                    if (!mediaInfo.AudioStreams.Any())
                    {
                        reasons.Add(ProcessingDecisionReason.MissingAudioStream);
                        throw new AudioStreamNotFoundException(ErrorMessages.InputFileDoesNotContainAudioStream, nameof(mediaInfo));
                    }

                    return new MediaProcessingPlan(
                        scenario,
                        MediaProcessingStrategy.NormalizeAudio,
                        Format.wav,
                        false,
                        reasons);

                case ProcessingScenario.BrowserPlayback:
                    reasons.Add(ProcessingDecisionReason.ScenarioBrowserCompatibility);
                    var targetContainer = constraints.PreferredContainer ?? Format.mp4;
                    var isBrowserCompatible = IsBrowserCompatible(mediaInfo, targetContainer);

                    if (isBrowserCompatible)
                    {
                        reasons.Add(ProcessingDecisionReason.BrowserCodecsCompatible);
                        var strategy = SelectAdaptiveStrategy(
                            scenario,
                            analysisRecord,
                            constraints,
                            reasons,
                            MediaProcessingStrategy.Remux,
                            MediaProcessingStrategy.Remux,
                            MediaProcessingStrategy.FullTranscode);

                        if (strategy == MediaProcessingStrategy.Remux && constraints.AllowRemux)
                        {
                            reasons.Add(ProcessingDecisionReason.RemuxAllowed);
                            return new MediaProcessingPlan(
                                scenario,
                                MediaProcessingStrategy.Remux,
                                targetContainer,
                                false,
                                reasons);
                        }

                        if (strategy == MediaProcessingStrategy.FullTranscode && constraints.AllowTranscode)
                        {
                            reasons.Add(ProcessingDecisionReason.TranscodeAllowed);
                            return new MediaProcessingPlan(
                                scenario,
                                MediaProcessingStrategy.FullTranscode,
                                Format.mp4,
                                useHardwareAcceleration,
                                reasons);
                        }
                    }

                    reasons.Add(isBrowserCompatible
                        ? ProcessingDecisionReason.RemuxNotAllowed
                        : ProcessingDecisionReason.BrowserCodecsIncompatible);

                    if (!constraints.AllowTranscode)
                    {
                        reasons.Add(ProcessingDecisionReason.TranscodeNotAllowed);
                        return new MediaProcessingPlan(
                            scenario,
                            MediaProcessingStrategy.Remux,
                            targetContainer,
                            false,
                            reasons);
                    }

                    reasons.Add(ProcessingDecisionReason.TranscodeAllowed);
                    return new MediaProcessingPlan(
                        scenario,
                        MediaProcessingStrategy.FullTranscode,
                        Format.mp4,
                        useHardwareAcceleration,
                        reasons);

                default:
                    var defaultStrategy = SelectAdaptiveStrategy(
                        scenario,
                        analysisRecord,
                        constraints,
                        reasons,
                        MediaProcessingStrategy.Remux,
                        MediaProcessingStrategy.Remux,
                        MediaProcessingStrategy.FullTranscode);

                    if (defaultStrategy == MediaProcessingStrategy.Remux && constraints.AllowRemux)
                    {
                        reasons.Add(ProcessingDecisionReason.RemuxAllowed);
                        return new MediaProcessingPlan(
                            scenario,
                            MediaProcessingStrategy.Remux,
                            constraints.PreferredContainer ?? DetectFormatFromPath(mediaInfo.Path) ?? Format.mp4,
                            false,
                            reasons);
                    }

                    reasons.Add(ProcessingDecisionReason.RemuxNotAllowed);
                    return new MediaProcessingPlan(
                        scenario,
                        MediaProcessingStrategy.FullTranscode,
                        constraints.PreferredContainer ?? Format.mp4,
                        useHardwareAcceleration,
                        reasons);
            }
        }

        public async Task<IConversion> BuildConversionAsync(
            string inputPath,
            string outputPath,
            ProcessingScenario scenario,
            ProcessingConstraints constraints = null,
            EnvironmentCapabilities capabilities = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException(ErrorMessages.InputPathMustBeProvided, nameof(inputPath));
            }

            constraints = constraints ?? ProcessingConstraints.Default;
            capabilities = capabilities ?? EnvironmentCapabilities.DetectFromCurrentProcess();

            IMediaInfo mediaInfo = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken).ConfigureAwait(false);
            var analysisRecord = await TryLoadAnalysisRecordAsync(mediaInfo, scenario, constraints, capabilities, cancellationToken).ConfigureAwait(false);
            var plan = DecideProcessingPlan(mediaInfo, scenario, constraints, capabilities, analysisRecord);
            return await BuildConversionAsync(inputPath, outputPath, plan, mediaInfo, constraints, capabilities, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IConversion> BuildConversionAsync(
            string inputPath,
            string outputPath,
            MediaProcessingPlan plan,
            CancellationToken cancellationToken = default)
        {
            return await BuildConversionAsync(
                    inputPath,
                    outputPath,
                    plan,
                    null,
                    ProcessingConstraints.Default,
                    EnvironmentCapabilities.DetectFromCurrentProcess(),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task ReportExecutionAsync(
            MediaAnalysisSession session,
            DateTime startTime,
            DateTime endTime,
            string arguments,
            bool succeeded,
            string failureType,
            ExecutionResourceMetrics resourceMetrics = null)
        {
            if (!MediaOrchestrator.MediaAnalysisLearningEnabled || session == null)
            {
                return;
            }

            var store = MediaOrchestrator.MediaAnalysisStore;
            if (store == null)
            {
                return;
            }

            var record = await store.GetAsync(session.AnalysisKey, CancellationToken.None).ConfigureAwait(false)
                         ?? MediaAnalysisRecord.Create(session.AnalysisKey, session.Scenario, session.ProbeSnapshot, session.Constraints, session.Capabilities);

            record.LastPlan = MediaProcessingPlanSnapshot.Create(session.Plan);
            record.UpdatedUtc = DateTimeOffset.UtcNow;

            var sample = CreateExecutionSample(session, startTime, endTime, arguments, succeeded, failureType, resourceMetrics);
            record.RecentExecutions.Add(sample);
            while (record.RecentExecutions.Count > MaxRecentExecutions)
            {
                record.RecentExecutions.RemoveAt(0);
            }

            UpdateStrategyStatistics(record.GetStrategyStatistics(session.Plan.Strategy), sample);
            await store.SaveAsync(record, CancellationToken.None).ConfigureAwait(false);

            var operationDuration = endTime - startTime;
            var operationKey = MediaOrchestrator.BuildOperationKey(
                session.AnalysisKey,
                string.Empty,
                session.Scenario,
                session.Plan.Strategy,
                session.ProbeSnapshot?.PrimaryVideoCodec,
                session.ProbeSnapshot?.PrimaryAudioCodec,
                session.ProbeSnapshot?.DurationSeconds,
                sample.UsedHardwareAcceleration);

            MediaOrchestrator.RecordOperationDuration(operationKey, operationDuration, succeeded);
        }

        private async Task<IConversion> BuildConversionAsync(
            string inputPath,
            string outputPath,
            MediaProcessingPlan plan,
            IMediaInfo mediaInfo,
            ProcessingConstraints constraints,
            EnvironmentCapabilities capabilities,
            CancellationToken cancellationToken)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var effectiveMediaInfo = mediaInfo ?? await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken).ConfigureAwait(false);
            var session = CreateAnalysisSession(effectiveMediaInfo, plan, constraints, capabilities);
            await PersistDecisionSnapshotAsync(session, cancellationToken).ConfigureAwait(false);

            IConversion conversion;
            switch (plan.Scenario)
            {
                case ProcessingScenario.AiTranscription:
                    conversion = mediaInfo != null
                        ? Conversion.NormalizeAudioForTranscription(mediaInfo, inputPath, outputPath, null, cancellationToken)
                        : await MediaOrchestrator.Conversions.FromSnippet
                            .NormalizeAudioForTranscription(inputPath, outputPath, cancellationToken)
                            .ConfigureAwait(false);
                    break;

                case ProcessingScenario.BrowserPlayback:
                    if (plan.IsRemux)
                    {
                        conversion = mediaInfo != null
                            ? Conversion.RemuxStreamAsync(mediaInfo, outputPath, plan.TargetContainer, keepSubtitles: true)
                            : await MediaOrchestrator.Conversions.FromSnippet
                                .RemuxStream(inputPath, outputPath, keepSubtitles: true, outputFormat: plan.TargetContainer, cancellationToken)
                                .ConfigureAwait(false);
                    }
                    else
                    {
                        conversion = mediaInfo != null
                            ? Conversion.ToMp4(mediaInfo, outputPath)
                            : await MediaOrchestrator.Conversions.FromSnippet
                                .ToMp4(inputPath, outputPath, cancellationToken)
                                .ConfigureAwait(false);
                    }

                    break;

                default:
                    if (plan.IsRemux)
                    {
                        conversion = mediaInfo != null
                            ? Conversion.RemuxStreamAsync(mediaInfo, outputPath, plan.TargetContainer, keepSubtitles: true)
                            : await MediaOrchestrator.Conversions.FromSnippet
                                .RemuxStream(inputPath, outputPath, keepSubtitles: true, outputFormat: plan.TargetContainer, cancellationToken)
                                .ConfigureAwait(false);
                    }
                    else
                    {
                        conversion = mediaInfo != null
                            ? Conversion.ToMp4(mediaInfo, outputPath)
                            : await MediaOrchestrator.Conversions.FromSnippet
                                .ToMp4(inputPath, outputPath, cancellationToken)
                                .ConfigureAwait(false);
                    }

                    break;
            }

            return AttachAnalyticsSession(conversion, session);
        }

        private static MediaProcessingStrategy SelectAdaptiveStrategy(
            ProcessingScenario scenario,
            MediaAnalysisRecord analysisRecord,
            ProcessingConstraints constraints,
            IList<ProcessingDecisionReason> reasons,
            MediaProcessingStrategy defaultPreferredStrategy,
            params MediaProcessingStrategy[] candidates)
        {
            var availableCandidates = candidates
                .Where(candidate => IsCandidateAllowed(candidate, constraints))
                .Distinct()
                .ToList();

            if (!availableCandidates.Any())
            {
                return defaultPreferredStrategy;
            }

            if (analysisRecord == null || !MediaOrchestrator.MediaAnalysisLearningEnabled)
            {
                return availableCandidates.Contains(defaultPreferredStrategy)
                    ? defaultPreferredStrategy
                    : availableCandidates[0];
            }

            double bestScore = double.MinValue;
            var bestStrategy = defaultPreferredStrategy;
            foreach (var candidate in availableCandidates)
            {
                double score = GetBaseStrategyScore(scenario, candidate, defaultPreferredStrategy);
                var statistics = analysisRecord.GetStrategyStatistics(candidate);
                if (statistics.Attempts > 0)
                {
                    score += statistics.SuccessRate * 35;
                    score -= statistics.FailureRate * 30;
                    score += Math.Min(20, statistics.AverageSpeedFactor * 10);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestStrategy = candidate;
                }
            }

            if (bestStrategy == MediaProcessingStrategy.Remux && bestStrategy != defaultPreferredStrategy)
            {
                reasons.Add(ProcessingDecisionReason.HistoricalPreferenceForRemux);
            }
            else if (bestStrategy == MediaProcessingStrategy.FullTranscode && bestStrategy != defaultPreferredStrategy)
            {
                reasons.Add(ProcessingDecisionReason.HistoricalPreferenceForTranscode);
            }

            return bestStrategy;
        }

        private async Task<MediaAnalysisRecord> TryLoadAnalysisRecordAsync(
            IMediaInfo mediaInfo,
            ProcessingScenario scenario,
            ProcessingConstraints constraints,
            EnvironmentCapabilities capabilities,
            CancellationToken cancellationToken)
        {
            if (!MediaOrchestrator.MediaAnalysisLearningEnabled)
            {
                return null;
            }

            var store = MediaOrchestrator.MediaAnalysisStore;
            if (store == null)
            {
                return null;
            }

            var snapshot = MediaProbeSnapshot.Create(mediaInfo);
            var key = BuildAnalysisKey(snapshot, scenario, constraints, capabilities);
            return await store.GetAsync(key, cancellationToken).ConfigureAwait(false);
        }

        private async Task PersistDecisionSnapshotAsync(MediaAnalysisSession session, CancellationToken cancellationToken)
        {
            if (!MediaOrchestrator.MediaAnalysisLearningEnabled || session == null)
            {
                return;
            }

            var store = MediaOrchestrator.MediaAnalysisStore;
            if (store == null)
            {
                return;
            }

            var record = await store.GetAsync(session.AnalysisKey, cancellationToken).ConfigureAwait(false)
                         ?? MediaAnalysisRecord.Create(session.AnalysisKey, session.Scenario, session.ProbeSnapshot, session.Constraints, session.Capabilities);
            record.LastPlan = MediaProcessingPlanSnapshot.Create(session.Plan);
            record.UpdatedUtc = DateTimeOffset.UtcNow;
            await store.SaveAsync(record, cancellationToken).ConfigureAwait(false);
        }

        private static MediaExecutionSample CreateExecutionSample(
            MediaAnalysisSession session,
            DateTime startTime,
            DateTime endTime,
            string arguments,
            bool succeeded,
            string failureType,
            ExecutionResourceMetrics resourceMetrics)
        {
            double processingMilliseconds = Math.Max(0, (endTime - startTime).TotalMilliseconds);
            double inputDurationSeconds = session.ProbeSnapshot?.DurationSeconds ?? 0;
            double speedFactor = 0;
            if (processingMilliseconds > 0 && inputDurationSeconds > 0)
            {
                speedFactor = inputDurationSeconds / (processingMilliseconds / 1000d);
            }

            return new MediaExecutionSample
            {
                TimestampUtc = new DateTimeOffset(endTime.ToUniversalTime(), TimeSpan.Zero),
                Strategy = session.Plan.Strategy,
                Succeeded = succeeded,
                ProcessingMilliseconds = processingMilliseconds,
                InputDurationSeconds = inputDurationSeconds,
                SpeedFactor = speedFactor,
                UsedHardwareAcceleration = UsesHardwareAcceleration(arguments),
                PeakWorkingSetBytes = resourceMetrics?.PeakWorkingSetBytes ?? 0,
                AverageCpuUsagePercent = resourceMetrics?.AverageCpuUsagePercent ?? 0,
                PeakCpuUsagePercent = resourceMetrics?.PeakCpuUsagePercent ?? 0,
                LogicalCoreCount = resourceMetrics?.LogicalCoreCount ?? Environment.ProcessorCount,
                AverageAcceleratorUsagePercent = resourceMetrics?.AverageAcceleratorUsagePercent ?? 0,
                PeakAcceleratorUsagePercent = resourceMetrics?.PeakAcceleratorUsagePercent ?? 0,
                FailureType = SimplifyFailureType(failureType),
                FailureCategory = ClassifyFailureCategory(failureType),
                ErrorCode = ResolveErrorCode(failureType),
                Arguments = Truncate(arguments, MaxStoredArgumentsLength)
            };
        }

        private static string ResolveErrorCode(string failureType)
        {
            if (string.IsNullOrWhiteSpace(failureType))
            {
                return string.Empty;
            }

            var errorCode = MediaErrorCatalog.Resolve(failureType);
            return MediaErrorCatalog.Get(errorCode).Code;
        }

        private static string SimplifyFailureType(string failureType)
        {
            if (string.IsNullOrWhiteSpace(failureType))
            {
                return string.Empty;
            }

            int separatorIndex = failureType.LastIndexOf('.');
            return separatorIndex >= 0 && separatorIndex < failureType.Length - 1
                ? failureType.Substring(separatorIndex + 1)
                : failureType;
        }

        private static string ClassifyFailureCategory(string failureType)
        {
            if (string.IsNullOrWhiteSpace(failureType))
            {
                return string.Empty;
            }

            var normalized = SimplifyFailureType(failureType);
            if (normalized.IndexOf("OperationCanceled", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "cancellation";
            }

            if (normalized.IndexOf("Disk", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "disk";
            }

            if (normalized.IndexOf("HardwareAccelerator", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "hardware";
            }

            if (normalized.IndexOf("Decoder", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("Codec", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "codec";
            }

            if (normalized.IndexOf("Mapping", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("Stream", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "stream";
            }

            if (normalized.IndexOf("Output", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("Path", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("Directory", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "io";
            }

            if (normalized.IndexOf("Conversion", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "ffmpeg";
            }

            return "other";
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || maxLength <= 0)
            {
                return string.Empty;
            }

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        private static void UpdateStrategyStatistics(StrategyExecutionStatistics statistics, MediaExecutionSample sample)
        {
            statistics.Attempts++;
            if (sample.Succeeded)
            {
                statistics.Successes++;
            }
            else
            {
                statistics.Failures++;
            }

            if (sample.ProcessingMilliseconds > 0)
            {
                statistics.TotalProcessingMilliseconds += sample.ProcessingMilliseconds;
                statistics.ProcessingSamples++;
            }

            if (sample.SpeedFactor > 0)
            {
                statistics.TotalSpeedFactor += sample.SpeedFactor;
                statistics.SpeedFactorSamples++;
            }

            if (sample.PeakWorkingSetBytes > 0)
            {
                statistics.TotalPeakWorkingSetBytes += sample.PeakWorkingSetBytes;
                statistics.PeakWorkingSetSamples++;
                statistics.MaxPeakWorkingSetBytes = Math.Max(statistics.MaxPeakWorkingSetBytes, sample.PeakWorkingSetBytes);
            }

            if (sample.AverageCpuUsagePercent > 0)
            {
                statistics.TotalAverageCpuUsagePercent += sample.AverageCpuUsagePercent;
                statistics.AverageCpuUsageSamples++;
            }

            if (sample.PeakCpuUsagePercent > 0)
            {
                statistics.MaxPeakCpuUsagePercent = Math.Max(statistics.MaxPeakCpuUsagePercent, sample.PeakCpuUsagePercent);
            }

            if (sample.LogicalCoreCount > 0)
            {
                statistics.TotalLogicalCoreCount += sample.LogicalCoreCount;
                statistics.LogicalCoreSamples++;
            }

            if (sample.AverageAcceleratorUsagePercent > 0)
            {
                statistics.TotalAverageAcceleratorUsagePercent += sample.AverageAcceleratorUsagePercent;
                statistics.AverageAcceleratorUsageSamples++;
            }

            if (sample.PeakAcceleratorUsagePercent > 0)
            {
                statistics.MaxPeakAcceleratorUsagePercent = Math.Max(statistics.MaxPeakAcceleratorUsagePercent, sample.PeakAcceleratorUsagePercent);
            }

            statistics.LastUpdatedUtc = sample.TimestampUtc;
        }

        private static MediaAnalysisSession CreateAnalysisSession(
            IMediaInfo mediaInfo,
            MediaProcessingPlan plan,
            ProcessingConstraints constraints,
            EnvironmentCapabilities capabilities)
        {
            var snapshot = MediaProbeSnapshot.Create(mediaInfo);
            return new MediaAnalysisSession
            {
                AnalysisKey = BuildAnalysisKey(snapshot, plan.Scenario, constraints, capabilities),
                Scenario = plan.Scenario,
                Constraints = constraints ?? ProcessingConstraints.Default,
                Capabilities = capabilities ?? EnvironmentCapabilities.DetectFromCurrentProcess(),
                ProbeSnapshot = snapshot,
                Plan = plan
            };
        }

        private static bool UsesHardwareAcceleration(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return false;
            }

            return arguments.IndexOf("-hwaccel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   arguments.IndexOf("_nvenc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   arguments.IndexOf("_qsv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   arguments.IndexOf("_vaapi", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   arguments.IndexOf("_videotoolbox", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   arguments.IndexOf("_amf", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IConversion AttachAnalyticsSession(IConversion conversion, MediaAnalysisSession session)
        {
            var concrete = conversion as Conversion;
            if (concrete != null)
            {
                concrete.AttachAnalyticsSession(session);
            }

            return conversion;
        }

        private static bool IsCandidateAllowed(MediaProcessingStrategy candidate, ProcessingConstraints constraints)
        {
            switch (candidate)
            {
                case MediaProcessingStrategy.Remux:
                    return constraints.AllowRemux;
                case MediaProcessingStrategy.FullTranscode:
                case MediaProcessingStrategy.PartialTranscode:
                    return constraints.AllowTranscode;
                default:
                    return true;
            }
        }

        private static double GetBaseStrategyScore(
            ProcessingScenario scenario,
            MediaProcessingStrategy strategy,
            MediaProcessingStrategy defaultPreferredStrategy)
        {
            if (strategy == defaultPreferredStrategy)
            {
                return 80;
            }

            switch (scenario)
            {
                case ProcessingScenario.BrowserPlayback:
                    return strategy == MediaProcessingStrategy.FullTranscode ? 55 : 50;
                default:
                    return strategy == MediaProcessingStrategy.FullTranscode ? 60 : 50;
            }
        }

        private static bool IsBrowserCompatible(IMediaInfo mediaInfo, Format targetContainer)
        {
            var videoCodec = mediaInfo.VideoStreams.FirstOrDefault()?.Codec;
            var audioCodec = mediaInfo.AudioStreams.FirstOrDefault()?.Codec;

            if (targetContainer == Format.webm)
            {
                return IsIn(videoCodec, "vp8", "vp9", "av1") && IsIn(audioCodec, "opus", "vorbis");
            }

            return IsIn(videoCodec, "h264", "avc1") && IsIn(audioCodec, "aac", "mp4a");
        }

        private static bool IsIn(string value, params string[] allowedValues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return allowedValues.Any(candidate => string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase));
        }

        private static Format? DetectFormatFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var extension = Path.GetExtension(path);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return null;
            }

            var normalized = extension.TrimStart('.');
            if (Enum.TryParse<Format>(normalized, true, out var format))
            {
                return format;
            }

            return null;
        }

        internal static string BuildAnalysisKey(
            MediaProbeSnapshot snapshot,
            ProcessingScenario scenario,
            ProcessingConstraints constraints,
            EnvironmentCapabilities capabilities)
        {
            constraints = constraints ?? ProcessingConstraints.Default;
            capabilities = capabilities ?? EnvironmentCapabilities.DetectFromCurrentProcess();

            return string.Join("|", new[]
            {
                "model:" + ModelVersion,
                "scenario:" + scenario,
                "probe:" + (snapshot?.ToSignature() ?? string.Empty),
                "container:" + (constraints.PreferredContainer?.ToString() ?? string.Empty),
                "remux:" + constraints.AllowRemux,
                "transcode:" + constraints.AllowTranscode,
                "hw:" + constraints.AllowHardwareAcceleration,
                "detected:" + (capabilities.DetectedHardwareAccelerator ?? string.Empty)
            });
        }
    }
}
