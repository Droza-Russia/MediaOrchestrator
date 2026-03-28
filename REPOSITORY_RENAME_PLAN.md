# Repository Rename Plan

Цель:

- безопасно переименовать корневой каталог репозитория в `MediaOrchestrator`
- не сломать локальную разработку, git remote, IDE-конфигурации и CI

Этот шаг лучше выполнять отдельно от кодовых изменений, потому что он затрагивает локальное окружение и внешние интеграции.

---

## 1. Целевое состояние

Текущее имя корня:

- `Xabe.FFMpeg.Custom`

Целевое имя:

- `MediaOrchestrator`

Желательный путь:

- `/Users/andrej/Git/MediaOrchestrator`

---

## 2. Что проверить перед переименованием

- все изменения в git либо закоммичены, либо осознанно оставлены в рабочем дереве
- нет внешних процессов, которые держат файлы в репозитории:
  - VS Code tasks
  - Rider / Visual Studio indexing
  - watcher-процессы
  - локальные сервисы, читающие конфиги из репозитория
- в CI/CD нет жёстко зашитого пути с `Xabe.FFMpeg.Custom`
- в локальных скриптах и shell aliases нет старого абсолютного пути

---

## 3. Порядок переезда

1. Закрыть IDE и фоновые процессы, работающие с репозиторием.
2. Переименовать каталог:
   - `mv /Users/andrej/Git/Xabe.FFMpeg.Custom /Users/andrej/Git/MediaOrchestrator`
3. Открыть репозиторий уже по новому пути.
4. Перегенерировать локальные build artifacts:
   - `dotnet restore`
   - `dotnet test ./MediaOrchestrator/MediaOrchestrator.sln`
5. Проверить:
   - VS Code workspace
   - launch/tasks
   - packaging scripts
   - example project

---

## 4. Что может потребовать ручной правки

- локальные IDE-workspace файлы вне репозитория
- terminal history / shell aliases
- внешние deployment-скрипты
- CI variables
- внутренние документы с абсолютными путями

---

## 5. Что уже подготовлено в репозитории

Внутри самого репозитория уже приведено к новому имени:

- директории проектов:
  - `MediaOrchestrator`
  - `MediaOrchestrator.Test`
- solution и project files
- example project
- VS Code tasks / launch / settings
- pack scripts
- XML doc filenames
- временные runtime/test-префиксы

То есть оставшийся шаг в основном инфраструктурный, а не кодовый.

---

## 6. Что осознанно не нужно переписывать

Упоминания `Xabe` в `LICENSE.md` должны остаться, потому что это часть upstream licensing history и юридического контекста форка.

---

## 7. Критерий успешного переезда

Переезд можно считать завершённым, если:

- репозиторий открыт из нового корневого каталога
- `dotnet test ./MediaOrchestrator/MediaOrchestrator.sln` проходит
- в пользовательской документации и рабочих путях больше нет старого имени
- старое имя осталось только в лицензии и при необходимости в git history
