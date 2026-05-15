using MrMoney.Api.DTOs;
using MrMoney.Api.Models;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _txRepo;
        private readonly IAccountRepository _accountRepo;

        public TransactionService(ITransactionRepository txRepo, IAccountRepository accountRepo)
        {
            _txRepo      = txRepo;
            _accountRepo = accountRepo;
        }

        // ── Query ─────────────────────────────────────────────────────────────

        public async Task<PagedTransactionResponse> GetAllAsync(string userId, TransactionFilterRequest filter)
        {
            var all = await _txRepo.GetAllByUserAsync(userId);

            // Apply filters
            var query = all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Type) && filter.Type != "All")
                query = query.Where(t => t.Type.Equals(filter.Type, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.AccountId))
                query = query.Where(t => t.AccountId == filter.AccountId);

            if (!string.IsNullOrWhiteSpace(filter.Category))
                query = query.Where(t => t.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(search) ||
                    t.Category.ToLower().Contains(search) ||
                    t.Description.ToLower().Contains(search));
            }

            if (filter.DateFrom.HasValue)
                query = query.Where(t => t.Date >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(t => t.Date <= filter.DateTo.Value.AddDays(1));

            var filtered = query.ToList();
            var totalCount = filtered.Count;

            var page     = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToResponse)
                .ToList();

            return new PagedTransactionResponse
            {
                Items      = items,
                TotalCount = totalCount,
                Page       = page,
                PageSize   = pageSize
            };
        }

        public async Task<TransactionResponse> GetByIdAsync(string userId, string transactionId)
        {
            var tx = await _txRepo.GetByIdAsync(userId, transactionId)
                ?? throw new KeyNotFoundException($"Transaction '{transactionId}' not found.");
            return MapToResponse(tx);
        }

        // ── Create ────────────────────────────────────────────────────────────

        public async Task<TransactionResponse> CreateAsync(string userId, CreateTransactionRequest request)
        {
            // Validate account belongs to user
            var account = await _accountRepo.GetByIdAsync(userId, request.AccountId.Trim())
                ?? throw new KeyNotFoundException($"Account '{request.AccountId}' not found.");

            var tx = new Transaction
            {
                Id          = Guid.NewGuid().ToString(),
                UserId      = userId,
                AccountId   = request.AccountId.Trim(),
                Name        = request.Name.Trim(),
                Category    = request.Category.Trim(),
                Amount      = request.Amount,
                Type        = request.Type.ToLower(),
                Description = request.Description?.Trim() ?? string.Empty,
                Status      = "Completed",
                Date        = request.Date?.ToUniversalTime() ?? DateTime.UtcNow,
                CreatedAt   = DateTime.UtcNow
            };

            // Update account balance
            account.Balance = tx.Type == "income"
                ? account.Balance + tx.Amount
                : account.Balance - tx.Amount;

            await _accountRepo.UpdateAsync(account);
            var created = await _txRepo.CreateAsync(tx);
            return MapToResponse(created);
        }

        // ── Update ────────────────────────────────────────────────────────────

        public async Task<TransactionResponse> UpdateAsync(string userId, string transactionId, UpdateTransactionRequest request)
        {
            var tx = await _txRepo.GetByIdAsync(userId, transactionId)
                ?? throw new KeyNotFoundException($"Transaction '{transactionId}' not found.");

            var oldAccountId = tx.AccountId;
            var oldAmount      = tx.Amount;
            var oldType        = tx.Type;

            if (request.Name        != null) tx.Name        = request.Name.Trim();
            if (request.Category    != null) tx.Category    = request.Category.Trim();
            if (request.Amount      != null) tx.Amount      = request.Amount.Value;
            if (request.Type        != null) tx.Type        = request.Type.ToLower();
            if (request.AccountId   != null) tx.AccountId   = request.AccountId.Trim();
            if (request.Description != null) tx.Description = request.Description.Trim();
            if (request.Date        != null) tx.Date        = request.Date.Value.ToUniversalTime();

            var newAccountId = tx.AccountId;
            var newAmount    = tx.Amount;
            var newType      = tx.Type;

            if (oldAccountId != newAccountId || oldAmount != newAmount || !string.Equals(oldType, newType, StringComparison.OrdinalIgnoreCase))
            {
                var oldContrib = BalanceContribution(oldType, oldAmount);
                var newContrib = BalanceContribution(newType, newAmount);

                if (string.Equals(oldAccountId, newAccountId, StringComparison.OrdinalIgnoreCase))
                {
                    var acc = await _accountRepo.GetByIdAsync(userId, newAccountId)
                        ?? throw new KeyNotFoundException($"Account '{newAccountId}' not found.");
                    acc.Balance -= oldContrib;
                    acc.Balance += newContrib;
                    await _accountRepo.UpdateAsync(acc);
                }
                else
                {
                    var oldAcc = await _accountRepo.GetByIdAsync(userId, oldAccountId)
                        ?? throw new KeyNotFoundException($"Account '{oldAccountId}' not found.");
                    var newAcc = await _accountRepo.GetByIdAsync(userId, newAccountId)
                        ?? throw new KeyNotFoundException($"Account '{newAccountId}' not found.");
                    oldAcc.Balance -= oldContrib;
                    newAcc.Balance += newContrib;
                    await _accountRepo.UpdateAsync(oldAcc);
                    await _accountRepo.UpdateAsync(newAcc);
                }
            }

            var updated = await _txRepo.UpdateAsync(tx);
            return MapToResponse(updated);
        }

        // ── Delete ────────────────────────────────────────────────────────────

        public async Task DeleteAsync(string userId, string transactionId)
        {
            var tx = await _txRepo.GetByIdAsync(userId, transactionId)
                ?? throw new KeyNotFoundException($"Transaction '{transactionId}' not found.");

            var account = await _accountRepo.GetByIdAsync(userId, tx.AccountId)
                ?? throw new KeyNotFoundException($"Account '{tx.AccountId}' not found.");

            account.Balance -= BalanceContribution(tx.Type, tx.Amount);
            await _accountRepo.UpdateAsync(account);
            await _txRepo.DeleteAsync(userId, transactionId);
        }

        // ── Transfer ──────────────────────────────────────────────────────────

        public async Task<(TransactionResponse From, TransactionResponse To)> TransferAsync(string userId, TransferRequest request)
        {
            var fromId = request.FromAccountId.Trim();
            var toId   = request.ToAccountId.Trim();

            if (string.Equals(fromId, toId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Source and destination accounts must be different.");

            var fromAccount = await _accountRepo.GetByIdAsync(userId, fromId)
                ?? throw new KeyNotFoundException($"Source account '{fromId}' not found.");

            var toAccount = await _accountRepo.GetByIdAsync(userId, toId)
                ?? throw new KeyNotFoundException($"Destination account '{toId}' not found.");

            if (fromAccount.Balance < request.Amount)
                throw new InvalidOperationException("Insufficient balance in source account.");

            var transferDate = request.Date?.ToUniversalTime() ?? DateTime.UtcNow;
            var now          = DateTime.UtcNow;

            // Debit from source
            var txOut = new Transaction
            {
                Id          = Guid.NewGuid().ToString(),
                UserId      = userId,
                AccountId   = fromId,
                Name        = "Transfer Out",
                Category    = "Transfer",
                Amount      = request.Amount,
                Type        = "expense",
                Description = $"Transfer to {toAccount.Name}",
                Status      = "Completed",
                Date        = transferDate,
                CreatedAt   = now
            };

            // Credit to destination
            var txIn = new Transaction
            {
                Id          = Guid.NewGuid().ToString(),
                UserId      = userId,
                AccountId   = toId,
                Name        = "Transfer In",
                Category    = "Transfer",
                Amount      = request.Amount,
                Type        = "income",
                Description = $"Transfer from {fromAccount.Name}",
                Status      = "Completed",
                Date        = transferDate,
                CreatedAt   = now
            };

            // Update balances
            fromAccount.Balance -= request.Amount;
            toAccount.Balance   += request.Amount;

            await _accountRepo.UpdateAsync(fromAccount);
            await _accountRepo.UpdateAsync(toAccount);
            await _txRepo.CreateAsync(txOut);
            await _txRepo.CreateAsync(txIn);

            return (MapToResponse(txOut), MapToResponse(txIn));
        }

        /// <summary>Ledger delta applied to an account when this transaction was recorded (same rule as <see cref="CreateAsync"/>).</summary>
        private static decimal BalanceContribution(string type, decimal amount)
        {
            return string.Equals(type, "income", StringComparison.OrdinalIgnoreCase)
                ? amount
                : -amount;
        }

        // ── Mapping ──────────────────────────────────────────────────────────

        private static TransactionResponse MapToResponse(Transaction t) => new()
        {
            Id          = t.Id,
            AccountId   = t.AccountId,
            Name        = t.Name,
            Category    = t.Category,
            Amount      = t.Amount,
            Type        = t.Type,
            Description = t.Description,
            Status      = t.Status,
            Date        = t.Date,
            CreatedAt   = t.CreatedAt
        };
    }
}
