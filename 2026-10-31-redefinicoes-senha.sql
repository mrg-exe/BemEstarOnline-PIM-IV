-- =============================================================
-- Atualização de 31/10/2026: tabela redefinicoes_senha ("Esqueci minha senha", RF33)
-- num banco que JÁ EXISTIA. Bancos novos não precisam disto: a tabela já está no 01_ddl.sql.
-- Pode rodar mais de uma vez sem problema (IF NOT EXISTS). Não apaga nada.
--
-- Como rodar com o Docker ligado (na pasta principal do projeto, no PowerShell):
--   Get-Content database\atualizacoes\2026-10-31-redefinicoes-senha.sql | docker compose exec -T banco mysql -uroot -pBemEstar@2026 --default-character-set=utf8mb4
-- =============================================================
SET NAMES utf8mb4;
USE db_bemestar;

CREATE TABLE IF NOT EXISTS redefinicoes_senha (
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