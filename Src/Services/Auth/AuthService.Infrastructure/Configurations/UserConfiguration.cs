using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Configurations;

/// <summary>
/// Configuração específica para a entidade User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configuração da tabela
        builder.ToTable("users");

        // Configurações de propriedades
        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Nome completo do usuário");

        builder.Property(e => e.Phone)
            .HasMaxLength(20)
            .HasComment("Telefone de contato");

        builder.Property(e => e.BirthDate)
            .HasComment("Data de nascimento");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Data de criação do registro");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Data da última atualização");

        builder.Property(e => e.LastLoginAt)
            .HasComment("Último login do usuário");

        // Índices
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("idx_users_email");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("idx_users_created_at");

        // Relacionamentos
        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.SecurityLogs)
            .WithOne(sl => sl.User)
            .HasForeignKey(sl => sl.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}