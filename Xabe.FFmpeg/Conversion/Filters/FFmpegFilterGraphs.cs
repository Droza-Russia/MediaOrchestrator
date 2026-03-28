using System.Collections.Generic;
using System.Linq;

namespace Xabe.FFmpeg
{
    internal static class FFmpegFilterGraphs
    {
        internal static FilterGraph BuildAudioVisualisation(
            VisualisationMode mode,
            FrequencyScale frequencyScale,
            AmplitudeScale amplitudeScale,
            PixelFormat pixelFormat,
            VideoSize size)
        {
            var builder = new FFmpegFilterGraphBuilder();
            var input = FilterLabel.AudioInput(0);
            var output = FilterLabel.Named("v");
            builder.AddChain(
                input,
                output,
                FFmpegFilters.ShowFreqs(mode, frequencyScale, amplitudeScale),
                FFmpegFilters.Format(pixelFormat),
                FFmpegFilters.Scale(size));
            return new FilterGraph(builder.Build(), output);
        }

        internal static FilterGraph BuildConcatGraph(IReadOnlyList<IMediaInfo> mediaInfos, IVideoStream maxResolutionMedia)
        {
            var builder = new FFmpegFilterGraphBuilder();

            for (var i = 0; i < mediaInfos.Count; i++)
            {
                var input = FilterLabel.VideoInput(i);
                var scaled = FilterLabel.Named($"v{i}");
                builder.AddChain(
                    input,
                    scaled,
                    FFmpegFilters.Scale(maxResolutionMedia.Width, maxResolutionMedia.Height),
                    FFmpegFilters.SetDisplayAspectRatio(maxResolutionMedia.Ratio),
                    FFmpegFilters.ResetPresentationTimestamps());
            }

            var concatInputs = new List<FilterLabel>();
            for (var i = 0; i < mediaInfos.Count; i++)
            {
                var video = FilterLabel.Named($"v{i}");
                concatInputs.Add(video);
                if (mediaInfos[i].AudioStreams.Any())
                {
                    concatInputs.Add(FilterLabel.AudioInput(i));
                }
            }

            var videoOutput = FilterLabel.Named("v");
            var audioOutput = FilterLabel.Named("a");
            builder.AddSegment(
                concatInputs,
                new[] { videoOutput, audioOutput },
                FFmpegFilters.Concat(mediaInfos.Count, 1, 1));
            return new FilterGraph(builder.Build(), videoOutput, audioOutput);
        }
    }
}
