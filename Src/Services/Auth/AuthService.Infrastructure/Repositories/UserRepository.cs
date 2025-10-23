using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Data;

namespace AuthService.Infrastructure.Repositories;

/// <summary>
/// Implementação do repositório para a entidade User
/// Utiliza Entity Framework Core para operações de banco de dados
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;

    /// <summary>
    /// Construtor que recebe o contexto de banco via injeção de dependência
    /// </summary>
    /// <param name="context">Contexto de banco de dados</param>
    public UserRepository(AuthDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Busca um usuário por ID
    /// </summary>
    /// <param name="id">ID do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Usuário encontrado ou null</returns>
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id.ToString(), cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao buscar usuário por ID {id}", ex);
        }
    }

    /// <summary>
    /// Busca todos os usuários
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de todos os usuários</returns>
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var users = await _context.Users
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            
            return users.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar todos os usuários", ex);
        }
    }

    /// <summary>
    /// Busca usuários que atendem ao predicado especificado
    /// </summary>
    /// <param name="predicate">Expressão de filtro</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista de usuários que atendem ao filtro</returns>
    public async Task<IReadOnlyList<User>> FindAsync(Expression<Func<User, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var users = await _context.Users
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync(cancellationToken);
            
            return users.AsReadOnly();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao buscar usuários com filtro", ex);
        }
    }

    /// <summary>
    /// Adiciona um novo usuário
    /// </summary>
    /// <param name="entity">Usuário a ser adicionado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Usuário adicionado</returns>
    public async Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entry = await _context.Users.AddAsync(entity, cancellationToken);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao adicionar usuário", ex);
        }
    }

    /// <summary>
    /// Atualiza um usuário existente
    /// </summary>
    /// <param name="entity">Usuário a ser atualizado</param>
    public void Update(User entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _context.Users.Update(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao atualizar usuário", ex);
        }
    }

    /// <summary>
    /// Remove um usuário
    /// </summary>
    /// <param name="entity">Usuário a ser removido</param>
    public void Remove(User entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _context.Users.Remove(entity);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Erro ao remover usuário", ex);
        }
    }
}