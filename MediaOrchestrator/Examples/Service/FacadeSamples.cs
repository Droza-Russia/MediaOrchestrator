using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator;
using MediaOrchestrator.Streams.SubtitleStream;

namespace MediaOrchestrator.Examples.Service
{
    public static class FacadeSamplesExample
    {
        public static async Task RunAsync(CancellationToken cancellationToken = default)
        {
            await EnsureBinariesAsync().ConfigureAwait(false);
            var snippets = MediaOrchestrator.Conversions.FromSnippet;

            var inputVideo = "service/input.mp4";
            var extraAudioTrack = "service/background.aac";
            var subtitleFile = "service/captions.srt";
            var watermarkImage = "service/logo.png";
            var hlsUri = new Uri("https://example.com/live/playlist.m3u8");
            var rtspUri = new Uri("rtsp://127.0.0.1/live/stream");

            var metadata = await MediaOrchestrator.GetMediaInfo(inputVideo, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Input duration: {metadata.Duration}");

            var devices = await MediaOrchestrator.GetAvailableDevices(cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Devices: {string.Join(", ", devices.Select(d => d.Name))}");
            MediaOrchestrator.ClearMediaInfoCache();

            var manualConversion = MediaOrchestrator.Conversions.New()
                .AddStream(metadata.Streams.ToArray())
                .SetOutput("service/manual.webm");

            var extract = await snippets.ExtractAudio(inputVideo, "service/extracted.mp3", cancellationToken).ConfigureAwait(false);
            var extractWithCodec = await snippets.ExtractAudio(
                    inputVideo,
                    "service/extracted.m4a",
                    AudioCodec.aac,
                    bitrate: 128000,
                    sampleRate: 44100,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var toWav = await MediaOrchestrator.Conversions.FromSnippet.ConvertToWav(inputVideo, "service/audio.wav", 16000, 1, cancellationToken).ConfigureAwait(false);

            var audioAdded = await snippets.AddAudio(inputVideo, extraAudioTrack, "service/with_audio.mp4", cancellationToken).ConfigureAwait(false);
            var splitAudio = await snippets.SplitAudioByTimecodes(
                    extraAudioTrack,
                    "service/audio_parts",
                    new[] { TimeSpan.Zero, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30) },
                    AudioCodec.mp3,
                    192000,
                    44100,
                    cancellationToken)
                .ConfigureAwait(false);
            Console.WriteLine($"Split into {splitAudio.Count} audio conversions");

            var toMp4 = await snippets.ToMp4(inputVideo, "service/output.mp4", cancellationToken).ConfigureAwait(false);
            var toTs = await snippets.ToTs(inputVideo, "service/output.ts", cancellationToken).ConfigureAwait(false);
            var toOgv = await snippets.ToOgv(inputVideo, "service/output.ogv", cancellationToken).ConfigureAwait(false);
            var toWebM = await snippets.ToWebM(inputVideo, "service/output.webm", cancellationToken).ConfigureAwait(false);
            var remux = await snippets.RemuxToWebM(inputVideo, "service/remuxed.webm", keepSubtitles: true, cancellationToken).ConfigureAwait(false);
            var toGif = await snippets.ToGif(inputVideo, "service/animated.gif", loop: 0, delay: 100, cancellationToken).ConfigureAwait(false);

            var hardwareConversion = await snippets.ConvertWithHardware(
                    inputVideo,
                    "service/hw_encode.mp4",
                    HardwareAccelerator.auto,
                    VideoCodec.h264_cuvid,
                    VideoCodec.h264_nvenc,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var burnSubtitle = await snippets.BurnSubtitle(inputVideo, "service/burned_subs.mp4", subtitleFile, cancellationToken).ConfigureAwait(false);
            var addSubtitle = await snippets.AddSubtitle(inputVideo, "service/with_subtitles.mp4", subtitleFile, language: "eng", cancellationToken).ConfigureAwait(false);
            var addSubtitleCodec = await snippets.AddSubtitle(
                    inputVideo,
                    "service/with_mov_text.mp4",
                    subtitleFile,
                    SubtitleCodec.mov_text,
                    language: "eng",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var watermark = await snippets.SetWatermark(inputVideo, "service/watermarked.mp4", watermarkImage, Position.BottomRight, cancellationToken).ConfigureAwait(false);
            var textLabel = await snippets.BurnRightSideTextLabel(inputVideo, "service/text_label.mp4", "LIVE", fontSize: 20, marginRight: 40, cancellationToken: cancellationToken).ConfigureAwait(false);
            var ptsLabel = await snippets.BurnRightSidePtsTimeLabel(
                    inputVideo,
                    "service/pts_label.mp4",
                    prefix: "PTS=",
                    suffix: " UTC",
                    fontSize: 22,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var smpteLabel = await snippets.BurnRightSideSmpteTimecode(
                    inputVideo,
                    "service/smpte_label.mp4",
                    frameRate: 30,
                    fontSize: 22,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var extractedVideo = await snippets.ExtractVideo(inputVideo, "service/video_only.mp4", cancellationToken).ConfigureAwait(false);
            var snapshot = await snippets.Snapshot(inputVideo, "service/snapshot.png", TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            var sizedManual = await snippets.ChangeSize(inputVideo, "service/sized_manual.mp4", 640, 360, cancellationToken).ConfigureAwait(false);
            var sizedPreset = await snippets.ChangeSize(inputVideo, "service/sized_preset.mp4", VideoSize.Hd480, cancellationToken).ConfigureAwait(false);
            var clipped = await snippets.Split(inputVideo, "service/clipped.mp4", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), cancellationToken).ConfigureAwait(false);

            var savePlaylist = await snippets.SaveM3U8Stream(hlsUri, "service/playlist.ts", duration: TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            var savePlaylistAudio = await snippets.SaveAudioStream(hlsUri, "service/playlist_audio.aac", outputFormat: Format.aac, cancellationToken).ConfigureAwait(false);
            var remuxString = await snippets.RemuxStream(hlsUri.ToString(), "service/remote.mp4", keepSubtitles: false, outputFormat: Format.mp4, cancellationToken).ConfigureAwait(false);
            var remuxUri = await snippets.RemuxStream(new Uri("https://media.example.com/live"), "service/remote.webm", keepSubtitles: false, outputFormat: Format.webm, cancellationToken).ConfigureAwait(false);

            await MediaOrchestrator.DownloadHostedVideoAsync(
                    "https://youtu.be/dQw4w9WgXcQ",
                    "service/hosted_download.mp4",
                    new HostedVideoDownloadSettings
                    {
                        MergeOutputFormat = "mp4",
                        AdditionalArguments = new[] { "--no-mtime" }
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            using (var stdinFile = File.OpenRead("service/stdin_sample.mkv"))
            {
                var stdinCopy = snippets.StreamFromStdin(stdinFile, "service/stdin_copy.mkv", outputFormat: Format.matroska);
            }

            using (var stdinAudioStream = File.OpenRead("service/stdin_sample.aac"))
            {
                var stdinAudio = snippets.StreamAudioFromStdin(stdinAudioStream, "service/stdin_audio.wav", outputFormat: Format.wav);
            }

            var concatenated = await snippets.Concatenate("service/combined.mp4", "service/part1.mp4", "service/part2.mp4").ConfigureAwait(false);
            var concatenatedWithToken = await snippets.Concatenate("service/combined_with_cts.mp4", cancellationToken, "service/part1.mp4", "service/part2.mp4").ConfigureAwait(false);

            var simpleConvert = await snippets.Convert(inputVideo, "service/simple_convert.mp4", keepSubtitles: true, cancellationToken).ConfigureAwait(false);
            var transcode = await snippets.Transcode(inputVideo, "service/transcoded.mkv", VideoCodec.h264, AudioCodec.aac, SubtitleCodec.mov_text, keepSubtitles: true, cancellationToken).ConfigureAwait(false);
            var transcodeDefaults = await snippets.Transcode(inputVideo, "service/transcoded_default.mp4", keepSubtitles: false, cancellationToken).ConfigureAwait(false);

            var visualise = await snippets.VisualiseAudio(
                    extraAudioTrack,
                    "service/audio_viz.mp4",
                    VideoSize.Hd720,
                    pixelFormat: PixelFormat.yuv420p,
                    mode: VisualisationMode.bar,
                    amplitudeScale: AmplitudeScale.lin,
                    frequencyScale: FrequencyScale.log,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var rtspPush = await snippets.SendToRtspServer(inputVideo, rtspUri, cancellationToken).ConfigureAwait(false);
            var desktopPush = await snippets.SendDesktopToRtspServer(new Uri("rtsp://127.0.0.1:8554/desktop"), cancellationToken).ConfigureAwait(false);

            Console.WriteLine("Facade демонстрация построена. Запускайте нужные конверсии по условиям сервиса.");
        }

        private static Task EnsureBinariesAsync()
        {
            if (string.IsNullOrWhiteSpace(MediaOrchestrator.ExecutablesPath))
            {
                MediaOrchestrator.SetExecutablesPath("/usr/local/bin", tryDetectHardwareAcceleration: false);
            }

            MediaOrchestrator.SetLocalizationLanguage(LocalizationLanguage.English);
            return Task.CompletedTask;
        }
    }
}
