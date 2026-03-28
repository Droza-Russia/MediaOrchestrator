using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;
using MediaOrchestrator.Streams.SubtitleStream;

namespace MediaOrchestrator
{
    public partial class Conversion
    {
        /// <summary>
        ///     Add subtitles to video stream
        /// </summary>
        /// <param name="inputPath">Video</param>
        /// <param name="outputPath">Output file</param>
        /// <param name="subtitlesPath">Subtitles</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> AddSubtitlesAsync(string inputPath, string outputPath, string subtitlesPath, CancellationToken cancellationToken = default)
        {
            IMediaInfo info = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken);

            IVideoStream videoStream = RequireVideoStreamForSubtitles(info, nameof(inputPath))
                                           .AddSubtitles(subtitlesPath);

            return New()
                .AddStream(videoStream)
                .AddStream(info.AudioStreams.FirstOrDefault())
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Add subtitle to file. It will be added as new stream so if you want to burn subtitles into video you should use
        ///     SetSubtitles method.
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="subtitlePath">Path to subtitle file in .srt format</param>
        /// <param name="language">Language code in ISO 639. Example: "eng", "pol", "pl", "de", "ger"</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> AddSubtitleAsync(string inputPath, string outputPath, string subtitlePath, string language = null, CancellationToken cancellationToken = default)
        {
            IMediaInfo mediaInfo = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken);
            IMediaInfo subtitleInfo = await MediaOrchestrator.GetMediaInfo(subtitlePath, cancellationToken);

            ISubtitleStream subtitleStream = RequireSubtitleStream(subtitleInfo, nameof(subtitlePath))
                                                         .SetLanguage(language);

            return New()
                .AddStream(mediaInfo.VideoStreams)
                .AddStream(mediaInfo.AudioStreams)
                .AddStream(subtitleStream.SetCodec(SubtitleCodec.copy))
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Add subtitle to file. It will be added as new stream so if you want to burn subtitles into video you should use
        ///     SetSubtitles method.
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="subtitlePath">Path to subtitle file in .srt format</param>
        /// <param name="subtitleCodec">The Subtitle Codec to Use to Encode the Subtitles</param>
        /// <param name="language">Language code in ISO 639. Example: "eng", "pol", "pl", "de", "ger"</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> AddSubtitleAsync(string inputPath, string outputPath, string subtitlePath, SubtitleCodec subtitleCodec, string language = null, CancellationToken cancellationToken = default)
        {
            IMediaInfo mediaInfo = await MediaOrchestrator.GetMediaInfo(inputPath, cancellationToken);
            IMediaInfo subtitleInfo = await MediaOrchestrator.GetMediaInfo(subtitlePath, cancellationToken);

            ISubtitleStream subtitleStream = RequireSubtitleStream(subtitleInfo, nameof(subtitlePath))
                                                         .SetLanguage(language);

            return New()
                .AddStream(mediaInfo.VideoStreams)
                .AddStream(mediaInfo.AudioStreams)
                .AddStream(subtitleStream.SetCodec(subtitleCodec))
                .SetOutput(outputPath);
        }

        private static IVideoStream RequireVideoStreamForSubtitles(IMediaInfo info, string paramName)
        {
            var videoStream = info.VideoStreams.FirstOrDefault();
            if (videoStream == null)
            {
                throw new VideoStreamNotFoundException(ErrorMessages.InputFileDoesNotContainVideoStream, paramName);
            }

            return videoStream;
        }

        private static ISubtitleStream RequireSubtitleStream(IMediaInfo info, string paramName)
        {
            var subtitleStream = info.SubtitleStreams.FirstOrDefault();
            if (subtitleStream == null)
            {
                throw new SubtitleStreamNotFoundException(ErrorMessages.InputFileDoesNotContainSubtitleStream, paramName);
            }

            return subtitleStream;
        }
    }
}
