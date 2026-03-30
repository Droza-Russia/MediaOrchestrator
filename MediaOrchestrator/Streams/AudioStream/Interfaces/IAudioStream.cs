using System;

namespace MediaOrchestrator
{
    /// <summary>
    ///     Аудиопоток.
    /// </summary>
    public interface IAudioStream : IStream
    {
        /// <summary>
        ///     Длительность.
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        ///     Битрейт.
        /// </summary>
        long Bitrate { get; }

        /// <summary>
        ///     Частота дискретизации.
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        ///     Количество каналов.
        /// </summary>
        int Channels { get; }

        /// <summary>
        /// Язык.
        /// </summary>
        string Language { get; }

        /// <summary>
        /// Заголовок.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// По умолчанию.
        /// </summary>
        int? Default { get; }

        /// <summary>
        /// Принудительно.
        /// </summary>
        int? Forced { get; }

        /// <summary>
        ///     Переводит поток в режим копирования с оригинальным кодеком.
        /// </summary>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream CopyStream();

        /// <summary>
        ///     Разворачивает аудиопоток в обратном направлении.
        /// </summary>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream Reverse();

        /// <summary>
        ///     Устанавливает количество аудиоканалов (опция -ac).
        /// </summary>
        /// <param name="channels">Количество каналов в выходном потоке.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetChannels(int channels);

        /// <summary>
        ///     Устанавливает аудиокодек.
        /// </summary>
        /// <param name="codec">Аудиокодек.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetCodec(AudioCodec codec);

        /// <summary>
        ///     Устанавливает аудиокодек.
        /// </summary>
        /// <param name="codec">Аудиокодек.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetCodec(string codec);

        /// <summary>
        ///     Устанавливает фильтр.
        /// </summary>
        /// <param name="filter">Фильтр.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetBitstreamFilter(BitstreamFilter filter);

        /// <summary>
        ///     Устанавливает битрейт аудиопотока.
        /// </summary>
        /// <param name="bitRate">Битрейт аудиопотока в байтах.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetBitrate(long bitRate);

        /// <summary>
        ///     Устанавливает диапазон битрейта аудиопотока.
        /// </summary>
        /// <param name="minBitrate">Минимальный битрейт в битах.</param>
        /// <param name="maxBitrate">Максимальный битрейт в битах.</param>
        /// <param name="buffersize">Размер буфера в битах.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetBitrate(long minBitrate, long maxBitrate, long bufferSize);

        /// <summary>
        ///     Устанавливает частоту дискретизации аудиопотока (опция -ar).
        /// </summary>
        /// <param name="sampleRate">Частота дискретизации аудиопотока в Гц.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetSampleRate(int sampleRate);

        /// <summary>
        ///     Изменяет скорость воспроизведения.
        /// </summary>
        /// <param name="multiplicator">Множитель скорости (0.5 - 2.0). Для удвоения скорости установите 2.0.</param>
        /// <returns>Объект IAudioStream.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Когда скорость не находится в диапазоне от 0.5 до 2.0.</exception>
        IAudioStream ChangeSpeed(double multiplicator);

        /// <summary>
        ///     Возвращает фрагмент аудио.
        /// </summary>
        /// <param name="startTime">Начальная точка.</param>
        /// <param name="duration">Длительность нового фрагмента.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream Split(TimeSpan startTime, TimeSpan duration);

        /// <summary>
        ///     Перемещает позицию чтения во входном файле (аргумент -ss).
        /// </summary>
        /// <param name="seek">Позиция.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetSeek(TimeSpan? seek);

        /// <summary>
        ///     Устанавливает фильтр.
        /// </summary>
        /// <param name="filter">Фильтр.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetBitstreamFilter(string filter);

        /// <summary>
        /// Устанавливает формат входного файла через опцию -f перед именем входного файла.
        /// </summary>
        /// <param name="inputFormat">Формат входного файла.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetInputFormat(string inputFormat);

        /// <summary>
        /// Устанавливает формат входного файла через опцию -f перед именем входного файла.
        /// </summary>
        /// <param name="inputFormat">Формат входного файла.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetInputFormat(Format inputFormat);

        /// <summary>
        ///     Параметр "-re". Читает входные данные с нативной частотой кадров.
        ///     Обычно используется для имитации устройства захвата или live-потока (например, при чтении из файла).
        ///     Не рекомендуется использовать с реальными устройствами захвата или live-потоками, так как это может вызывать потери пакетов.
        ///     По умолчанию ffmpeg читает входные данные максимально быстро; эта опция замедляет чтение до нативной частоты.
        ///     Полезно для вывода в реальном времени (например, стриминга).
        /// </summary>
        /// <param name="readInputAtNativeFrameRate">Читать вход с нативной частотой кадров. При False используется значение по умолчанию.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream UseNativeInputRead(bool readInputAtNativeFrameRate);

        /// <summary>
        ///     Параметр "-stream_loop". Устанавливает количество повторов входного потока.
        /// </summary>
        /// <param name="loopCount">0 - без повтора, -1 - бесконечный повтор.</param>
        /// <returns>Объект IAudioStream.</returns>
        IAudioStream SetStreamLoop(int loopCount);
    }
}
