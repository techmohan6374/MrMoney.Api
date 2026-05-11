using MrMoney.Api.DTOs;

namespace MrMoney.Api.Services
{
    public interface ITransactionService
    {
        Task<PagedTransactionResponse> GetAllAsync(string userId, TransactionFilterRequest filter);
        Task<TransactionResponse> GetByIdAsync(string userId, string transactionId);
        Task<TransactionResponse> CreateAsync(string userId, CreateTransactionRequest request);
        Task<TransactionResponse> UpdateAsync(string userId, string transactionId, UpdateTransactionRequest request);
        Task DeleteAsync(string userId, string transactionId);
        Task<(TransactionResponse From, TransactionResponse To)> TransferAsync(string userId, TransferRequest request);
    }
}
