using AuthService.Domain.Entities;
using BuildingBlocks.Data;

namespace AuthService.Domain.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken> 
{
    
}