using Ride_HailingApi.Entities;

namespace Ride_HailingApi.Services.Interface;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user); 
}