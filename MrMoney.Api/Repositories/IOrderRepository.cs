using System.Collections.Generic;
using System.Threading.Tasks;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public interface IOrderRepository
    {
        Task<List<Order>> GetAllAsync();
        Task<List<Order>> GetByUserIdAsync(string userId);
        Task<Order?> GetByIdAsync(string id);
        Task<Order> CreateAsync(Order order);
        Task<Order> UpdateStatusAsync(string id, string status);
    }
}
