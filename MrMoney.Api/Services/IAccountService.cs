using MrMoney.Api.DTOs;

namespace MrMoney.Api.Services
{
    public interface IAccountService
    {
        Task<List<AccountResponse>> GetAllAsync(string userId);
        Task<AccountResponse> GetByIdAsync(string userId, string accountId);
        Task<AccountResponse> CreateAsync(string userId, CreateAccountRequest request);
        Task<AccountResponse> UpdateAsync(string userId, string accountId, UpdateAccountRequest request);
        Task DeleteAsync(string userId, string accountId);
    }
}
