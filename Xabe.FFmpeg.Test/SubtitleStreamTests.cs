using Xunit;

namespace Xabe.FFmpeg.Test
{
    public class SubtitleStreamTests
    {
        [Fact]
        public void SetLanguage_UsesTypedArgument()
        {
            var stream = new SubtitleStream
            {
                Index = 2
            };

            stream.SetLanguage("eng");

            var parameters = stream.BuildParameters(ParameterPosition.PostInput);
            Assert.Contains(FFmpegSubtitleStreamArguments.SetLanguage(2, "eng"), parameters);
        }

        [Fact]
        public void SetCodec_UsesTypedArgument()
        {
            var stream = new SubtitleStream();

            stream.SetCodec("mov_text");

            var parameters = stream.BuildParameters(ParameterPosition.PostInput);
            Assert.Contains(FFmpegSubtitleStreamArguments.SetCodec("mov_text"), parameters);
        }
    }
}
