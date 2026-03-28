using System;
using System.Collections.Generic;

namespace MediaOrchestrator
{
    public sealed class MediaDirectoryDestination
    {
        internal enum DirectoryDestinationKind
        {
            FileSystem = 0,
            Memory = 1
        }

        private readonly Dictionary<string, byte[]> _files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        private MediaDirectoryDestination(DirectoryDestinationKind kind, string directoryPath, int bufferSize)
        {
            Kind = kind;
            DirectoryPath = directoryPath;
            BufferSize = bufferSize <= 0 ? 81920 : bufferSize;
        }

        internal DirectoryDestinationKind Kind { get; }

        internal string DirectoryPath { get; }

        internal int BufferSize { get; }

        public static MediaDirectoryDestination ToDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Directory path must be provided.", nameof(directoryPath));
            }

            return new MediaDirectoryDestination(DirectoryDestinationKind.FileSystem, directoryPath, 81920);
        }

        public static MediaDirectoryDestination ToMemory(int bufferSize = 81920)
        {
            return new MediaDirectoryDestination(DirectoryDestinationKind.Memory, null, bufferSize);
        }

        public IReadOnlyDictionary<string, byte[]> GetFiles()
        {
            return _files;
        }

        internal void SetFile(string relativePath, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            _files[relativePath] = bytes ?? Array.Empty<byte>();
        }
    }
}
