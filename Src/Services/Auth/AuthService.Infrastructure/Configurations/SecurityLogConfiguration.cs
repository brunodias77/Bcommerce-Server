using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Configurations;

/// <summary>
/// Configuração específica para a entidade SecurityLog
/// </summary>
public class SecurityLogConfiguration : IEntityTypeConfiguration<SecurityLog>
{
    public void Configure(EntityTypeBuilder<SecurityLog> builder)
    {
        // Configuração da tabela
        builder.ToTable("security_logs");

        // Chave primária
        builder.HasKey(e => e.Id);

        // Configurações de propriedades
        builder.Property(e => e.Id)
            .HasDefaultValueSql("uuid_generate_v4()")
            .HasComment("Identificador do log");

        builder.Property(e => e.UserId)
            .HasMaxLength(450)
            .HasComment("Usuário relacionado ao evento");

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Tipo de evento (ex: LOGIN_SUCCESS, LOGIN_FAILED)");

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45)
            .HasComment("Endereço IP do evento");

        builder.Property(e => e.Message)
            .HasComment("Detalhes adicionais");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .HasComment("Data de criação do log");

        // Índices
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("idx_security_logs_user_id");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("idx_security_logs_created_at");

        // Relacionamento com User (opcional)
        builder.HasOne(e => e.User)
            .WithMany(u => u.SecurityLogs)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}