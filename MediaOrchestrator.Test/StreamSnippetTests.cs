using System.IO;
using MediaOrchestrator.Test.TestSupport;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class StreamSnippetTests
    {
        [Theory]
        [InlineData(Format.wav, AudioCodec.pcm_s16le)]
        [InlineData(Format.mp3, AudioCodec.mp3)]
        [InlineData(Format.flac, AudioCodec.flac)]
        [InlineData(Format.ogg, AudioCodec.libvorbis)]
        [InlineData(Format.oga, AudioCodec.libvorbis)]
        [InlineData(Format.spx, AudioCodec.libvorbis)]
        [InlineData(Format.opus, AudioCodec.libopus)]
        [InlineData(Format.webm, AudioCodec.libopus)]
        [InlineData(Format.aac, AudioCodec.aac)]
        [InlineData(Format.mp4, AudioCodec.aac)]
        [InlineData(Format.mov, AudioCodec.aac)]
        [InlineData(Format.ipod, AudioCodec.aac)]
        [InlineData(Format.ac3, AudioCodec.ac3)]
        [InlineData(Format.eac3, AudioCodec.ac3)]
        public void GetPreferredAudioCodec_ReturnsExpectedCodec(Format format, AudioCodec expectedCodec)
        {
            var codec = Conversion.GetPreferredAudioCodec(format);

            Assert.Equal(expectedCodec, codec);
        }

        [Fact]
        public void GetPreferredAudioCodec_ReturnsNull_ForContainerWithoutForcedAudioCodec()
        {
            var codec = Conversion.GetPreferredAudioCodec(Format.matroska);

            Assert.Null(codec);
        }

        [Fact]
        public void StreamAudioFromStdin_UsesPreferredCodec_ForWav()
        {
            using var input = new MemoryStream(new byte[] { 1, 2, 3 });

            var conversion = (Conversion)Conversion.StreamAudioFromStdin(input, "audio.wav", Format.wav);
            var command = conversion.Should();
            command.Video.ShouldDisableOutput();
            command.Audio.ShouldUseCodec(AudioCodec.pcm_s16le).ShouldNotCopyCodec();
            command.Container.ShouldUseOutputFormat(Format.wav);
        }

        [Fact]
        public void StreamAudioFromStdin_UsesPreferredCodec_ForOgg()
        {
            using var input = new MemoryStream(new byte[] { 1, 2, 3 });

            var conversion = (Conversion)Conversion.StreamAudioFromStdin(input, "audio.ogg", Format.ogg);
            var command = conversion.Should();
            command.Audio.ShouldUseCodec(AudioCodec.libvorbis).ShouldNotCopyCodec();
            command.Container.ShouldUseOutputFormat(Format.ogg);
        }

        [Fact]
        public void StreamAudioFromStdin_UsesCopy_WhenFormatIsNotSpecified()
        {
            using var input = new MemoryStream(new byte[] { 1, 2, 3 });

            var conversion = (Conversion)Conversion.StreamAudioFromStdin(input, "audio.bin");
            conversion.Should().Audio.ShouldCopyCodec();
        }
    }
}
