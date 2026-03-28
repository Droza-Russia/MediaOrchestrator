namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Описание кода ошибки MediaOrchestrator для документации и UI.
    /// </summary>
    public sealed class MediaErrorDescriptor
    {
        public MediaErrorCode ErrorCode { get; set; }

        public string Code { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }
    }
}
