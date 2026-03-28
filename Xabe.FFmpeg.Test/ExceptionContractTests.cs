using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xabe.FFmpeg.Exceptions;
using Xunit;

namespace Xabe.FFmpeg.Test
{
    public class ExceptionContractTests
    {
        [Fact]
        public void MapAudioStream_ThrowsTypedException_ForNegativeInputIndex()
        {
            var exception = Assert.Throws<StreamIndexOutOfRangeException>(() =>
                new Conversion().MapAudioStream(-1, 0));

            Assert.Equal("inputIndex", exception.ParamName);
            Assert.StartsWith(ErrorMessages.StreamIndexOutOfRange, exception.Message);
        }

        [Fact]
        public void MapAudioStream_ThrowsTypedException_ForNegativeAudioStreamIndex()
        {
            var exception = Assert.Throws<StreamIndexOutOfRangeException>(() =>
                new Conversion().MapAudioStream(0, -1));

            Assert.Equal("audioStreamIndex", exception.ParamName);
            Assert.StartsWith(ErrorMessages.StreamIndexOutOfRange, exception.Message);
        }

        [Fact]
        public void CreateOutputDirectoryIfNotExists_ThrowsTypedException_WhenPathCollidesWithFile()
        {
            var directory = Path.Combine(Path.GetTempPath(), "xabe-output-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var blockingFile = Path.Combine(directory, "existing.file");
            File.WriteAllText(blockingFile, "content");

            var conversion = new Conversion().SetOutput(Path.Combine(blockingFile, "out.mp4"));
            var method = typeof(Conversion).GetMethod("CreateOutputDirectoryIfNotExists", BindingFlags.NonPublic | BindingFlags.Instance);

            var targetException = Assert.Throws<TargetInvocationException>(() => method.Invoke(conversion, null));
            var exception = Assert.IsType<OutputPathAccessDeniedException>(targetException.InnerException);
            Assert.Contains("out.mp4", exception.Message);
        }

        [Fact]
        public void FFmpegExceptionCatcher_ThrowsStreamMappingException_ForMissingMappedStream()
        {
            var catcher = new FFmpegExceptionCatcher();

            var exception = Assert.Throws<ConversionException>(() =>
                catcher.CatchFFmpegErrors("Stream specifier :a:3 matches no streams.", "-map 0:a:3"));

            var mappingException = Assert.IsType<StreamMappingException>(exception.InnerException);
            Assert.Equal(ErrorMessages.StreamMappingFailed, mappingException.Message);
            Assert.Contains("-map 0:a:3", mappingException.InputParameters);
        }

        [Fact]
        public void FFmpegExceptionCatcher_ThrowsStreamCodecNotSupportedException_ForUnsupportedCodec()
        {
            var catcher = new FFmpegExceptionCatcher();

            var exception = Assert.Throws<ConversionException>(() =>
                catcher.CatchFFmpegErrors("Could not find tag for codec pcm_s16le in stream #0, codec not currently supported in container", "-c:a pcm_s16le"));

            var codecException = Assert.IsType<StreamCodecNotSupportedException>(exception.InnerException);
            Assert.Equal(ErrorMessages.StreamCodecNotSupported, codecException.Message);
            Assert.Contains("-c:a pcm_s16le", codecException.InputParameters);
        }

        [Fact]
        public void LibraryExceptions_CanBeCaughtBySingleBaseType()
        {
            var invalidInput = Record.Exception(() => new Conversion().MapAudioStream(-1, 0));
            Assert.IsAssignableFrom<XabeFFmpegException>(invalidInput);
            Assert.IsType<StreamIndexOutOfRangeException>(invalidInput);

            var ffmpegFailure = Record.Exception(() =>
                new FFmpegExceptionCatcher().CatchFFmpegErrors(
                    "Could not find tag for codec pcm_s16le in stream #0, codec not currently supported in container",
                    "-c:a pcm_s16le"));
            Assert.IsAssignableFrom<XabeFFmpegException>(ffmpegFailure);
            Assert.IsType<ConversionException>(ffmpegFailure);
        }

        [Fact]
        public void RequireVideoStream_ThrowsTypedException_WhenMediaInfoHasNoVideo()
        {
            var method = typeof(Conversion).GetMethod("RequireVideoStream", BindingFlags.NonPublic | BindingFlags.Static);

            var targetException = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, new object[] { new TestMediaInfo(), "inputPath" }));

            var exception = Assert.IsType<VideoStreamNotFoundException>(targetException.InnerException);
            Assert.Equal("inputPath", exception.ParamName);
            Assert.StartsWith(ErrorMessages.InputFileDoesNotContainVideoStream, exception.Message);
        }

        [Fact]
        public void RequireSubtitleStream_ThrowsTypedException_WhenMediaInfoHasNoSubtitles()
        {
            var method = typeof(Conversion).GetMethod("RequireSubtitleStream", BindingFlags.NonPublic | BindingFlags.Static);

            var targetException = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, new object[] { new TestMediaInfo(), "subtitlePath" }));

            var exception = Assert.IsType<SubtitleStreamNotFoundException>(targetException.InnerException);
            Assert.Equal("subtitlePath", exception.ParamName);
            Assert.StartsWith(ErrorMessages.InputFileDoesNotContainSubtitleStream, exception.Message);
        }

        private sealed class TestMediaInfo : IMediaInfo
        {
            public IEnumerable<IStream> Streams { get; } = Array.Empty<IStream>();
            public string Path { get; } = "test";
            public TimeSpan Duration { get; } = TimeSpan.Zero;
            public DateTime? CreationTime { get; } = null;
            public long Size { get; } = 0;
            public IEnumerable<IVideoStream> VideoStreams { get; } = Array.Empty<IVideoStream>();
            public IEnumerable<IAudioStream> AudioStreams { get; } = Array.Empty<IAudioStream>();
            public IEnumerable<ISubtitleStream> SubtitleStreams { get; } = Array.Empty<ISubtitleStream>();
        }
    }
}
