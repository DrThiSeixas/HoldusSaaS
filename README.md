# Holdus — Sistema de Constituição de Holdings

**Holdings · Estrutura Patrimonial · Proteção Jurídica**
Método Tríade Capital® — Planejamento Sucessório, Patrimonial e Tributário

---

## Arquitetura

```
┌─────────────────────────────────────────────────────────┐
│                    React / Next.js                       │
│              (TipTap, Tailwind, Shadcn/UI)              │
└──────────────────────┬──────────────────────────────────┘
                       │ HTTPS / JWT
┌──────────────────────▼──────────────────────────────────┐
│                  ASP.NET Core 9 API                      │
│         Identity + Multi-Tenant Middleware               │
│              Hangfire Background Jobs                    │
├─────────┬──────────┬──────────┬────────────┬────────────┤
│ Domain  │ Infra    │ App      │ API        │ Shared     │
│ Entities│ EF Core  │ Services │ Controllers│ DTOs       │
│ Enums   │ Npgsql   │ DTOs     │ Middleware │ Interfaces │
│ Interf. │ Redis    │ Mapping  │ Filters    │            │
│         │ MinIO    │          │ Extensions │            │
└────┬────┴────┬─────┴──────────┴─────┬──────┴────────────┘
     │         │                      │
┌────▼────┐ ┌──▼──┐              ┌────▼────┐
│PostgreSQL│ │Redis│              │  MinIO  │
│   16     │ │  7  │              │  (S3)   │
│Multi-ten.│ │Cache│              │ Storage │
└──────────┘ └─────┘              └─────────┘
```

## Stack Técnica

| Camada        | Tecnologia                    | Função                           |
|---------------|-------------------------------|----------------------------------|
| Frontend      | React / Next.js               | SPA com TipTap editor            |
| Backend       | ASP.NET Core 9                | API REST + Identity + JWT        |
| ORM           | Entity Framework Core 9       | Migrations + Query Filters       |
| Banco         | PostgreSQL 16 + Npgsql        | Multi-tenant com RLS             |
| Cache         | Redis 7                       | Sessões, rate limiting           |
| Storage       | MinIO (S3-compatible)         | Documentos, PDFs, anexos         |
| Jobs          | Hangfire + PostgreSQL          | NFS-e, geração docs, alertas     |
| Containerização | Docker + Docker Compose     | Dev/staging/production           |

## Estrutura do Projeto

```
HoldusSaaS/
├── Holdus.sln
├── docker-compose.yml
├── docker/
│   ├── Dockerfile
│   └── init-db.sql                  # Extensions + RLS setup
├── docs/
└── src/
    ├── Holdus.Domain/         # Entidades, Enums, Interfaces
    │   ├── Entities/
    │   │   ├── BaseEntity.cs        # Bases com Tenant + Audit + SoftDelete
    │   │   ├── Tenant.cs            # Escritório (raiz multi-tenant)
    │   │   ├── Usuario.cs           # Advogado/Assistente
    │   │   ├── CoreEntities.cs      # PessoaFisica, Projeto, Celula, Bem...
    │   │   └── SupportEntities.cs   # Templates, Financeiro, LGPD, NFS-e
    │   ├── Enums/
    │   │   └── Enums.cs             # Todos os enums do domínio
    │   └── Interfaces/
    │       └── IEntityInterfaces.cs # ITenantEntity, IAuditable, ISoftDeletable
    ├── Holdus.Infrastructure/  # EF Core, Data, Services
    │   └── Data/
    │       └── AppDbContext.cs       # DbContext com Query Filters + Audit
    ├── Holdus.Application/     # Services, DTOs, Mapping
    └── Holdus.API/             # Controllers, Middleware, Extensions
```

## Entidades (26 total)

### Infraestrutura SaaS (4)
- **Tenant** — Escritório de advocacia (raiz do isolamento)
- **Usuario** — Advogado/Assistente vinculado ao Identity
- **AuditLog** — Log imutável (LGPD Art. 37)
- **ConsentimentoLgpd** — Consentimento do titular (Art. 8º)

### Core Tríade (8)
- **PessoaFisica** — Cadastro completo (CPF/RG criptografados)
- **ProjetoTriade** — Agrupador família + células
- **ParticipanteProjeto** — Constituinte ou donatário
- **Celula** — Holding (Cofre/Destino/Veículo)
- **SocioCelula** — Quotas e participação
- **Bem** — Patrimonial (dados específicos em JSONB)
- **FaseProjeto** — Fases do Método Tríade
- **PassoFase** — Passos dentro de cada fase

### Documentos (4)
- **TemplateDocumento** — Modelo editável com placeholders
- **DocumentoGerado** — Instância concreta versionada
- **Clausula** — Banco de cláusulas reutilizáveis
- **ArquivoStorage** — Referência polimórfica S3/MinIO

### Financeiro (3)
- **ContratoHonorarios** — Contrato vinculado ao projeto
- **Parcela** — Parcelas com controle de pagamento
- **NotaFiscal** — NFS-e (SIMPLISS/ABRASF)

### Captação (1)
- **Captacao** — Wizard de viabilidade (12 perguntas)

## Multi-Tenancy

Estratégia: **Column Discriminator + Global Query Filters**

- Toda entidade que implementa `ITenantEntity` recebe `TenantId`
- O `AppDbContext` aplica query filter automático via `OnModelCreating`
- O `SaveChanges` interceptor injeta `TenantId` em novos registros
- O `ITenantProvider` lê o tenant do JWT claim no request

## LGPD Compliance

| Requisito | Implementação |
|-----------|---------------|
| Art. 7º — Base legal | Enum `BaseLegalLgpd` + entidade `ConsentimentoLgpd` |
| Art. 8º — Consentimento | Hash SHA-256 + IP + timestamp de aceite |
| Art. 18 — Direitos titular | Soft delete + anonimização via endpoint |
| Art. 37 — Registro operações | `AuditLog` append-only (bigint serial) |
| Art. 46 — Segurança | AES-256 via Data Protection nos campos sensíveis |

Campos criptografados: CPF, RG, Data Nascimento, Filiação, Celular, E-mail.

## Como Executar

```bash
# 1. Clonar e subir infraestrutura
git clone <repo>
cd HoldusSaaS
docker-compose up -d postgres redis minio

# 2. Rodar migrations
cd src/Holdus.API
dotnet ef database update --project ../Holdus.Infrastructure

# 3. Rodar API
dotnet run

# Ou tudo via Docker
docker-compose up --build
```

## Próximos Passos

- [ ] Program.cs com DI completo (Identity, JWT, Hangfire, Redis, MinIO)
- [ ] TenantMiddleware que extrai TenantId do JWT
- [ ] Primeiro controller: AuthController (login, register, refresh)
- [ ] Seed de dados (tenant demo, templates padrão, 31 configurações)
- [ ] Pipeline CI/CD (GitHub Actions)
- [ ] Frontend React com TipTap editor

---

**Thiago Seixas Advocacia Empresarial**
*Holdus SaaS v2.0.0*
