using System;
using Xabe.FFmpeg.Test.TestSupport;
using Xunit;

namespace Xabe.FFmpeg.Test
{
    public class ConversionTests : IDisposable
    {
        public ConversionTests()
        {
            FFmpeg.SetGlobalOutputLimits();
        }

        public void Dispose()
        {
            FFmpeg.SetGlobalOutputLimits();
        }

        [Fact]
        public void Build_AppliesGlobalOutputLimits_WhenStreamsExceedCaps()
        {
            FFmpeg.SetGlobalOutputLimits(maxOutputVideoFrameRate: 30, maxOutputAudioSampleRate: 48000, maxOutputAudioChannels: 2);

            var video = new VideoStream
            {
                Path = "input.mp4",
                Index = 0,
                Framerate = 60
            };
            video.SetCodec(VideoCodec.h264);

            var audio = new AudioStream
            {
                Path = "input.mp4",
                Index = 1,
                SampleRate = 96000,
                Channels = 6
            };
            audio.SetCodec(AudioCodec.aac);

            var conversion = new Conversion()
                .AddStream(video)
                .AddStream(audio)
                .SetOutput("output.mp4");

            var command = conversion.Should();
            command.Video.ShouldSetFrameRate(30);
            command.Audio.ShouldSetSampleRate(48000).ShouldSetChannels(2);
        }

        [Fact]
        public void Build_DoesNotApplyGlobalOutputLimits_WhenSuppressed()
        {
            FFmpeg.SetGlobalOutputLimits(maxOutputVideoFrameRate: 30, maxOutputAudioSampleRate: 48000, maxOutputAudioChannels: 2);

            var video = new VideoStream
            {
                Path = "input.mp4",
                Index = 0,
                Framerate = 60
            };
            video.SetCodec(VideoCodec.h264);

            var audio = new AudioStream
            {
                Path = "input.mp4",
                Index = 1,
                SampleRate = 96000,
                Channels = 6
            };
            audio.SetCodec(AudioCodec.aac);

            var conversion = new Conversion(suppressGlobalOutputLimits: true)
                .AddStream(video)
                .AddStream(audio)
                .SetOutput("output.mp4");

            var command = conversion.Should();
            command.Video.ShouldNotSetFrameRate(30);
            command.Audio.ShouldNotSetSampleRate(48000).ShouldNotSetChannels(2);
        }

        [Fact]
        public void Build_KeepsStringFilterCompatibilityLayer_Working()
        {
#pragma warning disable CS0618
            var conversion = new Conversion()
                .SetFilterComplex("[0:a]anull[v]")
                .MapFilterOutput("[v]")
                .SetOutput("output.mp4");
#pragma warning restore CS0618

            var args = conversion.Build();

            Assert.Contains("-filter_complex \"[0:a]anull[v]\"", args);
            Assert.Contains("-map \"[v]\"", args);
        }

        [Fact]
        public void Build_AddsTypedLavfiInputSource()
        {
            var conversion = new Conversion()
                .AddInput(InputSource.Lavfi("anullsrc=r=48000:cl=stereo", TimeSpan.FromSeconds(1)))
                .SetOutput("output.mp4");

            var command = conversion.Should();
            command.Input
                .ShouldSetDuration(TimeSpan.FromSeconds(1))
                .ShouldUseFormat(FFmpegInputArguments.LavfiFormat)
                .ShouldAddSource("anullsrc=r=48000:cl=stereo");
        }

        [Fact]
        public void Build_UsesTypedAudioMapArgument()
        {
            var conversion = new Conversion()
                .MapAudioStream(2, 1)
                .SetOutput("output.mp4");

            conversion.Should().Container.ShouldMapAudioStream(2, 1);
        }

        [Fact]
        public void Build_UsesTypedPipeSpecifier()
        {
            var conversion = new Conversion()
                .PipeOutput(PipeDescriptor.stdout);

            conversion.Should().Input.ShouldUsePipe(PipeDescriptor.stdout);
        }

        [Fact]
        public void Build_UsesTypedHardwareAccelerationArguments()
        {
            var conversion = new Conversion()
                .UseHardwareAcceleration("cuda", "h264_cuvid", "h264_nvenc", device: 1)
                .SetOutput("output.mp4");

            conversion.Should().Video.ShouldUseHardwareAcceleration("cuda", "h264_cuvid", "h264_nvenc", 1);
        }
    }
}
