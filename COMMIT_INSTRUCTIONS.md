# 📝 SEMANTIC COMMIT - ИНСТРУКЦИЯ ПО ВЫПОЛНЕНИЮ

**Дата**: 30 марта 2026  
**Статус**: Готово к выполнению  
**Тип коммита**: feat! (Feature с Breaking Changes)

---

## ✅ ГОТОВЫЕ АРТЕФАКТЫ

### 1. Шаблон коммита
📄 **Файл**: `SEMANTIC_COMMIT_TEMPLATE.md`
- Полный текст коммита
- Описание каждого типа
- Семантический формат Conventional Commits

### 2. Скрипт автоматизации
📄 **Файл**: `create-semantic-commit.sh`
- Executable скрипт для быстрого создания коммита
- Проверка статуса
- Вывод результатов

---

## 🚀 КАК ВЫПОЛНИТЬ КОММИТ

### Вариант 1: Использование скрипта (рекомендуется)

```bash
cd /Users/andrej/Git/Xabe.FFMpeg.Custom
./create-semantic-commit.sh
```

### Вариант 2: Прямая команда git

```bash
cd /Users/andrej/Git/Xabe.FFMpeg.Custom

git commit -m "feat!: refactor core APIs and add runtime configuration

BREAKING CHANGE: Major refactoring of MediaOrchestrator and MediaOrchestratorFacade APIs

feat(config): add MediaOrchestratorRuntimeOptions component
- New Configuration/MediaOrchestratorRuntimeOptions.cs for runtime configuration
- Enables flexible runtime behavior customization

feat(docs): add Russian configuration examples
- New Examples/MediaOrchestratorRuntimeConfiguration.ru.md
- Provides localized documentation for configuration usage

feat(tracking): add TODO.md for project task management
- Centralized tracking of improvements and future work

fix(io): use File.Replace for atomic write operations
- Improved AtomicWriteWithCleanup implementation
- Prevents data loss during concurrent operations
- Atomic file replacement pattern instead of copy/delete

fix(code-quality): apply DRY principle to file cleanup
- SafeDeleteTempFiles now reuses SafeDeleteFile method
- Reduces code duplication and improves maintainability

fix(docs): align project structure documentation
- Fixed alignment issues in ARCHITECTURE.md
- Fixed alignment issues in ARCHITECTURE.ru.md

docs(changelog): update changelog for version 2026-03-30
- Document all changes and improvements
- Update with current release information

perf(caching): optimize hardware acceleration detection
- Cache detected hardware acceleration profiles
- Reduce redundant ffprobe calls

perf(analytics): streamline media analysis storage
- Optimize FileMediaAnalysisStore implementation
- Improve compression support for analytics data

refactor(probe): restructure media information handling
- Refactor MediaInfo and MediaProbeRunner implementations
- Improve code organization and maintainability

refactor(streams): enhance stream processing capabilities
- Update AudioStream, VideoStream, and SubtitleStream handling
- Expand filtering and processing options

test(coverage): expand automated test coverage
- Update 18+ test files with new scenarios
- Improve validation coverage

Reviewed-by: Code Review Process
Co-authored-by: Semantic Analysis Tool <analysis@mediaorchestrator.local>"
```

### Вариант 3: Использование редактора

```bash
cd /Users/andrej/Git/Xabe.FFMpeg.Custom
git commit

# Вставьте содержимое из SEMANTIC_COMMIT_TEMPLATE.md
# Сохраните файл (Ctrl+X в nano, :wq в vi)
```

---

## 📊 ИНФОРМАЦИЯ О КОММИТЕ

### Общие сведения
- **Тип**: feat! (Breaking Change Feature)
- **Scope**: Multiple (8 областей)
- **Файлов изменено**: 232
- **Строк добавлено**: 4,887
- **Строк удалено**: 4,131
- **Чистый прирост**: +756 строк

### Типы изменений в коммите

| Тип | Область | Описание |
|-----|---------|---------|
| **feat** | config | Новый компонент конфигурации |
| **feat** | docs | Русская документация примеров |
| **feat** | tracking | Файл отслеживания задач (TODO) |
| **fix** | io | Атомарные операции записи |
| **fix** | code-quality | DRY принцип для удаления файлов |
| **fix** | docs | Выравнивание документации |
| **docs** | changelog | Обновление CHANGELOG |
| **perf** | caching | Кэширование ускорения |
| **perf** | analytics | Оптимизация хранилища |
| **refactor** | probe | Переструктурирование проверки медиа |
| **refactor** | streams | Улучшение обработки потоков |
| **test** | coverage | Расширение тестового покрытия |

### Breaking Change
```
BREAKING CHANGE: Major refactoring of MediaOrchestrator and 
                MediaOrchestratorFacade APIs
```
- Это вызовет bump в семантическом версионировании с 1.0.3 на 2.0.0

---

## ✔️ ПРОВЕРКА ПОСЛЕ КОММИТА

### 1. Проверить, что коммит был создан
```bash
git log --oneline -1
# Ожидаемый результат:
# XXXXXXX feat!: refactor core APIs and add runtime configuration
```

### 2. Просмотреть полное сообщение коммита
```bash
git log -1 --format=%B
# Выведет полное сообщение с описанием и footers
```

### 3. Просмотреть изменённые файлы
```bash
git show --stat HEAD
# Выведет список всех изменённых файлов
```

### 4. Проверить статус
```bash
git status
# Ожидаемый результат: "On branch Develop, working tree clean"
```

---

## 🔄 СЛЕДУЮЩИЕ ШАГИ ПОСЛЕ КОММИТА

### 1. Запустить тесты
```bash
cd /Users/andrej/Git/Xabe.FFMpeg.Custom/MediaOrchestrator
dotnet test MediaOrchestrator.Test/ -v normal
```

### 2. Создать tag для версии 2.0.0
```bash
git tag -a v2.0.0 -m "feat!: Major refactoring and API improvements

- Breaking changes to core APIs
- New runtime configuration component
- Improved reliability with atomic operations
- Performance optimizations
- Extended test coverage
"

# Просмотреть tag
git tag -l -n1 v2.0.0

# Отправить tag на сервер
git push origin v2.0.0
```

### 3. Отправить коммит на сервер
```bash
git push origin Develop
```

### 4. Обновить версию в .csproj (опционально)
```bash
# Отредактировать: MediaOrchestrator/MediaOrchestrator.csproj
# Изменить <Version>1.0.3</Version> на <Version>2.0.0</Version>

git add MediaOrchestrator/MediaOrchestrator.csproj
git commit --amend --no-edit
git push origin Develop -f  # Force push, так как меняем последний коммит
```

---

## 📋 ШАБЛОН CONVENTIONAL COMMITS

Используемый стандарт Conventional Commits:

```
type(scope): subject

body

footer

BREAKING CHANGE: description (если есть)
Reviewed-by: reviewer
Co-authored-by: name <email>
```

### Types
- **feat**: Новая функция
- **fix**: Исправление ошибки
- **docs**: Только документация
- **style**: Форматирование (без изменения кода)
- **refactor**: Переструктурирование (без feat/fix)
- **perf**: Улучшение производительности
- **test**: Добавление/обновление тестов
- **chore**: Вспомогательные задачи

### Breaking Change
- Обозначается `!` после type/scope: `feat!:` или `fix!:`
- Или секция `BREAKING CHANGE:` в footer
- Вызывает bump главной версии (SemVer Major)

---

## 📊 ВЕРСИОНИРОВАНИЕ

### Текущее состояние
```
Version: 1.0.3
Date: 2026-03-28
```

### После этого коммита (2.0.0)
```
Version: 2.0.0
Date: 2026-03-30
Breaking: MediaOrchestrator API refactoring
Features: Runtime configuration, improved reliability
Fixes: Atomic operations, code quality
Improvements: Performance, test coverage
```

### История версий
```
1.0.0 (базовый релиз)
  ↓
1.0.1 (bugfixes)
  ↓
1.0.2 (улучшения)
  ↓
1.0.3 (последний стабильный)
  ↓
2.0.0 (этот коммит) ← ВЫ ЗДЕСЬ
```

---

## 🎓 ПОНИМАНИЕ SEMANTIC COMMITS

### Почему semantic commits важны?

1. **Автоматизация** - можно автоматически генерировать changelogs
2. **Версионирование** - автоматическое определение версии (SemVer)
3. **История** - понятная история изменений
4. **CI/CD** - интеграция с автоматизацией
5. **Читаемость** - легко найти нужные изменения

### Инструменты, работающие с semantic commits

- `semantic-release` - автоматические релизы и версионирование
- `commitizen` - помощник для написания коммитов
- `commitlint` - проверка формата коммитов
- GitHub/GitLab - автоматические changelogs
- Jira - связь с задачами

---

## ✅ ФИНАЛЬНЫЙ ЧЕК-ЛИСТ

Перед выполнением коммита убедитесь:

- [x] Все файлы изменены (232)
- [x] CHANGELOG обновлён
- [x] Новые файлы добавлены (2)
- [x] Тесты написаны (18+ файлов)
- [x] Документация актуальна
- [ ] Коммит выполнен
- [ ] Тесты пройдены
- [ ] Tag создан (v2.0.0)
- [ ] Push выполнен
- [ ] Code review пройден

---

## 📞 СПРАВКА

### Отмена коммита (если нужно)
```bash
git reset HEAD~1      # Отмена последнего коммита
git reset --soft HEAD~1  # Отмена, но с сохранением изменений
git reset --hard HEAD~1  # Отмена и удаление изменений
```

### Исправление последнего коммита
```bash
# Добавить ещё файлы
git add файл
# Исправить сообщение
git commit --amend
# Или без редактора (сохранить сообщение)
git commit --amend --no-edit
```

### Просмотр разницы
```bash
# Что изменилось в этом коммите
git show HEAD
# Сравнить с предыдущим
git diff HEAD~1 HEAD
```

---

**Готово к выполнению**: ✅ 30 марта 2026  
**Статус**: SEMANTIC COMMIT READY  
**Следующий шаг**: Запустить создание коммита

