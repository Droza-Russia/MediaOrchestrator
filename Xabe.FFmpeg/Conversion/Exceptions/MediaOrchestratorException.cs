using System;

namespace MediaOrchestrator.Exceptions
{
    /// <summary>
    ///     Базовое исключение библиотеки MediaOrchestrator.
    /// </summary>
    public class MediaOrchestratorException : Exception
    {
        public MediaOrchestratorException(string message) : base(message)
        {
        }

        public MediaOrchestratorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        ///     Стабильный код ошибки библиотеки для аналитики, документации и UI.
        /// </summary>
        public MediaErrorCode ErrorCode => MediaErrorCatalog.Resolve(GetType());

        /// <summary>
        ///     Строковый код ошибки для отображения пользователю, например MOR-CV-1001.
        /// </summary>
        public string ErrorCodeId => MediaErrorCatalog.Get(ErrorCode).Code;
    }
}
