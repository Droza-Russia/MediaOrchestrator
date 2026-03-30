using System;
using System.Buffers;

namespace MediaOrchestrator
{
    internal static class BufferPool
    {
        private const int DefaultBufferSize = 81920;
        private const int LargeBufferSize = 1024 * 1024;

        public static byte[] RentDefault()
        {
            return ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        }

        public static byte[] RentLarge()
        {
            return ArrayPool<byte>.Shared.Rent(LargeBufferSize);
        }

        public static void ReturnDefault(byte[] buffer)
        {
            if (buffer != null && buffer.Length >= DefaultBufferSize)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static void ReturnLarge(byte[] buffer)
        {
            if (buffer != null && buffer.Length >= LargeBufferSize)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static ArrayPool<byte> Shared => ArrayPool<byte>.Shared;
    }
}