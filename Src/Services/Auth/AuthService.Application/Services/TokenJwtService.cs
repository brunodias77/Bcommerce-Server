using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using AuthService.Domain.Entities;
using AuthService.Domain.Services.Token;

namespace AuthService.Application.Services;

/// <summary>
/// Implementação do serviço de tokens JWT
/// Responsável por gerar, validar e extrair informações de tokens JWT
/// </summary>
public class TokenJwtService : ITokenJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public TokenJwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _tokenHandler = new JwtSecurityTokenHandler();
        
        // Configurar parâmetros de validação de token
        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Remove tolerância de tempo padrão
        };
    }

    /// <summary>
    /// Gera um access token JWT para o usuário com suas roles
    /// </summary>
    public async Task<string> GenerateAccessTokenAsync(User user, IList<string> roles)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        if (string.IsNullOrEmpty(user.Id)) throw new ArgumentException("User ID cannot be null or empty", nameof(user));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("FullName", user.FullName)
        };

        // Adicionar roles como claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Adicionar telefone se disponível
        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new Claim(ClaimTypes.MobilePhone, user.PhoneNumber));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = credentials
        };

        var token = _tokenHandler.CreateToken(tokenDescriptor);
        return await Task.FromResult(_tokenHandler.WriteToken(token));
    }

    /// <summary>
    /// Gera um refresh token aleatório
    /// </summary>
    public async Task<string> GenerateRefreshTokenAsync()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return await Task.FromResult(Convert.ToBase64String(randomBytes));
    }

    /// <summary>
    /// Valida um token JWT
    /// </summary>
    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);
            
            // Verificar se é um JWT válido
            if (validatedToken is not JwtSecurityToken jwtToken || 
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return await Task.FromResult(principal);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Obtém o ClaimsPrincipal de um token expirado (para refresh)
    /// </summary>
    public async Task<ClaimsPrincipal?> GetPrincipalFromExpiredTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            // Criar parâmetros de validação sem validar tempo de vida
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false, // Não validar expiração
                ClockSkew = TimeSpan.Zero
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // Verificar se é um JWT válido
            if (validatedToken is not JwtSecurityToken jwtToken || 
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return await Task.FromResult(principal);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extrai o ID do usuário de um token JWT
    /// </summary>
    public async Task<string?> GetUserIdFromTokenAsync(string token)
    {
        var principal = await GetPrincipalFromExpiredTokenAsync(token);
        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }
}