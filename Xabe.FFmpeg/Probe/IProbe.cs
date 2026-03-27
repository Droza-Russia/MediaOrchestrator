using System.Threading;
using System.Threading.Tasks;

namespace Xabe.FFmpeg
{
    /// <summary>
    ///     Позволяет подготовить и запустить IProbe.
    /// </summary>
    public interface IProbe
    {
        /// <summary>
        /// Запускает probe и возвращает результат из консоли.
        /// </summary>
        /// <param name="args">Аргументы, передаваемые в FFprobe.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Вывод консоли в заданном формате.</returns>
        Task<string> Start(string args, CancellationToken cancellationToken = default);
    }
}
