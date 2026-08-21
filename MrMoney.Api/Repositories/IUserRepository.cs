using System.Collections.Generic;
using System.Threading.Tasks;
using MrMoney.Api.Models;

namespace MrMoney.Api.Repositories
{
    public interface IUserRepository
    {
        Task<List<UserProfile>> GetAllAsync();
        Task<UserProfile?> GetByIdAsync(string userId);
        Task<UserProfile?> GetByEmailAsync(string email);
        Task<UserProfile> CreateAsync(UserProfile user);
        Task<UserProfile> UpdateAsync(UserProfile user);
        Task DeleteAsync(string userId);
    }
}
