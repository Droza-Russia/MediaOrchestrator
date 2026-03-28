using System;
using System.IO;

namespace MediaOrchestrator
{
    public sealed class MediaDestination
    {
        internal enum DestinationKind
        {
            File = 0,
            Stream = 1,
            Bytes = 2
        }

        private byte[] _bytes;

        private MediaDestination(DestinationKind kind, string path, Stream stream, string fileExtension, int bufferSize, bool leaveOpen)
        {
            Kind = kind;
            Path = path;
            Stream = stream;
            FileExtension = NormalizeExtension(fileExtension, path);
            BufferSize = bufferSize <= 0 ? 81920 : bufferSize;
            LeaveOpen = leaveOpen;
        }

        internal DestinationKind Kind { get; }

        internal string Path { get; }

        internal Stream Stream { get; }

        internal string FileExtension { get; }

        internal int BufferSize { get; }

        internal bool LeaveOpen { get; }

        public static MediaDestination ToFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(ErrorMessages.OutputPathMustBeProvided, nameof(path));
            }

            return new MediaDestination(DestinationKind.File, path, null, null, 81920, true);
        }

        public static MediaDestination ToStream(Stream stream, string fileExtension, int bufferSize = 81920, bool leaveOpen = true)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanWrite)
            {
                throw new ArgumentException("Stream must be writable.", nameof(stream));
            }

            return new MediaDestination(DestinationKind.Stream, null, stream, fileExtension, bufferSize, leaveOpen);
        }

        public static MediaDestination ToBytes(string fileExtension, int bufferSize = 81920)
        {
            return new MediaDestination(DestinationKind.Bytes, null, null, fileExtension, bufferSize, true);
        }

        public byte[] GetBytes()
        {
            return _bytes ?? Array.Empty<byte>();
        }

        internal void SetBytes(byte[] bytes)
        {
            _bytes = bytes ?? Array.Empty<byte>();
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
