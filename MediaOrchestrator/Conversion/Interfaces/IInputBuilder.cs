using System;
using System.Collections.Generic;
using System.IO;

namespace MediaOrchestrator
{
    /// <summary>
    /// Интерфейс для формирования списка входных файлов,
    /// используемого вместе с методом BuildVideoFromImages.
    /// </summary>
    internal interface IInputBuilder
    {
        /// <summary>
        /// Список файлов, используемых как входные данные.
        /// </summary>
        List<FileInfo> FileList { get; }

        /// <summary>
        /// Подготавливает список файлов для входа: переименовывает их в единый формат
        /// и копирует во временную директорию.
        /// </summary>
        /// <param name="files">Список путей к файлам для подготовки.</param>
        /// <param name="directory">Путь к временной директории с подготовленными файлами.</param>
        /// <returns>Делегат для генерации входного аргумента на основе списка файлов.</returns>
        Func<string, string> PrepareInputFiles(List<string> files, out string directory);
    }
}
