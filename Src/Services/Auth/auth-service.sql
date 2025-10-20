CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =============================================
-- AUTH SERVICE - auth_service_db
-- =============================================
-- NOTA: ASP.NET Core Identity já cria automaticamente as tabelas padrão:
-- AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims,
-- AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims
-- =============================================
CREATE TABLE users (
    -- Armazena informações adicionais de perfil do usuário
    user_id VARCHAR(450) PRIMARY KEY,
    -- ID do usuário (mesmo ID do AspNetUsers)
    full_name VARCHAR(255) NOT NULL,
    -- Nome completo do usuário
    phone VARCHAR(20),
    -- Telefone de contato
    birth_date DATE,
    -- Data de nascimento
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data de criação do registro
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data da última atualização
    last_login_at TIMESTAMP -- Último login do usuário
);

CREATE TABLE refresh_tokens (
    -- Armazena tokens de atualização (para renovar JWTs)
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- Identificador único do token
    user_id VARCHAR(450) NOT NULL,
    -- ID do usuário associado ao token
    token VARCHAR(500) UNIQUE NOT NULL,
    -- Valor do token de atualização
    expires_at TIMESTAMP NOT NULL,
    -- Data e hora de expiração
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data de criação
    revoked_at TIMESTAMP,
    -- Data de revogação (caso o token seja invalidado)
    is_active BOOLEAN GENERATED ALWAYS AS (
        revoked_at IS NULL
        AND expires_at > CURRENT_TIMESTAMP
    ) STORED -- Campo calculado que indica se o token está ativo
);

CREATE TABLE security_logs (
    -- Registra eventos de segurança (login, falhas, etc.)
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- Identificador do log
    user_id VARCHAR(450),
    -- Usuário relacionado ao evento
    event_type VARCHAR(50) NOT NULL,
    -- Tipo de evento (ex: LOGIN_SUCCESS, LOGIN_FAILED)
    ip_address VARCHAR(45),
    -- Endereço IP do evento
    message TEXT,
    -- Detalhes adicionais
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- Data de criação do log
);
