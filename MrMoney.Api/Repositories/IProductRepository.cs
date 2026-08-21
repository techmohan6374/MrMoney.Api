using System.Collections.Generic;
using System.Threading.Tasks;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(string id);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task DeleteAsync(string id);
    }
}
