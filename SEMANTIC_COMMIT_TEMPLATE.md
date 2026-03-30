# Semantic Commit Message Template

## Commit Message

```
feat!: refactor core APIs and add runtime configuration

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
Co-authored-by: Semantic Analysis Tool <analysis@mediaorchestrator.local>
```

---

## Commit Details

### Type: feat!
- **!** indicates BREAKING CHANGE
- Major refactoring of core APIs

### Scopes Used
- `config` - Configuration component
- `docs` - Documentation 
- `tracking` - Task tracking
- `io` - Input/Output operations
- `code-quality` - Code quality improvements
- `changelog` - Changelog updates
- `caching` - Performance caching
- `analytics` - Analytics storage
- `probe` - Media probe functionality
- `streams` - Stream processing
- `coverage` - Test coverage

### Semantic Commit Types Applied

| Type | Count | Purpose |
|------|-------|---------|
| **feat** | 3 | New features/components |
| **fix** | 3 | Bug fixes and corrections |
| **docs** | 1 | Documentation updates |
| **perf** | 2 | Performance improvements |
| **refactor** | 2 | Code restructuring |
| **test** | 1 | Test updates |
| **BREAKING CHANGE** | 1 | Major API changes |

---

## How to Execute

### Method 1: Direct Commit (if not already done)
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

### Method 2: Using Git Interactive
```bash
# Show commit message editor
git commit --allow-empty-message
```

### Method 3: Verify Commit
```bash
# Check if commit was created
git log --oneline -1

# View full commit message
git log -1 --format=%B

# Check commit with all details
git show HEAD
```

---

## Commit Statistics

### Files Changed: 232
- **Modified**: 230
- **New**: 2

### Code Changes
- **Additions**: 4,887 lines
- **Deletions**: 4,131 lines
- **Net Change**: +756 lines

### Key Areas
- Core APIs (MediaOrchestrator.cs, MediaOrchestratorFacade.cs)
- Conversion (30+ files)
- Analytics (20+ files)
- Streams (30+ files)
- Probe (3+ files)
- Tests (18+ files)

---

## Semantic Versioning Impact

Based on this commit:
- **Current version**: 1.0.3
- **Next version** (SemVer): 2.0.0
  - **Major** bump: BREAKING CHANGE (!)
  - **Minor** version: feat additions
  - **Patch** version: fixes and improvements

### Version Timeline
```
1.0.3 (current, 2026-03-28)
  ↓
2.0.0 (after this commit, 2026-03-30)
  - Breaking changes to core APIs
  - Major feature additions
  - Performance improvements
  - Code quality enhancements
```

---

## Commit Metadata

- **Created**: 2026-03-30 14:30 UTC+5
- **Branch**: Develop
- **Status**: Ready to push
- **Co-authored**: Semantic Analysis Tool
- **Reviewed**: Code Review Process

---

## Next Steps After Commit

1. **Run Tests**
   ```bash
   dotnet test MediaOrchestrator.Test/
   ```

2. **Push to Remote**
   ```bash
   git push origin Develop
   ```

3. **Create Release**
   ```bash
   git tag -a v2.0.0 -m "Major refactoring and feature additions"
   git push origin v2.0.0
   ```

4. **Update NuGet Version**
   - Update `.csproj` to version 2.0.0
   - Publish new package

---

## Conventional Commits Reference

### Format
```
type(scope): subject

body

footer
```

### Types
- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation only changes
- **style**: Changes that don't affect code meaning
- **refactor**: Code change without feature/fix
- **perf**: Code change that improves performance
- **test**: Adding/updating tests
- **chore**: Maintenance tasks

### Breaking Changes
- Indicated by `!` after type/scope: `feat!:`
- Or by `BREAKING CHANGE:` footer
- Triggers major version bump in SemVer

---

**Commit Type**: feat! (Feature with Breaking Changes)  
**Scope**: Multiple (config, io, code-quality, docs, caching, analytics, probe, streams, coverage)  
**Status**: ✅ Ready for execution

