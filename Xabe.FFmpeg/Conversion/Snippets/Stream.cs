using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg.Exceptions;
using Xabe.FFmpeg.Streams.SubtitleStream;

namespace Xabe.FFmpeg
{
    public partial class Conversion
    {
        internal static async Task<IConversion> RemuxStreamAsync(string inputPath, string outputPath, Format? outputFormat = null, bool keepSubtitles = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException(ErrorMessages.InputPathMustBeProvided, nameof(inputPath));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(ErrorMessages.OutputPathMustBeProvided, nameof(outputPath));
            }

            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath, cancellationToken);
            var conversion = New().SetOutput(outputPath);

            if (outputFormat.HasValue)
            {
                conversion.SetOutputFormat(outputFormat.Value);
            }

            if (info.VideoStreams.Any())
            {
                conversion.AddStream(info.VideoStreams.Select(videoStream => videoStream.SetCodec(VideoCodec.copy)).ToArray());
            }

            if (info.AudioStreams.Any())
            {
                conversion.AddStream(info.AudioStreams.Select(audioStream => audioStream.SetCodec(AudioCodec.copy)).ToArray());
            }

            if (keepSubtitles && info.SubtitleStreams.Any())
            {
                conversion.AddStream(info.SubtitleStreams.Select(subtitleStream => subtitleStream.SetCodec(SubtitleCodec.copy)).ToArray());
            }

            return conversion;
        }

        internal static IConversion StreamFromStdin(Stream inputStream, string outputPath, Format? outputFormat = null)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(ErrorMessages.OutputPathMustBeProvided, nameof(outputPath));
            }

            var conversion = New()
                .PipeInput(inputStream)
                .MapAllStreams()
                .CopyAllCodecs()
                .SetOutput(outputPath);

            if (outputFormat.HasValue)
            {
                conversion.SetOutputFormat(outputFormat.Value);
            }

            return conversion;
        }

        internal static IConversion StreamAudioFromStdin(Stream inputStream, string outputPath, Format? outputFormat = null)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(ErrorMessages.OutputPathMustBeProvided, nameof(outputPath));
            }

            var conversion = New()
                .PipeInput(inputStream)
                .MapAudioStreams()
                .DisableVideo()
                .SetOutput(outputPath);

            if (outputFormat.HasValue)
            {
                conversion.SetOutputFormat(outputFormat.Value);
                ApplyPreferredAudioCodec(conversion, outputFormat.Value);
            }
            else
            {
                conversion.CopyAudioCodec();
            }

            return conversion;
        }

        internal static async Task<IConversion> SaveAudioStreamAsync(string inputPath, string outputPath, Format? outputFormat = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                throw new ArgumentException(ErrorMessages.InputPathMustBeProvided, nameof(inputPath));
            }

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new ArgumentException(ErrorMessages.OutputPathMustBeProvided, nameof(outputPath));
            }

            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath, cancellationToken);
            var audioStreams = info.AudioStreams.ToArray();
            if (!audioStreams.Any())
            {
                throw new AudioStreamNotFoundException(ErrorMessages.InputFileDoesNotContainAudioStream, nameof(inputPath));
            }

            var conversion = New().SetOutput(outputPath);
            if (outputFormat.HasValue)
            {
                conversion.SetOutputFormat(outputFormat.Value);
                conversion.DisableVideo();
                ApplyPreferredAudioCodec(conversion, outputFormat.Value, audioStreams);
            }
            else
            {
                conversion.AddStream(audioStreams.Select(audioStream => audioStream.SetCodec(AudioCodec.copy)).ToArray());
            }

            return conversion;
        }

        internal static void ApplyPreferredAudioCodec(IConversion conversion, Format outputFormat, params IAudioStream[] audioStreams)
        {
            var preferredCodec = GetPreferredAudioCodec(outputFormat);
            if (!preferredCodec.HasValue)
            {
                conversion.CopyAudioCodec();
                return;
            }

            if (audioStreams != null && audioStreams.Length > 0)
            {
                foreach (var audioStream in audioStreams.Where(stream => stream != null))
                {
                    audioStream.SetCodec(preferredCodec.Value);
                }

                conversion.AddStream(audioStreams.Where(stream => stream != null).ToArray());
                return;
            }

            conversion.SetAudioCodec(preferredCodec.Value);
        }

        internal static AudioCodec? GetPreferredAudioCodec(Format outputFormat)
        {
            switch (outputFormat)
            {
                case Format.wav:
                case Format.aiff:
                case Format.au:
                    return AudioCodec.pcm_s16le;
                case Format.mp3:
                    return AudioCodec.mp3;
                case Format.aac:
                case Format.adts:
                case Format.mp4:
                case Format.mov:
                case Format.ipod:
                    return AudioCodec.aac;
                case Format.ac3:
                case Format.eac3:
                    return AudioCodec.ac3;
                case Format.flac:
                    return AudioCodec.flac;
                case Format.oga:
                case Format.ogg:
                case Format.spx:
                    return AudioCodec.libvorbis;
                case Format.opus:
                case Format.webm:
                    return AudioCodec.libopus;
                default:
                    return null;
            }
        }
    }
}
