using MediaOrchestrator.Exceptions;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class ErrorCodeCatalogTests
    {
        [Fact]
        public void Exception_ExposesStableErrorCode()
        {
            var exception = new AudioStreamNotFoundException("Audio missing", "inputPath");

            Assert.Equal(MediaErrorCode.AudioStreamNotFound, exception.ErrorCode);
            Assert.Equal("MOR-IN-3007", exception.ErrorCodeId);
        }

        [Fact]
        public void ErrorCatalog_ReturnsPublicDescriptor()
        {
            var descriptor = MediaErrorCatalog.Get(MediaErrorCode.UnknownDecoder);

            Assert.Equal("MOR-CV-1001", descriptor.Code);
            Assert.Equal("Unknown decoder", descriptor.Title);
        }
    }
}
