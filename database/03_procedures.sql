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
