-- ═══════════════════════════════════════════════════════════════
-- HOLDUS SaaS — Criar banco de dados
-- Execute este script no pgAdmin conectado ao servidor PostgreSQL
-- (conecte-se ao banco 'postgres' para executar o CREATE DATABASE)
-- ═══════════════════════════════════════════════════════════════

-- 1. Criar o banco
CREATE DATABASE holdus
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'Portuguese_Brazil.1252'
    LC_CTYPE = 'Portuguese_Brazil.1252'
    TEMPLATE = template0;

-- 2. Conectar ao banco holdus (no pgAdmin: clique com botão direito no banco holdus > Query Tool)
-- Depois execute o bloco abaixo:

-- ═══════════════════════════════════════════════════════════════
-- EXTENSÕES (executar conectado ao banco 'holdus')
-- ═══════════════════════════════════════════════════════════════
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
-- CREATE EXTENSION IF NOT EXISTS "pgcrypto";
-- CREATE EXTENSION IF NOT EXISTS "unaccent";

-- ═══════════════════════════════════════════════════════════════
-- NOTA: As tabelas serão criadas automaticamente pelo EF Core
-- ao rodar a API pela primeira vez (EnsureCreatedAsync).
-- Não é necessário criar tabelas manualmente.
-- ═══════════════════════════════════════════════════════════════
