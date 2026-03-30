namespace MediaOrchestrator.Analytics.Models
{
    /// <summary>
    ///     Целевой сценарий обработки медиа.
    /// </summary>
    public enum ProcessingScenario
    {
        BrowserPlayback = 0,
        AiTranscription = 1,
        AiEmbeddings = 2,
        FrameExtraction = 3,
        ArchivalNormalize = 4,
        Custom = 5
    }
}
