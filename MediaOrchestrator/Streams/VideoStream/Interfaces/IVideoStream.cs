using System;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Видеопоток
    /// </summary>
    public interface IVideoStream : IStream
    {
        /// <summary>
        ///     Длительность
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        ///     Ширина
        /// </summary>
        int Width { get; }

        /// <summary>
        ///     Высота
        /// </summary>
        int Height { get; }

        /// <summary>
        ///     Частота кадров
        /// </summary>
        double Framerate { get; }

        /// <summary>
        ///     Соотношение сторон экрана
        /// </summary>
        string Ratio { get; }

        /// <summary>
        ///     Битрейт видео
        /// </summary>
        long Bitrate { get; }

        /// <summary>
        ///     По умолчанию
        /// </summary>
        int? Default { get; }

        /// <summary>
        ///     Заголовок потока.
        /// </summary>
        string Title { get; }

        /// <summary>
        ///     Принудительно
        /// </summary>
        int? Forced { get; }

        /// <summary>
        ///     Формат пикселей
        /// </summary>
        string PixelFormat { get; }

        /// <summary>
        ///     Угол поворота
        /// </summary>
        int? Rotation { get; }

        /// <summary>
        ///     Поворачивает видео
        /// </summary>
        /// <param name="rotateDegrees">Тип поворота</param>
        /// <returns>IVideoStream</returns>
        IVideoStream Rotate(RotateDegrees rotateDegrees);

        /// <summary>
        ///     Добавляет черные полосы к видео до указанной высоты и ширины.
        /// </summary>
        /// <param name="width">Ширина</param>
        /// <param name="height">Высота</param>
        /// <returns>IVideoStream</returns>
        IVideoStream Pad(int width, int height);

        /// <summary>
        ///     Изменяет скорость видео
        /// </summary>
        /// <param name="multiplicator">Значение скорости. (0.5 - 2.0). Чтобы удвоить скорость, установите значение 2.0</param>
        /// <returns>IVideoStream</returns>
        /// <exception cref="ArgumentOutOfRangeException">Когда скорость не находится в диапазоне от 0.5 до 2.0.</exception>
        IVideoStream ChangeSpeed(double multiplicator);

        /// <summary>
        ///     Встраивает водяной знак в видео
        /// </summary>
        /// <param name="imagePath">Водяной знак</param>
        /// <param name="position">Позиция водяного знака</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetWatermark(string imagePath, Position position);

        /// <summary>
        ///     Накладывает на видео текст у правого края кадра (фильтр drawtext).
        /// </summary>
        /// <param name="text">Текст подписи.</param>
        /// <param name="fontColor">Цвет шрифта (например white, yellow или 0xFFFFFF).</param>
        /// <param name="fontSize">Размер шрифта.</param>
        /// <param name="marginRight">Отступ от правого края в пикселях.</param>
        /// <param name="marginY">
        ///     Для <see cref="DrawTextVerticalAlign.Top"/> — отступ сверху; для <see cref="DrawTextVerticalAlign.Bottom"/> — отступ снизу;
        ///     для <see cref="DrawTextVerticalAlign.Center"/> не используется.
        /// </param>
        /// <param name="verticalAlign">Вертикальное положение у правой кромки.</param>
        /// <param name="fontFilePath">Необязательный путь к TTF/OTF (если не задан — шрифт по умолчанию MediaOrchestrator).</param>
        /// <returns>Текущий видеопоток.</returns>
        IVideoStream SetRightSideDrawText(
            string text,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null);

        /// <summary>
        ///     Накладывает у правого края динамическое время по меткам PTS в формате ЧЧ:ММ:СС (выражение drawtext %{pts:hms}).
        ///     Для таймкода с фиксированным fps и полем «кадр» (SMPTE-подобный) используйте <see cref="SetRightSideSmpteTimecodeOverlay"/>.
        /// </summary>
        /// <param name="prefix">Необязательный префикс перед временем.</param>
        /// <param name="suffix">Необязательный суффикс после времени.</param>
        /// <param name="useLocalWallClock">
        ///     Если true — подставляется %{localtime} (локальное время системы при кодировании), иначе — время по PTS.
        /// </param>
        /// <param name="fontColor">Цвет шрифта.</param>
        /// <param name="fontSize">Размер шрифта.</param>
        /// <param name="marginRight">Отступ от правого края в пикселях.</param>
        /// <param name="marginY">Отступ сверху/снизу (см. <see cref="SetRightSideDrawText"/>).</param>
        /// <param name="verticalAlign">Вертикальное выравнивание.</param>
        /// <param name="fontFilePath">Необязательный путь к шрифту.</param>
        /// <returns>Текущий видеопоток.</returns>
        IVideoStream SetRightSidePtsTimeOverlay(
            string prefix = null,
            string suffix = null,
            bool useLocalWallClock = false,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null);

        /// <summary>
        ///     Накладывает у правого края SMPTE-подобный таймкод (опции drawtext timecode/rate), считая от заданного старта и с заданным fps.
        /// </summary>
        /// <param name="startTimecode">Начальное значение, обычно HH:MM:SS:ff (четверка полей через двоеточие).</param>
        /// <param name="frameRate">Частота кадров для инкремента кадрового поля (например 25 или 29.97).</param>
        /// <param name="fontColor">Цвет шрифта.</param>
        /// <param name="fontSize">Размер шрифта.</param>
        /// <param name="marginRight">Отступ от правого края.</param>
        /// <param name="marginY">Отступ сверху/снизу.</param>
        /// <param name="verticalAlign">Вертикальное выравнивание.</param>
        /// <param name="fontFilePath">Необязательный путь к шрифту.</param>
        /// <returns>Текущий видеопоток.</returns>
        IVideoStream SetRightSideSmpteTimecodeOverlay(
            string startTimecode = "00:00:00:00",
            double frameRate = 25,
            string fontColor = "white",
            int fontSize = 24,
            int marginRight = 20,
            int marginY = 16,
            DrawTextVerticalAlign verticalAlign = DrawTextVerticalAlign.Center,
            string fontFilePath = null);

        /// <summary>
        ///     Обращает видео
        /// </summary>
        /// <returns>IVideoStream</returns>
        IVideoStream Reverse();

        /// <summary>
        ///     Устанавливает флаги для конвертации (опция -flags)
        /// </summary>
        /// <param name="flags">Флаги для использования</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetFlags(params Flag[] flags);

        /// <summary>
        ///     Устанавливает флаги для конвертации (опция -flags)
        /// </summary>
        /// <param name="flags">Флаги для использования</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetFlags(params string[] flags);

        /// <summary>
        ///     Устанавливает частоту кадров видео (опция -r)
        /// </summary>
        /// <param name="framerate">Частота кадров в FPS</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetFramerate(double framerate);

        /// <summary>
        ///     Устанавливает битрейт видео (опция -b:v)
        /// </summary>
        /// <param name="minBitrate">Битрейт в битах</param>
        /// <param name="maxBitrate">Битрейт в битах</param>
        /// <param name="buffersize">Размер буфера в битах</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetBitrate(long minBitrate, long maxBitrate, long bufferSize);

        /// <summary>
        ///     Устанавливает битрейт видео (опция -b:v)
        /// </summary>
        /// <param name="bitrate">Битрейт в битах</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetBitrate(long bitrate);

        /// <summary>
        ///     Устанавливает размер видео
        /// </summary>
        /// <param name="size">VideoSize</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetSize(VideoSize size);

        /// <summary>
        ///     Устанавливает размер видео.
        /// </summary>
        /// <param name="width">Ширина</param>
        /// <param name="height">Высота</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetSize(int width, int height);

        /// <summary>
        ///     Устанавливает видеокодек
        /// </summary>
        /// <param name="codec">Видеокодек</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetCodec(VideoCodec codec);

        /// <summary>
        ///     Устанавливает видеокодек
        /// </summary>
        /// <param name="codec">Видеокодек</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetCodec(string codec);

        /// <summary>
        ///     Устанавливает поток для копирования с оригинальным кодеком
        /// </summary>
        /// <returns>IVideoStream</returns>
        IVideoStream CopyStream();

        /// <summary>
        ///     Устанавливает фильтр.
        /// </summary>
        /// <param name="filter">Фильтр.</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetBitstreamFilter(BitstreamFilter filter);

        /// <summary>
        ///     Зацикливает входной поток.(-loop)
        /// </summary>
        /// <param name="count">Количество повторов</param>
        /// <param name="delay">Задержка между повторами (в секундах)</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetLoop(int count, int delay = 0);

        /// <summary>
        ///     Устанавливает количество выходных кадров
        /// </summary>
        /// <param name="number">Количество кадров</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetOutputFramesCount(int number);

        /// <summary>
        ///     Переходит к позиции во входном файле. (аргумент -ss)
        /// </summary>
        /// <param name="seek">Позиция</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetSeek(TimeSpan seek);

        /// <summary>
        ///     Встраивает субтитры в файл
        /// </summary>
        /// <param name="subtitlePath">Путь к файлу субтитров в формате .srt</param>
        /// <param name="encode">Устанавливает кодировку символов входных субтитров. Полезно только если не UTF-8.</param>
        /// <param name="style">
        ///     Переопределяет параметры стиля по умолчанию или информации о скрипте субтитров. Принимает строку, содержащую
        ///     пары формата ASS стиля KEY=VALUE, разделенные запятыми
        /// </param>
        /// <returns>IVideoStream</returns>
        IVideoStream AddSubtitles(string subtitlePath, string encode = null, string style = null);

        /// <summary>
        ///     Встраивает субтитры в файл.
        /// </summary>
        /// <param name="subtitlePath">Путь к файлу субтитров в формате .srt.</param>
        /// <param name="encode">Кодировка символов входных субтитров. Нужна только если не UTF-8.</param>
        /// <param name="style">
        ///     Переопределяет параметры стиля по умолчанию или script info субтитров.
        ///     Принимает строку с парами формата ASS вида KEY=VALUE, разделенными запятыми.
        /// </param>
        /// <param name="originalSize">
        ///     Указывает размер оригинального видео, для которого был составлен ASS стиль. Это
        ///     необходимо для правильного масштабирования шрифтов, если соотношение сторон было изменено.
        /// </param>
        /// <returns>IVideoStream</returns>
        IVideoStream AddSubtitles(string subtitlePath, VideoSize originalSize, string encode = null, string style = null);

        /// <summary>
        ///     Получает часть видео
        /// </summary>
        /// <param name="startTime">Начальная точка</param>
        /// <param name="duration">Длительность нового видео</param>
        /// <returns>IVideoStream</returns>
        IVideoStream Split(TimeSpan startTime, TimeSpan duration);

        /// <summary>
        ///     Устанавливает фильтр
        /// </summary>
        /// <param name="filter">Фильтр</param>
        /// <returns>IVideoStream</returns>
        IVideoStream SetBitstreamFilter(string filter);

        /// <summary>
        /// Устанавливает формат для входного файла, используя опцию -f перед именем входного файла
        /// </summary>
        /// <param name="inputFormat">Входной формат для установки</param>
        /// <returns>Объект IConversion</returns>
        IVideoStream SetInputFormat(string inputFormat);

        /// <summary>
        /// Устанавливает формат для входного файла, используя опцию -f перед именем входного файла
        /// </summary>
        /// <param name="inputFormat">Входной формат для установки</param>
        /// <returns>Объект IConversion</returns>
        IVideoStream SetInputFormat(Format inputFormat);

        /// <summary>
        ///     Параметр "-re". Читает входные данные с нативной частотой кадров. В основном используется для имитации устройства захвата или живого входного потока (например, при чтении из файла). Не следует использовать с реальными устройствами захвата или живыми входными потоками (где это может вызвать потерю пакетов). По умолчанию ffmpeg пытается читать входные данные как можно быстрее. Эта опция замедлит чтение входных данных до нативной частоты кадров входных данных. Полезна для вывода в реальном времени (например, живая трансляция).
        /// </summary>
        /// <param name="readInputAtNativeFrameRate">Читать входные данные с нативной частотой кадров. False устанавливает параметр в значение по умолчанию.</param>
        /// <returns>Объект IConversion</returns>
        IVideoStream UseNativeInputRead(bool readInputAtNativeFrameRate);

        /// <summary>
        ///     Параметр "-stream_loop". Устанавливает количество раз, которое входной поток должен быть зациклен. 
        /// </summary>
        /// <param name="loopCount">Цикл 0 означает отсутствие цикла, цикл -1 означает бесконечный цикл.</param>
        /// <returns>Объект IConversion</returns>
        IVideoStream SetStreamLoop(int loopCount);
    }
}
