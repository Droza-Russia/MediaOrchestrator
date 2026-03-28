using System.Threading;
using System.Threading.Tasks;

namespace MediaOrchestrator
{
    /// <inheritdoc />
    public sealed class Probe : IProbe
    {
        /// <summary>
        ///     Создает новый экземпляр Probe.
        /// </summary>
        /// <returns>Объект IProbe.</returns>
        public static IProbe New()
        {
            return new Probe();
        }

        /// <inheritdoc />
        public Task<string> Start(string args, CancellationToken cancellationToken = default)
        {
            var wrapper = new MediaProbeRunner();
            return wrapper.Start(args, cancellationToken);
        }
    }
}
