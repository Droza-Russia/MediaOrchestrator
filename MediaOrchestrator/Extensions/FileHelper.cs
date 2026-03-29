using System;
using System.IO;

namespace MediaOrchestrator.Extensions
{
    internal static class FileHelper
    {
        public static void SafeDeleteFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }

        public static void SafeDeleteTempFiles(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            SafeDeleteFile(path);

            try
            {
                string tempWithExt = path + ".tmp";
                if (File.Exists(tempWithExt))
                {
                    File.Delete(tempWithExt);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }

        public static T ExecuteFileOperation<T>(Func<T> operation, T fallbackValue)
        {
            try
            {
                return operation();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return fallbackValue;
            }
        }

        public static bool TryGetFileInfo(string path, out FileInfo fileInfo)
        {
            fileInfo = ExecuteFileOperation(() => new FileInfo(path), null);
            return fileInfo != null && fileInfo.Exists;
        }

        public static void AtomicWriteWithCleanup(string tempPath, string finalPath, Action writeAction)
        {
            writeAction();

            if (File.Exists(finalPath))
            {
                SafeDeleteFile(finalPath);
            }

            File.Move(tempPath, finalPath);
        }
    }
}
