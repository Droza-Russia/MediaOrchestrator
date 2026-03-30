using System;
using System.IO;

namespace MediaOrchestrator
{
    public sealed class MediaSource
    {
        internal enum SourceKind
        {
            File = 0,
            Stream = 1,
            Bytes = 2
        }

        private MediaSource(SourceKind kind, string path, Stream stream, byte[] data, string fileExtension, int bufferSize, bool leaveOpen)
        {
            Kind = kind;
            Path = path;
            Stream = stream;
            Data = data;
            FileExtension = NormalizeExtension(fileExtension, path);
            BufferSize = bufferSize <= 0 ? 81920 : bufferSize;
            LeaveOpen = leaveOpen;
        }

        internal SourceKind Kind { get; }

        internal string Path { get; }

        internal Stream Stream { get; }

        internal byte[] Data { get; }

        internal string FileExtension { get; }

        internal int BufferSize { get; }

        internal bool LeaveOpen { get; }

        public static MediaSource FromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(ErrorMessages.InputPathMustBeProvided, nameof(path));
            }

            return new MediaSource(SourceKind.File, path, null, null, null, 81920, true);
        }

        public static MediaSource FromStream(Stream stream, string fileExtension, int bufferSize = 81920, bool leaveOpen = true)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(ErrorMessages.StreamMustBeReadable, nameof(stream));
            }

            return new MediaSource(SourceKind.Stream, null, stream, null, fileExtension, bufferSize, leaveOpen);
        }

        public static MediaSource FromBytes(byte[] data, string fileExtension)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return new MediaSource(SourceKind.Bytes, null, null, data, fileExtension, 81920, true);
        }

        private static string NormalizeExtension(string fileExtension, string path)
        {
            var extension = fileExtension;
            if (string.IsNullOrWhiteSpace(extension) && !string.IsNullOrWhiteSpace(path))
            {
                extension = System.IO.Path.GetExtension(path);
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                return ".bin";
            }

            return extension.StartsWith(".") ? extension : "." + extension;
        }
    }
}
