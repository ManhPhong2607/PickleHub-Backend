using Microsoft.AspNetCore.Http;
using PickleHub.Common.Exceptions;
using PickleHub.Common.Interfaces;
using System.Security.Claims;

namespace PickleHub.Common.Service
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId
        {
            get
            {
                var claimValue = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

                if (Guid.TryParse(claimValue, out var claimId))
                    return claimId;

                throw new UnauthorizedException("Không xác định được người dùng.");
            }
        }

        public string Role
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.Role)?.Value
                    ?? _httpContextAccessor.HttpContext?.User.FindFirst("role")?.Value
                    ?? string.Empty;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                var userId = _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

                var isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true
                    && !string.IsNullOrWhiteSpace(userId)
                    && Guid.TryParse(userId, out _);

                return isAuthenticated;
            }
        }

        public string Email
        {
            get
            {
                return _httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.Email)?.Value
                    ?? _httpContextAccessor.HttpContext?.User.FindFirst("email")?.Value
                    ?? string.Empty;
            }
        }
    }
}
