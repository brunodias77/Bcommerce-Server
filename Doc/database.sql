-- =============================================
-- EXTENSÕES NECESSÁRIAS
-- =============================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Habilita a extensão que permite gerar UUIDs no PostgreSQL
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

-- Índices para otimização de consultas
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);

CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token);

CREATE INDEX idx_security_logs_user_id ON security_logs(user_id);

CREATE INDEX idx_security_logs_created_at ON security_logs(created_at);

-- =============================================
-- USER  SERVICE - user_db
-- =============================================
CREATE TABLE user_references (
    -- Referência cruzada entre serviços e o usuário principal
    user_id VARCHAR(450) PRIMARY KEY,
    -- ID do usuário (vinculado ao auth_service)
    email VARCHAR(255) NOT NULL,
    -- E-mail principal
    full_name VARCHAR(255) NOT NULL,
    -- Nome completo
    is_active BOOLEAN DEFAULT TRUE,
    -- Indica se o perfil está ativo
    synced_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- Última sincronização com auth_service
);

CREATE TABLE addresses (
    -- Armazena os endereços dos usuários
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- ID único do endereço
    user_id VARCHAR(450) NOT NULL,
    -- ID do usuário proprietário
    recipient_name VARCHAR(255) NOT NULL,
    -- Nome do destinatário
    street VARCHAR(255) NOT NULL,
    -- Rua
    number VARCHAR(20) NOT NULL,
    -- Número da residência
    complement VARCHAR(255),
    -- Complemento (bloco, apto, etc.)
    neighborhood VARCHAR(100) NOT NULL,
    -- Bairro
    city VARCHAR(100) NOT NULL,
    -- Cidade
    state VARCHAR(2) NOT NULL,
    -- UF (estado)
    zip_code VARCHAR(10) NOT NULL,
    -- CEP
    is_default BOOLEAN DEFAULT FALSE,
    -- Indica se é o endereço padrão
    deleted_at TIMESTAMP,
    -- Marca de exclusão lógica
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data de criação
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- Última atualização
);

CREATE TABLE cards (
    -- Cartões de pagamento do usuário
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- ID do cartão
    user_id VARCHAR(450) NOT NULL,
    -- ID do usuário
    cardholder_name VARCHAR(255) NOT NULL,
    -- Nome no cartão
    card_number_last_four CHAR(4) NOT NULL,
    -- Últimos 4 dígitos (para exibição)
    card_brand VARCHAR(50) NOT NULL,
    -- Bandeira (Visa, MasterCard, etc.)
    expiry_month CHAR(2) NOT NULL,
    -- Mês de expiração
    expiry_year CHAR(4) NOT NULL,
    -- Ano de expiração
    is_default BOOLEAN DEFAULT FALSE,
    -- Indica se é o cartão padrão
    token VARCHAR(255) NOT NULL,
    -- Token de identificação (tokenizado pelo gateway)
    deleted_at TIMESTAMP,
    -- Marca de exclusão lógica
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data de criação
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- Última atualização
);

-- Índices para otimizar buscas por e-mail, usuário e cartões padrão
CREATE INDEX idx_user_references_email ON user_references(email);

CREATE INDEX idx_addresses_user_id ON addresses(user_id);

CREATE INDEX idx_addresses_is_default ON addresses(user_id, is_default);

CREATE INDEX idx_cards_user_id ON cards(user_id);

CREATE INDEX idx_cards_is_default ON cards(user_id, is_default);

-- =============================================
-- CATALOG SERVICE - catalog_db
-- =============================================
CREATE TABLE categories (
    -- Categorias de produtos
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- ID da categoria
    name VARCHAR(255) NOT NULL,
    -- Nome da categoria
    slug VARCHAR(255) UNIQUE NOT NULL,
    -- Slug amigável (para URLs)
    parent_id UUID REFERENCES categories(id) ON DELETE
    SET
        NULL,
        -- Categoria pai (hierarquia)
        is_active BOOLEAN DEFAULT TRUE,
        -- Categoria ativa/inativa
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- Data de criação
);

CREATE TABLE products (
    -- Produtos do catálogo
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    -- ID do produto
    name VARCHAR(255) NOT NULL,
    -- Nome do produto
    slug VARCHAR(255) UNIQUE NOT NULL,
    -- Slug único
    description TEXT,
    -- Descrição detalhada
    category_id UUID REFERENCES categories(id) ON DELETE
    SET
        NULL,
        -- Categoria do produto
        price DECIMAL(10, 2) NOT NULL,
        -- Preço atual
        stock_quantity INT NOT NULL DEFAULT 0,
        -- Quantidade em estoque
        is_active BOOLEAN DEFAULT TRUE,
        -- Produto ativo/inativo
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        -- Data de criação
        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- Última atualização
);

CREATE TABLE product_images (
    -- Imagens associadas aos produtos
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    -- Produto relacionado
    image_url TEXT NOT NULL,
    -- URL da imagem
    display_order INT DEFAULT 0,
    -- Ordem de exibição
    is_primary BOOLEAN DEFAULT FALSE,
    -- Marca a imagem principal
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- Data de criação
);

CREATE TABLE favorite_products (
    -- Produtos favoritados pelos usuários
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(450) NOT NULL,
    -- ID do usuário
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    -- Produto favoritado
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Data de adição
    UNIQUE(user_id, product_id) -- Evita duplicidade
);

CREATE TABLE product_reviews (
    -- Avaliações de produtos
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    -- Produto avaliado
    user_id VARCHAR(450) NOT NULL,
    -- Usuário que avaliou
    order_id UUID,
    -- Pedido associado (para validar compra)
    rating INT NOT NULL CHECK (
        rating >= 1
        AND rating <= 5
    ),
    -- Nota de 1 a 5
    comment TEXT,
    -- Comentário do usuário
    is_verified_purchase BOOLEAN DEFAULT FALSE,
    -- Marca se o usuário realmente comprou o produto
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP -- Data da avaliação
);

-- Índices de performance
CREATE INDEX idx_categories_slug ON categories(slug);

CREATE INDEX idx_categories_parent_id ON categories(parent_id);

CREATE INDEX idx_products_slug ON products(slug);

CREATE INDEX idx_products_category_id ON products(category_id);

CREATE INDEX idx_products_is_active ON products(is_active);

CREATE INDEX idx_product_images_product_id ON product_images(product_id);

CREATE INDEX idx_favorite_products_user_id ON favorite_products(user_id);

CREATE INDEX idx_favorite_products_product_id ON favorite_products(product_id);

CREATE INDEX idx_product_reviews_product_id ON product_reviews(product_id);

CREATE INDEX idx_product_reviews_user_id ON product_reviews(user_id);

-- =============================================
-- CART SERVICE - cart_db
-- =============================================
CREATE TABLE carts (
    -- Carrinhos de compra
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(450) UNIQUE NOT NULL,
    -- Cada usuário tem um carrinho ativo
    subtotal DECIMAL(10, 2) DEFAULT 0,
    -- Valor parcial dos itens
    total_items INT DEFAULT 0,
    -- Quantidade total de itens
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP -- Data de expiração (para carrinhos abandonados)
);

CREATE TABLE cart_items (
    -- Itens dentro de um carrinho
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL REFERENCES carts(id) ON DELETE CASCADE,
    -- Carrinho associado
    product_id UUID NOT NULL,
    -- Produto adicionado
    quantity INT NOT NULL CHECK (quantity > 0),
    -- Quantidade do produto
    unit_price DECIMAL(10, 2) NOT NULL,
    -- Preço unitário
    subtotal DECIMAL(10, 2) NOT NULL,
    -- Subtotal calculado (quantidade * preço)
    product_snapshot JSONB,
    -- Snapshot dos dados do produto (para histórico)
    added_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(cart_id, product_id) -- Evita duplicidade do mesmo produto no carrinho
);

CREATE TABLE abandoned_carts (
    -- Registro de carrinhos abandonados
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL,
    user_id VARCHAR(450) NOT NULL,
    subtotal DECIMAL(10, 2),
    total_items INT,
    cart_snapshot JSONB NOT NULL,
    -- Snapshot completo do carrinho
    abandoned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Quando foi considerado abandonado
    recovery_email_sent BOOLEAN DEFAULT FALSE,
    -- Se foi enviado e-mail de recuperação
    recovered BOOLEAN DEFAULT FALSE -- Se o carrinho foi recuperado posteriormente
);

CREATE TYPE cart_event_type AS ENUM (
    -- Enum de tipos de eventos do carrinho
    'ITEM_ADDED',
    'ITEM_REMOVED',
    'ITEM_QUANTITY_UPDATED',
    'CART_CLEARED',
    'CART_ABANDONED',
    'CART_CONVERTED_TO_ORDER'
);

CREATE TABLE cart_events (
    -- Eventos do carrinho (para auditoria e rastreio)
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cart_id UUID NOT NULL,
    user_id VARCHAR(450) NOT NULL,
    event_type cart_event_type NOT NULL,
    -- Tipo de evento
    product_id UUID,
    -- Produto envolvido (se aplicável)
    quantity INT,
    -- Quantidade envolvida
    metadata JSONB,
    -- Dados adicionais do evento
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índices para performance e auditoria
CREATE INDEX idx_carts_user_id ON carts(user_id);

CREATE INDEX idx_carts_updated_at ON carts(updated_at);

CREATE INDEX idx_carts_expires_at ON carts(expires_at);

CREATE INDEX idx_cart_items_cart_id ON cart_items(cart_id);

CREATE INDEX idx_cart_items_product_id ON cart_items(product_id);

CREATE INDEX idx_abandoned_carts_user_id ON abandoned_carts(user_id);

CREATE INDEX idx_abandoned_carts_abandoned_at ON abandoned_carts(abandoned_at);

CREATE INDEX idx_cart_events_cart_id ON cart_events(cart_id);

CREATE INDEX idx_cart_events_user_id ON cart_events(user_id);

CREATE INDEX idx_cart_events_created_at ON cart_events(created_at);

-- =============================================
-- PROMOTION SERVICE - promotion_db
-- =============================================
CREATE TYPE coupon_type AS ENUM ('GLOBAL', 'INDIVIDUAL');

-- Tipo de cupom (geral ou individual)
CREATE TYPE discount_type AS ENUM ('PERCENTAGE', 'FIXED_AMOUNT');

-- Tipo de desconto (percentual ou fixo)
CREATE TABLE coupons (
    -- Definição de cupons de desconto
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(50) UNIQUE NOT NULL,
    -- Código do cupom
    discount_type discount_type NOT NULL,
    -- Tipo do desconto
    discount_value DECIMAL(10, 2) NOT NULL,
    -- Valor do desconto
    coupon_type coupon_type NOT NULL,
    -- Tipo do cupom
    min_purchase_amount DECIMAL(10, 2),
    -- Valor mínimo de compra
    max_uses INT,
    -- Limite total de uso
    uses_per_user INT DEFAULT 1,
    -- Limite de uso por usuário
    current_uses INT DEFAULT 0,
    -- Quantidade atual de usos
    valid_from TIMESTAMP NOT NULL,
    -- Início da validade
    valid_until TIMESTAMP NOT NULL,
    -- Fim da validade
    is_active BOOLEAN DEFAULT TRUE,
    -- Ativo/inativo
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE user_coupons (
    -- Relação entre usuários e cupons utilizados
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(450) NOT NULL,
    coupon_id UUID NOT NULL REFERENCES coupons(id) ON DELETE CASCADE,
    -- Cupom associado
    times_used INT DEFAULT 0,
    -- Quantas vezes o usuário utilizou
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, coupon_id)
);

-- Índices de performance
CREATE INDEX idx_coupons_code ON coupons(code);

CREATE INDEX idx_coupons_is_active ON coupons(is_active);

CREATE INDEX idx_coupons_validity ON coupons(valid_from, valid_until);

CREATE INDEX idx_user_coupons_user_id ON user_coupons(user_id);

CREATE INDEX idx_user_coupons_coupon_id ON user_coupons(coupon_id);

-- =============================================
-- ORDER SERVICE - order_db
-- =============================================
CREATE TYPE order_status AS ENUM (
    -- Enum para status dos pedidos
    'PENDING_PAYMENT',
    'PAYMENT_CONFIRMED',
    'PROCESSING',
    'SHIPPED',
    'DELIVERED',
    'CANCELLED',
    'REFUNDED'
);

CREATE TABLE orders (
    -- Pedidos realizados pelos clientes
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_number VARCHAR(50) UNIQUE NOT NULL,
    -- Número do pedido
    user_id VARCHAR(450) NOT NULL,
    -- Usuário comprador
    address_snapshot JSONB NOT NULL,
    -- Snapshot do endereço usado
    card_snapshot JSONB,
    -- Snapshot do cartão usado
    coupon_id UUID,
    -- Cupom aplicado (opcional)
    coupon_snapshot JSONB,
    -- Snapshot do cupom no momento da compra
    subtotal DECIMAL(10, 2) NOT NULL,
    -- Subtotal dos produtos
    discount_amount DECIMAL(10, 2) DEFAULT 0,
    -- Valor de desconto aplicado
    shipping_cost DECIMAL(10, 2) NOT NULL,
    -- Custo de frete
    total_amount DECIMAL(10, 2) NOT NULL,
    -- Valor total do pedido
    status order_status DEFAULT 'PENDING_PAYMENT',
    -- Status atual
    payment_method VARCHAR(50),
    -- Método de pagamento (cartão, PIX, etc.)
    payment_gateway_id VARCHAR(255),
    -- ID de referência no gateway de pagamento
    notes TEXT,
    -- Observações do cliente
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE order_items (
    -- Itens contidos em um pedido
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    -- Pedido associado
    product_id UUID NOT NULL,
    -- ID do produto
    product_snapshot JSONB NOT NULL,
    -- Snapshot dos dados do produto
    quantity INT NOT NULL CHECK (quantity > 0),
    -- Quantidade comprada
    unit_price DECIMAL(10, 2) NOT NULL,
    -- Preço unitário
    subtotal DECIMAL(10, 2) NOT NULL,
    -- Subtotal
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE order_status_history (
    -- Histórico de mudanças de status do pedido
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    status order_status NOT NULL,
    -- Novo status
    notes TEXT,
    -- Observações adicionais
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE order_tracking (
    -- Rastreamento de entrega
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    carrier VARCHAR(100),
    -- Nome da transportadora
    tracking_code VARCHAR(255),
    -- Código de rastreamento
    tracking_url TEXT,
    -- URL para rastreamento online
    estimated_delivery DATE,
    -- Data estimada de entrega
    delivered_at TIMESTAMP,
    -- Data real de entrega
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índices para performance em consultas e relatórios
CREATE INDEX idx_orders_user_id ON orders(user_id);

CREATE INDEX idx_orders_order_number ON orders(order_number);

CREATE INDEX idx_orders_status ON orders(status);

CREATE INDEX idx_orders_created_at ON orders(created_at);

CREATE INDEX idx_order_items_order_id ON order_items(order_id);

CREATE INDEX idx_order_items_product_id ON order_items(product_id);

CREATE INDEX idx_order_status_history_order_id ON order_status_history(order_id);

CREATE INDEX idx_order_tracking_order_id ON order_tracking(order_id);

-- =============================================
-- NOTIFICATION SERVICE - notification_db
-- =============================================
CREATE TYPE notification_type AS ENUM ('EMAIL', 'SMS');

-- Tipo de notificação
CREATE TYPE notification_status AS ENUM ('PENDING', 'SENT', 'FAILED');

-- Status da notificação
CREATE TABLE notifications (
    -- Armazena notificações enviadas aos usuários
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id VARCHAR(450) NOT NULL,
    type notification_type NOT NULL,
    -- Tipo (email, sms)
    recipient VARCHAR(255) NOT NULL,
    -- Destinatário
    subject VARCHAR(255),
    -- Assunto (para e-mails)
    body TEXT NOT NULL,
    -- Conteúdo da mensagem
    status notification_status DEFAULT 'PENDING',
    -- Estado atual
    sent_at TIMESTAMP,
    -- Data de envio
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_notifications_user_id ON notifications(user_id);

CREATE INDEX idx_notifications_status ON notifications(status);

CREATE INDEX idx_notifications_created_at ON notifications(created_at);

-- =============================================
-- EVENT STORE - events_db
-- =============================================
CREATE TABLE domain_events (
    -- Armazena eventos de domínio (DDD/Event Sourcing)
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    aggregate_id UUID NOT NULL,
    -- ID do agregado que gerou o evento
    aggregate_type VARCHAR(100) NOT NULL,
    -- Tipo de agregado (ex: Order, Cart)
    event_type VARCHAR(100) NOT NULL,
    -- Tipo do evento (ex: OrderCreated)
    payload JSONB NOT NULL,
    -- Dados do evento (em formato JSON)
    user_id VARCHAR(450),
    -- Usuário que disparou o evento
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE outbox_events (
    -- Outbox pattern (eventos a serem processados)
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    status VARCHAR(50) DEFAULT 'PENDING',
    -- Status do processamento
    processed_at TIMESTAMP,
    -- Quando foi processado
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índices para performance e rastreabilidade de eventos
CREATE INDEX idx_domain_events_aggregate_id ON domain_events(aggregate_id);

CREATE INDEX idx_domain_events_aggregate_type ON domain_events(aggregate_type);

CREATE INDEX idx_domain_events_created_at ON domain_events(created_at);

CREATE INDEX idx_outbox_events_status ON outbox_events(status);

CREATE INDEX idx_outbox_events_created_at ON outbox_events(created_at);