using System.Threading;
using System.Threading.Tasks;

namespace MudaeFarm
{
    /// <summary>
    /// Represents a module of the bot.
    /// Modules will be initialized using reflection on startup and run in parallel.
    /// </summary>
    public interface IModule
    {
        void Initialize();

        Task RunAsync(CancellationToken cancellationToken = default);
    }
}