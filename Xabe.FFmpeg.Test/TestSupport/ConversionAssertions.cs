using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Xabe.FFmpeg.Test.TestSupport
{
    internal sealed class ConversionCommandAssertions
    {
        private readonly string _command;

        internal ConversionCommandAssertions(string command)
        {
            _command = command ?? string.Empty;
        }

        internal InputCommandAssertions Input => new InputCommandAssertions(_command);

        internal AudioCommandAssertions Audio => new AudioCommandAssertions(_command);

        internal VideoCommandAssertions Video => new VideoCommandAssertions(_command);

        internal ContainerCommandAssertions Container => new ContainerCommandAssertions(_command);
    }

    internal sealed class InputCommandAssertions
    {
        private readonly string _command;

        internal InputCommandAssertions(string command)
        {
            _command = command ?? string.Empty;
        }

        internal InputCommandAssertions ShouldUseFormat(string format)
        {
            Assert.Contains(FFmpegInputArguments.SetInputFormat(format), _command, StringComparison.Ordinal);
            return this;
        }

        internal InputCommandAssertions ShouldAddSource(string value)
        {
            Assert.Contains(FFmpegInputArguments.AddInput(value), _command, StringComparison.Ordinal);
            return this;
        }

        internal InputCommandAssertions ShouldUsePipe(PipeDescriptor descriptor)
        {
            Assert.Contains(FFmpegConversionArguments.PipeSpecifier(descriptor), _command, StringComparison.Ordinal);
            return this;
        }

        internal InputCommandAssertions ShouldSetDuration(TimeSpan duration)
        {
            Assert.Contains($"-t {duration.ToFFmpeg()}", _command, StringComparison.Ordinal);
            return this;
        }
    }

    internal sealed class AudioCommandAssertions
    {
        private readonly string _command;

        internal AudioCommandAssertions(string command)
        {
            _command = command ?? string.Empty;
        }

        internal AudioCommandAssertions ShouldUseCodec(AudioCodec codec)
        {
            Assert.Contains(FFmpegAudioArguments.SetCodec(codec), _command, StringComparison.Ordinal);
            return this;
        }

        internal AudioCommandAssertions ShouldCopyCodec()
        {
            Assert.Contains(FFmpegAudioArguments.CopyCodecValue, _command, StringComparison.Ordinal);
            return this;
        }

        internal AudioCommandAssertions ShouldNotCopyCodec()
        {
            Assert.DoesNotContain(FFmpegAudioArguments.CopyCodecValue, _command, StringComparison.Ordinal);
            return this;
        }

        internal AudioCommandAssertions ShouldSetSampleRate(int sampleRate)
        {
            Assert.Contains(FFmpegAudioArguments.SetSampleRate(sampleRate), _command, StringComparison.Ordinal);
            return this;
        }

        internal AudioCommandAssertions ShouldSetChannels(int channels)
        {
            Assert.Contains(FFmpegAudioArguments.SetChannels(channels), _command, StringComparison.Ordinal);
            return this;
        }

        internal AudioCommandAssertions ShouldNotSetSampleRate(int sampleRate)
        {
            Assert.DoesNotContain(FFmpegAudioArguments.SetSampleRate(sampleRate), _command, StringComparison.Ordinal);
            return this;
        }

        internal AudioCommandAssertions ShouldNotSetChannels(int channels)
        {
            Assert.DoesNotContain(FFmpegAudioArguments.SetChannels(channels), _command, StringComparison.Ordinal);
            return this;
        }
    }

    internal sealed class VideoCommandAssertions
    {
        private readonly string _command;

        internal VideoCommandAssertions(string command)
        {
            _command = command ?? string.Empty;
        }

        internal VideoCommandAssertions ShouldDisableOutput()
        {
            Assert.Contains(FFmpegVideoArguments.DisableOutputFlag, _command, StringComparison.Ordinal);
            return this;
        }

        internal VideoCommandAssertions ShouldSetFrameRate(double frameRate)
        {
            Assert.Contains(FFmpegVideoArguments.SetFrameRate(frameRate), _command, StringComparison.Ordinal);
            return this;
        }

        internal VideoCommandAssertions ShouldNotSetFrameRate(double frameRate)
        {
            Assert.DoesNotContain(FFmpegVideoArguments.SetFrameRate(frameRate), _command, StringComparison.Ordinal);
            return this;
        }

        internal VideoCommandAssertions ShouldUseTune(ConversionTune tune)
        {
            Assert.Contains(FFmpegEncodingArguments.SetTune(tune), _command, StringComparison.Ordinal);
            return this;
        }

        internal VideoCommandAssertions ShouldUseHardwareAcceleration(string hardwareAccelerator, string decoder, string encoder, int? device = null)
        {
            Assert.Contains(FFmpegHardwareAccelerationArguments.SetHardwareAcceleration(hardwareAccelerator), _command, StringComparison.Ordinal);
            Assert.Contains(FFmpegHardwareAccelerationArguments.SetVideoDecoder(decoder), _command, StringComparison.Ordinal);
            Assert.Contains(FFmpegHardwareAccelerationArguments.SetVideoEncoder(encoder), _command, StringComparison.Ordinal);
            if (device.HasValue)
            {
                Assert.Contains(FFmpegHardwareAccelerationArguments.SetHardwareAccelerationDevice(device.Value), _command, StringComparison.Ordinal);
            }

            return this;
        }
    }

    internal sealed class ContainerCommandAssertions
    {
        private readonly string _command;

        internal ContainerCommandAssertions(string command)
        {
            _command = command ?? string.Empty;
        }

        internal ContainerCommandAssertions ShouldUseOutputFormat(Format format)
        {
            Assert.Contains(FFmpegContainerArguments.SetOutputFormat(format), _command, StringComparison.Ordinal);
            return this;
        }

        internal ContainerCommandAssertions ShouldCopyAllCodecs()
        {
            Assert.Contains(FFmpegContainerArguments.CopyAllCodecsValue, _command, StringComparison.Ordinal);
            return this;
        }

        internal ContainerCommandAssertions ShouldMapAllStreams()
        {
            Assert.Contains(FFmpegContainerArguments.MapAllStreamsValue, _command, StringComparison.Ordinal);
            return this;
        }

        internal ContainerCommandAssertions ShouldMapAudioStream(int inputIndex, int audioStreamIndex)
        {
            Assert.Contains(FFmpegConversionArguments.MapAudioStream(inputIndex, audioStreamIndex), _command, StringComparison.Ordinal);
            return this;
        }
    }

    internal sealed class HostedVideoArgumentAssertions
    {
        private readonly IReadOnlyCollection<string> _arguments;

        internal HostedVideoArgumentAssertions(IEnumerable<string> arguments)
        {
            _arguments = arguments?.ToArray() ?? Array.Empty<string>();
        }

        internal HostedVideoArgumentAssertions ShouldUseDefaultDownloadFormat()
        {
            Assert.Contains(FFmpegHostedVideoArguments.DefaultFormat, _arguments);
            return this;
        }

        internal HostedVideoArgumentAssertions ShouldIncludeFfmpegLocation(string path)
        {
            Assert.Contains(FFmpegHostedVideoArguments.FfmpegLocationOption, _arguments);
            Assert.Contains(path, _arguments);
            return this;
        }

        internal HostedVideoArgumentAssertions ShouldIncludeArgument(string argument)
        {
            Assert.Contains(argument, _arguments);
            return this;
        }
    }

    internal static class AssertionExtensions
    {
        internal static ConversionCommandAssertions Should(this IConversion conversion)
        {
            var concrete = Assert.IsType<Conversion>(conversion);
            return new ConversionCommandAssertions(concrete.Build());
        }

        internal static ConversionCommandAssertions Should(this Conversion conversion)
        {
            return new ConversionCommandAssertions(conversion.Build());
        }

        internal static HostedVideoArgumentAssertions Should(this IEnumerable<string> arguments)
        {
            return new HostedVideoArgumentAssertions(arguments);
        }
    }
}
