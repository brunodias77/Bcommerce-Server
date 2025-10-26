using AuthService.Domain.Entities;

namespace AuthService.Domain.Services;

public interface ILoggedUser
{
    public Task<User> User();
}