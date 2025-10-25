using System.Text.RegularExpressions;
using AuthService.Domain.Entities;
using BuildingBlocks.Validations;

namespace AuthService.Domain.Validations;

/// <summary>
/// Classe de validação para a entidade User
/// Implementa validações de formato, tamanho e obrigatoriedade
/// </summary>
public static class UserValidation
{
    /// <summary>
    /// Valida todos os campos da entidade User
    /// </summary>
    /// <param name="user">Usuário a ser validado</param>
    /// <returns>ValidationHandler com os erros encontrados</returns>
    public static ValidationHandler Validate(User user)
    {
        var handler = new ValidationHandler();

        ValidateFullName(user.FullName, handler);
        ValidateEmail(user.Email, handler);
        ValidateUserName(user.UserName, handler);
        ValidatePhone(user.Phone, handler);
        ValidateBirthDate(user.BirthDate, handler);

        return handler;
    }

    /// <summary>
    /// Valida o nome completo do usuário
    /// </summary>
    /// <param name="fullName">Nome completo</param>
    /// <param name="handler">Handler de validação</param>
    public static void ValidateFullName(string? fullName, ValidationHandler handler)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            handler.Add("O nome completo é obrigatório.");
            return;
        }

        if (fullName.Length < 2)
        {
            handler.Add("O nome completo deve ter pelo menos 2 caracteres.");
        }

        if (fullName.Length > 100)
        {
            handler.Add("O nome completo não pode exceder 100 caracteres.");
        }

        // Verifica se contém apenas letras, espaços, acentos e hífens
        if (!Regex.IsMatch(fullName, @"^[a-zA-ZÀ-ÿ\s\-']+$"))
        {
            handler.Add("O nome completo deve conter apenas letras, espaços, acentos e hífens.");
        }

        // Verifica se não contém apenas espaços
        if (fullName.Trim().Length == 0)
        {
            handler.Add("O nome completo não pode conter apenas espaços.");
        }
    }

    /// <summary>
    /// Valida o email do usuário
    /// </summary>
    /// <param name="email">Email</param>
    /// <param name="handler">Handler de validação</param>
    public static void ValidateEmail(string? email, ValidationHandler handler)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            handler.Add("O email é obrigatório.");
            return;
        }

        if (email.Length > 256)
        {
            handler.Add("O email não pode exceder 256 caracteres.");
        }

        // Validação de formato de email
        var emailRegex = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        if (!Regex.IsMatch(email, emailRegex))
        {
            handler.Add("O formato do email é inválido.");
        }
    }

    /// <summary>
    /// Valida o nome de usuário
    /// </summary>
    /// <param name="userName">Nome de usuário</param>
    /// <param name="handler">Handler de validação</param>
    public static void ValidateUserName(string? userName, ValidationHandler handler)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            handler.Add("O nome de usuário é obrigatório.");
            return;
        }

        if (userName.Length < 3)
        {
            handler.Add("O nome de usuário deve ter pelo menos 3 caracteres.");
        }

        if (userName.Length > 50)
        {
            handler.Add("O nome de usuário não pode exceder 50 caracteres.");
        }

        // Verifica se contém apenas letras, números, pontos, hífens e underscores
        if (!Regex.IsMatch(userName, @"^[a-zA-Z0-9._-]+$"))
        {
            handler.Add("O nome de usuário deve conter apenas letras, números, pontos, hífens e underscores.");
        }

        // Não pode começar ou terminar com ponto, hífen ou underscore
        if (Regex.IsMatch(userName, @"^[._-]|[._-]$"))
        {
            handler.Add("O nome de usuário não pode começar ou terminar com ponto, hífen ou underscore.");
        }
    }

    /// <summary>
    /// Valida o telefone do usuário
    /// </summary>
    /// <param name="phone">Telefone</param>
    /// <param name="handler">Handler de validação</param>
    public static void ValidatePhone(string? phone, ValidationHandler handler)
    {
        // Telefone é opcional
        if (string.IsNullOrWhiteSpace(phone))
            return;

        if (phone.Length > 20)
        {
            handler.Add("O telefone não pode exceder 20 caracteres.");
        }

        // Remove espaços, parênteses, hífens e sinais de mais para validação
        var cleanPhone = Regex.Replace(phone, @"[\s\(\)\-\+]", "");

        if (cleanPhone.Length < 8)
        {
            handler.Add("O telefone deve ter pelo menos 8 dígitos.");
        }

        if (cleanPhone.Length > 15)
        {
            handler.Add("O telefone não pode ter mais de 15 dígitos.");
        }

        // Verifica se contém apenas números após limpeza
        if (!Regex.IsMatch(cleanPhone, @"^\d+$"))
        {
            handler.Add("O telefone deve conter apenas números, espaços, parênteses, hífens e sinal de mais.");
        }
    }

    /// <summary>
    /// Valida a data de nascimento do usuário
    /// </summary>
    /// <param name="birthDate">Data de nascimento</param>
    /// <param name="handler">Handler de validação</param>
    public static void ValidateBirthDate(DateTime? birthDate, ValidationHandler handler)
    {
        // Data de nascimento é opcional
        if (!birthDate.HasValue)
            return;

        var today = DateTime.Today;
        var age = today.Year - birthDate.Value.Year;

        // Ajusta a idade se o aniversário ainda não passou este ano
        if (birthDate.Value.Date > today.AddYears(-age))
            age--;

        if (birthDate.Value.Date > today)
        {
            handler.Add("A data de nascimento não pode ser uma data futura.");
        }

        if (age < 13)
        {
            handler.Add("O usuário deve ter pelo menos 13 anos de idade.");
        }

        if (age > 120)
        {
            handler.Add("A data de nascimento não pode indicar uma idade superior a 120 anos.");
        }

        // Verifica se a data não é muito antiga (antes de 1900)
        if (birthDate.Value.Year < 1900)
        {
            handler.Add("A data de nascimento não pode ser anterior ao ano 1900.");
        }
    }

    /// <summary>
    /// Valida apenas os campos obrigatórios para criação de usuário
    /// </summary>
    /// <param name="user">Usuário a ser validado</param>
    /// <returns>ValidationHandler com os erros encontrados</returns>
    public static ValidationHandler ValidateForCreation(User user)
    {
        var handler = new ValidationHandler();

        ValidateFullName(user.FullName, handler);
        ValidateEmail(user.Email, handler);
        // UserName não é validado pois no ASP.NET Identity o UserName é igual ao Email

        return handler;
    }

    /// <summary>
    /// Valida apenas os campos para atualização de perfil
    /// </summary>
    /// <param name="user">Usuário a ser validado</param>
    /// <returns>ValidationHandler com os erros encontrados</returns>
    public static ValidationHandler ValidateForUpdate(User user)
    {
        var handler = new ValidationHandler();

        ValidateFullName(user.FullName, handler);
        ValidatePhone(user.Phone, handler);
        ValidateBirthDate(user.BirthDate, handler);

        return handler;
    }
}