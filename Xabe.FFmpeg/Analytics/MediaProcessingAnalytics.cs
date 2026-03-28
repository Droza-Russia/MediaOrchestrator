using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg.Analytics.Models;
using Xabe.FFmpeg.Exceptions;

namespace Xabe.FFmpeg.Analytics
{
    /// <summary>
    ///     Decision layer для выбора сценария обработки.
    /// </summary>
    public sealed class MediaProcessingAnalytics
    {
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

            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputPath, cancellationToken).ConfigureAwait(false);
            return DecideProcessingPlan(mediaInfo, scenario, constraints, capabilities);
        }

        internal MediaProcessingPlan DecideProcessingPlan(
            IMediaInfo mediaInfo,
            ProcessingScenario scenario,
            ProcessingConstraints constraints,
            EnvironmentCapabilities capabilities)
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

                    if (isBrowserCompatible && constraints.AllowRemux)
                    {
                        reasons.Add(ProcessingDecisionReason.BrowserCodecsCompatible);
                        reasons.Add(ProcessingDecisionReason.RemuxAllowed);
                        return new MediaProcessingPlan(
                            scenario,
                            MediaProcessingStrategy.Remux,
                            targetContainer,
                            false,
                            reasons);
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
                    if (constraints.AllowRemux)
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
            var plan = await DecideProcessingPlanAsync(inputPath, scenario, constraints, capabilities, cancellationToken).ConfigureAwait(false);
            return await BuildConversionAsync(inputPath, outputPath, plan, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IConversion> BuildConversionAsync(
            string inputPath,
            string outputPath,
            MediaProcessingPlan plan,
            CancellationToken cancellationToken = default)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            switch (plan.Scenario)
            {
                case ProcessingScenario.AiTranscription:
                    return await FFmpeg.Conversions.FromSnippet
                        .NormalizeAudioForTranscription(inputPath, outputPath, cancellationToken)
                        .ConfigureAwait(false);

                case ProcessingScenario.BrowserPlayback:
                    if (plan.IsRemux)
                    {
                        return await FFmpeg.Conversions.FromSnippet
                            .RemuxStream(inputPath, outputPath, keepSubtitles: true, outputFormat: plan.TargetContainer, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return await FFmpeg.Conversions.FromSnippet
                        .ToMp4(inputPath, outputPath, cancellationToken)
                        .ConfigureAwait(false);

                default:
                    if (plan.IsRemux)
                    {
                        return await FFmpeg.Conversions.FromSnippet
                            .RemuxStream(inputPath, outputPath, keepSubtitles: true, outputFormat: plan.TargetContainer, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return await FFmpeg.Conversions.FromSnippet
                        .ToMp4(inputPath, outputPath, cancellationToken)
                        .ConfigureAwait(false);
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
    }
}
