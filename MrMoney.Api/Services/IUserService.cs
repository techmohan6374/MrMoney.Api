using MrMoney.Api.DTOs;

namespace MrMoney.Api.Services
{
    public interface IUserService
    {
        Task<UserProfileResponse> GetProfileAsync(string userId);
        Task<UserProfileResponse> UpdateProfileAsync(string userId, UpdateUserProfileRequest request);
    }
}
