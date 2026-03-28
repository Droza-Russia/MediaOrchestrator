using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediaOrchestrator.Analytics.Models;

namespace MediaOrchestrator.Analytics.Stores
{
    internal interface IMediaAnalysisStore
    {
        Task<MediaAnalysisRecord> GetAsync(string analysisKey, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<MediaAnalysisRecord>> GetAllAsync(CancellationToken cancellationToken = default);

        Task SaveAsync(MediaAnalysisRecord record, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
