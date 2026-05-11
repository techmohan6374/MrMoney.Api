using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public interface IAccountRepository
    {
        Task<List<Account>> GetAllByUserAsync(string userId);
        Task<Account?> GetByIdAsync(string userId, string accountId);
        Task<Account> CreateAsync(Account account);
        Task<Account> UpdateAsync(Account account);
        Task DeleteAsync(string userId, string accountId);
    }
}
