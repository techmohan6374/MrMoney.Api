using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>> GetAllByUserAsync(string userId);
        Task<Transaction?> GetByIdAsync(string userId, string transactionId);
        Task<Transaction> CreateAsync(Transaction transaction);
        Task<Transaction> UpdateAsync(Transaction transaction);
        Task DeleteAsync(string userId, string transactionId);
    }
}
