using MrMoney.Api.DTOs;
using MrMoney.Api.Repositories;

namespace MrMoney.Api.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<UserProfileResponse> GetProfileAsync(string userId)
        {
            var user = await _userRepo.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            return MapToResponse(user);
        }

        public async Task<UserProfileResponse> UpdateProfileAsync(string userId, UpdateUserProfileRequest request)
        {
            var user = await _userRepo.GetByIdAsync(userId)
                ?? throw new KeyNotFoundException($"User '{userId}' not found.");

            if (request.Name               != null) user.Name               = request.Name.Trim();
            if (request.Picture            != null) user.Picture             = request.Picture.Trim();
            if (request.Currency           != null) user.Currency            = request.Currency;
            if (request.EmailNotifications != null) user.EmailNotifications  = request.EmailNotifications.Value;
            if (request.Theme              != null) user.Theme               = request.Theme;

            var updated = await _userRepo.UpdateAsync(user);
            return MapToResponse(updated);
        }

        private static UserProfileResponse MapToResponse(Models.UserProfile u) => new()
        {
            Id                 = u.Id,
            Email              = u.Email,
            Name               = u.Name,
            Picture            = u.Picture,
            Currency           = u.Currency,
            EmailNotifications = u.EmailNotifications,
            Theme              = u.Theme,
            CreatedAt          = u.CreatedAt,
            LastLoginAt        = u.LastLoginAt
        };
    }
}
