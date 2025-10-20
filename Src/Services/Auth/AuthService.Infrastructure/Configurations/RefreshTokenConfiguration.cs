using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Configurations;

/// <summary>
/// Configuração específica para a entidade RefreshToken
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Configuração da tabela
        builder.ToTable("refresh_tokens");

        // Chave primária
        builder.HasKey(e => e.Id);

        // Configurações de propriedades
        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuid_generate_v4()")
            .HasComment("Identificador único do token");

        builder.Property(e => e.UserId)
            .IsRequired()
            .HasMaxLength(450)
            .HasComment("ID do usuário associado ao token");

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment("Valor do token de atualização");

        builder.Property(e => e.ExpiresAt)
            .IsRequired()
            .HasComment("Data e hora de expiração");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Data de criação");

        builder.Property(e => e.RevokedAt)
            .HasComment("Data de revogação (caso o token seja invalidado)");

        // Propriedade calculada (não mapeada para o banco)
        builder.Ignore(e => e.IsActive);

        // Índices
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("idx_refresh_tokens_user_id");

        builder.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("idx_refresh_tokens_token");

        // Relacionamento com User
        builder.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}