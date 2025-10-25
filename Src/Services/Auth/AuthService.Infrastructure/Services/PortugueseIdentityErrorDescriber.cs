using Microsoft.AspNetCore.Identity;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Descritor de erros personalizado para traduzir mensagens do Identity para português
/// </summary>
public class PortugueseIdentityErrorDescriber : IdentityErrorDescriber
{
    #region Password Errors

    /// <summary>
    /// Retorna um erro indicando que a senha deve conter pelo menos um dígito
    /// </summary>
    public override IdentityError PasswordRequiresDigit()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresDigit),
            Description = "A senha deve conter pelo menos um dígito ('0'-'9')."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que a senha deve conter pelo menos uma letra minúscula
    /// </summary>
    public override IdentityError PasswordRequiresLower()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresLower),
            Description = "A senha deve conter pelo menos uma letra minúscula ('a'-'z')."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que a senha deve conter pelo menos uma letra maiúscula
    /// </summary>
    public override IdentityError PasswordRequiresUpper()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresUpper),
            Description = "A senha deve conter pelo menos uma letra maiúscula ('A'-'Z')."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que a senha deve conter pelo menos um caractere especial
    /// </summary>
    public override IdentityError PasswordRequiresNonAlphanumeric()
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresNonAlphanumeric),
            Description = "A senha deve conter pelo menos um caractere especial."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que a senha é muito curta
    /// </summary>
    public override IdentityError PasswordTooShort(int length)
    {
        return new IdentityError
        {
            Code = nameof(PasswordTooShort),
            Description = $"A senha deve ter pelo menos {length} caracteres."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que a senha deve ter mais caracteres únicos
    /// </summary>
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars)
    {
        return new IdentityError
        {
            Code = nameof(PasswordRequiresUniqueChars),
            Description = $"A senha deve conter pelo menos {uniqueChars} caracteres únicos."
        };
    }

    #endregion

    #region User Errors

    /// <summary>
    /// Retorna um erro indicando que o nome de usuário é inválido
    /// </summary>
    public override IdentityError InvalidUserName(string? userName)
    {
        return new IdentityError
        {
            Code = nameof(InvalidUserName),
            Description = $"O nome de usuário '{userName}' é inválido. Pode conter apenas letras, números e os caracteres '-', '.', '_', '@', '+'."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que o nome de usuário já está em uso
    /// </summary>
    public override IdentityError DuplicateUserName(string userName)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateUserName),
            Description = $"O nome de usuário '{userName}' já está sendo usado."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que o email é inválido
    /// </summary>
    public override IdentityError InvalidEmail(string? email)
    {
        return new IdentityError
        {
            Code = nameof(InvalidEmail),
            Description = $"O email '{email}' é inválido."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que o email já está em uso
    /// </summary>
    public override IdentityError DuplicateEmail(string email)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateEmail),
            Description = $"O email '{email}' já está sendo usado."
        };
    }

    #endregion

    #region Role Errors

    /// <summary>
    /// Retorna um erro indicando que o nome da role é inválido
    /// </summary>
    public override IdentityError InvalidRoleName(string? role)
    {
        return new IdentityError
        {
            Code = nameof(InvalidRoleName),
            Description = $"O nome da função '{role}' é inválido."
        };
    }

    /// <summary>
    /// Retorna um erro indicando que a role já existe
    /// </summary>
    public override IdentityError DuplicateRoleName(string role)
    {
        return new IdentityError
        {
            Code = nameof(DuplicateRoleName),
            Description = $"A função '{role}' já existe."
        };
    }

    #endregion

    #region Token Errors

    /// <summary>
    /// Retorna um erro indicando que o token é inválido
    /// </summary>
    public override IdentityError InvalidToken()
    {
        return new IdentityError
        {
            Code = nameof(InvalidToken),
            Description = "Token inválido."
        };
    }

    #endregion

    #region Lockout Errors

    /// <summary>
    /// Retorna um erro indicando que o usuário está bloqueado
    /// </summary>
    public override IdentityError UserLockoutNotEnabled()
    {
        return new IdentityError
        {
            Code = nameof(UserLockoutNotEnabled),
            Description = "O bloqueio não está habilitado para este usuário."
        };
    }

    #endregion

    #region General Errors

    /// <summary>
    /// Retorna um erro padrão
    /// </summary>
    public override IdentityError DefaultError()
    {
        return new IdentityError
        {
            Code = nameof(DefaultError),
            Description = "Ocorreu um erro desconhecido."
        };
    }

    /// <summary>
    /// Retorna um erro indicando falha na operação
    /// </summary>
    public override IdentityError ConcurrencyFailure()
    {
        return new IdentityError
        {
            Code = nameof(ConcurrencyFailure),
            Description = "Falha de concorrência otimista. O objeto foi modificado."
        };
    }

    #endregion
}