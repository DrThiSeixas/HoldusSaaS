-- ═══════════════════════════════════════════════════════════════
-- HOLDUS SaaS — Script Completo de Criação do Banco
-- PostgreSQL 16 | 20 tabelas | Seed incluído
-- 
-- INSTRUÇÕES:
-- 1. Criar o banco: CREATE DATABASE holdus;
-- 2. Conectar ao banco holdus
-- 3. Executar este script inteiro
-- ═══════════════════════════════════════════════════════════════

-- Extensões
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "unaccent";

-- ═══════════════════════════════════════════════════════════════
-- 1. TENANTS (raiz do multi-tenant)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Tenants" (
    "Id"                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Nome"                  varchar(200) NOT NULL,
    "Slug"                  varchar(50) NOT NULL,
    "CnpjEscritorio"       varchar(18),
    "Plano"                 integer NOT NULL DEFAULT 0,
    "MaxUsuarios"           integer NOT NULL DEFAULT 2,
    "MaxProjetos"           integer NOT NULL DEFAULT 5,
    "StorageLimiteMb"       bigint NOT NULL DEFAULT 1024,
    "LogoUrl"               text,
    "CorPrimaria"           varchar(7),
    "RegimeTributario"      integer NOT NULL DEFAULT 0,
    "IssAliquota"           numeric(5,2) NOT NULL DEFAULT 5.00,
    "Municipio"             varchar(100) NOT NULL DEFAULT '',
    "Uf"                    varchar(2) NOT NULL DEFAULT '',
    "ConfiguracoesJson"     jsonb,
    "Ativo"                 boolean NOT NULL DEFAULT true,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tenants_Slug" ON "Tenants" ("Slug");

-- ═══════════════════════════════════════════════════════════════
-- 2. USUARIOS
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Usuarios" (
    "Id"                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "IdentityUserId"        varchar(200) NOT NULL DEFAULT '',
    "Role"                  integer NOT NULL DEFAULT 1,
    "NomeCompleto"          varchar(200) NOT NULL,
    "Email"                 varchar(200) NOT NULL,
    "OabNumero"             varchar(20),
    "OabUf"                 varchar(2),
    "Telefone"              text,
    "Ativo"                 boolean NOT NULL DEFAULT true,
    "UltimoAcesso"          timestamptz,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Usuarios_Tenant_Email" ON "Usuarios" ("TenantId", "Email");

-- ═══════════════════════════════════════════════════════════════
-- 3. AUDIT LOG (append-only, LGPD Art. 37)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id"                    bigserial PRIMARY KEY,
    "TenantId"              uuid NOT NULL,
    "UsuarioId"             uuid,
    "Acao"                  integer NOT NULL,
    "Entidade"              varchar(100) NOT NULL,
    "EntidadeId"            varchar(50) NOT NULL,
    "CamposAcessados"       text[],
    "IpOrigem"              varchar(45),
    "UserAgent"             text,
    "DetalhesJson"          jsonb,
    "Timestamp"             timestamptz NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TenantId" ON "AuditLogs" ("TenantId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Timestamp" ON "AuditLogs" ("Timestamp");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Entidade" ON "AuditLogs" ("TenantId", "Entidade", "EntidadeId");

-- ═══════════════════════════════════════════════════════════════
-- 4. PESSOA FISICA
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "PessoasFisicas" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "Nome"                  varchar(200) NOT NULL,
    "Cpf"                   varchar(500) NOT NULL,
    "Rg"                    varchar(500),
    "RgOrgaoEmissor"        text,
    "RgUfEmissor"           text,
    "DataNascimento"        date,
    "Naturalidade"          text,
    "Nacionalidade"         varchar(100) NOT NULL DEFAULT 'Brasileira',
    "Profissao"             text,
    "EstadoCivil"           integer NOT NULL DEFAULT 0,
    "RegimeBens"            integer,
    "ConjugeId"             integer REFERENCES "PessoasFisicas"("Id") ON DELETE SET NULL,
    "NomePai"               varchar(500),
    "NomeMae"               varchar(500),
    "Cep"                   text,
    "Logradouro"            text,
    "Numero"                text,
    "Complemento"           text,
    "Bairro"                text,
    "Cidade"                text,
    "Uf"                    varchar(2),
    "Celular"               varchar(500),
    "Email"                 varchar(500),
    "WhatsApp"              text,
    "Observacoes"           text,
    "Ativo"                 boolean NOT NULL DEFAULT true,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PF_Tenant_Cpf" ON "PessoasFisicas" ("TenantId", "Cpf") WHERE "DeletedAt" IS NULL;

-- ═══════════════════════════════════════════════════════════════
-- 5. PROJETO TRIADE
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Projetos" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "Codigo"                varchar(20) NOT NULL,
    "NomeProjeto"           varchar(200) NOT NULL,
    "NomeFamilia"           varchar(200) NOT NULL,
    "ModeloEscolhido"       integer NOT NULL DEFAULT 2,
    "Status"                integer NOT NULL DEFAULT 0,
    "DataContratacao"       timestamptz,
    "DataConclusao"         timestamptz,
    "AdvogadoResponsavelId" uuid REFERENCES "Usuarios"("Id"),
    "Observacoes"           text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Projetos_Tenant_Codigo" ON "Projetos" ("TenantId", "Codigo") WHERE "DeletedAt" IS NULL;

-- ═══════════════════════════════════════════════════════════════
-- 6. PARTICIPANTE PROJETO
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Participantes" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "ProjetoId"             integer NOT NULL REFERENCES "Projetos"("Id"),
    "PessoaFisicaId"        integer NOT NULL REFERENCES "PessoasFisicas"("Id"),
    "Papel"                 integer NOT NULL DEFAULT 0,
    "Observacoes"           text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 7. CELULA (HOLDING)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Celulas" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "ProjetoId"             integer NOT NULL REFERENCES "Projetos"("Id"),
    "Tipo"                  integer NOT NULL DEFAULT 0,
    "Status"                integer NOT NULL DEFAULT 0,
    "NomeCelula"            varchar(300) NOT NULL,
    "ObjetoSocial"          text,
    "CapitalSocialPrevisto" numeric(18,2) NOT NULL DEFAULT 0,
    "Cnpj"                  varchar(18),
    "RazaoSocial"           varchar(300),
    "NomeFantasia"          varchar(300),
    "CapitalSocialEfetivo"  numeric(18,2),
    "Nire"                  varchar(20),
    "DataRegistro"          date,
    "EnderecoJson"          jsonb,
    "AdministradorId"       integer REFERENCES "PessoasFisicas"("Id"),
    "Observacoes"           text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 8. SOCIO CELULA
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "SociosCelulas" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "CelulaId"              integer NOT NULL REFERENCES "Celulas"("Id"),
    "PessoaFisicaId"        integer NOT NULL REFERENCES "PessoasFisicas"("Id"),
    "QuantidadeQuotas"      integer NOT NULL DEFAULT 0,
    "ValorPorQuota"         numeric(18,2) NOT NULL DEFAULT 1.00,
    "PercentualParticipacao" numeric(5,2) NOT NULL DEFAULT 0,
    "TipoQuota"             integer NOT NULL DEFAULT 0,
    "PesoVoto"              integer,
    "UsufrutoVitalicio"     boolean NOT NULL DEFAULT false,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SocioCelula_Unique" ON "SociosCelulas" ("CelulaId", "PessoaFisicaId");

-- ═══════════════════════════════════════════════════════════════
-- 9. BEM PATRIMONIAL
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Bens" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "ProprietarioId"        integer NOT NULL REFERENCES "PessoasFisicas"("Id"),
    "Tipo"                  integer NOT NULL DEFAULT 0,
    "Descricao"             varchar(500) NOT NULL,
    "ValorDeclaracaoIR"     numeric(18,2) NOT NULL DEFAULT 0,
    "ValorMercado"          numeric(18,2) NOT NULL DEFAULT 0,
    "DataAvaliacao"         date,
    "DadosEspecificosJson"  jsonb,
    "CelulaDestinoId"       integer REFERENCES "Celulas"("Id"),
    "Observacoes"           text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 10. FASE PROJETO
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "FasesProjeto" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "ProjetoId"             integer NOT NULL REFERENCES "Projetos"("Id"),
    "NumeroFase"            integer NOT NULL,
    "NomeFase"              varchar(200) NOT NULL,
    "Status"                varchar(50) NOT NULL DEFAULT 'Pendente',
    "DataInicio"            timestamptz,
    "DataConclusao"         timestamptz,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 11. PASSO FASE
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "PassosFase" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "FaseId"                integer NOT NULL REFERENCES "FasesProjeto"("Id"),
    "Ordem"                 integer NOT NULL,
    "Descricao"             text NOT NULL,
    "Status"                varchar(50) NOT NULL DEFAULT 'Pendente',
    "DataConclusao"         timestamptz,
    "Observacoes"           text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 12. TEMPLATE DOCUMENTO
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Templates" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "Nome"                  varchar(200) NOT NULL,
    "Categoria"             integer NOT NULL DEFAULT 0,
    "Versao"                integer NOT NULL DEFAULT 1,
    "ConteudoHtml"          text NOT NULL DEFAULT '',
    "PlaceholdersJson"      jsonb,
    "SchemaValidacao"       jsonb,
    "IsPublico"             boolean NOT NULL DEFAULT false,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 13. DOCUMENTO GERADO
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "DocumentosGerados" (
    "Id"                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "TemplateId"            integer NOT NULL REFERENCES "Templates"("Id"),
    "ProjetoId"             integer REFERENCES "Projetos"("Id"),
    "Titulo"                varchar(300) NOT NULL,
    "ConteudoHtml"          text NOT NULL DEFAULT '',
    "DadosPreenchimentoJson" jsonb,
    "Versao"                integer NOT NULL DEFAULT 1,
    "VersaoAnteriorId"      uuid REFERENCES "DocumentosGerados"("Id"),
    "Status"                integer NOT NULL DEFAULT 0,
    "PdfStorageKey"         text,
    "DocxStorageKey"        text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 14. CLAUSULA
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Clausulas" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "Titulo"                varchar(200) NOT NULL,
    "Categoria"             integer NOT NULL DEFAULT 0,
    "TextoHtml"             text NOT NULL DEFAULT '',
    "Tags"                  text[] NOT NULL DEFAULT '{}',
    "VezesUtilizada"        integer NOT NULL DEFAULT 0,
    "IsPublica"             boolean NOT NULL DEFAULT false,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 15. ARQUIVO STORAGE
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Arquivos" (
    "Id"                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "StorageKey"            varchar(500) NOT NULL,
    "NomeOriginal"          varchar(200) NOT NULL,
    "ContentType"           varchar(100) NOT NULL,
    "TamanhoBytes"          bigint NOT NULL DEFAULT 0,
    "EntidadeRef"           varchar(50) NOT NULL,
    "EntidadeRefId"         varchar(50) NOT NULL,
    "Categoria"             text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);
CREATE INDEX IF NOT EXISTS "IX_Arquivos_EntidadeRef" ON "Arquivos" ("TenantId", "EntidadeRef", "EntidadeRefId");

-- ═══════════════════════════════════════════════════════════════
-- 16. CONTRATO HONORARIOS
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "ContratosHonorarios" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "ProjetoId"             integer NOT NULL REFERENCES "Projetos"("Id"),
    "ValorBruto"            numeric(18,2) NOT NULL DEFAULT 0,
    "ValorDeducoes"         numeric(18,2) NOT NULL DEFAULT 0,
    "ValorIncentivo"        numeric(18,2) NOT NULL DEFAULT 0,
    "DataContrato"          date NOT NULL,
    "Status"                integer NOT NULL DEFAULT 0,
    "DocumentoGeradoId"     uuid REFERENCES "DocumentosGerados"("Id"),
    "Observacoes"           text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Contrato_Projeto" ON "ContratosHonorarios" ("ProjetoId");

-- ═══════════════════════════════════════════════════════════════
-- 17. PARCELA
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Parcelas" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "ContratoId"            integer NOT NULL REFERENCES "ContratosHonorarios"("Id"),
    "Numero"                integer NOT NULL,
    "Descricao"             varchar(100) NOT NULL DEFAULT '',
    "Valor"                 numeric(18,2) NOT NULL DEFAULT 0,
    "DataVencimento"        date NOT NULL,
    "DataPagamento"         date,
    "ValorPago"             numeric(18,2),
    "FormaPagamento"        integer,
    "Status"                integer NOT NULL DEFAULT 0,
    "NotaFiscalId"          uuid,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Parcela_Contrato_Num" ON "Parcelas" ("ContratoId", "Numero");

-- ═══════════════════════════════════════════════════════════════
-- 18. NOTA FISCAL
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "NotasFiscais" (
    "Id"                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "ParcelaId"             integer NOT NULL REFERENCES "Parcelas"("Id"),
    "NumeroNfse"            text,
    "CodigoVerificacao"     text,
    "XmlEnvio"              text,
    "XmlRetorno"            text,
    "ValorServico"          numeric(18,2) NOT NULL DEFAULT 0,
    "IssAliquota"           numeric(5,2) NOT NULL DEFAULT 0,
    "IssValor"              numeric(18,2) NOT NULL DEFAULT 0,
    "Status"                integer NOT NULL DEFAULT 0,
    "EmitidaEm"             timestamptz,
    "ErroMensagem"          text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 19. CONSENTIMENTO LGPD
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "ConsentimentosLgpd" (
    "Id"                    uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "PessoaFisicaId"        integer NOT NULL REFERENCES "PessoasFisicas"("Id"),
    "BaseLegal"             integer NOT NULL DEFAULT 0,
    "Finalidade"            varchar(500) NOT NULL,
    "AceitoEm"              timestamptz NOT NULL DEFAULT NOW(),
    "ValidoAte"             timestamptz,
    "RevogadoEm"            timestamptz,
    "IpAceite"              varchar(45),
    "HashEvidencia"         varchar(128) NOT NULL DEFAULT '',
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- 20. CAPTACAO (Wizard de Viabilidade)
-- ═══════════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS "Captacoes" (
    "Id"                    serial PRIMARY KEY,
    "TenantId"              uuid NOT NULL REFERENCES "Tenants"("Id"),
    "NomeCompleto"          varchar(200) NOT NULL,
    "Telefone"              varchar(500) NOT NULL,
    "Email"                 varchar(500) NOT NULL,
    "Cidade"                text,
    "Uf"                    varchar(2),
    "Profissao"             text,
    "RendaMensalEstimada"   numeric(18,2),
    "FaixaPatrimonio"       integer NOT NULL DEFAULT 0,
    "NaturezaBens"          integer NOT NULL DEFAULT 0,
    "DadosFamiliaJson"      jsonb,
    "ComoConheceu"          integer NOT NULL DEFAULT 0,
    "IndicadoPor"           text,
    "Status"                integer NOT NULL DEFAULT 0,
    "PessoaFisicaGeradaId"  integer,
    "ProjetoGeradoId"       integer,
    "ConsentimentoLgpdId"   uuid,
    "Observacoes"           text,
    "CreatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt"             timestamptz NOT NULL DEFAULT NOW(),
    "CreatedBy"             uuid,
    "UpdatedBy"             uuid,
    "DeletedAt"             timestamptz,
    "DeletedBy"             uuid
);

-- ═══════════════════════════════════════════════════════════════
-- SEED — Tenant demo + Usuário admin + Templates + Cláusulas
-- ═══════════════════════════════════════════════════════════════

-- Tenant demo
INSERT INTO "Tenants" ("Id", "Nome", "Slug", "CnpjEscritorio", "Plano", "MaxUsuarios", "MaxProjetos", "StorageLimiteMb", "RegimeTributario", "IssAliquota", "Municipio", "Uf", "Ativo")
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'Thiago Seixas Advocacia Empresarial',
    'thiago-seixas',
    '00.000.000/0001-00',
    1, 10, 50, 10240, 0, 5.00,
    'São João da Boa Vista', 'SP', true
) ON CONFLICT ("Id") DO NOTHING;

-- Usuário admin
INSERT INTO "Usuarios" ("Id", "TenantId", "IdentityUserId", "Role", "NomeCompleto", "Email", "OabNumero", "OabUf", "Ativo")
VALUES (
    '22222222-2222-2222-2222-222222222222',
    '11111111-1111-1111-1111-111111111111',
    'seed-admin-identity',
    0,
    'Thiago Seixas',
    'thiago@holdus.com.br',
    '000.000', 'SP', true
) ON CONFLICT ("Id") DO NOTHING;

-- Templates padrão
INSERT INTO "Templates" ("TenantId", "Nome", "Categoria", "Versao", "ConteudoHtml", "IsPublico")
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Contrato social — Holding patrimonial Ltda', 0, 1, '<p>Template de contrato social</p>', false),
    ('11111111-1111-1111-1111-111111111111', 'Alteração contratual — Cessão de quotas', 1, 1, '<p>Template de alteração contratual</p>', false),
    ('11111111-1111-1111-1111-111111111111', 'Ata de reunião de sócios', 2, 1, '<p>Template de ata</p>', false),
    ('11111111-1111-1111-1111-111111111111', 'Procuração societária ad judicia et extra', 3, 1, '<p>Template de procuração</p>', false),
    ('11111111-1111-1111-1111-111111111111', 'Contrato de honorários advocatícios', 4, 1, '<p>Template de honorários</p>', false),
    ('11111111-1111-1111-1111-111111111111', 'Instrumento de doação com reserva de usufruto', 5, 1, '<p>Template de doação</p>', false)
ON CONFLICT DO NOTHING;

-- Cláusulas padrão
INSERT INTO "Clausulas" ("TenantId", "Titulo", "Categoria", "TextoHtml", "Tags", "IsPublica")
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Inalienabilidade vitalícia das quotas', 0, '<p>As quotas sociais são inalienáveis e impenhoráveis enquanto viver o(a) doador(a).</p>', ARRAY['inalienabilidade','proteção'], false),
    ('11111111-1111-1111-1111-111111111111', 'Incomunicabilidade de quotas', 0, '<p>As quotas sociais não se comunicam com o patrimônio do cônjuge ou companheiro(a).</p>', ARRAY['incomunicabilidade','casamento'], false),
    ('11111111-1111-1111-1111-111111111111', 'Reserva de usufruto vitalício', 0, '<p>Os doadores reservam para si o usufruto vitalício das quotas doadas.</p>', ARRAY['usufruto','doação'], false),
    ('11111111-1111-1111-1111-111111111111', 'Administração exclusiva do constituinte', 1, '<p>A administração da sociedade será exercida exclusivamente pelo(a) sócio(a) administrador(a).</p>', ARRAY['administração','governança'], false),
    ('11111111-1111-1111-1111-111111111111', 'Distribuição desproporcional de lucros', 3, '<p>Os lucros poderão ser distribuídos de forma desproporcional à participação societária.</p>', ARRAY['lucros','distribuição'], false),
    ('11111111-1111-1111-1111-111111111111', 'Cláusula de call option', 4, '<p>O sócio administrador terá direito de compra compulsória das quotas dos demais sócios.</p>', ARRAY['call','opção','compra'], false)
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════
-- FIM — 20 tabelas criadas + seed executado
-- ═══════════════════════════════════════════════════════════════
SELECT 'Holdus SaaS — Banco criado com sucesso! ' || COUNT(*) || ' tabelas.' 
FROM information_schema.tables 
WHERE table_schema = 'public' AND table_type = 'BASE TABLE';
