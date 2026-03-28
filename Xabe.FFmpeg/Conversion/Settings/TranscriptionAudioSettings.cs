namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Settings for normalizing audio before speech-to-text transcription.
    /// </summary>
    public sealed class TranscriptionAudioSettings
    {
        /// <summary>
        ///     Zero-based index of the audio stream within the input audio streams collection.
        ///     If null, the first available audio stream is used.
        /// </summary>
        public int? AudioStreamIndex { get; set; }

        /// <summary>
        ///     Target sample rate in Hz. Default is 16000.
        /// </summary>
        public int SampleRate { get; set; } = 16000;

        /// <summary>
        ///     Target channel count. Default is 1 (mono).
        /// </summary>
        public int Channels { get; set; } = 1;

        /// <summary>
        ///     Target audio codec. Default is PCM signed 16-bit little-endian.
        /// </summary>
        public AudioCodec Codec { get; set; } = AudioCodec.pcm_s16le;

        /// <summary>
        ///     Target output container format. Default is WAV.
        /// </summary>
        public Format Format { get; set; } = Format.wav;
    }
}
