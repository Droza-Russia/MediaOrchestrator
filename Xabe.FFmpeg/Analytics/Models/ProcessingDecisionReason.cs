namespace Xabe.FFmpeg.Analytics.Models
{
    public enum ProcessingDecisionReason
    {
        Unknown = 0,
        ScenarioAiTranscriptionContract = 1,
        ScenarioBrowserCompatibility = 2,
        MissingAudioStream = 3,
        BrowserCodecsCompatible = 4,
        BrowserCodecsIncompatible = 5,
        RemuxAllowed = 6,
        RemuxNotAllowed = 7,
        TranscodeAllowed = 8,
        TranscodeNotAllowed = 9,
        HardwareAccelerationAllowed = 10,
        HardwareAccelerationNotAllowed = 11
    }
}
