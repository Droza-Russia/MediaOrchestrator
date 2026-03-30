# Справка По Runtime-Конфигурации MediaOrchestrator

## Назначение

`MediaOrchestrator` поддерживает runtime-конфиг для защитных лимитов и внутренней диагностики.

Сейчас через него настраиваются:

- размер stderr-буфера, который хранится для каждой `ffmpeg`-операции
- размер hash-cache для analytics store

Если конфигурация явно не задана, `MediaOrchestrator` сам создаёт default-настройки и при необходимости берёт значения из переменных окружения.

## Параметры

### `MaxProcessOutputLogLines`

Максимальное количество строк stderr, которое `MediaOrchestrator` хранит в памяти на одну операцию.

Зачем нужен:

- чтобы длинные и шумные `ffmpeg`-запуски не раздували память
- чтобы при ошибке всё равно оставался хвост лога для диагностики

Default:

- `512` строк

Переменная окружения:

- `MEDIA_ORCHESTRATOR_MAX_PROCESS_OUTPUT_LOG_LINES`

### `MaxAnalyticsHashCacheSize`

Максимальный размер внутреннего cache для SHA-хэшей analysis keys.

Зачем нужен:

- чтобы не пересчитывать хэш по одной и той же строке слишком часто
- чтобы cache не рос бесконтрольно при большом числе уникальных записей

Default:

- `10000` элементов

Переменная окружения:

- `MEDIA_ORCHESTRATOR_MAX_ANALYTICS_HASH_CACHE_SIZE`

## Как Инициализировать В Веб-Приложении

Инициализацию лучше делать один раз на старте приложения.

```csharp
using MediaOrchestrator;
using MediaOrchestrator.Configuration;

MediaOrchestrator.ConfigureRuntime(new MediaOrchestratorRuntimeOptions
{
    MaxProcessOutputLogLines = 1024,
    MaxAnalyticsHashCacheSize = 20000
});
```

После этого все новые операции `MediaOrchestrator` будут использовать эти значения.

## Пример С IConfiguration

Сам `MediaOrchestrator` не зависит от `Microsoft.Extensions.Configuration`, но веб-приложение может собрать options самостоятельно и передать их при старте:

```csharp
using MediaOrchestrator;
using MediaOrchestrator.Configuration;

var section = builder.Configuration.GetSection("MediaOrchestrator");

MediaOrchestrator.ConfigureRuntime(new MediaOrchestratorRuntimeOptions
{
    MaxProcessOutputLogLines = section.GetValue<int?>("MaxProcessOutputLogLines") ?? 1024,
    MaxAnalyticsHashCacheSize = section.GetValue<int?>("MaxAnalyticsHashCacheSize") ?? 20000
});
```

## Пример appsettings.json

```json
{
  "MediaOrchestrator": {
    "MaxProcessOutputLogLines": 1024,
    "MaxAnalyticsHashCacheSize": 20000
  }
}
```

## Если Конфигурацию Не Передавать

Тогда `MediaOrchestrator` работает так:

- сам создаёт default runtime-конфиг
- пытается прочитать env vars
- если env vars нет, использует встроенные значения

Это позволяет не ломать существующие интеграции.

## Практические Рекомендации

- `MaxProcessOutputLogLines` лучше не ставить слишком высоким без причины
- если analytics keys очень разнообразны, `MaxAnalyticsHashCacheSize` можно увеличить
- если сервер ограничен по RAM, оба лимита лучше держать консервативными
