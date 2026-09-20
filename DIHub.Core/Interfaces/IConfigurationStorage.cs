using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Models;

namespace DIHub.Core.Interfaces
{
    public interface IConfigurationStorage
    {
        Task<AppConfiguration> LoadAsync(CancellationToken ct = default);
        Task SaveAsync(AppConfiguration config, CancellationToken ct = default);
    }
}