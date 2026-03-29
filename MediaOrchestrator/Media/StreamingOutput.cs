using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Represents a streaming output sink for ffmpeg output.
    /// </summary>
    public sealed class StreamingOutput : IDisposable
    {
        private readonly Stream _outputStream;
        private readonly bool _leaveOpen;
        private bool _isDisposed;

        /// <summary>
        ///     Creates a streaming output that writes to a provided stream.
        /// </summary>
        public StreamingOutput(Stream outputStream, bool leaveOpen = false)
        {
            _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        ///     Creates a streaming output that writes to stdout stream.
        /// </summary>
        public static StreamingOutput CreateStdout()
        {
            return new StreamingOutput(Console.OpenStandardOutput(), leaveOpen: false);
        }

        /// <summary>
        ///     Creates a streaming output that writes to a file stream.
        /// </summary>
        public static StreamingOutput CreateFileStream(string filePath, bool append = false)
        {
            var fileStream = new FileStream(
                filePath,
                append ? FileMode.Append : FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            return new StreamingOutput(fileStream, leaveOpen: false);
        }

        /// <summary>
        ///     Creates a streaming output that writes to memory stream.
        /// </summary>
        public static StreamingOutput CreateMemoryStream()
        {
            return new StreamingOutput(new MemoryStream(), leaveOpen: false);
        }

        internal Stream OutputStream => _outputStream;

        internal bool CanWrite => _outputStream?.CanWrite ?? false;

        /// <summary>
        ///     Writes data to the stream asynchronously.
        /// </summary>
        public async Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_isDisposed || _outputStream == null)
            {
                throw new ObjectDisposedException(nameof(StreamingOutput));
            }

            await _outputStream.WriteAsync(data, offset, count, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Flushes the stream.
        /// </summary>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed || _outputStream == null)
            {
                return;
            }

            await _outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Gets the written data as memory stream (only works with CreateMemoryStream).
        /// </summary>
        public MemoryStream GetWrittenDataAsMemoryStream()
        {
            if (_outputStream is MemoryStream ms)
            {
                ms.Position = 0;
                return ms;
            }

            throw new InvalidOperationException("This method only works with CreateMemoryStream()");
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_outputStream != null && !_leaveOpen)
            {
                try
                {
                    _outputStream.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}