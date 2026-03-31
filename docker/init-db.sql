-- ═══════════════════════════════════════════════════════════════
-- Holdus SaaS — Inicialização do PostgreSQL
-- Extensões, RLS, e configurações de segurança
-- ═══════════════════════════════════════════════════════════════

-- Extensões necessárias
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";      -- Geração de UUIDs
CREATE EXTENSION IF NOT EXISTS "pgcrypto";        -- Criptografia (AES-256)
CREATE EXTENSION IF NOT EXISTS "unaccent";        -- Busca sem acentos
CREATE EXTENSION IF NOT EXISTS "pg_trgm";         -- Busca por similaridade (trigram)

-- Collation para português brasileiro
-- (Já definido via POSTGRES_INITDB_ARGS no docker-compose)

-- Função para gerar UUID v4 (atalho)
CREATE OR REPLACE FUNCTION gen_uuid() RETURNS uuid AS $$
  SELECT uuid_generate_v4();
$$ LANGUAGE sql;

-- Função para busca normalizada (sem acentos, minúsculo)
CREATE OR REPLACE FUNCTION normalize_text(input text) RETURNS text AS $$
  SELECT lower(unaccent(input));
$$ LANGUAGE sql IMMUTABLE;

-- ═══════════════════════════════════════════════════════════════
-- Row-Level Security (RLS) — Setup base
-- Cada tabela multi-tenant terá RLS habilitado via migration
-- A app seta: SET app.current_tenant_id = 'uuid-do-tenant';
-- ═══════════════════════════════════════════════════════════════

-- Função que retorna o tenant_id da sessão atual
CREATE OR REPLACE FUNCTION current_tenant_id() RETURNS uuid AS $$
  SELECT COALESCE(
    NULLIF(current_setting('app.current_tenant_id', true), '')::uuid,
    '00000000-0000-0000-0000-000000000000'::uuid
  );
$$ LANGUAGE sql STABLE;

-- Função de trigger para updated_at automático
CREATE OR REPLACE FUNCTION trigger_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
  NEW."UpdatedAt" = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- ═══════════════════════════════════════════════════════════════
-- Comentário de documentação
-- ═══════════════════════════════════════════════════════════════
COMMENT ON DATABASE holdus IS 'Holdus Tríade SaaS — Sistema de Constituição de Holdings Familiares';
