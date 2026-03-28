using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;

namespace MediaOrchestrator
{
    public partial class Conversion
    {
        /// <summary>
        ///     Convert file to MP4
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Destination file</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ToMp4(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            IMediaInfo info = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken);
            return ToMp4(info, outputPath);
        }

        internal static IConversion ToMp4(IMediaInfo info, string outputPath)
        {
            IStream videoStream = info.VideoStreams.FirstOrDefault()
                                      ?.SetCodec(MediaOrchestrator.ResolveTranscodeVideoCodecToString(VideoCodec.h264));
            IStream audioStream = info.AudioStreams.FirstOrDefault()
                                      ?.SetCodec(MediaOrchestrator.ResolveTranscodeAudioCodecToString(AudioCodec.aac));

            return New()
                .AddStream(videoStream, audioStream)
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Convert file to TS
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Destination file</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ToTs(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            IMediaInfo info = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken);

            IStream videoStream = info.VideoStreams.FirstOrDefault()
                                      ?.SetCodec(VideoCodec.mpeg2video);
            IStream audioStream = info.AudioStreams.FirstOrDefault()
                                      ?.SetCodec(AudioCodec.mp2);

            return New()
                .AddStream(videoStream, audioStream)
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Convert file to OGV
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Destination file</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ToOgv(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            IMediaInfo info = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken);

            IStream videoStream = info.VideoStreams.FirstOrDefault()
                                      ?.SetCodec(MediaOrchestrator.ResolveTranscodeVideoCodecToString(VideoCodec.theora));
            IStream audioStream = info.AudioStreams.FirstOrDefault()
                                      ?.SetCodec(MediaOrchestrator.ResolveTranscodeAudioCodecToString(AudioCodec.libvorbis));

            return New()
                .AddStream(videoStream, audioStream)
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Convert file to WebM
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Destination file</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ToWebM(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            IMediaInfo info = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken);

            IStream videoStream = info.VideoStreams.FirstOrDefault()
                                      ?.SetCodec(MediaOrchestrator.ResolveTranscodeVideoCodecToString(VideoCodec.vp8));
            IStream audioStream = info.AudioStreams.FirstOrDefault()
                                      ?.SetCodec(MediaOrchestrator.ResolveTranscodeAudioCodecToString(AudioCodec.libvorbis));

            return New()
                .AddStream(videoStream, audioStream)
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Remux file to WebM without re-encoding streams.
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Destination file</param>
        /// <param name="keepSubtitles">Whether to keep subtitle streams</param>
        /// <returns>Conversion result</returns>
        internal static Task<IConversion> RemuxToWebM(string inputPath, string outputPath, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MediaFileSignatureValidator.ValidateOrThrow(inputPath);
            var conversion = New()
                .AddInput(inputPath)
                .MapAllStreams()
                .CopyAllCodecs()
                .SetOutputFormat(Format.webm)
                .SetOutput(outputPath);

            if (!keepSubtitles)
            {
                conversion.DisableSubtitles();
            }

            return Task.FromResult(conversion);
        }

        /// <summary>
        ///     Convert image video stream to gif
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="loop">Number of repeats</param>
        /// <param name="delay">Delay between repeats (in seconds)</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ToGif(string inputPath, string outputPath, int loop, int delay = 0, CancellationToken cancellationToken = default)
        {
            IMediaInfo info = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken);

            IVideoStream videoStream = info.VideoStreams.FirstOrDefault();
            if (videoStream == null)
            {
                throw new VideoStreamNotFoundException(ErrorMessages.InputFileDoesNotContainVideoStream, nameof(inputPath));
            }

            videoStream = videoStream.SetLoop(loop, delay);

            return New()
                .AddStream(videoStream)
                .SetOutput(outputPath);
        }
    }
}
