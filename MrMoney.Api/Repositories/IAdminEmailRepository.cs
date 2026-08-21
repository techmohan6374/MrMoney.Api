using System.Collections.Generic;
using System.Threading.Tasks;

namespace MrMoney.Api.Repositories
{
    public interface IAdminEmailRepository
    {
        Task<List<string>> GetAllAsync();
        Task AddAsync(string email);
        Task DeleteAsync(string email);
    }
}
