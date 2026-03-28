namespace MediaOrchestrator
{
    /// <summary>
    /// Форматы хеширования ("ffmpeg -i INPUT -f hash").
    /// </summary>
    public enum Hash
    {
        /// <summary>
        ///     Алгоритм хеширования MD5.
        /// </summary>
        MD5,

        /// <summary>
        ///     Алгоритм хеширования murmur3.
        /// </summary>
        murmur3,

        /// <summary>
        ///     Алгоритм хеширования RIPEMD 128-bit.
        /// </summary>
        RIPEMD128,

        /// <summary>
        ///     Алгоритм хеширования RIPEMD 160-bit.
        /// </summary>
        RIPEMD160,

        /// <summary>
        ///     Алгоритм хеширования RIPEMD 256-bit.
        /// </summary>
        RIPEMD256,

        /// <summary>
        ///     Алгоритм хеширования RIPEMD 320-bit.
        /// </summary>
        RIPEMD320,

        /// <summary>
        ///     Алгоритм хеширования SHA 160-bit.
        /// </summary>
        SHA160,

        /// <summary>
        ///     Алгоритм хеширования SHA 224-bit.
        /// </summary>
        SHA224,

        /// <summary>
        ///     Алгоритм хеширования SHA 256-bit.
        ///     Используется по умолчанию, если аргумент -hash не указан.
        /// </summary>
        SHA256,

        /// <summary>
        ///     Алгоритм хеширования SHA 512-bit/224-bit.
        /// </summary>
        SHA512_224,

        /// <summary>
        ///     Алгоритм хеширования SHA 512-bit/256-bit.
        /// </summary>
        SHA512_256,

        /// <summary>
        ///     Алгоритм хеширования SHA 384-bit.
        /// </summary>
        SHA384,

        /// <summary>
        ///     Алгоритм хеширования SHA 512-bit.
        /// </summary>
        SHA512,

        /// <summary>
        ///     Алгоритм хеширования CRC32.
        /// </summary>
        CRC32,

        /// <summary>
        ///     Алгоритм хеширования Adler32.
        /// </summary>
        adler32,
    }
}
