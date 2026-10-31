-- =============================================================
-- Atualização de 31/10/2026: coluna usuarios.trocar_senha (RF35, gestão da equipe)
-- num banco que JÁ EXISTIA. Bancos novos não precisam disto: a coluna já está no 01_ddl.sql.
-- Pode rodar mais de uma vez sem problema (confere antes de criar). Não apaga nada.
--
-- Como rodar com o Docker ligado (na pasta principal do projeto, no PowerShell):
--   Get-Content database\atualizacoes\2026-10-31-equipe-trocar-senha.sql | docker compose exec -T banco mysql -uroot -pBemEstar@2026 --default-character-set=utf8mb4
-- =============================================================
SET NAMES utf8mb4;
USE db_bemestar;

-- O MySQL 8 não tem "ADD COLUMN IF NOT EXISTS": conferimos no dicionário de dados.
SET @existe := (SELECT COUNT(*) FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = 'db_bemestar' AND TABLE_NAME = 'usuarios' AND COLUMN_NAME = 'trocar_senha');
SET @comando := IF(@existe = 0,
    'ALTER TABLE usuarios ADD COLUMN trocar_senha BOOLEAN NOT NULL DEFAULT FALSE COMMENT ''Senha provisória dada pelo admin: troca obrigatória no próximo acesso (RF35)'' AFTER ativo',
    'SELECT ''coluna trocar_senha já existe'' AS aviso');
PREPARE passo FROM @comando;
EXECUTE passo;
DEALLOCATE PREPARE passo;
