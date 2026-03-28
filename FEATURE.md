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
