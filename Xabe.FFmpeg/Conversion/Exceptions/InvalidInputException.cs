using System.IO;

namespace Xabe.FFmpeg.Exceptions
{
    /// <inheritdoc />
    /// <summary>
    ///     Исключение, выбрасываемое, когда входной файл не существует.
    /// </summary>
    public class InvalidInputException : FileNotFoundException
    {
        /// <summary>
        ///     Исключение, выбрасываемое, когда входной файл не существует.
        /// </summary>
        /// <param name="msg">Текст сообщения об ошибке.</param>
        public InvalidInputException(string msg) : base(msg)
        {
        }
    }
}
