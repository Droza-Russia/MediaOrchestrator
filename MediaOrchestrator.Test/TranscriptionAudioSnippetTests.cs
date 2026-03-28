using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaOrchestrator.Exceptions;
using MediaOrchestrator.Test.TestSupport;
using Xunit;

namespace MediaOrchestrator.Test
{
    public class TranscriptionAudioSnippetTests : IDisposable
    {
        private readonly Func<MediaProbeRunner, string, CancellationToken, Task<string>> _originalProbeExecutor = MediaProbeRunner.ProbeCommandExecutor;

        public void Dispose()
        {
            MediaProbeRunner.ProbeCommandExecutor = _originalProbeExecutor;
            MediaOrchestrator.ClearMediaInfoCache();
            MediaOrchestrator.MediaInfoCacheEnabled = true;
            MediaOrchestrator.SetExecutablesPath(null);
        }

        [Fact]
        public async Task NormalizeAudioForTranscription_UsesDefaultTranscriptionContract()
        {
            var tempDir = CreateTempDirectory();
            var inputPath = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllBytes(inputPath, CreateIsoBmffHeader());
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) => Task.FromResult(CreateAudioProbeJson(audioStreamCount: 1));

            var conversion = await MediaOrchestrator.Conversions.FromSnippet
                .NormalizeAudioForTranscription(inputPath, Path.Combine(tempDir, "transcription.wav"))
                .ConfigureAwait(false);

            var command = conversion.Should();
            command.Input.ShouldAddSource(inputPath);
            command.Container.ShouldMapAudioStream(0, 0).ShouldUseOutputFormat(Format.wav);
            command.Video.ShouldDisableOutput();
            command.Audio.ShouldUseCodec(AudioCodec.pcm_s16le)
                .ShouldSetSampleRate(16000)
                .ShouldSetChannels(1)
                .ShouldNotCopyCodec();
        }

        [Fact]
        public async Task NormalizeAudioForTranscription_UsesCustomSettings()
        {
            var tempDir = CreateTempDirectory();
            var inputPath = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllBytes(inputPath, CreateIsoBmffHeader());
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) => Task.FromResult(CreateAudioProbeJson(audioStreamCount: 2));

            var conversion = await MediaOrchestrator.Conversions.FromSnippet.NormalizeAudioForTranscription(
                    inputPath,
                    Path.Combine(tempDir, "transcription.flac"),
                    new TranscriptionAudioSettings
                    {
                        AudioStreamIndex = 1,
                        SampleRate = 8000,
                        Channels = 1,
                        Codec = AudioCodec.flac,
                        Format = Format.flac
                    })
                .ConfigureAwait(false);

            var command = conversion.Should();
            command.Container.ShouldMapAudioStream(0, 1).ShouldUseOutputFormat(Format.flac);
            command.Video.ShouldDisableOutput();
            command.Audio.ShouldUseCodec(AudioCodec.flac)
                .ShouldSetSampleRate(8000)
                .ShouldSetChannels(1);
        }

        [Fact]
        public async Task NormalizeAudioForTranscription_ThrowsWhenAudioIsMissing()
        {
            var tempDir = CreateTempDirectory();
            var inputPath = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllBytes(inputPath, CreateIsoBmffHeader());
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) => Task.FromResult(CreateAudioProbeJson(audioStreamCount: 0));

            await Assert.ThrowsAsync<AudioStreamNotFoundException>(() =>
                    MediaOrchestrator.Conversions.FromSnippet.NormalizeAudioForTranscription(
                        inputPath,
                        Path.Combine(tempDir, "transcription.wav")))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task NormalizeAudioForTranscription_ThrowsWhenAudioStreamIndexIsOutOfRange()
        {
            var tempDir = CreateTempDirectory();
            var inputPath = Path.Combine(tempDir, "sample.mp4");
            File.WriteAllBytes(inputPath, CreateIsoBmffHeader());
            CreateFakeExecutable(tempDir, "ffmpeg");
            CreateFakeExecutable(tempDir, "ffprobe");
            MediaOrchestrator.SetExecutablesPath(tempDir);
            MediaProbeRunner.ProbeCommandExecutor = (_, _, _) => Task.FromResult(CreateAudioProbeJson(audioStreamCount: 1));

            await Assert.ThrowsAsync<StreamIndexOutOfRangeException>(() =>
                    MediaOrchestrator.Conversions.FromSnippet.NormalizeAudioForTranscription(
                        inputPath,
                        Path.Combine(tempDir, "transcription.wav"),
                        new TranscriptionAudioSettings { AudioStreamIndex = 1 }))
                .ConfigureAwait(false);
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "media-orchestrator-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static byte[] CreateIsoBmffHeader()
        {
            return new byte[]
            {
                0x00, 0x00, 0x00, 0x18,
                0x66, 0x74, 0x79, 0x70,
                0x69, 0x73, 0x6F, 0x6D,
                0x00, 0x00, 0x02, 0x00
            };
        }

        private static string CreateFakeExecutable(string directory, string baseName)
        {
            var fileName = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? baseName + ".exe"
                : baseName;
            var path = Path.Combine(directory, fileName);

            File.WriteAllBytes(path, CreateExecutableBytes());
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.UserExecute);
            }

            return path;
        }

        private static byte[] CreateExecutableBytes()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return new byte[] { 0x4D, 0x5A, 0x00, 0x00 };
            }

            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                return new byte[] { 0xCF, 0xFA, 0xED, 0xFE };
            }

            return new byte[] { 0x7F, 0x45, 0x4C, 0x46 };
        }

        private static string CreateAudioProbeJson(int audioStreamCount)
        {
            if (audioStreamCount <= 0)
            {
                return "{\n" +
                       "  \"streams\": [\n" +
                       "    {\n" +
                       "      \"codec_name\": \"h264\",\n" +
                       "      \"codec_type\": \"video\",\n" +
                       "      \"width\": 1280,\n" +
                       "      \"height\": 720,\n" +
                       "      \"r_frame_rate\": \"30/1\",\n" +
                       "      \"duration\": 2.0,\n" +
                       "      \"index\": 0,\n" +
                       "      \"pix_fmt\": \"yuv420p\",\n" +
                       "      \"disposition\": { \"default\": 1, \"forced\": 0 }\n" +
                       "    }\n" +
                       "  ],\n" +
                       "  \"format\": {\n" +
                       "    \"format_name\": \"mov,mp4,m4a,3gp,3g2,mj2\",\n" +
                       "    \"size\": \"4096\",\n" +
                       "    \"duration\": 2.0,\n" +
                       "    \"bit_rate\": 800000\n" +
                       "  }\n" +
                       "}";
            }

            var audioStreams = string.Empty;
            for (var i = 0; i < audioStreamCount; i++)
            {
                if (i > 0)
                {
                    audioStreams += ",\n";
                }

                audioStreams +=
                    "    {\n" +
                    "      \"codec_name\": \"aac\",\n" +
                    "      \"codec_type\": \"audio\",\n" +
                    "      \"duration\": 2.0,\n" +
                    "      \"bit_rate\": 128000,\n" +
                    "      \"index\": " + (i + 1) + ",\n" +
                    "      \"channels\": 2,\n" +
                    "      \"sample_rate\": 48000,\n" +
                    "      \"disposition\": { \"default\": " + (i == 0 ? 1 : 0) + ", \"forced\": 0 }\n" +
                    "    }";
            }

            return "{\n" +
                   "  \"streams\": [\n" +
                   "    {\n" +
                   "      \"codec_name\": \"h264\",\n" +
                   "      \"codec_type\": \"video\",\n" +
                   "      \"width\": 1280,\n" +
                   "      \"height\": 720,\n" +
                   "      \"r_frame_rate\": \"30/1\",\n" +
                   "      \"duration\": 2.0,\n" +
                   "      \"index\": 0,\n" +
                   "      \"pix_fmt\": \"yuv420p\",\n" +
                   "      \"disposition\": { \"default\": 1, \"forced\": 0 }\n" +
                   "    },\n" +
                   audioStreams + "\n" +
                   "  ],\n" +
                   "  \"format\": {\n" +
                   "    \"format_name\": \"mov,mp4,m4a,3gp,3g2,mj2\",\n" +
                   "    \"size\": \"4096\",\n" +
                   "    \"duration\": 2.0,\n" +
                   "    \"bit_rate\": 928000\n" +
                   "  }\n" +
                   "}";
        }
    }
}
