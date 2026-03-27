using System;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace Xabe.FFmpeg.Examples.Service
{
    public static class OverlayTimecodeExample
    {
        public static async Task RunAsync()
        {
            await EnsureBinariesAsync().ConfigureAwait(false);
            var snippets = FFmpeg.Conversions.FromSnippet;

            var inputPath = "input.mp4";
            var outputPath = "input_timestamped.mp4";

            var info = await FFmpeg.GetMediaInfo(inputPath, default).ConfigureAwait(false);
            var videoStream = info.VideoStreams.FirstOrDefault();
            if (videoStream == null)
            {
                Console.WriteLine("Видео-поток не найден");
                return;
            }

            var annotated = videoStream
                .SetRightSidePtsTimeOverlay(prefix: "PTS: ", suffix: " UTC", fontSize: 28)
                .SetRightSideDrawText("Service ID: 42", fontSize: 18, marginY: 60);

            var conversion = FFmpeg.Conversions.New()
                .AddStream(annotated)
                .AddStream(info.AudioStreams.ToArray())
                .SetOutput(outputPath);

            await conversion.Start().ConfigureAwait(false);
        }

        private static Task EnsureBinariesAsync()
        {
            if (string.IsNullOrWhiteSpace(FFmpeg.ExecutablesPath))
            {
                FFmpeg.SetExecutablesPath("/usr/local/bin", tryDetectHardwareAcceleration: false);
            }

            return Task.CompletedTask;
        }
    }
}
