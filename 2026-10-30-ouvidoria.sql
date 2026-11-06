-- =============================================================
-- Atualização de 30/10/2026: cria a tabela da Ouvidoria (RF30) num banco que JÁ EXISTIA.
-- Bancos novos não precisam disto: a tabela já está no 01_ddl.sql.
-- O Docker ignora esta pasta (só roda os arquivos que ficam direto em database/).
-- Pode rodar mais de uma vez sem problema (IF NOT EXISTS). Não apaga nada.
--
-- Como rodar com o Docker ligado (na pasta principal do projeto, no PowerShell):
--   Get-Content database\atualizacoes\2026-10-30-ouvidoria.sql | docker compose exec -T banco mysql -uroot -pBemEstar@2026 --default-character-set=utf8mb4
-- Ou abra este arquivo no MySQL Workbench e execute.
-- =============================================================
SET NAMES utf8mb4;
USE db_bemestar;

CREATE TABLE IF NOT EXISTS ouvidoria (
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
