using System.Linq;
using Xunit;

namespace Xabe.FFmpeg.Test
{
    public class VideoStreamTests
    {
        [Fact]
        public void Rotate_UsesTypedFilterExpression()
        {
            var stream = new VideoStream();

            stream.Rotate(RotateDegrees.Clockwise);

            var parameters = stream.BuildParameters(ParameterPosition.PostInput);
            Assert.Contains(FFmpegVideoStreamArguments.SetVideoFilter(
                FFmpegVideoFilterExpressions.Rotate(RotateDegrees.Clockwise)), parameters);
        }

        [Fact]
        public void Pad_UsesTypedFilterExpression()
        {
            var stream = new VideoStream();

            stream.Pad(1280, 720);

            var parameters = stream.BuildParameters(ParameterPosition.PostInput);
            Assert.Contains(FFmpegVideoStreamArguments.SetVideoFilter(
                FFmpegVideoFilterExpressions.PadToFit(1280, 720)), parameters);
        }

        [Fact]
        public void SetRightSideDrawText_UsesTypedDrawTextBuilder()
        {
            var stream = new VideoStream();

            stream.SetRightSideDrawText("Hello: world", fontColor: "yellow", fontSize: 18, marginRight: 12, marginY: 8, verticalAlign: DrawTextVerticalAlign.Top);

            var drawText = ((IFilterable)stream).GetFilters().Single().Filters[StreamFilterName.DrawText.ToArgumentName()];
            var expected = FFmpegVideoFilterExpressions.BuildDrawText(
                FFmpegVideoFilterExpressions.BuildDrawTextClause("Hello: world"),
                "yellow",
                18,
                12,
                8,
                DrawTextVerticalAlign.Top,
                null);

            Assert.Equal(expected, drawText);
        }

        [Fact]
        public void UseNativeInputRead_UsesTypedArgument()
        {
            var stream = new VideoStream();

            stream.UseNativeInputRead(true);

            var parameters = stream.BuildParameters(ParameterPosition.PreInput);
            Assert.Contains(FFmpegVideoStreamArguments.UseNativeInputRead(), parameters);
        }

        [Fact]
        public void SetWatermark_UsesTypedOverlayExpression()
        {
            var stream = new VideoStream
            {
                Path = "input.mp4"
            };

            stream.SetWatermark("wm.png", Position.UpperRight);

            var overlay = ((IFilterable)stream).GetFilters().Single().Filters[StreamFilterName.Overlay.ToArgumentName()];
            Assert.Equal(FFmpegVideoFilterExpressions.BuildOverlayPosition(Position.UpperRight), overlay);
        }

        [Fact]
        public void AddSubtitles_UsesTypedSubtitleExpression()
        {
            var stream = new VideoStream();

            stream.AddSubtitles("subs.srt", VideoSize.Hd1080, "utf-8", "FontSize=18");

            var subtitles = ((IFilterable)stream).GetFilters().Single().Filters[StreamFilterName.Subtitles.ToArgumentName()];
            Assert.Equal(
                FFmpegVideoFilterExpressions.BuildSubtitles("subs.srt", VideoSize.Hd1080, "utf-8", "FontSize=18"),
                subtitles);
        }

        [Fact]
        public void SetBitrate_UsesTypedArgument()
        {
            var stream = new VideoStream();

            stream.SetBitrate(1200000);

            var parameters = stream.BuildParameters(ParameterPosition.PostInput);
            Assert.Contains(FFmpegVideoStreamArguments.SetBitrate(1200000), parameters);
        }
    }
}
