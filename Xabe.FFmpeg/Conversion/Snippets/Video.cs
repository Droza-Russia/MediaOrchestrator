using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg.Streams.SubtitleStream;

namespace Xabe.FFmpeg
{
    /// <inheritdoc />
    public partial class Conversion
    {
        /// <summary>
        ///     Melt watermark into video
        /// </summary>
        /// <param name="inputPath">Input video path</param>
        /// <param name="outputPath">Output file</param>
        /// <param name="inputImage">Watermark</param>
        /// <param name="position">Position of watermark</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> SetWatermarkAsync(string inputPath, string outputPath, string inputImage, Position position)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);

            IVideoStream videoStream = info.VideoStreams.FirstOrDefault()
                                           .SetWatermark(inputImage, position);

            return New()
                .AddStream(videoStream)
                .AddStream(info.AudioStreams.ToArray())
                .SetOutput(outputPath);
        }

        internal static async Task<IConversion> BurnRightSideTextLabelAsync(
            string inputPath,
            string outputPath,
            string text,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);
            IVideoStream videoStream = info.VideoStreams.FirstOrDefault()
                                       ?.SetRightSideDrawText(text, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath);
            if (videoStream == null)
            {
                throw new ArgumentException(ErrorMessages.InputFileDoesNotContainVideoStream, nameof(inputPath));
            }

            return New()
                .AddStream(videoStream)
                .AddStream(info.AudioStreams.ToArray())
                .SetOutput(outputPath);
        }

        internal static async Task<IConversion> BurnRightSidePtsTimeLabelAsync(
            string inputPath,
            string outputPath,
            string prefix = null,
            string suffix = null,
            bool useLocalWallClock = false,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);
            IVideoStream videoStream = info.VideoStreams.FirstOrDefault()
                                       ?.SetRightSidePtsTimeOverlay(prefix, suffix, useLocalWallClock, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath);
            if (videoStream == null)
            {
                throw new ArgumentException(ErrorMessages.InputFileDoesNotContainVideoStream, nameof(inputPath));
            }

            return New()
                .AddStream(videoStream)
                .AddStream(info.AudioStreams.ToArray())
                .SetOutput(outputPath);
        }

        internal static async Task<IConversion> BurnRightSideSmpteTimecodeAsync(
            string inputPath,
            string outputPath,
            string startTimecode = "00:00:00:00",
            double frameRate = 25,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);
            IVideoStream videoStream = info.VideoStreams.FirstOrDefault()
                                       ?.SetRightSideSmpteTimecodeOverlay(startTimecode, frameRate, fontColor, fontSize, marginRight, marginY, verticalAlign, fontFilePath);
            if (videoStream == null)
            {
                throw new ArgumentException(ErrorMessages.InputFileDoesNotContainVideoStream, nameof(inputPath));
            }

            return New()
                .AddStream(videoStream)
                .AddStream(info.AudioStreams.ToArray())
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Extract video from file
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Output audio stream</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ExtractVideoAsync(string inputPath, string outputPath)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);

            IVideoStream videoStream = info.VideoStreams.FirstOrDefault();

            return New()
                .AddStream(videoStream)
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Saves snapshot of video
        /// </summary>
        /// <param name="inputPath">Video</param>
        /// <param name="outputPath">Output file</param>
        /// <param name="captureTime">TimeSpan of snapshot</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> SnapshotAsync(string inputPath, string outputPath, TimeSpan captureTime)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);

            IVideoStream videoStream = info.VideoStreams.FirstOrDefault()
                                           .SetOutputFramesCount(1)
                                           .SetSeek(captureTime);

            return New()
                .AddStream(videoStream)
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Change video size
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="width">Expected width</param>
        /// <param name="height">Expected height</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ChangeSizeAsync(string inputPath, string outputPath, int width, int height)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);

            IVideoStream videoStream = info.VideoStreams.FirstOrDefault()
                                           .SetSize(width, height);
            return New()
                .AddStream(videoStream)
                .AddStream(info.AudioStreams.ToArray())
                .AddStream(info.SubtitleStreams.ToArray())
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Change video size
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="size">Expected size</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ChangeSizeAsync(string inputPath, string outputPath, VideoSize size)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);

            IVideoStream videoStream = info.VideoStreams.FirstOrDefault()
                                           .SetSize(size);
            return New()
                .AddStream(videoStream)
                .AddStream(info.AudioStreams.ToArray())
                .AddStream(info.SubtitleStreams.ToArray())
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Get part of video
        /// </summary>
        /// <param name="inputPath">Video</param>
        /// <param name="outputPath">Output file</param>
        /// <param name="startTime">Start point</param>
        /// <param name="duration">Duration of new video</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> SplitAsync(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);

            var streams = new List<IStream>();
            foreach (IVideoStream stream in info.VideoStreams)
            {
                streams.Add(stream.Split(startTime, duration));
            }

            foreach (IAudioStream stream in info.AudioStreams)
            {
                streams.Add(stream.Split(startTime, duration));
            }

            return New()
                .AddStream(streams)
                .SetOutput(outputPath);
        }

        /// <summary>
        /// Save M3U8 stream
        /// </summary>
        /// <param name="uri">Uri to stream</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="duration">Duration of stream</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> SaveM3U8StreamAsync(Uri uri, string outputPath, TimeSpan? duration = null)
        {
            var mediaInfo = await FFmpeg.GetMediaInfo(uri.ToString());
            return New()
                .AddStream(mediaInfo.Streams)
                .SetInputTime(duration)
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Concat multiple inputVideos.
        /// </summary>
        /// <param name="output">Concatenated inputVideos</param>
        /// <param name="inputVideos">Videos to add</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> Concatenate(string output, params string[] inputVideos)
        {
            if (inputVideos.Length <= 1)
            {
                throw new ArgumentException(ErrorMessages.ConcatAtLeastTwoFiles, "inputVideos");
            }

            var mediaInfos = new List<IMediaInfo>();

            IConversion conversion = New();
            foreach (var inputVideo in inputVideos)
            {
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputVideo);

                mediaInfos.Add(mediaInfo);
                conversion.AddParameter($"-i {inputVideo.Escape()} ");
            }

            conversion.AddParameter($"-t 1 -f lavfi -i anullsrc=r=48000:cl=stereo");
            conversion.AddParameter($"-filter_complex \"");

            IVideoStream maxResolutionMedia = mediaInfos.Select(x => x.VideoStreams.OrderByDescending(z => z.Width)
                                                                      .First())
                                                        .OrderByDescending(x => x.Width)
                                                        .First();
            for (var i = 0; i < mediaInfos.Count; i++)
            {
                conversion.AddParameter(
                    $"[{i}:v]scale={maxResolutionMedia.Width}:{maxResolutionMedia.Height},setdar={maxResolutionMedia.Ratio},setpts=PTS-STARTPTS[v{i}]; ");
            }

            for (var i = 0; i < mediaInfos.Count; i++)
            {
                conversion.AddParameter(!mediaInfos[i].AudioStreams.Any() ? $"[v{i}]" : $"[v{i}][{i}:a]");
            }

            conversion.AddParameter($"concat=n={inputVideos.Length}:v=1:a=1 [v] [a]\" -map \"[v]\" -map \"[a]\"");
            conversion.AddParameter($"-aspect {maxResolutionMedia.Ratio}");
            return conversion.SetOutput(output);
        }

        /// <summary>
        ///     Convert one file to another with destination format.
        /// </summary>
        /// <param name="inputFilePath">Path to file</param>
        /// <param name="outputFilePath">Path to file</param>
        /// <param name="keepSubtitles">Whether to Keep Subtitles in the output video</param>
        /// <returns>IConversion object</returns>
        internal static async Task<IConversion> ConvertAsync(string inputFilePath, string outputFilePath, bool keepSubtitles = false)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputFilePath);

            var conversion = New(suppressGlobalOutputLimits: !info.VideoStreams.Any()).SetOutput(outputFilePath);

            foreach (var stream in info.Streams)
            {
                if (stream is IVideoStream videoStream)
                {
                    // PR #268 We have to force the framerate here due to an FFmpeg bug with videos > 100fps from android devices
                    conversion.AddStream(videoStream.SetFramerate(videoStream.Framerate));
                }
                else if (stream is IAudioStream audioStream)
                {
                    conversion.AddStream(audioStream);
                }
                else if (stream is ISubtitleStream subtitleStream && keepSubtitles)
                {
                    conversion.AddStream(subtitleStream.SetCodec(SubtitleCodec.mov_text));
                }
            }

            return conversion;
        }

        /// <summary>
        ///     Transcode one file to another with destination format and codecs.
        /// </summary>
        /// <param name="inputFilePath">Path to file</param>
        /// <param name="outputFilePath">Path to file</param>
        /// <param name="audioCodec"> The Audio Codec to Transcode the input to</param>
        /// <param name="videoCodec"> The Video Codec to Transcode the input to</param>
        /// <param name="videoCodec"> The Subtitle Codec to Transcode the input to</param>
        /// <param name="keepSubtitles">Whether to Keep Subtitles in the output video</param>
        /// <returns>IConversion object</returns>
        internal static async Task<IConversion> TranscodeAsync(string inputFilePath, string outputFilePath, VideoCodec videoCodec, AudioCodec audioCodec, SubtitleCodec subtitleCodec, bool keepSubtitles = false)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputFilePath);

            var conversion = New(suppressGlobalOutputLimits: !info.VideoStreams.Any()).SetOutput(outputFilePath);

            foreach (var stream in info.Streams)
            {
                if (stream is IVideoStream videoStream)
                {
                    // PR #268 We have to force the framerate here due to an FFmpeg bug with videos > 100fps from android devices
                    conversion.AddStream(videoStream.SetCodec(FFmpeg.ResolveTranscodeVideoCodecToString(videoCodec)).SetFramerate(videoStream.Framerate));
                }
                else if (stream is IAudioStream audioStream)
                {
                    conversion.AddStream(audioStream.SetCodec(FFmpeg.ResolveTranscodeAudioCodecToString(audioCodec)));
                }
                else if (stream is ISubtitleStream subtitleStream && keepSubtitles)
                {
                    conversion.AddStream(subtitleStream.SetCodec(subtitleCodec));
                }
            }

            return conversion;
        }
    }
}
