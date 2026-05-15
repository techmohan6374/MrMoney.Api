using MrMoney.Api.DTOs;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepo;

        public AccountService(IAccountRepository accountRepo)
        {
            _accountRepo = accountRepo;
        }

        public async Task<List<AccountResponse>> GetAllAsync(string userId)
        {
            var accounts = await _accountRepo.GetAllByUserAsync(userId);
            return accounts.Select(MapToResponse).ToList();
        }

        public async Task<AccountResponse> GetByIdAsync(string userId, string accountId)
        {
            var id = accountId.Trim();
            var account = await _accountRepo.GetByIdAsync(userId, id)
                ?? throw new KeyNotFoundException($"Account '{id}' not found.");
            return MapToResponse(account);
        }

        public async Task<AccountResponse> CreateAsync(string userId, CreateAccountRequest request)
        {
            // If this is set as default, clear existing default first
            if (request.IsDefault)
                await ClearDefaultFlagAsync(userId);

            var account = new Account
            {
                Id          = Guid.NewGuid().ToString(),
                UserId      = userId,
                Name        = request.Name.Trim(),
                HolderName  = request.HolderName.Trim(),
                Balance     = request.Balance,
                Type        = request.Type,
                Color       = request.Color,
                IsDefault   = request.IsDefault,
                CreatedAt   = DateTime.UtcNow
            };

            var created = await _accountRepo.CreateAsync(account);
            return MapToResponse(created);
        }

        public async Task<AccountResponse> UpdateAsync(string userId, string accountId, UpdateAccountRequest request)
        {
            var id = accountId.Trim();
            var account = await _accountRepo.GetByIdAsync(userId, id)
                ?? throw new KeyNotFoundException($"Account '{id}' not found.");

            // If setting as default, clear existing default first
            if (request.IsDefault == true && !account.IsDefault)
                await ClearDefaultFlagAsync(userId);

            if (request.Name        != null) account.Name        = request.Name.Trim();
            if (request.HolderName  != null) account.HolderName  = request.HolderName.Trim();
            if (request.Balance     != null) account.Balance      = request.Balance.Value;
            if (request.Type        != null) account.Type         = request.Type;
            if (request.Color       != null) account.Color        = request.Color;
            if (request.IsDefault   != null) account.IsDefault    = request.IsDefault.Value;

            var updated = await _accountRepo.UpdateAsync(account);
            return MapToResponse(updated);
        }

        public async Task DeleteAsync(string userId, string accountId)
        {
            var id = accountId.Trim();
            _ = await _accountRepo.GetByIdAsync(userId, id)
                ?? throw new KeyNotFoundException($"Account '{id}' not found.");

            await _accountRepo.DeleteAsync(userId, id);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task ClearDefaultFlagAsync(string userId)
        {
            var accounts = await _accountRepo.GetAllByUserAsync(userId);
            foreach (var acc in accounts.Where(a => a.IsDefault))
            {
                acc.IsDefault = false;
                await _accountRepo.UpdateAsync(acc);
            }
        }

        private static AccountResponse MapToResponse(Account a) => new()
        {
            Id          = a.Id,
            Name        = a.Name,
            HolderName  = a.HolderName,
            Balance     = a.Balance,
            Type        = a.Type,
            Color       = a.Color,
            IsDefault   = a.IsDefault,
            CreatedAt   = a.CreatedAt
        };
    }
}
