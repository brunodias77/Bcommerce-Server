using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Infrastructure.Configurations;

/// <summary>
/// Configuração específica para a entidade AccountToken
/// </summary>
public class AccountTokenConfiguration : IEntityTypeConfiguration<AccountToken>
{
    public void Configure(EntityTypeBuilder<AccountToken> builder)
    {
        // Configuração da tabela
        builder.ToTable("account_tokens");

        // Chave primária
        builder.HasKey(e => e.Id);

        // Configurações de propriedades
        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuid_generate_v4()")
            .HasComment("Identificador único do token");

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(450)
            .HasComment("ID do usuário (mesmo ID do AspNetUsers.Id)");

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("Valor único do token (ex: hash SHA256)");

        builder.Property(e => e.TokenType)
            .IsRequired()
            .HasConversion<string>() // Converte enum para string no banco
            .HasMaxLength(50)
            .HasComment("Tipo do token (ACCOUNT_ACTIVATION / PASSWORD_RESET)");

        builder.Property(e => e.ExpiresAt)
            .IsRequired()
            .HasComment("Data e hora de expiração do token");

        builder.Property(e => e.UsedAt)
            .HasComment("Data e hora em que o token foi utilizado");

        builder.Property(e => e.RevokedAt)
            .HasComment("Data e hora em que o token foi revogado (se aplicável)");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Data de criação do registro");

        // Propriedade calculada (não mapeada para o banco)
        builder.Ignore(e => e.IsActive);

        // Índices
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("idx_account_tokens_user_id");

        builder.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("idx_account_tokens_token");

        // Relacionamento com User
        builder.HasOne(e => e.User)
            .WithMany(u => u.AccountTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}