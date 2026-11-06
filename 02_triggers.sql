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
