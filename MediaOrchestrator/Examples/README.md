# Service-ready примеры для MediaOrchestrator

Примеры теперь оформлены как отдельный demo-проект `Examples/MediaOrchestrator.Examples.csproj`. Он ссылается на библиотеку и позволяет запускать сценарии по имени, не смешивая demo-код со сборкой NuGet-пакета.

Запуск:

```bash
dotnet run --project MediaOrchestrator/Examples/MediaOrchestrator.Examples.csproj -- help
dotnet run --project MediaOrchestrator/Examples/MediaOrchestrator.Examples.csproj -- facade
dotnet run --project MediaOrchestrator/Examples/MediaOrchestrator.Examples.csproj -- stream-capture
dotnet run --project MediaOrchestrator/Examples/MediaOrchestrator.Examples.csproj -- stream-remux
dotnet run --project MediaOrchestrator/Examples/MediaOrchestrator.Examples.csproj -- overlay-timecode
```

Сценарии находятся в `Examples/Service`:

1. `StreamCapture.cs` — берет ссылку на поток (HLS/RTSP/HTTP), сохраняет полное видео и чистое аудио, а также показывает работу с `FFmpeg.StreamFromStdin`/`StreamAudioFromStdin`.
2. `OverlayTimecode.cs` — демонстрирует `FFmpeg.BurnRightSideTextLabel`, `BurnRightSidePtsTimeLabel` и `BurnRightSideSmpteTimecode` (либо альтернативно `SetRightSideDrawText`/`SetRightSide...Overlay`).
3. `FacadeSamples.cs` — обходит все публичные сценарии API (`ToMp4`, `RemuxStream`, `SaveAudioStream`, `DownloadHostedVideoAsync`, `Concatenate`, `ConvertWithHardware`, `SendToRtspServer` и др.) и показывает, как организовать сервисные пайплайны с ними.
4. Примеры отражают текущий рекомендуемый стиль библиотеки: `Conversion` собирается через специализированные методы (`AddInput`, `MapAllStreams`, `SetAudioCodec`, `UseFilterGraph`, `SetTune`, `UseHardwareAcceleration`), а не через набор `AddParameter("-...")`.

Для встраивания в ваш сервис замените URI и локальные пути на реальные источники и при необходимости перенесите логику из `Examples/Service` в контроллер, background job или worker.
