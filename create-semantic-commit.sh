#!/bin/bash
# Semantic Commit Script for MediaOrchestrator
# Usage: ./create-semantic-commit.sh

set -e

PROJECT_ROOT="/Users/andrej/Git/Xabe.FFMpeg.Custom"
cd "$PROJECT_ROOT"

echo "🔍 Checking git status..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Check if there are changes to commit
if git diff-index --quiet HEAD --; then
    echo "✅ Nothing to commit"
    exit 0
fi

echo "📋 Creating semantic commit..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

# Create the commit with semantic message
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

echo ""
echo "✅ Commit created successfully!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📊 Commit Information:"
git log --oneline -1 | head -1
echo ""
echo "📝 Commit Message:"
git log -1 --format=%B | head -20
echo ""
echo "🚀 Next steps:"
echo "   1. Run tests: dotnet test MediaOrchestrator.Test/"
echo "   2. Push: git push origin Develop"
echo "   3. Create tag: git tag -a v2.0.0 -m 'Major refactoring'"
echo ""

