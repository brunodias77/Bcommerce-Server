# Auth Service - Commands e Queries

## 📋 Visão Geral

Este documento descreve todos os Commands e Queries planejados para o Auth Service, seguindo os padrões CQRS e Mediator definidos no BuildingBlocks.

---

## 🔐 Autenticação

### Commands

#### **RegisterUserCommand**
Registra um novo usuário no sistema.

**Request:**
```csharp
- Email: string (required)
- Password: string (required, min 8 chars)
- FullName: string (required, max 255 chars)
- Phone: string (optional, max 20 chars)
- BirthDate: DateTime? (optional)
```

**Response:**
```csharp
- UserId: string
- Message: string
```

**Validações:**
- Email único no sistema
- Senha atende requisitos (maiúscula, minúscula, número)
- Email em formato válido
- Nome completo não vazio

**Efeitos Colaterais:**
- Cria registro na tabela `AspNetUsers`
- Cria registro na tabela `users` (perfil)
- Gera `AccountToken` para ativação de conta
- Publica evento `UserRegisteredEvent`

---

#### **LoginCommand**
Realiza login do usuário e gera tokens de acesso.

**Request:**
```csharp
- Email: string (required)
- Password: string (required)
- IpAddress: string (optional)
- UserAgent: string (optional)
```

**Response:**
```csharp
- AccessToken: string (JWT)
- RefreshToken: string
- ExpiresIn: int (segundos)
- TokenType: string ("Bearer")
```

**Validações:**
- Credenciais válidas
- Conta ativa (não bloqueada)
- Email confirmado (se configurado)

**Efeitos Colaterais:**
- Cria `RefreshToken`
- Atualiza `LastLoginAt` do usuário
- Registra `SecurityLog` (LOGIN_SUCCESS)
- Publica evento `UserLoggedInEvent`

---

#### **RefreshTokenCommand**
Renova o token de acesso usando um refresh token válido.

**Request:**
```csharp
- RefreshToken: string (required)
```

**Response:**
```csharp
- AccessToken: string (novo JWT)
- RefreshToken: string (novo refresh token)
- ExpiresIn: int
- TokenType: string
```

**Validações:**
- Refresh token existe e está ativo
- Refresh token não expirado
- Refresh token não revogado
- Usuário ainda ativo

**Efeitos Colaterais:**
- Revoga refresh token antigo
- Cria novo `RefreshToken`
- Registra `SecurityLog` (TOKEN_REFRESHED)

---

#### **LogoutCommand**
Realiza logout do usuário revogando seus tokens.

**Request:**
```csharp
- UserId: string (required)
- RefreshToken: string (optional)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Efeitos Colaterais:**
- Revoga refresh token específico ou todos do usuário
- Registra `SecurityLog` (LOGOUT)
- Publica evento `UserLoggedOutEvent`

---

#### **ForgotPasswordCommand**
Inicia processo de recuperação de senha.

**Request:**
```csharp
- Email: string (required)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Validações:**
- Email existe no sistema

**Efeitos Colaterais:**
- Gera `AccountToken` tipo PASSWORD_RESET
- Publica evento `PasswordResetRequestedEvent` (para envio de email)
- Registra `SecurityLog` (PASSWORD_RESET_REQUESTED)

---

#### **ResetPasswordCommand**
Redefine a senha do usuário usando token válido.

**Request:**
```csharp
- Token: string (required)
- NewPassword: string (required, min 8 chars)
- ConfirmPassword: string (required)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Validações:**
- Token existe e está ativo
- Token não expirado
- Token tipo PASSWORD_RESET
- Senhas coincidem
- Nova senha atende requisitos

**Efeitos Colaterais:**
- Atualiza senha do usuário
- Marca token como `UsedAt`
- Revoga todos os refresh tokens do usuário
- Registra `SecurityLog` (PASSWORD_RESET_SUCCESS)
- Publica evento `PasswordResetCompletedEvent`

---

#### **ChangePasswordCommand**
Altera senha do usuário autenticado.

**Request:**
```csharp
- UserId: string (required)
- CurrentPassword: string (required)
- NewPassword: string (required, min 8 chars)
- ConfirmPassword: string (required)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Validações:**
- Senha atual correta
- Senhas novas coincidem
- Nova senha diferente da atual
- Nova senha atende requisitos

**Efeitos Colaterais:**
- Atualiza senha
- Revoga todos refresh tokens (força re-login)
- Registra `SecurityLog` (PASSWORD_CHANGED)
- Publica evento `PasswordChangedEvent`

---

#### **ActivateAccountCommand**
Ativa conta de usuário usando token de ativação.

**Request:**
```csharp
- Token: string (required)
```

**Response:**
```csharp
- Success: bool
- Message: string
- UserId: string
```

**Validações:**
- Token existe e está ativo
- Token não expirado
- Token tipo ACCOUNT_ACTIVATION
- Conta ainda não ativada

**Efeitos Colaterais:**
- Marca `EmailConfirmed = true` no Identity
- Marca token como `UsedAt`
- Registra `SecurityLog` (ACCOUNT_ACTIVATED)
- Publica evento `AccountActivatedEvent`

---

#### **ResendActivationTokenCommand**
Reenvia token de ativação de conta.

**Request:**
```csharp
- Email: string (required)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Validações:**
- Email existe
- Conta ainda não ativada

**Efeitos Colaterais:**
- Revoga tokens anteriores do tipo ACCOUNT_ACTIVATION
- Gera novo `AccountToken`
- Publica evento `ActivationTokenResentEvent`

---

## 👤 Gerenciamento de Perfil

### Commands

#### **UpdateProfileCommand**
Atualiza informações do perfil do usuário.

**Request:**
```csharp
- UserId: string (required)
- FullName: string (optional, max 255)
- Phone: string (optional, max 20)
- BirthDate: DateTime? (optional)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Validações:**
- Usuário existe
- Nome válido se fornecido
- Telefone em formato válido se fornecido

**Efeitos Colaterais:**
- Atualiza registro na tabela `users`
- Atualiza `UpdatedAt`
- Publica evento `ProfileUpdatedEvent`

---

#### **UpdateEmailCommand**
Inicia processo de alteração de email.

**Request:**
```csharp
- UserId: string (required)
- NewEmail: string (required)
- Password: string (required)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Validações:**
- Senha correta
- Novo email único no sistema
- Email em formato válido

**Efeitos Colaterais:**
- Gera token de confirmação
- Publica evento `EmailChangeRequestedEvent`
- Registra `SecurityLog` (EMAIL_CHANGE_REQUESTED)

---

#### **ConfirmEmailChangeCommand**
Confirma alteração de email usando token.

**Request:**
```csharp
- Token: string (required)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Validações:**
- Token válido e não expirado
- Novo email ainda disponível

**Efeitos Colaterais:**
- Atualiza email no Identity e tabela users
- Marca token como usado
- Registra `SecurityLog` (EMAIL_CHANGED)
- Publica evento `EmailChangedEvent`

---

#### **DeleteAccountCommand**
Desativa ou exclui conta de usuário.

**Request:**
```csharp
- UserId: string (required)
- Password: string (required)
- Reason: string (optional)
```

**Response:**
```csharp
- Success: bool
- Message: string
```

**Validações:**
- Senha correta
- Usuário existe

**Efeitos Colaterais:**
- Soft delete ou hard delete (conforme configuração)
- Revoga todos tokens
- Registra `SecurityLog` (ACCOUNT_DELETED)
- Publica evento `AccountDeletedEvent`

---

## 🔍 Queries

### **GetUserProfileQuery**
Obtém perfil completo do usuário.

**Request:**
```csharp
- UserId: string (required)
```

**Response:**
```csharp
- UserId: string
- Email: string
- FullName: string
- Phone: string
- BirthDate: DateTime?
- CreatedAt: DateTime
- UpdatedAt: DateTime
- LastLoginAt: DateTime?
- EmailConfirmed: bool
```

---

### **GetUserByEmailQuery**
Busca usuário por email.

**Request:**
```csharp
- Email: string (required)
```

**Response:**
```csharp
- UserId: string
- Email: string
- FullName: string
- EmailConfirmed: bool
- IsActive: bool
```

---

### **GetActiveRefreshTokensQuery**
Lista refresh tokens ativos do usuário.

**Request:**
```csharp
- UserId: string (required)
```

**Response:**
```csharp
- Tokens: List<RefreshTokenDto>
  - Id: Guid
  - Token: string (masked)
  - CreatedAt: DateTime
  - ExpiresAt: DateTime
  - IsActive: bool
```

---

### **GetSecurityLogsQuery**
Obtém logs de segurança do usuário (paginado).

**Request:**
```csharp
- UserId: string (required)
- PageNumber: int (default 1)
- PageSize: int (default 20)
- EventType: string (optional)
- StartDate: DateTime? (optional)
- EndDate: DateTime? (optional)
```

**Response:**
```csharp
- PagedResult<SecurityLogDto>
  - Id: Guid
  - EventType: string
  - IpAddress: string
  - Message: string
  - CreatedAt: DateTime
```

---

### **ValidateTokenQuery**
Valida se um token (JWT ou refresh) é válido.

**Request:**
```csharp
- Token: string (required)
- TokenType: TokenType (AccessToken/RefreshToken)
```

**Response:**
```csharp
- IsValid: bool
- UserId: string (if valid)
- ExpiresAt: DateTime (if valid)
- Message: string
```

---

### **GetUserRolesQuery**
Obtém roles (perfis) do usuário.

**Request:**
```csharp
- UserId: string (required)
```

**Response:**
```csharp
- Roles: List<string>
```

---

### **CheckEmailAvailabilityQuery**
Verifica se email está disponível.

**Request:**
```csharp
- Email: string (required)
```

**Response:**
```csharp
- IsAvailable: bool
```

---

## 📊 Domain Events

### Eventos Publicados pelo Auth Service

1. **UserRegisteredEvent**
   - **Quando:** Usuário criado com sucesso
   - **Dados:** UserId, Email, FullName
   - **Handlers:** Envio de email de ativação

2. **UserLoggedInEvent**
   - **Quando:** Login bem-sucedido
   - **Dados:** UserId, IpAddress, Timestamp
   - **Handlers:** Atualização de métricas

3. **UserLoggedOutEvent**
   - **Quando:** Logout realizado
   - **Dados:** UserId, Timestamp

4. **PasswordResetRequestedEvent**
   - **Quando:** Solicitação de reset de senha
   - **Dados:** UserId, Email, Token
   - **Handlers:** Envio de email com link

5. **PasswordResetCompletedEvent**
   - **Quando:** Senha resetada com sucesso
   - **Dados:** UserId, Timestamp

6. **PasswordChangedEvent**
   - **Quando:** Senha alterada pelo usuário
   - **Dados:** UserId, Timestamp
   - **Handlers:** Notificação por email

7. **AccountActivatedEvent**
   - **Quando:** Conta ativada
   - **Dados:** UserId, Email, Timestamp

8. **ProfileUpdatedEvent**
   - **Quando:** Perfil atualizado
   - **Dados:** UserId, UpdatedFields

9. **EmailChangedEvent**
   - **Quando:** Email alterado
   - **Dados:** UserId, OldEmail, NewEmail

10. **AccountDeletedEvent**
    - **Quando:** Conta excluída
    - **Dados:** UserId, Email, Reason

---

## 🔒 Segurança e Validações

### Políticas de Senha (Identity)
- Mínimo 8 caracteres
- Pelo menos 1 dígito
- Pelo menos 1 minúscula
- Pelo menos 1 maiúscula
- Caracteres especiais opcionais
- 1 caractere único

### Tokens
- **Access Token (JWT):** Expira em 15 minutos
- **Refresh Token:** Expira em 7 dias
- **Account Activation:** Expira em 24 horas
- **Password Reset:** Expira em 1 hora

### Rate Limiting (Recomendado)
- **Login:** 5 tentativas por 15 minutos
- **Password Reset:** 3 requisições por hora
- **Resend Activation:** 3 requisições por hora

---

## 📁 Estrutura de Pastas Sugerida

```
AuthService.Application/
├── Commands/
│   ├── Auth/
│   │   ├── RegisterUser/
│   │   │   ├── RegisterUserCommand.cs
│   │   │   ├── RegisterUserCommandHandler.cs
│   │   │   └── RegisterUserCommandValidator.cs
│   │   ├── Login/
│   │   ├── RefreshToken/
│   │   ├── Logout/
│   │   ├── ForgotPassword/
│   │   ├── ResetPassword/
│   │   ├── ChangePassword/
│   │   └── ActivateAccount/
│   └── Profile/
│       ├── UpdateProfile/
│       ├── UpdateEmail/
│       ├── ConfirmEmailChange/
│       └── DeleteAccount/
├── Queries/
│   ├── GetUserProfile/
│   ├── GetUserByEmail/
│   ├── GetActiveRefreshTokens/
│   ├── GetSecurityLogs/
│   ├── ValidateToken/
│   ├── GetUserRoles/
│   └── CheckEmailAvailability/
├── Events/
│   ├── UserRegisteredEvent.cs
│   ├── UserLoggedInEvent.cs
│   ├── PasswordResetRequestedEvent.cs
│   └── ... (outros eventos)
├── EventHandlers/
│   ├── UserRegisteredEventHandler.cs
│   └── ... (outros handlers)
└── DTOs/
    ├── Auth/
    └── Profile/
```

---

## 🎯 Próximos Passos

1. ✅ Implementar Commands de autenticação básica (Register, Login, Refresh)
2. ✅ Implementar Commands de recuperação de senha
3. ✅ Implementar Queries de perfil
4. ✅ Adicionar validações com FluentValidation
5. ✅ Implementar Events e Handlers
6. ✅ Adicionar testes unitários
7. ✅ Implementar rate limiting
8. ✅ Documentar APIs no Swagger

---

## 📝 Observações Importantes

- Todos os Commands devem ter validação via FluentValidation ou ValidationHandler
- Todas as operações sensíveis devem gerar SecurityLog
- Erros devem retornar ApiResponse com estrutura consistente
- Usar CancellationToken em todos os métodos assíncronos
- Seguir padrão CQRS rigorosamente (Commands alteram estado, Queries apenas leem)
- Events devem ser tratados de forma assíncrona e não devem bloquear o fluxo principal