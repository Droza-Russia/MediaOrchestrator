# Toolchain Package Inventory

Цель этого файла:

- собрать инвентаризацию NuGet-пакетов с бинарниками toolchain
- зафиксировать layout assets по ОС и архитектурам
- отделить версию пакета от фактической версии бинарников
- подготовить исходные данные для `toolchain resolver`, `installer` и `version detector`

Этот файл намеренно оформлен как рабочий шаблон. Его можно заполнять по мере исследования пакетов и уточнения install-стратегии.

---

## 1. Область анализа

Что считаем релевантным пакетом:

- пакет содержит исполняемые файлы toolchain
- пакет содержит runtime-зависимости, без которых executable не стартует
- пакет может использоваться библиотекой для автоустановки на одной или нескольких ОС

Что нужно подтвердить по каждому пакету:

- package id
- package version
- поддерживаемые ОС
- поддерживаемые архитектуры
- структура файлов внутри пакета
- имена основных executable
- наличие вспомогательных библиотек и каталогов
- можно ли надёжно определить версию установленного бинарника

---

## 2. Canonical Inventory Table

| PackageId | PackageVersion | OS | Arch | AssetRoot | MainExecutable | ProbeExecutable | RuntimeFiles | PackageVersionMatchesBinaryVersion | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `Candidate` | `TODO` |

Статусы:

- `Candidate`
- `Confirmed`
- `Rejected`
- `NeedsVerification`

---

## 3. Платформенная матрица

### Windows

| Arch | PackageId | AssetRoot | MainExecutable | ProbeExecutable | InstallReady | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `x64` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` |
| `arm64` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` |

### Linux

| Arch | PackageId | AssetRoot | MainExecutable | ProbeExecutable | InstallReady | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `x64` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` |
| `arm64` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` |

### macOS

| Arch | PackageId | AssetRoot | MainExecutable | ProbeExecutable | InstallReady | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `x64` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` |
| `arm64` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` |

---

## 4. Карточка пакета

Копировать этот блок для каждого подтверждённого пакета.

### Package: `TODO`

- PackageId: `TODO`
- PackageVersion: `TODO`
- Source: `TODO`
- PackagePurpose: `TODO`
- SupportedPlatforms: `TODO`
- SupportedArchitectures: `TODO`
- MainExecutable: `TODO`
- ProbeExecutable: `TODO`
- PackageStatus: `Candidate`

#### Asset Layout

- Root folder: `TODO`
- Executable path: `TODO`
- Probe path: `TODO`
- Companion libraries: `TODO`
- Required subdirectories: `TODO`
- Permissions normalization needed: `Yes/No`

#### Version Signals

- NuGet package version: `TODO`
- File version available: `Yes/No`
- CLI version command: `TODO`
- Example version output: `TODO`
- Binary version parse rule: `TODO`
- Package version equals binary version: `Yes/No/Unknown`

#### Install Notes

- Can install as-is: `Yes/No`
- Needs full directory copy: `Yes/No`
- Needs post-install chmod: `Yes/No`
- Needs symlink handling: `Yes/No`
- Expected install footprint: `TODO`

#### Risks

- `TODO`

#### Verification Steps

1. `TODO`
2. `TODO`
3. `TODO`

---

## 5. Asset Layout Comparison

Использовать этот раздел, если layouts между пакетами отличаются и нужен быстрый обзор различий.

| PackageId | OS | Arch | UsesToolsFolder | UsesRuntimesFolder | FlatExecutablePath | RequiresDirectoryCopy | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` |

---

## 6. Версионная модель

Нужно заполнять по мере анализа.

| PackageId | PackageVersion | DetectedBinaryVersion | DetectionCommand | ParseStable | MismatchObserved | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` | `TODO` |

Правило на будущее:

- хранить `PackageVersion` отдельно от `DetectedBinaryVersion`
- не делать предположение, что они совпадают
- в install-report возвращать обе версии

---

## 7. Требования к resolver

Resolver должен уметь:

- выбрать пакет по `OS + Architecture`
- выбирать exact-match перед fallback
- возвращать typed descriptor, а не сырые строки
- уметь объяснить, почему пакет был выбран или отклонён

Что нужно проверить во время анализа пакетов:

- есть ли exact-match для всех целевых платформ
- есть ли платформы без покрытия
- есть ли конфликтующие кандидаты на одну и ту же платформу

---

## 8. Требования к installer

Installer должен уметь:

- использовать локальный NuGet cache как первый источник
- скачивать пакет только при отсутствии локальной копии
- распаковывать пакет в version-aware install path
- устанавливать бинарники атомарно
- не переустанавливать совпадающую версию без причины

Что нужно зафиксировать при анализе:

- можно ли распаковывать только один подкаталог или нужен весь пакет
- какие файлы обязательно копировать рядом с executable
- требуется ли специальная post-install подготовка для Unix-платформ

---

## 9. Требования к version detector

Version detector должен уметь:

- проверять наличие executable
- запускать команду определения версии
- парсить stdout в typed version model
- отмечать install как `Corrupted`, если executable есть, но версия не определяется

Что нужно собрать заранее:

- список команд для определения версии по каждому типу бинарника
- примеры stdout
- регулярные выражения или parse-правила

---

## 10. Открытые вопросы

- `TODO`: где хранить install cache по умолчанию на разных ОС
- `TODO`: нужна ли поддержка pinned version policy
- `TODO`: нужна ли фоновая проверка обновлений пакетов
- `TODO`: требуется ли offline-only режим
- `TODO`: нужен ли импорт уже установленного toolchain без NuGet-пакета

---

## 11. Критерии завершения исследования

Исследование можно считать достаточным для начала реализации, если:

- есть подтверждённый пакетный кандидат минимум для каждой целевой ОС
- понятен asset layout для каждой поддерживаемой архитектуры
- определён способ получения фактической версии бинарника
- понятны обязательные runtime-файлы рядом с executable
- описаны хотя бы основные failure cases install-layer
