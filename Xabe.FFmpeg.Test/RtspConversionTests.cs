using System;
using Xabe.FFmpeg.Test.TestSupport;
using Xunit;

namespace Xabe.FFmpeg.Test
{
    public class RtspConversionTests
    {
        [Fact]
        public void SendDesktopToRtspServer_UsesTypedTuneApi()
        {
            var conversion = Conversion.SendDesktopToRtspServer(new Uri("rtsp://127.0.0.1:8554/live"));

            conversion.Should().Video.ShouldUseTune(ConversionTune.ZeroLatency);
        }
    }
}
