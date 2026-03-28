# Feature Plan

## Аналитическо-адаптивная модель подбора параметров обработки медиа

Цель: построить слой, который не просто запускает FFmpeg по заранее заданному шаблону, а анализирует входной медиафайл, контекст использования и ограничения среды, после чего выбирает оптимальный сценарий обработки: remux, normalize, transcode, extract или гибридный pipeline.

Этот файл намеренно хранится в проекте как рабочий журнал будущей feature-ветки. Его можно дополнять по мере уточнения требований, решений и результатов.

---

## 1. Бизнес-цель

Сделать библиотеку пригодной для серверного media-processing слоя, где нужно автоматически и предсказуемо:

- подготавливать медиа для браузерного воспроизведения
- подготавливать аудио для AI-задач, особенно транскрибации
- минимизировать лишнее перекодирование
- учитывать качество, скорость, стоимость CPU/GPU и совместимость
- давать объяснимое решение, почему выбран именно такой pipeline

---

## 2. Базовая идея

Нужен отдельный decision layer:

- на входе: `MediaInfo`, характеристики среды, целевой сценарий, ограничения
- внутри: набор правил, эвристик и scoring
- на выходе: typed plan обработки медиа

Не просто “вызвать snippet”, а сначала понять:

- можно ли ограничиться remux
- нужно ли нормализовать контейнер
- нужно ли перекодировать аудио
- нужно ли перекодировать видео
- есть ли смысл использовать hardware acceleration
- какие параметры являются оптимальными для данного сценария

---

## 3. Основные сценарии

### Browser Playback

Цель:

- сделать файл/поток пригодным для стабильного воспроизведения в браузере
- уменьшить проблемы с метриками, seek, duration, fragmented streams и нестандартными контейнерами

Типичные решения:

- remux `webm`
- remux `mkv`
- convert to `mp4` при плохой совместимости
- нормализовать ключевые параметры контейнера и потоков

### AI Transcription

Цель:

- привести аудио к стабильному контракту для ASR/STT

Базовый target:

- `wav`
- `pcm_s16le`
- `16000 Hz`
- `mono`

Типичные решения:

- extract first valid audio stream
- disable video
- normalize sample rate / channels / codec / container

### AI Ingestion

Цель:

- подготавливать медиа для downstream AI-задач: embeddings, VAD, diarization, scene analysis

Возможные target profiles:

- audio transcription profile
- audio embeddings profile
- browser-safe preview profile
- frame-extraction profile

### Minimal-Cost Processing

Цель:

- по возможности избежать transcoding

Правило:

- `remux if possible`
- `transcode only if required`

---

## 4. Предлагаемая архитектура

### 4.1. Входная аналитическая модель

Нужен typed object, условно:

- `MediaProcessingContext`

Состав:

- `IMediaInfo MediaInfo`
- `ProcessingScenario Scenario`
- `ProcessingConstraints Constraints`
- `EnvironmentCapabilities Capabilities`
- `StorageContext StorageContext`

### 4.2. Сценарий обработки

Нужен enum или typed profile:

- `BrowserPlayback`
- `AiTranscription`
- `AiEmbeddings`
- `FrameExtraction`
- `ArchivalNormalize`
- `Custom`

### 4.3. Ограничения

Нужен объект, где задаются рамки:

- max duration
- max output size
- preferred container
- preferred codecs
- allow remux
- allow transcode
- allow hardware acceleration
- preferred quality/speed balance
- expected browser compatibility level

### 4.4. Возможности среды

Нужен объект с фактическими возможностями:

- detected hardware acceleration
- available codecs
- ffmpeg executable profile
- CPU/GPU preference
- OS-specific constraints

### 4.5. Выход модели

Нужен typed plan, условно:

- `MediaProcessingPlan`

Содержимое:

- selected strategy
- target container
- target audio settings
- target video settings
- use remux / transcode flags
- hardware acceleration decision
- reason list / diagnostics
- estimated risk / compatibility notes

### 4.6. Персистентный слой анализа

Нужен отдельный persistent cache/store, который переживает рестарт процесса и позволяет лениво актуализировать результаты анализа.

Базовая идея:

- первый уровень: in-memory cache
- второй уровень: persistent local store
- третий уровень: probe + decision engine как source of truth

Предлагаемая абстракция:

- `IMediaAnalysisStore`

Возможные реализации:

- `FileMediaAnalysisStore`
- `SqliteMediaAnalysisStore`

Что хранить:

- `MediaProbeSnapshot`
- `MediaClassification`
- `MediaProcessingPlan`

Что использовать как ключ:

- normalized path or URI
- file size
- last write time
- optional quick fingerprint
- scenario / constraints identity
- decision model version

Поведение:

- на запрос сначала ищем свежую запись
- если запись валидна — возвращаем её
- если запись устарела или incomplete — пересчитываем и обновляем store

Это даёт:

- меньше повторных probe-вызовов
- меньше повторных decision-расчётов
- предсказуемость после рестарта сервиса
- основу для explainability и аудита решений

---

## 5. Decision pipeline

### Этап 1. Probe and classify

Определить:

- container
- video/audio/subtitle presence
- codec list
- sample rate / channels
- bitrate
- frame rate
- resolution
- duration
- stream count
- признаки нестабильного или “экзотического” входа

### Этап 2. Capability check

Понять:

- есть ли hwaccel
- доступны ли нужные кодеки
- можно ли remux без потерь
- можно ли обойтись без full transcode

### Этап 3. Scenario rules

Применить правила конкретного сценария:

- browser-safe rules
- transcription rules
- AI ingestion rules

### Этап 4. Cost/benefit evaluation

Сравнить варианты:

- remux
- audio-only normalize
- partial transcode
- full transcode

Критерии:

- compatibility
- speed
- quality loss
- compute cost
- implementation risk

### Этап 5. Final plan assembly

Построить typed plan и вернуть:

- решение
- аргументацию
- рекомендуемый snippet/conversion path

---

## 6. Первые high-level API-кандидаты

### 6.1. Probe and decide

```csharp
var plan = await FFmpeg.Analytics.DecideProcessingPlanAsync(inputPath, scenario, constraints);
```

### 6.2. Decide and build conversion

```csharp
var conversion = await FFmpeg.Analytics.BuildConversionAsync(inputPath, outputPath, scenario, constraints);
```

### 6.3. Browser-safe normalize

```csharp
var conversion = await FFmpeg.Conversions.FromSnippet.NormalizeForBrowserPlayback(inputPath, outputPath);
```

### 6.4. AI transcription normalize

```csharp
var conversion = await FFmpeg.Conversions.FromSnippet.NormalizeAudioForTranscription(inputPath, outputPath);
```

### 6.5. Remux if possible

```csharp
var conversion = await FFmpeg.Conversions.FromSnippet.RemuxIfPossibleElseTranscode(inputPath, outputPath, scenario);
```

---

## 7. Минимальный roadmap

### Phase 1. Аналитическая модель

- ввести typed контекст анализа
- ввести typed constraints
- ввести typed output plan
- ввести diagnostics / reason codes

### Phase 2. Scenario profiles

- browser playback
- AI transcription
- AI ingestion

### Phase 2.5. Persistent cache layer

- ввести `IMediaAnalysisStore`
- определить формат `MediaProbeSnapshot`
- определить fingerprint policy
- реализовать lazy invalidation
- сначала file-based store или SQLite

### Phase 3. Rule engine

- remux compatibility rules
- container normalization rules
- audio normalization rules
- hwaccel decision rules

### Phase 4. Integration with snippets

- построение `IConversion` из `MediaProcessingPlan`
- интеграция с текущими snippet-методами
- fallback path для сложных случаев

### Phase 5. Telemetry and explainability

- почему выбран такой pipeline
- почему не remux
- почему включён/выключен hwaccel
- какие ограничения повлияли на решение

---

## 8. Что важно не сломать

- typed exception contract
- локализацию ошибок
- кэш `MediaInfo`
- уже существующие high-level snippets
- предсказуемость server-side поведения

---

## 9. Риски

- переусложнение модели до того, как будут зафиксированы реальные use-cases
- смешение decision layer и execution layer
- попытка слишком рано покрыть все контейнеры и кодеки
- слишком “магическое” поведение без explainability
- устаревшие persistent записи при изменении decision rules или constraints model
- слишком слабый fingerprint для сетевых файлов и файлов, которые могут быть перезаписаны без смены path

---

## 10. Практический принцип

Сначала не “идеальный AI planner”, а объяснимый и полезный rule-based engine:

- простые typed входы
- явные сценарии
- прозрачные причины выбора
- безопасные default decisions

Если этого окажется мало, потом можно наращивать scoring и адаптивность.

---

## 11. Первая итерация на завтра

Предлагаемый порядок работы:

1. Зафиксировать доменные типы:
   - `ProcessingScenario`
   - `ProcessingConstraints`
   - `EnvironmentCapabilities`
   - `MediaProcessingPlan`
   - `ProcessingDecisionReason`
2. Сделать `AiTranscription` как первый полностью закрытый сценарий.
3. Сделать `BrowserPlayback` как второй сценарий.
4. Описать правило:
   - remux if possible
   - transcode if required
5. Построить adapter:
   - `MediaProcessingPlan -> IConversion`
6. Добавить тесты на decision logic отдельно от FFmpeg command rendering.

---

## 12. Статус

- [x] Базовая typed инфраструктура проекта подготовлена
- [x] Ошибки и исключения сведены к библиотечному контракту
- [x] Есть high-level transcription snippet
- [ ] Нет decision layer
- [ ] Нет typed processing plan
- [ ] Нет scenario-based planner
- [ ] Нет explainability модели

---

## 13. Рабочие заметки

- `webm` и `mkv` для браузерного playback стоит рассматривать как отдельный compatibility-risk profile
- для AI-аудио не всегда нужен full transcode video container, достаточно audio-only extraction + normalize
- нужно различать “оптимально по качеству”, “оптимально по скорости” и “оптимально по стоимости”
- explainability важна почти так же, как и само решение
- persistent cache стоит version-aware делать сразу, иначе миграция decision logic потом станет болезненной
- для старта можно начать с file-based store, но для реального server use-case SQLite, скорее всего, практичнее

---

## 14. План работ на завтра: автоустановка бинарников из NuGet и детекция версий по ОС

Цель:

- спроектировать и частично реализовать слой, который автоматически находит подходящие NuGet-пакеты с бинарниками toolchain
- извлекает из них бинарники под нужную ОС и архитектуру
- определяет фактически установленную версию toolchain
- даёт единый API без упоминаний legacy-брендов в сервисном слое

### 14.1. Что считаем результатом дня

К концу дня должны появиться:

- инвентаризация уже существующих NuGet-пакетов с бинарниками
- документированная mapping-таблица:
  - `OS + Architecture -> package id -> asset layout -> executable names`
- typed модель source-resolution для загрузки бинарников
- typed модель version detection для установленных бинарников
- минимальная реализация install/resolve pipeline
- тесты на path resolution, asset selection и version parsing

### 14.2. Что нужно проанализировать в первую очередь

Так как пакеты уже существуют, сначала нужен не код, а инвентаризация:

1. Найти все релевантные пакеты с бинарниками.
2. Зафиксировать:
   - package id
   - целевые ОС
   - поддерживаемые архитектуры
   - как устроены `runtimes`, `tools`, `contentFiles` или иные каталоги
   - какие файлы являются основными executable
   - есть ли сопутствующие библиотеки, которые нужно копировать вместе с executable
3. Проверить, есть ли в самих пакетах явная информация о версии:
   - package version
   - file version
   - internal tool version
4. Определить, совпадает ли версия NuGet-пакета с фактической версией бинарников.

### 14.3. Предлагаемый план разработки

#### Этап 1. Package discovery и inventory

Сделать отдельную рабочую таблицу или markdown-раздел с найденными пакетами.

Для каждого пакета зафиксировать:

- `PackageId`
- `PackageVersion`
- `Platform`
- `Architecture`
- `ExecutableName`
- `ProbeExecutableName`
- `AssetRoot`
- `InstallNotes`

Результат этапа:

- одна canonical-таблица, на базе которой дальше пишется resolver

#### Этап 2. Typed модель источников бинарников

Ввести модель, условно:

- `BinaryPackageDescriptor`
- `BinaryAssetDescriptor`
- `ToolchainBinaryDescriptor`
- `ToolchainInstallRequest`
- `ToolchainInstallResult`

Что должно быть в модели:

- идентификатор пакета
- версия пакета
- ОС
- архитектура
- имена основных бинарников
- список дополнительных файлов runtime
- относительные пути до assets внутри пакета
- признак, нужно ли распаковывать каталог целиком

Результат этапа:

- код больше не работает со строками и ad-hoc path logic напрямую

#### Этап 3. Runtime platform resolution

Сделать единый resolver текущей платформы:

- `Windows`
- `Linux`
- `macOS`

И отдельно архитектуры:

- `x64`
- `arm64`
- при необходимости `x86`

Что важно:

- platform detection не должна быть размазана по коду
- нужен единый typed результат, который потом используется и install-layer, и analytics-layer
- надо сразу предусмотреть fallback-политику, если для платформы нет exact-match пакета

Результат этапа:

- `CurrentPlatform -> package candidate list`

#### Этап 4. Download/restore strategy

Решить, как именно библиотека будет получать NuGet-пакет:

- через локальный NuGet cache
- через стандартный restore/download workflow
- через явное скачивание `.nupkg`

На завтра задача не обязательно доводить до всех вариантов. Достаточно выбрать основной путь и заложить extension point для альтернатив.

Минимально нужен контракт:

- проверить, установлен ли уже нужный пакет локально
- если нет, загрузить его
- распаковать в внутренний install/cache-каталог библиотеки
- не переустанавливать заново при совпадении версии

Результат этапа:

- повторный старт библиотеки не делает лишних скачиваний

#### Этап 5. Стратегия установки и layout install-каталога

Нужно заранее зафиксировать install layout, чтобы потом не ломать совместимость:

- корневой каталог toolchain cache
- подпапки по:
  - tool family
  - OS
  - architecture
  - version

Пример идеи:

- `<cache>/toolchains/<tool>/<os>/<arch>/<version>/...`

Что важно:

- рядом должны лежать все зависимые runtime-файлы
- установка должна быть атомарной
- нельзя оставлять полубитые каталоги после неудачного restore

Результат этапа:

- воспроизводимый install layout для всех ОС

#### Этап 6. Детекция версии бинарников

Нужен отдельный слой version detection, который работает не по имени пакета, а по фактическому бинарнику.

Проверять:

- существует ли executable
- запускается ли он
- что возвращает `-version` или аналогичный флаг
- можно ли стабильно распарсить:
  - semver tool version
  - build metadata
  - vendor string

Нужны typed объекты, условно:

- `ToolchainVersionInfo`
- `InstalledToolchainInfo`

Что хранить:

- package version
- detected binary version
- platform
- architecture
- install path
- detection timestamp
- status:
  - `Installed`
  - `VersionDetected`
  - `Corrupted`
  - `Unsupported`

Результат этапа:

- библиотека может объяснить, что именно установлено и какой версии

#### Этап 7. Public API для сервиса

Нужен чистый публичный API без legacy-названий в сервисном коде.

Минимально:

- `EnsureToolchainInstalledAsync(...)`
- `GetInstalledToolchainInfoAsync(...)`
- `GetAvailableToolchainPackages(...)`

Публичный контракт должен позволять:

- принудительно установить нужную версию
- получить auto-resolved версию для текущей ОС
- узнать, что сейчас реально установлено

Результат этапа:

- веб/API/фоновые сервисы получают единый orchestration entrypoint

#### Этап 8. Ошибки и диагностика

Сразу предусмотреть отдельные домены ошибок:

- package not found
- platform not supported
- asset layout invalid
- executable missing after install
- version detection failed
- corrupted install cache
- restore/download failed

Что важно:

- каждая ошибка должна иметь стабильный код
- ошибки должны быть пригодны для логирования, UI и поддержки
- диагностическое сообщение должно включать:
  - package id
  - version
  - platform
  - install path

Результат этапа:

- install layer пригоден для production-debugging

#### Этап 9. Тесты

Минимальный набор тестов на завтра:

- выбор пакета по `OS + Architecture`
- выбор корректного asset-root внутри пакета
- нормализация install-path
- защита от повторной переустановки той же версии
- парсинг версии из stdout бинарника
- поведение при unsupported platform
- поведение при повреждённом install-каталоге

Если останется время:

- интеграционный тест с реальным `.nupkg`
- smoke test на распаковку и детекцию версии

### 14.4. Риски и решения

- Риск: package version и binary version не совпадают.
  Решение: хранить обе версии отдельно и не подменять одну другой.

- Риск: layout пакетов отличается между ОС.
  Решение: не хардкодить единый path-шаблон, а описать layout через descriptors.

- Риск: частично распакованный install ломает последующие запуски.
  Решение: staging-directory + atomic move.

- Риск: скачивание будет долгим и нестабильным.
  Решение: сначала проверять локальный NuGet cache и уже потом сеть.

- Риск: сервису нужна быстрая проверка без скачивания.
  Решение: разделить `resolve`, `install` и `detect version` на отдельные операции.

### 14.5. Критерии готовности

Задачу на завтра можно считать выполненной, если:

- для текущей ОС библиотека умеет выбрать правильный пакет бинарников
- install-layer умеет развернуть пакет в внутренний cache-каталог
- библиотека умеет определить фактическую версию установленного бинарника
- публичный API возвращает typed install/status result
- тесты покрывают хотя бы основной happy path и два failure path

### 14.6. Желательный итоговый артефакт дня

К вечеру желательно иметь:

1. markdown-инвентаризацию найденных бинарных NuGet-пакетов
2. базовый `toolchain resolver`
3. базовый `toolchain installer`
4. базовый `version detector`
5. набор тестов на платформенный выбор и version parsing

Если останется время сверх минимума:

- добавить кэширование detection-result
- добавить lazy refresh установленной версии
- подготовить почву для автообновления бинарников по policy

Рабочий шаблон для инвентаризации пакетов:

- [TOOLCHAIN_PACKAGE_INVENTORY.md](TOOLCHAIN_PACKAGE_INVENTORY.md)
