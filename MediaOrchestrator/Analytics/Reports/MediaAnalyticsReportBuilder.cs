using System;
using System.Collections.Generic;
using System.Linq;
using MediaOrchestrator.Analytics.Models;

namespace MediaOrchestrator.Analytics.Reports
{
    internal static class MediaAnalyticsReportBuilder
    {
        internal static MediaAnalyticsReport Build(IEnumerable<MediaAnalysisRecord> records, MediaAnalyticsQuery query)
        {
            query = query ?? new MediaAnalyticsQuery();
            var samples = Flatten(records, query.FromUtc, query.ToUtc).ToList();

            return new MediaAnalyticsReport
            {
                GeneratedAtUtc = DateTimeOffset.UtcNow,
                FromUtc = query.FromUtc,
                ToUtc = query.ToUtc,
                Attempts = samples.Count,
                Successes = samples.Count(sample => sample.Sample.Succeeded),
                Failures = samples.Count(sample => !sample.Sample.Succeeded),
                HardwareAcceleratedRuns = samples.Count(sample => sample.Sample.UsedHardwareAcceleration),
                AverageProcessingMilliseconds = Average(samples.Select(sample => sample.Sample.ProcessingMilliseconds)),
                AverageSpeedFactor = Average(samples.Select(sample => sample.Sample.SpeedFactor)),
                AveragePeakWorkingSetBytes = Average(samples.Select(sample => (double)sample.Sample.PeakWorkingSetBytes)),
                PeakWorkingSetBytes = Max(samples.Select(sample => sample.Sample.PeakWorkingSetBytes)),
                AverageCpuUsagePercent = Average(samples.Select(sample => sample.Sample.AverageCpuUsagePercent)),
                PeakCpuUsagePercent = Max(samples.Select(sample => sample.Sample.PeakCpuUsagePercent)),
                AverageLogicalCoreCount = Average(samples.Select(sample => (double)sample.Sample.LogicalCoreCount)),
                AverageAcceleratorUsagePercent = Average(samples.Select(sample => sample.Sample.AverageAcceleratorUsagePercent)),
                PeakAcceleratorUsagePercent = Max(samples.Select(sample => sample.Sample.PeakAcceleratorUsagePercent)),
                TotalInputDurationSeconds = samples.Sum(sample => sample.Sample.InputDurationSeconds),
                // Deduplicate by AnalysisKey to avoid counting the same file multiple times.
                TotalInputSizeBytes = samples.GroupBy(s => s.Record.AnalysisKey, StringComparer.Ordinal)
                                             .Sum(g => g.First().Record.ProbeSnapshot?.InputSizeBytes ?? 0),
                ByScenario = BuildBreakdown(samples, sample => sample.Record.Scenario.ToString()),
                ByStrategy = BuildBreakdown(samples, sample => sample.Sample.Strategy.ToString()),
                ByFileType = BuildBreakdown(samples, sample => sample.Record.ProbeSnapshot?.ContainerHint ?? string.Empty),
                BySizeBucket = BuildBreakdown(samples, sample => SizeBucket(sample.Record.ProbeSnapshot?.InputSizeBytes ?? 0)),
                ByDurationBucket = BuildBreakdown(samples, sample => DurationBucket(sample.Record.ProbeSnapshot?.DurationSeconds ?? 0)),
                ByHardwareAccelerator = BuildBreakdown(samples, sample => sample.Record.DetectedHardwareAccelerator ?? string.Empty),
                ByErrorCode = BuildFailureBreakdown(samples, sample => sample.Sample.ErrorCode),
                ByFailureType = BuildFailureBreakdown(samples, sample => sample.Sample.FailureType),
                ByFailureCategory = BuildFailureBreakdown(samples, sample => sample.Sample.FailureCategory),
                Timeline = BuildTimeline(samples, query.TimelineBucket)
            };
        }

        private static IEnumerable<SampleEnvelope> Flatten(IEnumerable<MediaAnalysisRecord> records, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
        {
            foreach (var record in records ?? Enumerable.Empty<MediaAnalysisRecord>())
            {
                if (record?.RecentExecutions == null)
                {
                    continue;
                }

                foreach (var sample in record.RecentExecutions)
                {
                    if (sample == null)
                    {
                        continue;
                    }

                    if (fromUtc.HasValue && sample.TimestampUtc < fromUtc.Value)
                    {
                        continue;
                    }

                    if (toUtc.HasValue && sample.TimestampUtc > toUtc.Value)
                    {
                        continue;
                    }

                    yield return new SampleEnvelope
                    {
                        Record = record,
                        Sample = sample
                    };
                }
            }
        }

        private static IReadOnlyCollection<MediaAnalyticsBreakdownItem> BuildBreakdown(
            IEnumerable<SampleEnvelope> samples,
            Func<SampleEnvelope, string> keySelector)
        {
            return samples
                .GroupBy(sample => NormalizeKey(keySelector(sample)))
                .Select(group => new MediaAnalyticsBreakdownItem
                {
                    Key = group.Key,
                    Attempts = group.Count(),
                    Successes = group.Count(item => item.Sample.Succeeded),
                    Failures = group.Count(item => !item.Sample.Succeeded),
                    HardwareAcceleratedRuns = group.Count(item => item.Sample.UsedHardwareAcceleration),
                    AverageProcessingMilliseconds = Average(group.Select(item => item.Sample.ProcessingMilliseconds)),
                    AverageSpeedFactor = Average(group.Select(item => item.Sample.SpeedFactor)),
                    AveragePeakWorkingSetBytes = Average(group.Select(item => (double)item.Sample.PeakWorkingSetBytes)),
                    PeakWorkingSetBytes = Max(group.Select(item => item.Sample.PeakWorkingSetBytes)),
                    AverageCpuUsagePercent = Average(group.Select(item => item.Sample.AverageCpuUsagePercent)),
                    PeakCpuUsagePercent = Max(group.Select(item => item.Sample.PeakCpuUsagePercent)),
                    AverageLogicalCoreCount = Average(group.Select(item => (double)item.Sample.LogicalCoreCount)),
                    AverageAcceleratorUsagePercent = Average(group.Select(item => item.Sample.AverageAcceleratorUsagePercent)),
                    PeakAcceleratorUsagePercent = Max(group.Select(item => item.Sample.PeakAcceleratorUsagePercent)),
                    TotalInputDurationSeconds = group.Sum(item => item.Sample.InputDurationSeconds),
                    TotalInputSizeBytes = group.GroupBy(item => item.Record.AnalysisKey, StringComparer.Ordinal)
                                               .Sum(g => g.First().Record.ProbeSnapshot?.InputSizeBytes ?? 0)
                })
                .OrderByDescending(item => item.Attempts)
                .ThenBy(item => item.Key, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyCollection<MediaAnalyticsBreakdownItem> BuildFailureBreakdown(
            IEnumerable<SampleEnvelope> samples,
            Func<SampleEnvelope, string> keySelector)
        {
            return BuildBreakdown(
                samples.Where(sample => !sample.Sample.Succeeded && !string.IsNullOrWhiteSpace(keySelector(sample))),
                keySelector);
        }

        private static IReadOnlyCollection<MediaAnalyticsTimelinePoint> BuildTimeline(
            IEnumerable<SampleEnvelope> samples,
            MediaAnalyticsTimeBucket bucket)
        {
            return samples
                .GroupBy(sample => BucketStart(sample.Sample.TimestampUtc, bucket))
                .OrderBy(group => group.Key)
                .Select(group => new MediaAnalyticsTimelinePoint
                {
                    BucketStartUtc = group.Key,
                    Attempts = group.Count(),
                    Successes = group.Count(item => item.Sample.Succeeded),
                    Failures = group.Count(item => !item.Sample.Succeeded),
                    AverageProcessingMilliseconds = Average(group.Select(item => item.Sample.ProcessingMilliseconds)),
                    AverageSpeedFactor = Average(group.Select(item => item.Sample.SpeedFactor)),
                    AveragePeakWorkingSetBytes = Average(group.Select(item => (double)item.Sample.PeakWorkingSetBytes)),
                    PeakWorkingSetBytes = Max(group.Select(item => item.Sample.PeakWorkingSetBytes)),
                    AverageCpuUsagePercent = Average(group.Select(item => item.Sample.AverageCpuUsagePercent)),
                    PeakCpuUsagePercent = Max(group.Select(item => item.Sample.PeakCpuUsagePercent)),
                    AverageLogicalCoreCount = Average(group.Select(item => (double)item.Sample.LogicalCoreCount)),
                    AverageAcceleratorUsagePercent = Average(group.Select(item => item.Sample.AverageAcceleratorUsagePercent)),
                    PeakAcceleratorUsagePercent = Max(group.Select(item => item.Sample.PeakAcceleratorUsagePercent))
                })
                .ToArray();
        }

        private static DateTimeOffset BucketStart(DateTimeOffset value, MediaAnalyticsTimeBucket bucket)
        {
            switch (bucket)
            {
                case MediaAnalyticsTimeBucket.Hour:
                    return new DateTimeOffset(value.Year, value.Month, value.Day, value.Hour, 0, 0, TimeSpan.Zero);
                case MediaAnalyticsTimeBucket.Month:
                    return new DateTimeOffset(value.Year, value.Month, 1, 0, 0, 0, TimeSpan.Zero);
                default:
                    return new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, TimeSpan.Zero);
            }
        }

        private static string SizeBucket(long inputSizeBytes)
        {
            if (inputSizeBytes <= 0)
            {
                return "unknown";
            }

            const long mb = 1024 * 1024;
            if (inputSizeBytes < 10 * mb)
            {
                return "<10MB";
            }

            if (inputSizeBytes < 100 * mb)
            {
                return "10-100MB";
            }

            if (inputSizeBytes < 1024L * mb)
            {
                return "100MB-1GB";
            }

            return ">=1GB";
        }

        private static string DurationBucket(double durationSeconds)
        {
            if (durationSeconds <= 0)
            {
                return "unknown";
            }

            if (durationSeconds < 30)
            {
                return "<30s";
            }

            if (durationSeconds < 300)
            {
                return "30s-5m";
            }

            if (durationSeconds < 1800)
            {
                return "5m-30m";
            }

            return ">=30m";
        }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
        }

        private static double Average(IEnumerable<double> values)
        {
            double sum = 0;
            int count = 0;
            foreach (var value in values)
            {
                if (value <= 0)
                {
                    continue;
                }

                sum += value;
                count++;
            }

            return count == 0 ? 0 : sum / count;
        }

        private static long Max(IEnumerable<long> values)
        {
            long max = 0;
            foreach (var value in values)
            {
                if (value > max)
                {
                    max = value;
                }
            }

            return max;
        }

        private static double Max(IEnumerable<double> values)
        {
            double max = 0;
            foreach (var value in values)
            {
                if (value > max)
                {
                    max = value;
                }
            }

            return max;
        }

        private sealed class SampleEnvelope
        {
            public MediaAnalysisRecord Record { get; set; }

            public MediaExecutionSample Sample { get; set; }
        }
    }
}
