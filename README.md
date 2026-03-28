<!--
  Метаописание для зеркал / статических генераторов / копирования в сайт (GitHub README не вставляет это в <head>).
  <meta name="description" lang="ru" content="Xabe.FFMpeg.Custom — библиотека .NET Standard 2.0: обёртка FFmpeg и ffprobe, MediaInfo, конвертация, отмена, кэш. Глобальные лимиты FPS/аудио при инициализации для веба и потоковой конвертации. Бинарники — с учётом ОС; опционально hwaccel под платформу." />
  <meta name="description" lang="en" content="Xabe.FFMpeg.Custom — .NET Standard 2.0 library wrapping FFmpeg/ffprobe: MediaInfo, transcode, cancellation, cache. Binary resolution is OS-aware; optional hardware-acceleration profile adapts to the host OS and supported hwaccels." />
  <meta name="keywords" lang="ru" content="ffmpeg, ffprobe, обёртка ffmpeg, .NET, C#, netstandard, конвертация видео, конвертация аудио, медиа, MediaInfo, ffprobe C#, транскодирование, Xabe, NuGet, кроссплатформа, hwaccel, отмена операции, адаптация под ОС, аппаратное ускорение, автоопределение hwaccel" />
  <meta name="keywords" lang="en" content="ffmpeg, ffprobe, dotnet, csharp, netstandard2.0, video, audio, transcoding, MediaInfo, wrapper, Xabe, NuGet, cross-platform, hardware acceleration, async, CancellationToken, encode, decode, converter" />
  <meta name="keywords" content="localized errors, Russian English German, LocalizationLanguage, i18n exception messages" />
  <meta name="author" content="Xabe / custom fork" />
  <meta name="robots" content="index, follow" />
-->

# 🎬 Xabe.FFMpeg.Custom

**Кроссплатформенная обёртка над FFmpeg/ffprobe для .NET** — когда хочется кодировать видео из C#, а не из bash-скрипта длиной в четыре экрана. 🎞️

🔍 **Для поиска:** `ffmpeg` · `ffprobe` · `.NET` · `C#` · `NET Standard 2.0` · `MediaInfo` · `транскодирование` · `конвертация видео` · `обёртка FFmpeg` · `Xabe` · `NuGet` · `async` · `CancellationToken` · `кроссплатформа` · `macOS` · `Linux` · `Windows` · `hwaccel` · `VAAPI` · `NVENC` · **`LocalizationLanguage`** · **локализация ошибок (RU / EN / DE)** · **OS-aware binaries** · **аппаратное ускорение под ОС** · **глобальные лимиты выхода** · **веб-конвертация**

Этот репозиторий — доработанная ветка экосистемы [Xabe.FFmpeg](https://ffmpeg.xabe.net/index.html): те же идеи (флюентный API, `MediaInfo`, конвертации), плюс практичные улучшения для продакшена: **поиск и проверка бинарников с учётом операционной системы**, **адаптивное подключение аппаратного ускорения декода/кодека под железо и ОС**, **единоразовая настройка верхних границ выходного видео/аудио (FPS, sample rate, каналы)** — чтобы в веб-сервисах и пайплайнах «конвертация на лету» не раздувала битрейт и не требовала помнить лимиты в каждом вызове. Отмена, аккуратная работа с файлами и здравый смысл вокруг «а вдруг файл ещё качается». ☁️➡️📁

---

## 📑 Содержание

- [Ключевые слова и GitHub Topics](#seo-keywords)
- [Возможности](#features)
- [Требования](#requirements)
- [Установка](#install)
- [Быстрый старт](#quickstart)
- [Стиль API](#api-style)
- [Где взять FFmpeg](#ffmpeg-path)
- [Метаданные и «файл ещё не дорос»](#media-stable)
- [Конвертация и отмена](#conversion)
- [Сборка и пакет](#build)
- [Лицензия и благодарности](#license)

---

<a id="seo-keywords"></a>

## 🔎 Ключевые слова и GitHub Topics

Чтобы репозиторий лучше находился, добавьте в настройках GitHub **Topics** (темы), например:

`ffmpeg` · `ffprobe` · `dotnet` · `csharp` · `netstandard` · `video` · `audio` · `transcoding` · `media` · `converter` · `mediainfo` · `xabe` · `nuget-package` · `cross-platform` · `async` · `hardware-acceleration` · `os-adaptive` · `hwaccel-detection` · `aspnetcore` · `on-the-fly-transcoding`

Полный список формулировок для индексации (как обычный текст страницы): библиотека FFmpeg для C#, обёртка ffprobe, получение длительности и потоков видео файла, конвертация MP4 WebM MKV, отмена длительной кодировки, прогресс кодирования, нехватка места на диске при ffmpeg, поиск исполняемого файла ffmpeg в PATH, переменные окружения FFMPEG_EXECUTABLE, стабилизация файла перед чтением метаданных, загрузка файла ещё не завершена, Mac Apple Silicon ffmpeg, Linux VAAPI Windows NVIDIA, **локализация ошибок три языка русский английский немецкий**, LocalizationLanguage, **адаптивный резолв бинарников под Windows macOS Linux**, **сигнатура исполняемого файла для ОС**, **автоопределение hwaccels ffmpeg под платформу**, NVIDIA NVENC Intel QSV AMD AMF VideoToolbox, **ASP.NET веб-приложение конвертация видео на лету**, **MaxOutputVideoFrameRate MaxOutputAudioSampleRate MaxOutputAudioChannels**, SetGlobalOutputLimits.

---

<a id="features"></a>

## ✨ Возможности

| Область | Что вы получаете |
|--------|-------------------|
| 🔧 **Пути к бинарникам (адаптивно под ОС)** | Резолв **зависит от платформы**: типичные каталоги и суффиксы (в т.ч. `.exe` на Windows), поиск рядом с приложением с **именами папок под текущую ОС**, переменные окружения, **PATH**. Проверка, что найденный файл **действительно исполняемый на этой ОС** (`IsExecutable`). `SetExecutablesPath` — по желанию; иначе авто-поиск с кэшем (в т.ч. защита от пустого `PATH`). |
| 📊 **Метаданные** | `FFmpeg.GetMediaInfo` поверх ffprobe, кэш по пути/размеру/времени изменения, отмена через `CancellationToken`. |
| 🌐 **Лимиты выхода при инициализации** | В **`SetExecutablesPath`** (или позже **`FFmpeg.SetGlobalOutputLimits`**) задаются **верхние границы выхода**: частота кадров видео (`MaxOutputVideoFrameRate`), частота дискретизации и число каналов аудио (`MaxOutputAudioSampleRate`, `MaxOutputAudioChannels`). Они подмешиваются в аргументы **транскодирования с видео/аудио на выходе** — **один раз настроили в `Startup`, дальше не думаете** в каждом обработчике запроса. Удобно для **веб-приложений и очередей**, где нужна предсказуемая нагрузка и потоковая отдача без «случайного 120 fps с телефона». *Не распространяются на чистое извлечение/экспорт аудио, быстрый WAV и ряд сценариев без видео на выходе — см. XML-доки свойств.* |
| ⏳ **Загрузки с задержкой** | `MediaFileReadiness.WaitUntilStableAsync` и флаг `waitUntilFileStable` у `GetMediaInfo` — пока сторонний сервис дописывает файл, вы не стреляете ffprobe в полупустой контейнер. |
| 🔓 **Чтение входов** | Проверка сигнатуры для типичных расширений, `FileShare.Read`, асинхронное чтение заголовка с таймаутом (меньше сюрпризов от «особых» файлов). |
| 🎞️ **Конвертация** | Богатый набор сценариев (`FFmpeg.Conversions.*`), `CancellationToken` на цепочке, прогресс через `IProgress`, при ошибке/отмене можно **убрать незавершённый выходной файл** (после корректного завершения процесса). При включённом авто-HW цепочка **дополняется `-hwaccel` / декодером** с учётом профиля и кодеков **только там, где это уместно** (не ломает stream copy и явные настройки). |
| ⚠️ **Ошибки** | Тексты исключений и системных сообщений — **на трёх языках: русский, английский, немецкий** (`LocalizationLanguage` / `SetExecutablesPath` / `SetLocalizationLanguage`). Язык по умолчанию при настройке пути — русский. Все библиотечные ошибки сведены к общему базовому типу **`XabeFFmpegException`**, а файловые/потоковые/права доступа разнесены по более точным исключениям. |
| ⚡ **Железо (адаптивно под ОС и драйверы)** | По флагу в `SetExecutablesPath`: запуск **`ffmpeg -hwaccels`**, разбор вывода и выбор профиля **под вашу ОС** (в т.ч. **NVIDIA, Intel QSV, AMD AMF/D3D11, VAAPI, Video Toolbox** — как задокументировано в API). Дальше **`ApplyAutoHardwareAccelerationToConversions`** может подставлять аппаратный декод там, где включено пере-кодирование видео и профиль известен. Ручной режим — `UseHardwareAcceleration` / `ConvertWithHardware`. |

> 💻 **ОС и железо не абстракция, а контекст:** одна и та же сборка **netstandard2.0** везде, но **какие файлы считаются ffmpeg**, **откуда их ищут** и **какой hwaccel реалистичен** — решается **адаптивно** на машине пользователя, а не захардкожено «как в лаборатории».

> 📡 **Веб и «на лету»:** типичный сценарий — в `Program.cs` / `Startup` вы вызываете `SetExecutablesPath` с лимитами выхода (и при желании языком/HW). Дальше контроллер или фоновая задача просто дергает `Conversions.*` и `Start`: **потолок по FPS и аудио уже глобальный**, без дублирования магических чисел по проекту. Это не замена явному выбору кодека под продукт, но снимает головную боль «каждый аплоад утонул в лишнем качестве».

> 🌍 **Три языка ошибок и подсказок:** пользователь выбирает **Russian**, **English** или **German** — одна и та же ситуация (файл не найден, несовпадение сигнатуры, нехватка места, таймаут чтения и т.д.) описывается на выбранном языке, без смешения локалей в одном сообщении.

> 🎭 **Маленький дисклеймер с юмором:** библиотека не делает ваши исходные ролики лучше по сюжету. Только по кодекам.

> 🧱 **Состояние API:** основной проект уже заметно очищен от россыпи ffmpeg-строк. Для прикладного кода рекомендуется опираться на snippet/facade API и специализированные fluent-методы (`AddInput`, `MapAllStreams`, `SetAudioCodec`, `UseFilterGraph`, `SetTune`, `UseHardwareAcceleration`), а не собирать команду вручную через `AddParameter("-...")`.

> 🧩 **Структура проекта:** код разложен по семантике (`Interfaces`, `Implementations`, `Arguments`, `Filters`, `Inputs`, `Settings`, `Wrappers`, `Messages`, `Localization`, `Media`, `HardwareAcceleration`). Это не cosmetic move: так проще держать публичный API узким, а внутренние helper-слои — изолированными.

---

<a id="requirements"></a>

## 📋 Требования

- **.NET Standard 2.0** — подходит для .NET Framework 4.6.1+ и современного .NET. ✅
- Установленные **ffmpeg** и **ffprobe** (или путь к ним — см. ниже). 🎥

Зависимости пакета: **System.Text.Json** (диапазон версий задаётся в `.csproj`). 📎

---

<a id="install"></a>

## 📦 Установка

### NuGet (если выложите пакет в feed)

```bash
dotnet add package Xabe.FFMpeg.Custom
```

Или ссылка на проект в solution:

```xml
<ProjectReference Include="..\Xabe.FFmpeg\Xabe.FFmpeg.csproj" />
```

Текущая версия в репозитории: **1.0.3** (`PackageId`: `Xabe.FFMpeg.Custom`). 🏷️

### Локальная упаковка NuGet

Обычный `dotnet build` больше не делает `pack` автоматически, чтобы сборка и тесты оставались быстрыми. Для явной упаковки используйте один из скриптов:

```bash
./scripts/pack.sh
```

Или на PowerShell:

```powershell
./scripts/pack.ps1
```

По умолчанию пакет попадёт в `artifacts/nuget`.

---

<a id="quickstart"></a>

## 🚀 Быстрый старт

### 1️⃣ Явно указать каталог с бинарниками (рекомендуется в проде)

```csharp
using Xabe.FFmpeg;

FFmpeg.SetExecutablesPath(
    @"/usr/local/bin", // или @"C:\apps\ffmpeg"
    language: LocalizationLanguage.Russian, // или English, или German — тексты ошибок на выбранном языке
    maxOutputVideoFrameRate: 30,           // потолок FPS выходного видео (транскодирование)
    maxOutputAudioSampleRate: 48000,       // потолок sample rate выходного аудио (где применимо)
    maxOutputAudioChannels: 2,             // стерео для веба по умолчанию — не задавать в каждом запросе
    tryDetectHardwareAcceleration: false);

// Лимиты можно сменить без переопределения пути:
// FFmpeg.SetGlobalOutputLimits(maxOutputVideoFrameRate: 25, maxOutputAudioSampleRate: 44100, maxOutputAudioChannels: 2);

// Язык можно сменить и отдельно, без смены пути к бинарникам:
// FFmpeg.SetLocalizationLanguage(LocalizationLanguage.English);

// Аппаратное ускорение под вашу ОС / доступные hwaccels:
// tryDetectHardwareAcceleration: true в SetExecutablesPath
```

### 2️⃣ Ничего не указывать и надеяться на здравый смысл системы 🤞

Если `SetExecutablesPath` не вызывали, при первом использовании выполнится **`FFmpeg.EnsureExecutablesLocated()`** (или он вызовется из конструкторов обёрток / из `GetMediaInfo`). Поиск идёт по переменным окружения, типичным каталогам и **PATH**.

### 3️⃣ Метаданные

```csharp
using System.Linq;
using System.Threading;

var info = await FFmpeg.GetMediaInfo(@"C:\media\clip.mp4", CancellationToken.None);
Console.WriteLine($"Длительность: {info.Duration}, дорожек: {info.Streams.Count()}");
```

### 4️⃣ Конвертация в MP4

```csharp
using System.Threading;

var conversion = await FFmpeg.Conversions.ToMp4(
    @"C:\media\input.mov",
    @"C:\media\output.mp4",
    cancellationToken: CancellationToken.None);

var result = await conversion.Start(CancellationToken.None);
```

### 🕒 Таймкоды и надписи справа

Есть несколько быстрых хелперов для наложения текста/таймкода справа от кадра:

- `FFmpeg.BurnRightSideTextLabel`, `BurnRightSidePtsTimeLabel` и `BurnRightSideSmpteTimecode` — фасадные методы, которые берут вход, добавляют нужный `drawtext`+`overlay` и сохраняют результат без ручного `-vf`.
- Если нужен кастомный `Conversion`, возьмите `var info = await FFmpeg.GetMediaInfo(inputPath, cancellationToken);`, примените к нужному `IVideoStream` `SetRightSideDrawText`, `SetRightSidePtsTimeOverlay` или `SetRightSideSmpteTimecodeOverlay`, а затем `Conversion.New().AddStream(stream).SetOutput(outputPath);`.
- Все параметры (`fontColor`, `fontSize`, `marginRight`, `marginY`, `verticalAlign`, `fontFilePath`) управляются в методах; `prefix`/`suffix` или старт SMPTE позволяют отобразить нужную строку и таймкод.

Библиотека автоматически рассчитывает `drawtext`/`overlay` так, чтобы надпись не пересекалась с кадром (анкер по правому краю и вертикальное выравнивание через `DrawTextVerticalAlign`). При необходимости можно задать свой путь к TTF/OTF и изменить `marginY`/`marginRight` для адаптации к бегущим строчкам.

### 📡 Потоковые источники (HLS/RTSP/HTTP) и stdin

Для сервисов, получающих ссылку на live-поток, достаточно `FFmpeg.RemuxStream`/`SaveM3U8Stream`: они пропускают вход без перекодирования и сохраняют копию, включая видео, аудио и субтитры. Пример для HLS/RTSP/HTTP:

```csharp
var remux = await FFmpeg.RemuxStream(
    "https://example.com/live/stream.m3u8",
    @"C:\media\stream.webm",
    keepSubtitles: false,
    outputFormat: Format.webm);

await remux.Start(CancellationToken.None);
```

`FFmpeg.SaveAudioStream` выгодно использовать, когда сервису нужно лишь сохранить аудиодорожку (например, для расшифровки или подкаста). Он копирует только аудио-потоки и позволяет задать желаемый формат:

```csharp
var audioOnly = await FFmpeg.SaveAudioStream(
    "https://example.com/live/audio.m3u8",
    @"C:\media\live_audio.aac",
    outputFormat: Format.aac);

await audioOnly.Start(cancellationToken);
```

Если поток приходит через stdin (например, из HTTP-подпрограммы/pipe), используйте `FFmpeg.StreamFromStdin` для полного ремукса или `FFmpeg.StreamAudioFromStdin`, чтобы сохранить только звук (`-map 0:a?`). Пример:

```csharp
using var requestStream = httpContext.Request.Body;
var audioPipe = FFmpeg.StreamAudioFromStdin(requestStream, "upload_audio.wav", outputFormat: Format.wav);
await audioPipe.Start(cancellationToken);
```

Дополнительные шаблоны для сервисов (HLS/RTSP/HTTP, stdin, аудио + overlay) смотрите в `Examples/Service/StreamCapture.cs`.

### 📼 Скачивание из видеохостингов

`FFmpeg.DownloadHostedVideoAsync` оборачивает вызов `yt-dlp`/`youtube-dl` и сохраняет финальный файл (поддерживается YouTube, RuTube и похожие сервисы). При необходимости можно передать `HostedVideoDownloadSettings`, задать формат (по умолчанию `bestvideo+bestaudio/best`), дополнительные аргументы и даже собственный путь к загрузчику. Пример:

```csharp
await FFmpeg.DownloadHostedVideoAsync(
    "https://youtu.be/dQw4w9WgXcQ",
    "service/archive/track.mp4",
    new HostedVideoDownloadSettings
    {
        MergeOutputFormat = "mp4",
        AdditionalArguments = new[] { "--no-mtime" }
    });
```

Если `FFmpeg.ExecutablesPath` задан, `FFmpeg` автоматически подставит `--ffmpeg-location`, чтобы `yt-dlp` мог смёрджить дорожки через доступный ffmpeg.

> Примеры для сервисов (worker/контроллеры), использующих библиотеку, лежат в каталоге `Examples/Service` — они демонстрируют remux/HLS/RTSP/HTTP-потоки, stdin, аудио и overlay.

---

<a id="api-style"></a>

## 🧭 Стиль API

Рекомендуемый стиль использования сейчас такой:

- использовать `FFmpeg.Conversions.*`, `FFmpeg.Conversions.FromSnippet.*` и фасадные методы `FFmpeg.*`
- собирать `Conversion` через специализированные методы вроде `AddInput(...)`, `MapAllStreams()`, `SetAudioCodec(...)`, `DisableVideo()`, `UseFilterGraph(...)`, `MapFilterOutputs(...)`
- использовать `InputSource.Lavfi(...)` и typed overlay/stream helpers вместо ручного `-f lavfi`, `-map`, `-vf`, `-filter_complex`
- оставлять `AddParameter(...)` только для редких или экспериментальных ffmpeg-опций, которые ещё не подняты в API

Пример “в новом стиле”:

```csharp
var conversion = FFmpeg.Conversions.New()
    .AddInput(InputSource.Lavfi("anullsrc=r=48000:cl=stereo", TimeSpan.FromSeconds(1)))
    .MapAudioStreams()
    .DisableVideo()
    .SetAudioCodec(AudioCodec.aac)
    .SetOutputFormat(Format.adts)
    .SetOutput("service/out.aac");
```

Если нужен сложный `filter_complex`, лучше использовать типизированный путь:

```csharp
var graph = new FilterGraph(
    "[0:a]anull[v]",
    FilterLabel.Named("v"));

var conversion = FFmpeg.Conversions.New()
    .UseFilterGraph(graph)
    .MapFilterOutput(FilterLabel.Named("v"))
    .SetOutput("service/out.mp4");
```

Строковые overload-ы для фильтров оставлены для совместимости, но помечены как устаревающие.
---

<a id="ffmpeg-path"></a>

## 🗂️ Где взять FFmpeg

**Вариант A — вы сами:** положили `ffmpeg` и `ffprobe` в известную папку → `SetExecutablesPath`. 🎯

**Вариант B — переменные окружения** (имена из кода):

| Переменная | Назначение |
|------------|------------|
| `FFMPEG_EXECUTABLE` или `FFMPEG_PATH` | Полный путь к `ffmpeg` |
| `FFPROBE_EXECUTABLE` или `FFPROBE_PATH` | Полный путь к `ffprobe` |
| `FFMPEG_BINARIES` или `FFMPEG_BINARIES_PATH` | Каталог, где лежат оба бинарника |
| `PATH` | Как обычно в жизни: «где лежит ffmpeg» |

**Вариант C:** можно надеяться, что ОС и CI уже настроены людьми, которые любят вас. Иногда срабатывает. 💚

---

<a id="media-stable"></a>

## ⏳ Метаданные и «файл ещё не дорос»

Сценарий: микросервис положил путь в очередь, а байты ещё долетают. Два инструмента:

**А) Встроенно в `GetMediaInfo`:**

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;

var info = await FFmpeg.GetMediaInfo(
    path,
    cancellationToken,
    waitUntilFileStable: true,
    stabilityQuietPeriod: TimeSpan.FromMilliseconds(500),
    maximumWaitForStable: TimeSpan.FromMinutes(2));
```

**Б) Явное ожидание до любой своей логики:**

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

await MediaFileReadiness.WaitUntilStableAsync(
    path,
    stabilityQuietPeriod: MediaFileReadiness.DefaultStabilityQuietPeriod,
    pollInterval: MediaFileReadiness.DefaultPollInterval,
    maximumWait: MediaFileReadiness.DefaultMaximumWait,
    cancellationToken);
```

«Стабильность» здесь — **размер и время изменения не меняются заданный интервал**. Это эвристика, а не телепатия 🔮: если запись делается рывками с длинными паузами, при необходимости увеличьте тихий интервал или дождитесь явного сигнала готовности от загрузчика.

---

<a id="conversion"></a>

## 🔄 Конвертация и отмена

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

var conversion = await FFmpeg.Conversions.Transcode(
    inputPath,
    outputPath,
    cancellationToken: cts.Token);

conversion.SetProgressReporter(new Progress<ConversionProgressEventArgs>(p =>
{
    Console.WriteLine($"Прогресс: {p.Percent}%");
}));

try
{
    await conversion.Start(cts.Token);
}
catch (OperationCanceledException)
{
    // Частичный выходной файл при отмене/критической ошибке конвертации
    // может быть удалён реализацией (после остановки процесса ffmpeg).
    throw;
}
```

---

<a id="build"></a>

## 🛠️ Сборка и пакет

Из корня репозитория:

```bash
dotnet build ./Xabe.FFmpeg/Xabe.FFmpeg.csproj
dotnet test ./Xabe.FFmpeg.Test/Xabe.FFmpeg.Test.csproj
```

Для явной упаковки:

```bash
./scripts/pack.sh
```

Или:

```powershell
./scripts/pack.ps1
```

Для запуска demo-примеров:

```bash
dotnet run --project Xabe.FFmpeg/Examples/Xabe.FFmpeg.Examples.csproj -- help
dotnet run --project Xabe.FFmpeg/Examples/Xabe.FFmpeg.Examples.csproj -- facade
```

Готовый `.nupkg` попадает в `artifacts/nuget/`. 📦

### Контракт исключений

Для кода вызывающего сервиса можно опираться на единый базовый тип:

```csharp
try
{
    // вызовы библиотеки
}
catch (Xabe.FFmpeg.Exceptions.XabeFFmpegException ex)
{
    // единая точка обработки ошибок библиотеки
}
```

Поверх него библиотека уже различает более точные семейства ошибок:

- входной файл, сигнатура, пустой файл, нестабильный файл, блокировка, права доступа
- отсутствие нужного audio/video/subtitle stream
- ошибки mapping и несовместимости codec/container
- недоступность путей к бинарникам `ffmpeg` / `ffprobe`
- проблемы с выходным путём или директорией назначения

### XML-документация в NuGet

Пакет теперь включает:

- основной `lib/netstandard2.0/Xabe.FFmpeg.xml`
- локализованные шаблоны:
  - `docs/intellisense/en/Xabe.FFmpeg.xml`
  - `docs/intellisense/ru/Xabe.FFmpeg.xml`
- `buildTransitive`-target, который при restore/build потребителя выбирает язык по `PreferredUILang` / `UICulture` / `Culture` и обновляет один итоговый файл `Xabe.FFmpeg.xml`, который читает IDE рядом со сборкой пакета

Идея простая: у потребителя не пачка XML-файлов и не ручной выбор документации, а **один** `Xabe.FFmpeg.xml`, который IDE читает как обычно. Локализованный шаблон подставляется автоматически на этапе restore/build.

При необходимости язык можно переопределить в consuming-проекте:

```xml
<PropertyGroup>
  <XabeFFmpegDocumentationLanguage>ru</XabeFFmpegDocumentationLanguage>
</PropertyGroup>
```

Или полностью отключить механизм подмены:

```xml
<PropertyGroup>
  <XabeFFmpegInstallLocalizedXmlDocumentation>false</XabeFFmpegInstallLocalizedXmlDocumentation>
</PropertyGroup>
```

---

<a id="license"></a>

## 📜 Лицензия и благодарности

Проект опирается на **Xabe.FFmpeg** и наследует соответствующие условия использования. Оригинальная лицензия и документация: [ffmpeg.xabe.net/license.html](https://ffmpeg.xabe.net/license.html).

Для упаковки форка в NuGet в репозитории добавлен локальный файл лицензии: [LICENSE.md](/Users/andrej/Git/Xabe.FFMpeg.Custom/LICENSE.md). Он фиксирует, что права использования определяются условиями upstream-проекта Xabe.FFmpeg и отдельными условиями самого FFmpeg при распространении бинарников.

Сам **FFmpeg** — отдельный проект со своей лицензией; убедитесь, что ваш способ распространения бинарников ему соответствует (юридический блок на этом README намеренно короткий — он не заменяет консультанта). 👩‍⚖️

---

**🎯 Итог:** если вам нужен .NET-слой над FFmpeg без вечного copy-paste аргументов командной строки, с отменой, кэшем метаданных и относительно взрослой работой с путями и файлами — вы по адресу. Если же вы любите писать `-vf scale=...` от руки в Notepad — мы не мешаем; это тоже образ жизни. ⌨️✨
