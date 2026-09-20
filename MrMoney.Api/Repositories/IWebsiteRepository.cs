using System.Collections.Generic;
using System.Threading.Tasks;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public interface IWebsiteRepository
    {
        Task<List<Website>> GetAllAsync();
        Task<Website?> GetByIdAsync(string id);
        Task<Website> CreateAsync(Website website);
        Task<Website> UpdateAsync(string id, Website website);
        Task DeleteAsync(string id);
    }
}
