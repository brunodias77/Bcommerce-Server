CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- AUTH SERVICE - auth_service_db
-- =============================================
-- NOTA: ASP.NET Core Identity já cria automaticamente as tabelas padrão:
-- AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims,
-- AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims
-- =============================================
CREATE TYPE account_token_type AS ENUM ('ACCOUNT_ACTIVATION', 'PASSWORD_RESET');

-- ============================================================
-- TABELA: account_tokens
-- Armazena tokens de ativação e redefinição de senha
-- ============================================================
CREATE TABLE account_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- Identificador único do token
    user_id VARCHAR(450) NOT NULL,
    -- ID do usuário (mesmo ID do AspNetUsers.Id)
    token VARCHAR(500) UNIQUE NOT NULL,
    -- Valor único do token (ex: hash SHA256)
    token_type account_token_type NOT NULL,
    -- Tipo do token (ACCOUNT_ACTIVATION / PASSWORD_RESET)
    expires_at TIMESTAMP NOT NULL,
    -- Data e hora de expiração do token
    used_at TIMESTAMP,
    -- Data e hora em que o token foi utilizado
    revoked_at TIMESTAMP,
    -- Data e hora em que o token foi revogado (se aplicável)
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data de criação do registro
    is_active BOOLEAN GENERATED ALWAYS AS (
        -- Campo calculado que indica se o token está ativo
        used_at IS NULL
        AND revoked_at IS NULL
        AND expires_at > CURRENT_TIMESTAMP
    ) STORED,
    CONSTRAINT fk_account_tokens_user -- Foreign key: vincula token ao usuário do Identity
    FOREIGN KEY (user_id) REFERENCES aspnetusers (id) ON DELETE CASCADE -- Se o usuário for excluído, remove os tokens
);

-- ============================================================
-- TABELA: users
-- Armazena informações adicionais do perfil do usuário
-- ============================================================
CREATE TABLE users (
    user_id VARCHAR(450) PRIMARY KEY,
    -- ID do usuário (referência direta ao AspNetUsers.Id)
    full_name VARCHAR(255) NOT NULL,
    -- Nome completo do usuário
    phone VARCHAR(20),
    -- Telefone de contato
    birth_date DATE,
    -- Data de nascimento
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data de criação do registro
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data da última atualização do perfil
    last_login_at TIMESTAMP,
    -- Último login registrado do usuário
    CONSTRAINT fk_users_identity_user -- Foreign key: referência para AspNetUsers
    FOREIGN KEY (user_id) REFERENCES aspnetusers (id) ON DELETE CASCADE -- Exclui o perfil se o usuário for deletado
);

-- ============================================================
-- TABELA: refresh_tokens
-- Armazena tokens de atualização (refresh tokens para JWT)
-- ============================================================
CREATE TABLE refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- Identificador único do token
    user_id VARCHAR(450) NOT NULL,
    -- ID do usuário vinculado ao token
    token VARCHAR(500) UNIQUE NOT NULL,
    -- Valor único do refresh token
    expires_at TIMESTAMP NOT NULL,
    -- Data e hora de expiração do token
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data de criação
    revoked_at TIMESTAMP,
    -- Data em que o token foi revogado (se aplicável)
    is_active BOOLEAN GENERATED ALWAYS AS (
        -- Campo calculado que indica se o token ainda é válido
        revoked_at IS NULL
        AND expires_at > CURRENT_TIMESTAMP
    ) STORED,
    CONSTRAINT fk_refresh_tokens_user -- Foreign key: relação com AspNetUsers
    FOREIGN KEY (user_id) REFERENCES aspnetusers (id) ON DELETE CASCADE -- Remove tokens se o usuário for excluído
);

-- ============================================================
-- TABELA: security_logs
-- Registra eventos de segurança (login, falha, bloqueio, etc.)
-- ============================================================
CREATE TABLE security_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- Identificador único do log
    user_id VARCHAR(450),
    -- ID do usuário (pode ser NULL se falha antes do login)
    event_type VARCHAR(50) NOT NULL,
    -- Tipo de evento (LOGIN_SUCCESS, LOGIN_FAILED, etc.)
    ip_address VARCHAR(45),
    -- Endereço IP do evento (IPv4 ou IPv6)
    message TEXT,
    -- Detalhes adicionais do evento
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data e hora em que o log foi criado
    CONSTRAINT fk_security_logs_user -- Foreign key: referência opcional ao usuário do Identity
    FOREIGN KEY (user_id) REFERENCES aspnetusers (id) ON DELETE
    SET
        NULL -- Se o usuário for excluído, mantém o log mas zera o user_id
);