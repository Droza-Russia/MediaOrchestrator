using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator;
using MediaOrchestrator.Events;

namespace MediaOrchestrator.Examples.Service
{
    public static class StreamCaptureExample
    {
        public static async Task RunAsync(CancellationToken cancellationToken = default)
        {
            await EnsureBinariesAsync().ConfigureAwait(false);
            var snippets = MediaOrchestrator.Conversions.FromSnippet;

            var hlsUri = new Uri("https://example.com/live/playlist.m3u8");
            var rtspUri = new Uri("rtsp://127.0.0.1/live/stream");
            var httpAudioUri = new Uri("https://example.com/audio_only.m3u8");

            MediaOrchestrator.SetGlobalOutputLimits(maxOutputVideoFrameRate: 30, maxOutputAudioSampleRate: 48000, maxOutputAudioChannels: 2);

            var remuxConversion = await snippets.RemuxStream(
                    hlsUri,
                    "service/live_copy.webm",
                    keepSubtitles: false,
                    outputFormat: Format.webm)
                .ConfigureAwait(false);

            remuxConversion.SetProgressReporter(new Progress<ConversionProgressEventArgs>(progress =>
            {
                Console.WriteLine($"[HLS Remux] {progress.Percent}% [{progress.Duration}/{progress.TotalLength}]");
            }));

            await remuxConversion.Start(cancellationToken).ConfigureAwait(false);

            var audioOnly = await snippets.SaveAudioStream(
                    rtspUri,
                    "service/live_audio.aac",
                    outputFormat: Format.aac)
                .ConfigureAwait(false);

            await audioOnly.Start(cancellationToken).ConfigureAwait(false);

            var httpAudioOnly = await snippets.SaveAudioStream(
                    httpAudioUri.ToString(),
                    "service/http_audio.mp3",
                    outputFormat: Format.mp3)
                .ConfigureAwait(false);

            await httpAudioOnly.Start(cancellationToken).ConfigureAwait(false);

            using (var incomingStream = File.OpenRead("service/queued_audio.aac"))
            {
                var stdinAudio = snippets.StreamAudioFromStdin(incomingStream, "service/stdin_copy.wav", outputFormat: Format.wav);
                await stdinAudio.Start(cancellationToken).ConfigureAwait(false);
            }

            var limitedCapture = await snippets.SaveM3U8Stream(hlsUri, "service/short_hls.ts", duration: TimeSpan.FromSeconds(12)).ConfigureAwait(false);
            await limitedCapture.Start(cancellationToken).ConfigureAwait(false);
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
