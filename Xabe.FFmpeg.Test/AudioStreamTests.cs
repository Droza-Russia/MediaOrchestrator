using Xunit;

namespace Xabe.FFmpeg.Test
{
    public class AudioStreamTests
    {
        [Fact]
        public void Reverse_UsesTypedFilterExpression()
        {
            var stream = new AudioStream();

            stream.Reverse();

            var parameters = stream.BuildParameters(ParameterPosition.PostInput);
            Assert.Contains(
                FFmpegAudioStreamArguments.SetAudioFilter(FFmpegAudioFilterExpressions.Reverse()),
                parameters);
        }

        [Fact]
        public void SetSampleRate_UsesTypedArgument()
        {
            var stream = new AudioStream
            {
                Index = 1
            };

            stream.SetSampleRate(48000);

            var parameters = stream.BuildParameters(ParameterPosition.PostInput);
            Assert.Contains(FFmpegAudioStreamArguments.SetSampleRate(1, 48000), parameters);
        }

        [Fact]
        public void ChangeSpeed_UsesTypedTempoExpression()
        {
            var stream = new AudioStream();

            stream.ChangeSpeed(1.25);

            var filters = ((IFilterable)stream).GetFilters().GetEnumerator();
            Assert.True(filters.MoveNext());
            Assert.Equal(FFmpegAudioFilterExpressions.ChangeTempo(1.25), filters.Current.Filters[StreamFilterName.Atempo.ToArgumentName()]);
        }
    }
}
