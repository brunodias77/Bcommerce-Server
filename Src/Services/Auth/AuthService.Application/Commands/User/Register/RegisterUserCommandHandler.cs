using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AuthService.Domain.Entities;
using AuthService.Domain.Services;
using BuildingBlocks.Mediator;
using BuildingBlocks.Results;
using BuildingBlocks.Data;
using BuildingBlocks.Validations;

namespace AuthService.Application.Commands;




public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, ApiResponse<RegisterUserResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RegisterUserCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;


    public async Task<ApiResponse<RegisterUserResponse>> HandleAsync(RegisterUserCommand request, CancellationToken cancellationToken = default)
    {
        return null;
    }

   
}