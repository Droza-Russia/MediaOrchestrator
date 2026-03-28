namespace MediaOrchestrator.Analytics.Models
{
    public enum MediaProcessingStrategy
    {
        Remux = 0,
        NormalizeAudio = 1,
        PartialTranscode = 2,
        FullTranscode = 3
    }
}
