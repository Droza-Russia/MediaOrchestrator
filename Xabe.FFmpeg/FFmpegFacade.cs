using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg.Streams.SubtitleStream;

namespace Xabe.FFmpeg
{
    /// <summary> 
    ///     Обертка для FFmpeg
    /// </summary>
    public abstract partial class FFmpeg
    {
        /// <summary>
        ///     Директория, содержащая FFmpeg и FFprobe
        /// </summary>
        public static string ExecutablesPath { get; private set; }

        /// <summary>
        ///     Метод фильтрации для поиска файлов FFmpeg и FFprobe
        /// </summary>
        public static FileNameFilterMethod FilterMethod { get; private set; }

        /// <summary>
        ///     Выбирает, должен ли метод фильтрации учитывать регистр
        ///     Это будет использоваться для сравнения имен файлов
        /// </summary>
        public static IFormatProvider FormatProvider { get; private set; }

        /// <summary>
        ///     Получает новый экземпляр Conversion.
        /// </summary>
        /// <returns>Объект IConversion.</returns>
        public static Conversions Conversions = new Conversions();

        /// <summary>
        ///     Получает MediaInfo из файла
        /// </summary>
        /// <param name="filePath">Полный путь к файлу</param>
        /// <exception cref="ArgumentException">Файл не существует</exception>
        public static async Task<IMediaInfo> GetMediaInfo(string fileName)
        {
            return await MediaInfo.Get(fileName);
        }

        /// <summary>
        ///     Получает MediaInfo из файла
        /// </summary>
        /// <param name="filePath">Полный путь к файлу</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <exception cref="ArgumentException">Файл не существует</exception>
        /// <exception cref="TaskCanceledException">Операция занимает слишком много времени</exception>
        public static async Task<IMediaInfo> GetMediaInfo(string fileName, CancellationToken token)
        {
            return await MediaInfo.Get(fileName, token);
        }

        /// <summary>
        ///     Устанавливает путь к директории, содержащей FFmpeg и FFprobe
        /// </summary>
        /// <param name="directoryWithFFmpegAndFFprobe"></param>
        /// <param name="ffmpegExeutableName">Имя исполняемого файла FFmpeg</param>
        /// <param name="ffprobeExecutableName">Имя исполняемого файла FFprobe</param>
        /// <param name="filteringMethod">Выбирает метод сравнения имен файлов</param>
        /// <param name="filteringMethodCaseSensitive">Выбирает, должен ли фильтр учитывать регистр</param>
        public static void SetExecutablesPath(string directoryWithFFmpegAndFFprobe, string ffmpegExeutableName = "ffmpeg", string ffprobeExecutableName = "ffprobe", FileNameFilterMethod filteringMethod = FileNameFilterMethod.Contains, IFormatProvider formatprovider = null)
        {
            ExecutablesPath = directoryWithFFmpegAndFFprobe == null ? null : new DirectoryInfo(directoryWithFFmpegAndFFprobe).FullName;
            FilterMethod = filteringMethod;
            FormatProvider = formatprovider ?? CultureInfo.CurrentCulture;
            _ffmpegExecutableName = ffmpegExeutableName;
            _ffprobeExecutableName = ffprobeExecutableName;
        }

        /// <summary>
        ///     Получает доступные аудио и видео устройства (например, камеры или микрофоны)
        /// </summary>
        /// <returns>Список доступных устройств</returns>
        internal static async Task<Device[]> GetAvailableDevices()
        {
            return await Conversion.GetAvailableDevices();
        }
    }

    public class Conversions
    {
        /// <summary>
        ///     Получает новый экземпляр Conversion.
        /// </summary>
        /// <returns>Объект IConversion.</returns>
        public IConversion New()
        {
            return Conversion.New();
        }

        /// <summary>
        ///     Доступ к готовым сценариям конвертации.
        /// </summary>
        /// <returns>Объект Snippets.</returns>
        public readonly Snippets FromSnippet = new Snippets();

        internal Conversions()
        {

        }
    }

    public class Snippets
    {
        internal Snippets()
        {

        }

        /// <summary>
        ///     Извлекает аудио из файла
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной видеопоток</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ExtractAudio(string inputPath, string outputPath)
        {
            return await Conversion.ExtractAudio(inputPath, outputPath);
        }

        /// <summary>
        ///     Добавляет аудиопоток к видеофайлу
        /// </summary>
        /// <param name="videoPath">Видео</param>
        /// <param name="audioPath">Аудио</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> AddAudio(string videoPath, string audioPath, string outputPath)
        {
            return await Conversion.AddAudio(videoPath, audioPath, outputPath);
        }

        /// <summary>
        ///     Конвертирует файл в MP4
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToMp4(string inputPath, string outputPath)
        {
            return await Conversion.ToMp4(inputPath, outputPath);
        }

        /// <summary>
        ///     Конвертирует файл в TS
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToTs(string inputPath, string outputPath)
        {
            return await Conversion.ToTs(inputPath, outputPath);
        }

        /// <summary>
        ///     Конвертирует файл в OGV
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToOgv(string inputPath, string outputPath)
        {
            return await Conversion.ToOgv(inputPath, outputPath);
        }

        /// <summary>
        ///     Конвертирует файл в WebM
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToWebM(string inputPath, string outputPath)
        {
            return await Conversion.ToWebM(inputPath, outputPath);
        }

        /// <summary>
        ///     Конвертирует видеопоток изображений в gif
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной путь</param>
        /// <param name="loop">Количество повторов</param>
        /// <param name="delay">Задержка между повторами (в секундах)</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ToGif(string inputPath, string outputPath, int loop, int delay = 0)
        {
            return await Conversion.ToGif(inputPath, outputPath, loop, delay);
        }

        /// <summary>
        ///     Конвертирует один файл в другой с целевым форматом, используя аппаратное ускорение (если возможно). Использует cuvid. Работает только на Windows/Linux с видеокартой NVidia.
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу</param>
        /// <param name="outputFilePath">Путь к файлу</param>
        /// <param name="hardwareAccelerator">Аппаратный ускоритель. Список всех доступных ускорителей для вашей системы - "ffmpeg -hwaccels"</param>
        /// <param name="decoder">Кодек, используемый для декодирования входного видео (например, h264_cuvid)</param>
        /// <param name="encoder">Кодек, используемый для кодирования выходного видео (например, h264_nvenc)</param>
        /// <param name="device">Номер устройства (0 = видеокарта по умолчанию), если видеокарт больше одной.</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> ConvertWithHardware(string inputFilePath, string outputFilePath, HardwareAccelerator hardwareAccelerator, VideoCodec decoder, VideoCodec encoder, int device = 0)
        {
            return await Conversion.ConvertWithHardwareAsync(inputFilePath, outputFilePath, hardwareAccelerator, decoder, encoder, device);
        }

        /// <summary>
        ///     Добавляет субтитры к видеопотоку
        /// </summary>
        /// <param name="inputPath">Видео</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="subtitlesPath">Субтитры</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> BurnSubtitle(string inputPath, string outputPath, string subtitlesPath)
        {
            return await Conversion.AddSubtitlesAsync(inputPath, outputPath, subtitlesPath);
        }

        /// <summary>
        ///     Добавляет субтитры к файлу. Они будут добавлены как новый поток, поэтому если вы хотите встроить субтитры в видео, используйте
        ///     метод BurnSubtitle.
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной путь</param>
        /// <param name="subtitlePath">Путь к файлу субтитров в формате .srt</param>
        /// <param name="language">Код языка в ISO 639. Пример: "eng", "pol", "pl", "de", "ger"</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> AddSubtitle(string inputPath, string outputPath, string subtitlePath, string language = null)
        {
            return await Conversion.AddSubtitleAsync(inputPath, outputPath, subtitlePath, language);
        }

        /// <summary>
        ///     Добавляет субтитры в файл как новый поток.
        ///     Если нужно встроить субтитры в видео, используйте метод BurnSubtitle.
        /// </summary>
        /// <param name="inputPath">Входной путь.</param>
        /// <param name="outputPath">Выходной путь.</param>
        /// <param name="subtitlePath">Путь к файлу субтитров в формате .srt.</param>
        /// <param name="subtitleCodec">Кодек субтитров для кодирования субтитров</param>
        /// <param name="language">Код языка в ISO 639. Пример: "eng", "pol", "pl", "de", "ger".</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversion> AddSubtitle(string inputPath, string outputPath, string subtitlePath, SubtitleCodec subtitleCodec, string language = null)
        {
            return await Conversion.AddSubtitleAsync(inputPath, outputPath, subtitlePath, subtitleCodec, language);
        }

        /// <summary>
        ///     Встраивает водяной знак в видео
        /// </summary>
        /// <param name="inputPath">Входной путь к видео</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="inputImage">Водяной знак</param>
        /// <param name="position">Позиция водяного знака</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> SetWatermark(string inputPath, string outputPath, string inputImage, Position position)
        {
            return await Conversion.SetWatermarkAsync(inputPath, outputPath, inputImage, position);
        }

        /// <summary>
        ///     Извлекает видео из файла
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной аудиопоток</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ExtractVideo(string inputPath, string outputPath)
        {
            return await Conversion.ExtractVideoAsync(inputPath, outputPath);
        }

        /// <summary>
        ///     Сохраняет снимок видео
        /// </summary>
        /// <param name="inputPath">Видео</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="captureTime">Временной интервал снимка</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> Snapshot(string inputPath, string outputPath, TimeSpan captureTime)
        {
            return await Conversion.SnapshotAsync(inputPath, outputPath, captureTime);
        }

        /// <summary>
        ///     Изменяет размер видео
        /// </summary>
        /// <param name="inputPath">Входной путь</param>
        /// <param name="outputPath">Выходной путь</param>
        /// <param name="width">Ожидаемая ширина</param>
        /// <param name="height">Ожидаемая высота</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> ChangeSize(string inputPath, string outputPath, int width, int height)
        {
            return await Conversion.ChangeSizeAsync(inputPath, outputPath, width, height);
        }

        /// <summary>
        ///     Изменяет размер видео.
        /// </summary>
        /// <param name="inputPath">Входной путь.</param>
        /// <param name="outputPath">Выходной путь.</param>
        /// <param name="size">Ожидаемый размер</param>
        /// <returns>Результат конвертации.</returns>
        public async Task<IConversion> ChangeSize(string inputPath, string outputPath, VideoSize size)
        {
            return await Conversion.ChangeSizeAsync(inputPath, outputPath, size);
        }

        /// <summary>
        ///     Получает часть видео
        /// </summary>
        /// <param name="inputPath">Видео</param>
        /// <param name="outputPath">Выходной файл</param>
        /// <param name="startTime">Начальная точка</param>
        /// <param name="duration">Длительность нового видео</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> Split(string inputPath, string outputPath, TimeSpan startTime, TimeSpan duration)
        {
            return await Conversion.SplitAsync(inputPath, outputPath, startTime, duration);
        }

        /// <summary>
        /// Сохраняет поток M3U8
        /// </summary>
        /// <param name="uri">Uri потока</param>
        /// <param name="outputPath">Выходной путь</param>
        /// <param name="duration">Длительность потока</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> SaveM3U8Stream(Uri uri, string outputPath, TimeSpan? duration = null)
        {
            return await Conversion.SaveM3U8StreamAsync(uri, outputPath, duration);
        }

        /// <summary>
        ///     Объединяет несколько входных видео.
        /// </summary>
        /// <param name="output">Объединенные входные видео</param>
        /// <param name="inputVideos">Видео для добавления</param>
        /// <returns>Результат конвертации</returns>
        public async Task<IConversion> Concatenate(string output, params string[] inputVideos)
        {
            return await Conversion.Concatenate(output, inputVideos);
        }

        /// <summary>
        ///     Конвертирует один файл в другой с целевым форматом.
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу</param>
        /// <param name="outputFilePath">Путь к файлу</param>
        /// <param name="keepSubtitles">Сохранять ли субтитры в выходном видео</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> Convert(string inputFilePath, string outputFilePath, bool keepSubtitles = false)
        {
            return await Conversion.ConvertAsync(inputFilePath, outputFilePath, keepSubtitles);
        }

        /// <summary>
        ///     Транскодирует один файл в другой с целевым форматом и кодеками.
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу</param>
        /// <param name="outputFilePath">Путь к файлу</param>
        /// <param name="audioCodec">Аудиокодек для транскодирования входа</param>
        /// <param name="videoCodec">Видеокодек для транскодирования входа</param>
        /// <param name="videoCodec">Кодек субтитров для транскодирования входа</param>
        /// <param name="keepSubtitles">Сохранять ли субтитры в выходном видео</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> Transcode(string inputFilePath, string outputFilePath, VideoCodec videoCodec, AudioCodec audioCodec, SubtitleCodec subtitleCodec, bool keepSubtitles = false)
        {
            return await Conversion.TranscodeAsync(inputFilePath, outputFilePath, videoCodec, audioCodec, subtitleCodec, keepSubtitles);
        }

        /// <summary>
        /// Генерирует визуализацию аудиопотока с использованием фильтра 'showfreqs'
        /// </summary>
        /// <param name="inputPath">Путь к входному файлу, содержащему аудиопоток для визуализации</param>
        /// <param name="outputPath">Путь для вывода визуализированного аудиопотока</param>
        /// <param name="size">Размер выходного видеопотока</param>
        /// <param name="pixelFormat">Формат пикселей выхода (по умолчанию yuv420p)</param>
        /// <param name="mode">Режим визуализации (по умолчанию bar)</param>
        /// <param name="amplitudeScale">Шкала частоты (по умолчанию lin)</param>
        /// <param name="frequencyScale">Шкала амплитуды (по умолчанию log)</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> VisualiseAudio(string inputPath, string outputPath, VideoSize size,
            PixelFormat pixelFormat = PixelFormat.yuv420p,
            VisualisationMode mode = VisualisationMode.bar,
            AmplitudeScale amplitudeScale = AmplitudeScale.lin,
            FrequencyScale frequencyScale = FrequencyScale.log)
        {
            return await Conversion.VisualiseAudio(inputPath, outputPath, size, pixelFormat, mode, amplitudeScale, frequencyScale);
        }

        /// <summary>
        ///     Зацикливает файл бесконечно в потоки RTSP с некоторыми параметрами по умолчанию, такими как: -re, -preset ultrafast
        /// </summary>
        /// <param name="inputFilePath">Путь к файлу</param>
        /// <param name="rtspServerUri">Uri RTSP сервера в формате: rtsp://127.0.0.1:8554/name</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> SendToRtspServer(string inputFilePath, Uri rtspServerUri)
        {
            return await Conversion.SendToRtspServer(inputFilePath, rtspServerUri);
        }

        /// <summary>
        ///     Отправляет рабочий стол бесконечно в потоки RTSP с некоторыми параметрами по умолчанию, такими как: -re, -preset ultrafast
        /// </summary>
        /// <param name="rtspServerUri">Uri RTSP сервера в формате: rtsp://127.0.0.1:8554/name</param>
        /// <returns>Объект IConversion</returns>
        public async Task<IConversion> SendDesktopToRtspServer(Uri rtspServerUri)
        {
            return Conversion.SendDesktopToRtspServer(rtspServerUri);
        }
    }
}
