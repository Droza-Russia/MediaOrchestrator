using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Xabe.FFmpeg
{
    public partial class Conversion
    {
        /// <summary>
        ///     Extract audio from file
        /// </summary>
        /// <param name="inputPath">Input path</param>
        /// <param name="outputPath">Output video stream</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> ExtractAudio(string inputPath, string outputPath)
        {
            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);

            IAudioStream audioStream = info.AudioStreams.FirstOrDefault();

            return New()
                .AddStream(audioStream)
                .SetAudioBitrate(audioStream.Bitrate)
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Add audio stream to video file
        /// </summary>
        /// <param name="videoPath">Video</param>
        /// <param name="audioPath">Audio</param>
        /// <param name="outputPath">Output file</param>
        /// <returns>Conversion result</returns>
        internal static async Task<IConversion> AddAudio(string videoPath, string audioPath, string outputPath)
        {
            IMediaInfo videoInfo = await FFmpeg.GetMediaInfo(videoPath);

            IMediaInfo audioInfo = await FFmpeg.GetMediaInfo(audioPath);

            return New()
                .AddStream(videoInfo.VideoStreams.FirstOrDefault())
                .AddStream(audioInfo.AudioStreams.FirstOrDefault())
                .AddStream(videoInfo.SubtitleStreams.ToArray())
                .SetOutput(outputPath);
        }

        /// <summary>
        /// Generates a visualisation of an audio stream using the 'showfreqs' filter
        /// </summary>
        /// <param name="inputPath">Path to the input file containing the audio stream to visualise</param>
        /// <param name="outputPath">Path to output the visualised audio stream to</param>
        /// <param name="size">The Size of the outputted video stream</param>
        /// <param name="pixelFormat">The output pixel format (default is yuv420p)</param>
        /// <param name="mode">The visualisation mode (default is bar)</param>
        /// <param name="amplitudeScale">The frequency scale (default is lin)</param>
        /// <param name="frequencyScale">The amplitude scale (default is log)</param>
        /// <returns>IConversion object</returns>
        internal static async Task<IConversion> VisualiseAudio(string inputPath, string outputPath, VideoSize size,
            PixelFormat pixelFormat = PixelFormat.yuv420p,
            VisualisationMode mode = VisualisationMode.bar,
            AmplitudeScale amplitudeScale = AmplitudeScale.lin,
            FrequencyScale frequencyScale = FrequencyScale.log)
        {
            IMediaInfo inputInfo = await FFmpeg.GetMediaInfo(inputPath);
            IAudioStream audioStream = inputInfo.AudioStreams.FirstOrDefault();
            IVideoStream videoStream = inputInfo.VideoStreams.FirstOrDefault();

            var filter = $"\"[0:a]showfreqs=mode={mode}:fscale={frequencyScale}:ascale={amplitudeScale},format={pixelFormat},scale={size.ToFFmpegFormat()} [v]\"";

            return New()
                .AddStream(audioStream)
                .AddParameter($"-filter_complex {filter}")
                .AddParameter("-map [v]")
                .SetFrameRate(videoStream != null ? videoStream.Framerate : 30) // Pin framerate at the original rate or 30 fps to stop dropped or duplicated frames
                .SetOutput(outputPath);
        }

        /// <summary>
        ///     Split media file to audio parts using specified timecodes and convert each part.
        /// </summary>
        /// <param name="inputPath">Input media file path.</param>
        /// <param name="outputDirectory">Output directory for generated audio parts.</param>
        /// <param name="timecodes">Split points (timecodes). Method creates parts between neighbour points.</param>
        /// <param name="audioCodec">Output audio codec. Default is mp3.</param>
        /// <param name="bitrate">Output audio bitrate in bits.</param>
        /// <param name="sampleRate">Output audio sample rate in Hz.</param>
        /// <returns>List of configured conversions, one per output part.</returns>
        internal static async Task<IReadOnlyList<IConversion>> SplitAudioByTimecodesAsync(
            string inputPath,
            string outputDirectory,
            IEnumerable<TimeSpan> timecodes,
            AudioCodec audioCodec = AudioCodec.mp3,
            long bitrate = 192000,
            int sampleRate = 44100)
        {
            if (timecodes == null)
            {
                throw new ArgumentNullException(nameof(timecodes));
            }

            if (bitrate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitrate), "Bitrate must be greater than 0.");
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than 0.");
            }

            IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);
            IAudioStream sourceAudio = info.AudioStreams.FirstOrDefault();
            if (sourceAudio == null)
            {
                throw new ArgumentException("Input file does not contain an audio stream.", nameof(inputPath));
            }

            var boundaries = timecodes
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (!boundaries.Any() || boundaries.First() > TimeSpan.Zero)
            {
                boundaries.Insert(0, TimeSpan.Zero);
            }

            if (boundaries.Last() < info.Duration)
            {
                boundaries.Add(info.Duration);
            }

            if (boundaries.Count < 2)
            {
                throw new ArgumentException("At least two time boundaries are required.", nameof(timecodes));
            }

            foreach (TimeSpan timecode in boundaries)
            {
                if (timecode < TimeSpan.Zero || timecode > info.Duration)
                {
                    throw new ArgumentOutOfRangeException(nameof(timecodes), $"Timecode {timecode} is out of media duration range.");
                }
            }

            Directory.CreateDirectory(outputDirectory);

            var result = new List<IConversion>();
            string extension = GetAudioExtension(audioCodec);
            string fileName = Path.GetFileNameWithoutExtension(inputPath);
            for (int i = 0; i < boundaries.Count - 1; i++)
            {
                TimeSpan start = boundaries[i];
                TimeSpan end = boundaries[i + 1];
                TimeSpan duration = end - start;
                if (duration <= TimeSpan.Zero)
                {
                    throw new ArgumentException("Timecodes must define increasing ranges.", nameof(timecodes));
                }

                string outputPath = Path.Combine(outputDirectory, $"{fileName}_{i + 1:D3}.{extension}");
                IAudioStream outputStream = sourceAudio
                    .Split(start, duration)
                    .SetCodec(audioCodec)
                    .SetBitrate(bitrate)
                    .SetSampleRate(sampleRate);

                result.Add(New()
                    .AddStream(outputStream)
                    .SetOutput(outputPath));
            }

            return result;
        }

        private static string GetAudioExtension(AudioCodec audioCodec)
        {
            switch (audioCodec)
            {
                case AudioCodec.aac:
                    return "aac";
                case AudioCodec.mp3:
                    return "mp3";
                default:
                    return audioCodec.ToString();
            }
        }
    }
}
