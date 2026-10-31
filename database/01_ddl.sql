-- =============================================================
-- 01_ddl.sql  |  Bem Estar Online - criação do banco e das tabelas
-- SGBD: MySQL 8.0+   |   Charset: utf8mb4 (aceita acentos e emojis)
-- Ordem de execução: 01_ddl -> 02_triggers -> 03_procedures -> 04_seed
-- =============================================================
SET NAMES utf8mb4;

DROP DATABASE IF EXISTS db_bemestar;
CREATE DATABASE db_bemestar CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE db_bemestar;

-- -------------------------------------------------------------
-- Usuários: clientes e equipe (atendente e administrador)
-- -------------------------------------------------------------
CREATE TABLE usuarios (
    id_usuario      INT AUTO_INCREMENT PRIMARY KEY,
    nome            VARCHAR(120) NOT NULL,
    nome_social     VARCHAR(120) NULL COMMENT 'Nome pelo qual a pessoa quer ser chamada (Decreto 8.727/2016)',
    data_nascimento DATE NULL,
    email           VARCHAR(150) NOT NULL,
    cpf             VARCHAR(14)  NOT NULL,
    telefone        VARCHAR(20)  NULL,
    senha_hash      VARCHAR(255) NOT NULL COMMENT 'PBKDF2-SHA256 com sal; nunca a senha pura',
    perfil          ENUM('CLIENTE','ATENDENTE','ENTREGADOR','ADMIN') NOT NULL DEFAULT 'CLIENTE',
    ativo           BOOLEAN NOT NULL DEFAULT TRUE,
    trocar_senha    BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Senha provisória dada pelo admin: troca obrigatória no próximo acesso (RF35)',
    criado_em       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em   DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT uk_usuarios_email UNIQUE (email),
    CONSTRAINT uk_usuarios_cpf   UNIQUE (cpf)
) ENGINE=InnoDB;

CREATE TABLE enderecos (
    id_endereco  INT AUTO_INCREMENT PRIMARY KEY,
    id_usuario   INT NOT NULL,
    apelido      VARCHAR(60)  NULL,
    cep          VARCHAR(10)  NOT NULL,
    logradouro   VARCHAR(120) NOT NULL,
    numero       VARCHAR(20)  NOT NULL,
    complemento  VARCHAR(120) NULL,
    bairro       VARCHAR(80)  NOT NULL,
    cidade       VARCHAR(80)  NOT NULL,
    estado       CHAR(2)      NOT NULL,
    principal    BOOLEAN NOT NULL DEFAULT FALSE,
    latitude     DOUBLE NULL COMMENT 'Coordenadas para o mapa da entrega',
    longitude    DOUBLE NULL,
    criado_em    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_enderecos_usuarios FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_enderecos_usuario (id_usuario)
) ENGINE=InnoDB;

-- -------------------------------------------------------------
-- Catálogo
-- -------------------------------------------------------------
CREATE TABLE categorias (
    id_categoria INT AUTO_INCREMENT PRIMARY KEY,
    nome         VARCHAR(80)  NOT NULL,
    slug         VARCHAR(80)  NOT NULL,
    descricao    VARCHAR(255) NULL,
    icone        VARCHAR(16)  NULL,
    ordem        INT NOT NULL DEFAULT 0,
    ativo        BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uk_categorias_slug UNIQUE (slug)
) ENGINE=InnoDB;

CREATE TABLE medicamentos (
    id_medicamento    INT AUTO_INCREMENT PRIMARY KEY,
    id_categoria      INT NOT NULL,
    nome              VARCHAR(180) NOT NULL,
    slug              VARCHAR(180) NOT NULL,
    tipo              VARCHAR(80)  NOT NULL COMMENT 'Genérico, Referência, Suplemento, Higiene, Correlato...',
    principio_ativo   VARCHAR(150) NULL,
    descricao         TEXT NOT NULL,
    dosagem           VARCHAR(80)  NULL,
    apresentacao      VARCHAR(120) NULL,
    fabricante        VARCHAR(150) NOT NULL,
    via_administracao VARCHAR(80)  NULL,
    registro_anvisa   VARCHAR(50)  NULL,
    validade_lote     DATE NULL,
    preco             DECIMAL(10,2) NOT NULL,
    imagem_url        VARCHAR(255) NULL,
    exige_receita     BOOLEAN NOT NULL DEFAULT FALSE,
    uso_continuo      BOOLEAN NOT NULL DEFAULT FALSE,
    farmacia_popular  BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Disponível pelo programa federal Farmácia Popular',
    ativo             BOOLEAN NOT NULL DEFAULT TRUE,
    criado_em         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em     DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_medicamentos_categorias FOREIGN KEY (id_categoria)
        REFERENCES categorias(id_categoria) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT uk_medicamentos_slug UNIQUE (slug),
    CONSTRAINT ck_medicamentos_preco CHECK (preco >= 0),
    INDEX idx_medicamentos_nome (nome),
    INDEX idx_medicamentos_categoria (id_categoria),
    INDEX idx_medicamentos_validade (validade_lote),
    FULLTEXT INDEX ftx_medicamentos_busca (nome, principio_ativo, fabricante)
) ENGINE=InnoDB;

CREATE TABLE estoques (
    id_estoque        INT AUTO_INCREMENT PRIMARY KEY,
    id_medicamento    INT NOT NULL,
    quantidade        INT NOT NULL DEFAULT 0,
    quantidade_minima INT NOT NULL DEFAULT 5,
    localizacao       VARCHAR(80) NULL,
    atualizado_em     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_estoques_medicamentos FOREIGN KEY (id_medicamento)
        REFERENCES medicamentos(id_medicamento) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT uk_estoques_medicamento UNIQUE (id_medicamento),
    CONSTRAINT ck_estoques_quantidade CHECK (quantidade >= 0),
    CONSTRAINT ck_estoques_minima CHECK (quantidade_minima >= 0)
) ENGINE=InnoDB;

-- Preenchida automaticamente pelo trigger de baixa de estoque
CREATE TABLE alertas_estoque (
    id_alerta         INT AUTO_INCREMENT PRIMARY KEY,
    id_medicamento    INT NOT NULL,
    quantidade_atual  INT NOT NULL,
    quantidade_minima INT NOT NULL,
    resolvido         BOOLEAN NOT NULL DEFAULT FALSE,
    criado_em         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolvido_em      DATETIME NULL,
    CONSTRAINT fk_alertas_medicamentos FOREIGN KEY (id_medicamento)
        REFERENCES medicamentos(id_medicamento) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_alertas_pendentes (resolvido, id_medicamento)
) ENGINE=InnoDB;

-- -------------------------------------------------------------
-- Vendas
-- -------------------------------------------------------------
CREATE TABLE cupons (
    id_cupom            INT AUTO_INCREMENT PRIMARY KEY,
    codigo              VARCHAR(40)  NOT NULL,
    descricao           VARCHAR(180) NOT NULL,
    tipo                ENUM('PERCENTUAL','VALOR_FIXO','FRETE_GRATIS') NOT NULL,
    valor               DECIMAL(10,2) NOT NULL DEFAULT 0,
    valor_minimo_pedido DECIMAL(10,2) NOT NULL DEFAULT 0,
    data_inicio         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    data_fim            DATETIME NULL,
    limite_uso          INT NULL,
    usos_realizados     INT NOT NULL DEFAULT 0,
    ativo               BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT uk_cupons_codigo UNIQUE (codigo),
    CONSTRAINT ck_cupons_valor  CHECK (valor >= 0 AND valor_minimo_pedido >= 0)
) ENGINE=InnoDB;

CREATE TABLE carrinhos (
    id_carrinho   INT AUTO_INCREMENT PRIMARY KEY,
    id_usuario    INT NOT NULL,
    criado_em     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_carrinhos_usuarios FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT uk_carrinhos_usuario UNIQUE (id_usuario)
) ENGINE=InnoDB;

CREATE TABLE carrinho_itens (
    id_item_carrinho INT AUTO_INCREMENT PRIMARY KEY,
    id_carrinho      INT NOT NULL,
    id_medicamento   INT NOT NULL,
    quantidade       INT NOT NULL,
    preco_unitario   DECIMAL(10,2) NOT NULL,
    adicionado_em    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_carrinho_itens_carrinhos FOREIGN KEY (id_carrinho)
        REFERENCES carrinhos(id_carrinho) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_carrinho_itens_medicamentos FOREIGN KEY (id_medicamento)
        REFERENCES medicamentos(id_medicamento) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT uk_carrinho_item UNIQUE (id_carrinho, id_medicamento),
    CONSTRAINT ck_carrinho_itens_quantidade CHECK (quantidade > 0)
) ENGINE=InnoDB;

CREATE TABLE pedidos (
    id_pedido      INT AUTO_INCREMENT PRIMARY KEY,
    id_usuario     INT NOT NULL,
    id_endereco    INT NOT NULL,
    id_cupom       INT NULL,
    status         ENUM('AGUARDANDO_PAGAMENTO','PAGO','EM_SEPARACAO','SAIU_PARA_ENTREGA','ENTREGUE','CANCELADO')
                   NOT NULL DEFAULT 'AGUARDANDO_PAGAMENTO',
    canal          ENUM('SITE','APP') NOT NULL DEFAULT 'SITE',
    subtotal       DECIMAL(10,2) NOT NULL DEFAULT 0,
    desconto       DECIMAL(10,2) NOT NULL DEFAULT 0,
    frete          DECIMAL(10,2) NOT NULL DEFAULT 0,
    total          DECIMAL(10,2) NOT NULL DEFAULT 0,
    requer_receita BOOLEAN NOT NULL DEFAULT FALSE,
    receita_conferida BOOLEAN NOT NULL DEFAULT FALSE COMMENT 'Entregador conferiu a receita na entrega',
    observacao     VARCHAR(255) NULL,
    id_entregador  INT NULL,
    entregador_latitude  DOUBLE NULL COMMENT 'Última posição enviada pelo app do entregador',
    entregador_longitude DOUBLE NULL,
    localizacao_em DATETIME NULL,
    criado_em      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em  DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_pedidos_usuarios FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_pedidos_enderecos FOREIGN KEY (id_endereco)
        REFERENCES enderecos(id_endereco) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_pedidos_cupons FOREIGN KEY (id_cupom)
        REFERENCES cupons(id_cupom) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT fk_pedidos_entregador FOREIGN KEY (id_entregador)
        REFERENCES usuarios(id_usuario) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT ck_pedidos_valores CHECK (subtotal >= 0 AND desconto >= 0 AND frete >= 0 AND total >= 0),
    INDEX idx_pedidos_usuario (id_usuario),
    INDEX idx_pedidos_status (status),
    INDEX idx_pedidos_criado (criado_em)
) ENGINE=InnoDB;

CREATE TABLE pedido_itens (
    id_item_pedido INT AUTO_INCREMENT PRIMARY KEY,
    id_pedido      INT NOT NULL,
    id_medicamento INT NOT NULL,
    nome_produto   VARCHAR(180) NOT NULL COMMENT 'Cópia do nome no momento da compra',
    quantidade     INT NOT NULL,
    preco_unitario DECIMAL(10,2) NOT NULL COMMENT 'Cópia do preço no momento da compra',
    subtotal       DECIMAL(10,2) NOT NULL,
    CONSTRAINT fk_pedido_itens_pedidos FOREIGN KEY (id_pedido)
        REFERENCES pedidos(id_pedido) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_pedido_itens_medicamentos FOREIGN KEY (id_medicamento)
        REFERENCES medicamentos(id_medicamento) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_pedido_itens_quantidade CHECK (quantidade > 0),
    INDEX idx_pedido_itens_pedido (id_pedido)
) ENGINE=InnoDB;

-- Trajeto do entregador (cada posição recebida), preenchida pelo trigger trg_pedidos_au_trajeto
CREATE TABLE trajeto_entregas (
    id_ponto    INT AUTO_INCREMENT PRIMARY KEY,
    id_pedido   INT NOT NULL,
    latitude    DOUBLE NOT NULL,
    longitude   DOUBLE NOT NULL,
    registrado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_trajeto_pedidos FOREIGN KEY (id_pedido)
        REFERENCES pedidos(id_pedido) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_trajeto_pedido (id_pedido, registrado_em)
) ENGINE=InnoDB;

-- Linha do tempo do pedido (rastreamento), preenchida pelos triggers
CREATE TABLE pedido_historico_status (
    id_historico    INT AUTO_INCREMENT PRIMARY KEY,
    id_pedido       INT NOT NULL,
    status_anterior VARCHAR(30) NULL,
    status_novo     VARCHAR(30) NOT NULL,
    alterado_em     DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_historico_pedidos FOREIGN KEY (id_pedido)
        REFERENCES pedidos(id_pedido) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_historico_pedido (id_pedido)
) ENGINE=InnoDB;

CREATE TABLE pagamentos (
    id_pagamento     INT AUTO_INCREMENT PRIMARY KEY,
    id_pedido        INT NOT NULL,
    metodo           ENUM('PIX','CARTAO_CREDITO','CARTAO_DEBITO','DINHEIRO') NOT NULL,
    status           ENUM('PENDENTE','APROVADO','RECUSADO','ESTORNADO') NOT NULL DEFAULT 'PENDENTE',
    valor            DECIMAL(10,2) NOT NULL,
    codigo_transacao VARCHAR(120) NULL,
    pago_em          DATETIME NULL,
    criado_em        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_pagamentos_pedidos FOREIGN KEY (id_pedido)
        REFERENCES pedidos(id_pedido) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT ck_pagamentos_valor CHECK (valor >= 0),
    INDEX idx_pagamentos_pedido (id_pedido)
) ENGINE=InnoDB;

-- -------------------------------------------------------------
-- Serviços: assinatura de uso contínuo e delivery por receita
-- -------------------------------------------------------------
CREATE TABLE assinaturas (
    id_assinatura       INT AUTO_INCREMENT PRIMARY KEY,
    id_usuario          INT NOT NULL,
    id_medicamento      INT NOT NULL,
    nome_paciente       VARCHAR(120) NOT NULL,
    quantidade          INT NOT NULL DEFAULT 1,
    periodicidade_dias  INT NOT NULL DEFAULT 30,
    valor_unitario      DECIMAL(10,2) NOT NULL,
    desconto_percentual DECIMAL(10,2) NOT NULL DEFAULT 10,
    status              ENUM('ATIVA','PAUSADA','CANCELADA') NOT NULL DEFAULT 'ATIVA',
    criada_em           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    proxima_entrega     DATE NULL,
    CONSTRAINT fk_assinaturas_usuarios FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_assinaturas_medicamentos FOREIGN KEY (id_medicamento)
        REFERENCES medicamentos(id_medicamento) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_assinaturas_periodo CHECK (periodicidade_dias IN (15, 30, 60, 90)),
    CONSTRAINT ck_assinaturas_quantidade CHECK (quantidade BETWEEN 1 AND 10),
    INDEX idx_assinaturas_usuario (id_usuario),
    INDEX idx_assinaturas_proxima (status, proxima_entrega)
) ENGINE=InnoDB;

CREATE TABLE delivery_solicitacoes (
    id_delivery     INT AUTO_INCREMENT PRIMARY KEY,
    id_usuario      INT NULL,
    nome_cliente    VARCHAR(120) NOT NULL,
    telefone        VARCHAR(20)  NOT NULL,
    endereco_texto  VARCHAR(255) NOT NULL,
    observacao      TEXT NULL,
    receita_arquivo VARCHAR(255) NULL,
    status          ENUM('RECEBIDO','EM_ANALISE','EM_ROTA','CONCLUIDO','CANCELADO') NOT NULL DEFAULT 'RECEBIDO',
    criado_em       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_delivery_usuarios FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario) ON DELETE SET NULL ON UPDATE CASCADE,
    INDEX idx_delivery_status (status)
) ENGINE=InnoDB;

-- -------------------------------------------------------------
-- Ouvidoria e canal de denúncia de discriminação (RF30)
-- Discriminação é crime (Lei 7.716/1989; Estatuto da Igualdade Racial, Lei 12.288/2010).
-- Relato anônimo não guarda id_usuario nem contato (LGPD): a API garante o id_usuario
-- e a CHECK abaixo garante o contato. O protocolo é aleatório para não ser adivinhado.
-- -------------------------------------------------------------
CREATE TABLE ouvidoria (
    id_ouvidoria          INT AUTO_INCREMENT PRIMARY KEY,
    id_usuario            INT NULL,
    tipo                  ENUM('DISCRIMINACAO','ATENDIMENTO','ENTREGA','SUGESTAO','ELOGIO') NOT NULL,
    subtipo_discriminacao ENUM('RACIAL','RELIGIOSA','GENERO','DEFICIENCIA','IDADE','OUTRA') NULL,
    descricao             TEXT NOT NULL,
    anonimo               BOOLEAN NOT NULL DEFAULT FALSE,
    contato               VARCHAR(150) NULL,
    protocolo             VARCHAR(20) NOT NULL,
    status                ENUM('ABERTA','EM_ANALISE','RESPONDIDA','ENCERRADA') NOT NULL DEFAULT 'ABERTA',
    resposta              TEXT NULL,
    criado_em             DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    respondido_em         DATETIME NULL,
    CONSTRAINT uk_ouvidoria_protocolo UNIQUE (protocolo),
    CONSTRAINT fk_ouvidoria_usuarios FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT ck_ouvidoria_anonimo CHECK (anonimo = FALSE OR contato IS NULL),
    CONSTRAINT ck_ouvidoria_subtipo CHECK (subtipo_discriminacao IS NULL OR tipo = 'DISCRIMINACAO'),
    INDEX idx_ouvidoria_status (status, tipo)
) ENGINE=InnoDB;

-- -------------------------------------------------------------
-- Redefinição de senha por e-mail ("Esqueci minha senha", RF33)
-- Guarda só o SHA-256 do código do link, nunca o código: quem lê o banco não consegue usar o link.
-- Cada link vale 30 minutos e uma única vez (usado_em preenchido = já usado).
-- -------------------------------------------------------------
CREATE TABLE redefinicoes_senha (
    id_redefinicao INT AUTO_INCREMENT PRIMARY KEY,
    id_usuario     INT NOT NULL,
    token_hash     CHAR(64) NOT NULL,
    expira_em      DATETIME NOT NULL,
    usado_em       DATETIME NULL,
    criado_em      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_redefinicoes_token UNIQUE (token_hash),
    CONSTRAINT fk_redefinicoes_usuarios FOREIGN KEY (id_usuario)
        REFERENCES usuarios(id_usuario) ON DELETE CASCADE ON UPDATE CASCADE,
    INDEX idx_redefinicoes_usuario (id_usuario, criado_em)
) ENGINE=InnoDB;

-- -------------------------------------------------------------
-- View de apoio a relatórios (pedido + cliente + pagamento)
-- -------------------------------------------------------------
CREATE OR REPLACE VIEW vw_pedidos_completos AS
SELECT p.id_pedido,
       p.criado_em,
       p.status,
       p.canal,
       COALESCE(u.nome_social, u.nome) AS cliente,
       u.email,
       CONCAT(e.logradouro, ', ', e.numero, ' - ', e.bairro, ', ', e.cidade, '/', e.estado) AS endereco_entrega,
       p.subtotal, p.desconto, p.frete, p.total,
       COALESCE(ent.nome_social, ent.nome) AS entregador,
       pg.metodo AS forma_pagamento,
       pg.status AS status_pagamento,
       (SELECT SUM(i.quantidade) FROM pedido_itens i WHERE i.id_pedido = p.id_pedido) AS quantidade_itens
FROM pedidos p
JOIN usuarios u  ON u.id_usuario  = p.id_usuario
JOIN enderecos e ON e.id_endereco = p.id_endereco
LEFT JOIN usuarios ent  ON ent.id_usuario = p.id_entregador
LEFT JOIN pagamentos pg ON pg.id_pedido = p.id_pedido;
