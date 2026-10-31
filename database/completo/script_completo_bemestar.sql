-- SCRIPT COMPLETO (gerado juntando 01_ddl, 02_triggers, 03_procedures e 04_seed)
-- Pode ser executado de uma vez no MySQL Workbench.

-- >>>>>>>>>>>>>>>>>>>> database/01_ddl.sql
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

-- >>>>>>>>>>>>>>>>>>>> database/02_triggers.sql
-- =============================================================
-- 02_triggers.sql  |  Regras automáticas executadas pelo próprio MySQL
-- -------------------------------------------------------------
-- trg_pedido_itens_bi_valida_estoque : impede vender mais do que há no estoque
-- trg_pedido_itens_ai_baixa_estoque  : dá baixa no estoque e cria alerta de reposição
-- trg_pedidos_ai_historico           : registra o primeiro status do pedido (rastreamento)
-- trg_pedidos_au_status              : registra cada mudança de status e, se o pedido
--                                      for cancelado, devolve os itens ao estoque
-- trg_pedidos_au_trajeto             : guarda cada posição enviada pelo app do entregador
-- =============================================================
SET NAMES utf8mb4;
USE db_bemestar;

DROP TRIGGER IF EXISTS trg_pedido_itens_bi_valida_estoque;
DROP TRIGGER IF EXISTS trg_pedido_itens_ai_baixa_estoque;
DROP TRIGGER IF EXISTS trg_pedidos_ai_historico;
DROP TRIGGER IF EXISTS trg_pedidos_au_status;
DROP TRIGGER IF EXISTS trg_pedidos_au_trajeto;

DELIMITER $$

CREATE TRIGGER trg_pedido_itens_bi_valida_estoque
BEFORE INSERT ON pedido_itens
FOR EACH ROW
BEGIN
    DECLARE v_disponivel INT DEFAULT NULL;

    SELECT quantidade INTO v_disponivel
      FROM estoques
     WHERE id_medicamento = NEW.id_medicamento;

    IF v_disponivel IS NULL OR v_disponivel < NEW.quantidade THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Estoque insuficiente para um dos produtos do pedido.';
    END IF;
END$$

CREATE TRIGGER trg_pedido_itens_ai_baixa_estoque
AFTER INSERT ON pedido_itens
FOR EACH ROW
BEGIN
    DECLARE v_quantidade INT DEFAULT NULL;
    DECLARE v_minimo INT DEFAULT NULL;

    UPDATE estoques
       SET quantidade = GREATEST(quantidade - NEW.quantidade, 0)
     WHERE id_medicamento = NEW.id_medicamento;

    SELECT quantidade, quantidade_minima INTO v_quantidade, v_minimo
      FROM estoques
     WHERE id_medicamento = NEW.id_medicamento;

    -- Gera um alerta de reposição (só um alerta em aberto por produto)
    IF v_quantidade IS NOT NULL AND v_quantidade <= v_minimo
       AND NOT EXISTS (SELECT 1 FROM alertas_estoque
                        WHERE id_medicamento = NEW.id_medicamento AND resolvido = FALSE) THEN
        INSERT INTO alertas_estoque (id_medicamento, quantidade_atual, quantidade_minima)
        VALUES (NEW.id_medicamento, v_quantidade, v_minimo);
    END IF;
END$$

CREATE TRIGGER trg_pedidos_ai_historico
AFTER INSERT ON pedidos
FOR EACH ROW
BEGIN
    INSERT INTO pedido_historico_status (id_pedido, status_anterior, status_novo, alterado_em)
    VALUES (NEW.id_pedido, NULL, NEW.status, NEW.criado_em);
END$$

CREATE TRIGGER trg_pedidos_au_status
AFTER UPDATE ON pedidos
FOR EACH ROW
BEGIN
    IF NOT (OLD.status <=> NEW.status) THEN
        INSERT INTO pedido_historico_status (id_pedido, status_anterior, status_novo, alterado_em)
        VALUES (NEW.id_pedido, OLD.status, NEW.status, NOW());

        IF NEW.status = 'CANCELADO' THEN
            UPDATE estoques e
              JOIN pedido_itens i ON i.id_medicamento = e.id_medicamento
               SET e.quantidade = e.quantidade + i.quantidade
             WHERE i.id_pedido = NEW.id_pedido;
        END IF;
    END IF;
END$$

CREATE TRIGGER trg_pedidos_au_trajeto
AFTER UPDATE ON pedidos
FOR EACH ROW
BEGIN
    IF NEW.entregador_latitude IS NOT NULL AND NEW.entregador_longitude IS NOT NULL
       AND (NOT (OLD.entregador_latitude <=> NEW.entregador_latitude)
            OR NOT (OLD.entregador_longitude <=> NEW.entregador_longitude)) THEN
        INSERT INTO trajeto_entregas (id_pedido, latitude, longitude, registrado_em)
        VALUES (NEW.id_pedido, NEW.entregador_latitude, NEW.entregador_longitude, COALESCE(NEW.localizacao_em, NOW()));
    END IF;
END$$

DELIMITER ;

-- >>>>>>>>>>>>>>>>>>>> database/03_procedures.sql
-- =============================================================
-- 03_procedures.sql  |  Stored procedures usadas pelo painel administrativo
-- (chamadas pela API em Infraestrutura/Automacoes/Relatorios.cs)
-- =============================================================
SET NAMES utf8mb4;
USE db_bemestar;

DROP PROCEDURE IF EXISTS sp_relatorio_vendas;
DROP PROCEDURE IF EXISTS sp_produtos_estoque_baixo;
DROP PROCEDURE IF EXISTS sp_produtos_vencendo;
DROP PROCEDURE IF EXISTS sp_proximas_entregas_assinatura;

DELIMITER $$

-- Faturamento e quantidade de pedidos por dia (pedidos cancelados não contam)
CREATE PROCEDURE sp_relatorio_vendas(IN p_inicio DATE, IN p_fim DATE)
BEGIN
    SELECT DATE(criado_em)          AS dia,
           COUNT(*)                 AS quantidade_pedidos,
           COALESCE(SUM(total), 0)  AS faturamento
      FROM pedidos
     WHERE criado_em >= p_inicio
       AND criado_em <  DATE_ADD(p_fim, INTERVAL 1 DAY)
       AND status <> 'CANCELADO'
     GROUP BY DATE(criado_em)
     ORDER BY dia;
END$$

-- Produtos ativos que chegaram ao estoque mínimo (lista de compras)
CREATE PROCEDURE sp_produtos_estoque_baixo()
BEGIN
    SELECT m.id_medicamento,
           m.nome,
           e.quantidade,
           e.quantidade_minima
      FROM estoques e
      JOIN medicamentos m ON m.id_medicamento = e.id_medicamento
     WHERE e.quantidade <= e.quantidade_minima
       AND m.ativo = TRUE
     ORDER BY e.quantidade, m.nome;
END$$

-- Produtos cujo lote vence nos próximos p_dias dias (controle de validade)
CREATE PROCEDURE sp_produtos_vencendo(IN p_dias INT)
BEGIN
    SELECT id_medicamento,
           nome,
           validade_lote,
           DATEDIFF(validade_lote, CURDATE()) AS dias_restantes
      FROM medicamentos
     WHERE ativo = TRUE
       AND validade_lote IS NOT NULL
       AND validade_lote <= DATE_ADD(CURDATE(), INTERVAL p_dias DAY)
     ORDER BY validade_lote;
END$$

-- Assinaturas ativas com entrega nos próximos p_dias dias (roteiro de separação)
CREATE PROCEDURE sp_proximas_entregas_assinatura(IN p_dias INT)
BEGIN
    SELECT a.id_assinatura,
           COALESCE(u.nome_social, u.nome) AS cliente,
           a.nome_paciente,
           m.nome AS medicamento,
           a.quantidade,
           a.proxima_entrega,
           ROUND(a.valor_unitario * a.quantidade * (1 - a.desconto_percentual / 100), 2) AS valor_entrega
      FROM assinaturas a
      JOIN usuarios u     ON u.id_usuario = a.id_usuario
      JOIN medicamentos m ON m.id_medicamento = a.id_medicamento
     WHERE a.status = 'ATIVA'
       AND a.proxima_entrega <= DATE_ADD(CURDATE(), INTERVAL p_dias DAY)
     ORDER BY a.proxima_entrega;
END$$

DELIMITER ;

-- >>>>>>>>>>>>>>>>>>>> database/04_seed.sql
-- =============================================================
-- 04_seed.sql  |  Dados iniciais do catálogo e cupons
-- Mesmos dados usados pela API no modo demonstração (CatalogoSeed.cs).
-- Os usuários de teste e os pedidos de exemplo são criados pela API na
-- primeira execução, porque as senhas precisam ser criptografadas (PBKDF2).
-- =============================================================
SET NAMES utf8mb4;
USE db_bemestar;

INSERT INTO categorias (nome, slug, descricao, icone, ordem) VALUES
  ('Genéricos', 'genericos', 'Medicamentos genéricos com a mesma eficácia do de referência, por um preço menor.', '💊', 1),
  ('Referência', 'referencia', 'Medicamentos de marca que deram origem aos genéricos.', '🏷️', 2),
  ('Uso contínuo', 'uso-continuo', 'Pressão, diabetes, colesterol e tireoide: remédios do dia a dia, com opção de assinatura.', '🔁', 3),
  ('Vitaminas e suplementos', 'vitaminas', 'Vitaminas, minerais e suplementos para completar a alimentação.', '🍊', 4),
  ('Higiene pessoal', 'higiene', 'Cuidados diários com o corpo, a boca e os cabelos.', '🧼', 5),
  ('Dermocosméticos', 'dermocosmeticos', 'Proteção solar, hidratação e cuidados com a pele.', '🧴', 6),
  ('Mamãe e bebê', 'mamae-bebe', 'Fraldas, pomadas e cuidados para os pequenos.', '🍼', 7),
  ('Primeiros socorros', 'primeiros-socorros', 'Curativos, antissépticos e itens para pequenos acidentes.', '🩹', 8),
  ('Aparelhos de saúde', 'aparelhos', 'Termômetros, medidores de pressão e oxímetros para acompanhar a saúde em casa.', '🩺', 9);

INSERT INTO medicamentos (id_categoria, nome, slug, tipo, principio_ativo, descricao, dosagem, apresentacao,
  fabricante, via_administracao, registro_anvisa, validade_lote, preco, exige_receita, uso_continuo, farmacia_popular)
SELECT c.id_categoria, p.nome, p.slug, p.tipo, p.principio, p.descricao, p.dosagem, p.apresentacao,
       p.fabricante, p.via, p.anvisa, p.validade, p.preco, p.receita, p.continuo, p.popular
FROM (
  SELECT 'genericos' AS cat, 'Dipirona Sódica 500mg' AS nome, 'dipirona-sodica-500mg' AS slug, 'Genérico' AS tipo, 'Dipirona monoidratada' AS principio, 'Analgésico e antitérmico para dores leves a moderadas e febre.' AS descricao, '500mg' AS dosagem, '10 comprimidos' AS apresentacao, 'Lab. Vida Genéricos' AS fabricante, 'Oral' AS via, '1035500010001' AS anvisa, '2027-10-30' AS validade, 4.50 AS preco, FALSE AS receita, FALSE AS continuo, FALSE AS popular
  UNION ALL
  SELECT 'genericos', 'Paracetamol 750mg', 'paracetamol-750mg', 'Genérico', 'Paracetamol', 'Alívio de dores de cabeça, dores no corpo e febre.', '750mg', '20 comprimidos', 'Lab. Vida Genéricos', 'Oral', '1035500010002', '2027-08-31', 7.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'genericos', 'Ibuprofeno 600mg', 'ibuprofeno-600mg', 'Genérico', 'Ibuprofeno', 'Anti-inflamatório para dores musculares, cólicas e febre.', '600mg', '20 comprimidos', 'Pharmabrás', 'Oral', '1035500010003', '2027-06-30', 11.40, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'genericos', 'Loratadina 10mg', 'loratadina-10mg', 'Genérico', 'Loratadina', 'Antialérgico para rinite, coceira e espirros. Não costuma dar sono.', '10mg', '12 comprimidos', 'Pharmabrás', 'Oral', '1035500010004', '2027-09-30', 9.80, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'genericos', 'Omeprazol 20mg', 'omeprazol-20mg', 'Genérico', 'Omeprazol', 'Reduz a acidez do estômago; indicado para gastrite e refluxo.', '20mg', '28 cápsulas', 'Lab. Vida Genéricos', 'Oral', '1035500010005', '2027-05-31', 14.90, FALSE, TRUE, FALSE
  UNION ALL
  SELECT 'genericos', 'Amoxicilina 500mg', 'amoxicilina-500mg', 'Genérico', 'Amoxicilina tri-hidratada', 'Antibiótico para infecções bacterianas. Venda com retenção de receita.', '500mg', '21 cápsulas', 'Pharmabrás', 'Oral', '1035500010006', '2027-03-31', 24.90, TRUE, FALSE, FALSE
  UNION ALL
  SELECT 'genericos', 'Butilbrometo de Escopolamina 10mg', 'butilbrometo-de-escopolamina-10mg', 'Genérico', 'Butilbrometo de escopolamina', 'Alívio de cólicas abdominais e desconfortos intestinais.', '10mg', '20 drágeas', 'Lab. Vida Genéricos', 'Oral', '1035500010007', '2026-12-20', 12.60, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'referencia', 'Ibuprofeno 400mg Referência', 'ibuprofeno-400mg-referencia', 'Referência', 'Ibuprofeno', 'Anti-inflamatório com ação contra dor, febre e inflamação.', '400mg', '20 cápsulas', 'Indústria Saúde Total', 'Oral', '1035500020001', '2027-09-30', 19.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'referencia', 'Relaxan Plus', 'relaxan-plus', 'Referência', 'Dipirona + orfenadrina + cafeína', 'Relaxante muscular para dores de tensão e torcicolo.', '300mg+35mg+50mg', '24 comprimidos', 'Indústria Saúde Total', 'Oral', '1035500020002', '2027-11-30', 22.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'referencia', 'Cefalix Enxaqueca', 'cefalix-enxaqueca', 'Referência', 'Dipirona + isometepteno + cafeína', 'Indicado para dores de cabeça e enxaqueca.', '300mg', '20 drágeas', 'Indústria Saúde Total', 'Oral', '1035500020003', '2027-07-31', 26.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'referencia', 'Xarope Expectorante', 'xarope-expectorante', 'Referência', 'Ambroxol', 'Ajuda a soltar o catarro em tosses com secreção.', '30mg/5ml', 'Frasco 120ml', 'Indústria Saúde Total', 'Oral', '1035500020004', '2026-12-10', 29.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'uso-continuo', 'Losartana Potássica 50mg', 'losartana-potassica-50mg', 'Genérico', 'Losartana potássica', 'Controle da pressão alta e proteção dos rins em diabéticos.', '50mg', '30 comprimidos', 'Lab. Vida Genéricos', 'Oral', '1035500030001', '2027-12-31', 12.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Hidroclorotiazida 25mg', 'hidroclorotiazida-25mg', 'Genérico', 'Hidroclorotiazida', 'Diurético usado no tratamento da pressão alta e do inchaço.', '25mg', '30 comprimidos', 'Pharmabrás', 'Oral', '1035500030002', '2027-10-31', 6.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Atenolol 50mg', 'atenolol-50mg', 'Genérico', 'Atenolol', 'Controle da pressão arterial e dos batimentos do coração.', '50mg', '30 comprimidos', 'Lab. Vida Genéricos', 'Oral', '1035500030003', '2027-08-31', 9.50, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Metformina 850mg', 'metformina-850mg', 'Genérico', 'Cloridrato de metformina', 'Controle da glicose no diabetes tipo 2.', '850mg', '30 comprimidos', 'Pharmabrás', 'Oral', '1035500030004', '2027-11-30', 8.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Glibenclamida 5mg', 'glibenclamida-5mg', 'Genérico', 'Glibenclamida', 'Auxilia no controle do diabetes tipo 2 estimulando a insulina.', '5mg', '30 comprimidos', 'Lab. Vida Genéricos', 'Oral', '1035500030005', '2027-04-30', 5.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Sinvastatina 20mg', 'sinvastatina-20mg', 'Genérico', 'Sinvastatina', 'Reduz o colesterol e o risco de problemas no coração.', '20mg', '30 comprimidos', 'Pharmabrás', 'Oral', '1035500030006', '2027-09-30', 13.50, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Levotiroxina 50mcg', 'levotiroxina-50mcg', 'Referência', 'Levotiroxina sódica', 'Reposição do hormônio da tireoide.', '50mcg', '30 comprimidos', 'Indústria Saúde Total', 'Oral', '1035500030007', '2027-06-30', 16.80, TRUE, TRUE, FALSE
  UNION ALL
  SELECT 'uso-continuo', 'Salbutamol Spray 100mcg', 'salbutamol-spray-100mcg', 'Genérico', 'Sulfato de salbutamol', 'Broncodilatador para crises de asma e falta de ar.', '100mcg/dose', 'Frasco 200 doses', 'Pharmabrás', 'Inalatória', '1035500030008', '2027-02-28', 21.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'vitaminas', 'Vitamina C 1g Efervescente', 'vitamina-c-1g-efervescente', 'Suplemento', 'Ácido ascórbico', 'Vitamina C sabor laranja para apoiar a imunidade.', '1g', '10 pastilhas', 'VitaBem', 'Oral', NULL, '2027-11-30', 15.00, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'vitaminas', 'Vitamina D3 2000 UI', 'vitamina-d3-2000-ui', 'Suplemento', 'Colecalciferol', 'Ajuda na saúde dos ossos e na absorção de cálcio.', '2000 UI', '60 cápsulas', 'VitaBem', 'Oral', NULL, '2027-12-31', 34.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'vitaminas', 'Complexo B', 'complexo-b', 'Suplemento', 'Vitaminas do complexo B', 'Apoio ao metabolismo energético e ao sistema nervoso.', NULL, '30 comprimidos', 'VitaBem', 'Oral', NULL, '2027-07-31', 18.40, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'vitaminas', 'Ômega 3 1000mg', 'omega-3-1000mg', 'Suplemento', 'Óleo de peixe (EPA e DHA)', 'Fonte de ácidos graxos essenciais para o coração.', '1000mg', '60 cápsulas', 'NutriVida', 'Oral', NULL, '2027-05-31', 42.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'vitaminas', 'Polivitamínico A-Z', 'polivitaminico-a-z', 'Suplemento', 'Vitaminas e minerais', 'Combinação diária de vitaminas e minerais.', NULL, '60 comprimidos', 'NutriVida', 'Oral', NULL, '2027-10-31', 39.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Sabonete Líquido Neutro', 'sabonete-liquido-neutro', 'Higiene', NULL, 'pH neutro para peles sensíveis, sem fragrância artificial.', NULL, 'Frasco 250ml', 'Bem Care', 'Tópica', NULL, '2028-01-31', 18.20, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Creme Dental Sensível', 'creme-dental-sensivel', 'Higiene', 'Nitrato de potássio + flúor', 'Alívio da sensibilidade e proteção contra cáries.', NULL, 'Tubo 90g', 'Sorriso Leve', 'Tópica', NULL, '2028-03-31', 12.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Escova Dental Macia', 'escova-dental-macia', 'Higiene', NULL, 'Cerdas macias e cabeça pequena para alcançar o fundo da boca.', NULL, '2 unidades', 'Sorriso Leve', NULL, NULL, '2030-12-31', 9.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Desodorante Roll-on sem Álcool', 'desodorante-roll-on-sem-alcool', 'Higiene', NULL, 'Proteção de 48h, suave para a pele.', NULL, '50ml', 'Bem Care', 'Tópica', NULL, '2028-02-28', 14.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Shampoo Anticaspa', 'shampoo-anticaspa', 'Higiene', 'Piritionato de zinco', 'Controla a caspa e a coceira do couro cabeludo.', NULL, 'Frasco 200ml', 'Bem Care', 'Tópica', NULL, '2027-12-31', 24.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'dermocosmeticos', 'Protetor Solar FPS 50', 'protetor-solar-fps-50', 'Dermocosmético', NULL, 'Amplo espectro UVA e UVB, resistente à água, toque seco.', 'FPS 50', 'Frasco 200ml', 'Derma Sol', 'Tópica', NULL, '2027-08-31', 54.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'dermocosmeticos', 'Protetor Solar Facial FPS 60 com Cor', 'protetor-solar-facial-fps-60-com-cor', 'Dermocosmético', NULL, 'Proteção facial com cor universal e controle de oleosidade.', 'FPS 60', 'Bisnaga 40g', 'Derma Sol', 'Tópica', NULL, '2027-06-30', 69.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'dermocosmeticos', 'Hidratante Corporal Karité', 'hidratante-corporal-karite', 'Dermocosmético', 'Manteiga de karité + vitamina E', 'Hidratação profunda por até 48h.', NULL, 'Frasco 400ml', 'Bem Care', 'Tópica', NULL, '2028-04-30', 32.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'dermocosmeticos', 'Água Micelar', 'agua-micelar', 'Dermocosmético', NULL, 'Limpa e remove maquiagem sem precisar enxaguar.', NULL, 'Frasco 200ml', 'Derma Sol', 'Tópica', NULL, '2027-09-30', 36.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'mamae-bebe', 'Pomada para Assaduras', 'pomada-para-assaduras', 'Infantil', 'Óxido de zinco + vitaminas A e D', 'Previne e trata assaduras na área da fralda.', NULL, 'Bisnaga 45g', 'Pequeno Cuidado', 'Tópica', NULL, '2027-11-30', 19.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'mamae-bebe', 'Fralda Infantil Tamanho M', 'fralda-infantil-tamanho-m', 'Infantil', NULL, 'Até 12h de proteção com camada respirável.', 'Tamanho M', 'Pacote 32 unidades', 'Pequeno Cuidado', NULL, NULL, '2030-12-31', 44.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'mamae-bebe', 'Paracetamol Gotas Infantil', 'paracetamol-gotas-infantil', 'Genérico', 'Paracetamol', 'Antitérmico e analgésico em gotas para crianças. Siga a dose por peso indicada na bula.', '200mg/ml', 'Frasco 15ml', 'Lab. Vida Genéricos', 'Oral', '1035500070001', '2027-03-31', 8.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'mamae-bebe', 'Soro Fisiológico 0,9%', 'soro-fisiologico-0-9', 'Correlato', 'Cloreto de sódio 0,9%', 'Limpeza nasal e higiene dos olhos para toda a família.', '0,9%', 'Frasco 500ml', 'Pequeno Cuidado', 'Tópica', NULL, '2027-12-31', 7.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'primeiros-socorros', 'Kit Curativos Variados', 'kit-curativos-variados', 'Correlato', NULL, 'Curativos em 4 tamanhos com adesivo hipoalergênico.', NULL, '40 unidades', 'Health First', 'Tópica', NULL, '2028-06-30', 9.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'primeiros-socorros', 'Álcool 70% Gel', 'alcool-70-gel', 'Correlato', 'Álcool etílico 70%', 'Higienização das mãos sem água.', '70%', 'Frasco 500g', 'Health First', 'Tópica', NULL, '2027-12-31', 13.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'primeiros-socorros', 'Gaze Estéril', 'gaze-esteril', 'Correlato', NULL, 'Compressas estéreis para limpeza e cobertura de ferimentos.', '7,5 x 7,5 cm', '10 unidades', 'Health First', 'Tópica', NULL, '2029-06-30', 6.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'primeiros-socorros', 'Antisséptico Spray', 'antisseptico-spray', 'Correlato', 'Digliconato de clorexidina', 'Limpa pequenos ferimentos e ajuda a prevenir infecções.', '1%', 'Frasco 30ml', 'Health First', 'Tópica', NULL, '2027-07-31', 17.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'aparelhos', 'Termômetro Digital', 'termometro-digital', 'Correlato', NULL, 'Medição em cerca de 60 segundos, com aviso sonoro.', NULL, '1 unidade', 'MedCasa', NULL, NULL, '2031-12-31', 24.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'aparelhos', 'Medidor de Pressão de Pulso', 'medidor-de-pressao-de-pulso', 'Correlato', NULL, 'Aparelho automático com memória das últimas 60 medições.', NULL, '1 unidade', 'MedCasa', NULL, NULL, '2031-12-31', 149.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'aparelhos', 'Oxímetro de Dedo', 'oximetro-de-dedo', 'Correlato', NULL, 'Mostra a saturação de oxigênio e os batimentos por minuto.', NULL, '1 unidade', 'MedCasa', NULL, NULL, '2031-12-31', 89.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'aparelhos', 'Tiras para Glicemia', 'tiras-para-glicemia', 'Correlato', NULL, 'Tiras de teste para medidor de glicose MedCasa.', NULL, '50 tiras', 'MedCasa', NULL, NULL, '2027-04-30', 59.90, FALSE, TRUE, FALSE
) AS p
JOIN categorias c ON c.slug = p.cat;

-- Estoque inicial (alguns itens já começam abaixo do mínimo para demonstrar os alertas)
INSERT INTO estoques (id_medicamento, quantidade, quantidade_minima, localizacao)
SELECT m.id_medicamento, e.qtd, e.minimo, 'Depósito' FROM (
  SELECT 'dipirona-sodica-500mg' AS slug, 180 AS qtd, 20 AS minimo
  UNION ALL
  SELECT 'paracetamol-750mg', 150, 20
  UNION ALL
  SELECT 'ibuprofeno-600mg', 90, 15
  UNION ALL
  SELECT 'loratadina-10mg', 70, 10
  UNION ALL
  SELECT 'omeprazol-20mg', 120, 15
  UNION ALL
  SELECT 'amoxicilina-500mg', 40, 10
  UNION ALL
  SELECT 'butilbrometo-de-escopolamina-10mg', 8, 10
  UNION ALL
  SELECT 'ibuprofeno-400mg-referencia', 60, 10
  UNION ALL
  SELECT 'relaxan-plus', 85, 15
  UNION ALL
  SELECT 'cefalix-enxaqueca', 45, 10
  UNION ALL
  SELECT 'xarope-expectorante', 30, 8
  UNION ALL
  SELECT 'losartana-potassica-50mg', 200, 25
  UNION ALL
  SELECT 'hidroclorotiazida-25mg', 140, 20
  UNION ALL
  SELECT 'atenolol-50mg', 95, 15
  UNION ALL
  SELECT 'metformina-850mg', 160, 25
  UNION ALL
  SELECT 'glibenclamida-5mg', 6, 15
  UNION ALL
  SELECT 'sinvastatina-20mg', 110, 20
  UNION ALL
  SELECT 'levotiroxina-50mcg', 75, 15
  UNION ALL
  SELECT 'salbutamol-spray-100mcg', 35, 10
  UNION ALL
  SELECT 'vitamina-c-1g-efervescente', 130, 20
  UNION ALL
  SELECT 'vitamina-d3-2000-ui', 80, 15
  UNION ALL
  SELECT 'complexo-b', 65, 10
  UNION ALL
  SELECT 'omega-3-1000mg', 50, 10
  UNION ALL
  SELECT 'polivitaminico-a-z', 55, 10
  UNION ALL
  SELECT 'sabonete-liquido-neutro', 90, 15
  UNION ALL
  SELECT 'creme-dental-sensivel', 140, 20
  UNION ALL
  SELECT 'escova-dental-macia', 120, 20
  UNION ALL
  SELECT 'desodorante-roll-on-sem-alcool', 75, 10
  UNION ALL
  SELECT 'shampoo-anticaspa', 40, 10
  UNION ALL
  SELECT 'protetor-solar-fps-50', 60, 10
  UNION ALL
  SELECT 'protetor-solar-facial-fps-60-com-cor', 35, 8
  UNION ALL
  SELECT 'hidratante-corporal-karite', 70, 10
  UNION ALL
  SELECT 'agua-micelar', 4, 8
  UNION ALL
  SELECT 'pomada-para-assaduras', 80, 10
  UNION ALL
  SELECT 'fralda-infantil-tamanho-m', 50, 10
  UNION ALL
  SELECT 'paracetamol-gotas-infantil', 60, 10
  UNION ALL
  SELECT 'soro-fisiologico-0-9', 100, 15
  UNION ALL
  SELECT 'kit-curativos-variados', 110, 15
  UNION ALL
  SELECT 'alcool-70-gel', 130, 20
  UNION ALL
  SELECT 'gaze-esteril', 90, 15
  UNION ALL
  SELECT 'antisseptico-spray', 45, 10
  UNION ALL
  SELECT 'termometro-digital', 40, 8
  UNION ALL
  SELECT 'medidor-de-pressao-de-pulso', 15, 5
  UNION ALL
  SELECT 'oximetro-de-dedo', 20, 5
  UNION ALL
  SELECT 'tiras-para-glicemia', 30, 10
) AS e
JOIN medicamentos m ON m.slug = e.slug;

INSERT INTO cupons (codigo, descricao, tipo, valor, valor_minimo_pedido, data_fim) VALUES
  ('BEMESTAR10', '10% de desconto em todo o site', 'PERCENTUAL', 10.00, 20.00, '2027-12-31 23:59:59'),
  ('SAUDE15', '15% de desconto em compras acima de R$ 80', 'PERCENTUAL', 15.00, 80.00, '2027-12-31 23:59:59'),
  ('FRETEGRATIS', 'Frete grátis em compras acima de R$ 40', 'FRETE_GRATIS', 0.00, 40.00, '2027-12-31 23:59:59'),
  ('PRIMEIRACOMPRA', 'R$ 5,00 de desconto na primeira compra', 'VALOR_FIXO', 5.00, 15.00, '2027-12-31 23:59:59');
