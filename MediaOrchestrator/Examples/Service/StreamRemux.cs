using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator;
using MediaOrchestrator.Events;

namespace MediaOrchestrator.Examples.Service
{
    public static class StreamRemuxExample
    {
        public static async Task RunAsync()
        {
            await EnsureBinariesAsync().ConfigureAwait(false);
            var snippets = MediaOrchestrator.Conversions.FromSnippet;

            var liveSource = new Uri("https://example.com/live/playlist.m3u8");
            var outputPath = "stream_copy.webm";
            var audioOnlyPath = "stream_audio.aac";

            var conversion = await snippets.RemuxStream(liveSource, outputPath, keepSubtitles: false, outputFormat: Format.webm).ConfigureAwait(false);
            conversion.SetProgressReporter(new Progress<ConversionProgressEventArgs>(info =>
            {
                Console.WriteLine($"[Copy] {info.Percent}% ({info.Duration}/{info.TotalLength})");
            }));
            await conversion.Start().ConfigureAwait(false);

            var mediaInfo = await MediaOrchestrator.GetMediaInfo(liveSource.ToString(), CancellationToken.None).ConfigureAwait(false);
            var audioStreams = mediaInfo.AudioStreams.ToArray();
            if (!audioStreams.Any())
            {
                Console.WriteLine("Audio stream not found");
                return;
            }

            var audioOnly = MediaOrchestrator.Conversions.New()
                .AddStream(audioStreams.Select(a => a.SetCodec(AudioCodec.copy)).ToArray())
                .SetOutput(audioOnlyPath);
            await audioOnly.Start().ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }

        private static Task EnsureBinariesAsync()
        {
            if (string.IsNullOrWhiteSpace(MediaOrchestrator.ExecutablesPath))
            {
                MediaOrchestrator.SetExecutablesPath("/usr/local/bin", tryDetectHardwareAcceleration: false);
            }

            return Task.CompletedTask;
        }
    }
}
