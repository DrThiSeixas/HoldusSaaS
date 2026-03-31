# Holdus — Deploy no Railway

## Passo a passo para colocar o sistema online

### 1. Criar contas

1. **GitHub** — https://github.com (se não tem)
2. **Railway** — https://railway.com (login com GitHub)

### 2. Subir código no GitHub

Crie **2 repositórios** no GitHub:

```
github.com/SEU_USUARIO/holdus-backend
github.com/SEU_USUARIO/holdus-frontend
```

No terminal (PowerShell), dentro de cada pasta:

**Backend:**
```powershell
cd C:\holdus\HoldusSaaS
git init
git add .
git commit -m "Holdus Backend v1.0"
git remote add origin https://github.com/SEU_USUARIO/holdus-backend.git
git branch -M main
git push -u origin main
```

**Frontend:**
```powershell
cd C:\holdus\holdus-frontend
git init
git add .
git commit -m "Holdus Frontend v1.0"
git remote add origin https://github.com/SEU_USUARIO/holdus-frontend.git
git branch -M main
git push -u origin main
```

### 3. Criar projeto no Railway

1. Acesse https://railway.com/dashboard
2. Clique **"New Project"**
3. Escolha **"Empty Project"**
4. Renomeie o projeto para **"Holdus"**

### 4. Adicionar PostgreSQL

1. No projeto, clique **"+ New"** → **"Database"** → **"PostgreSQL"**
2. Railway cria o banco automaticamente
3. Clique no PostgreSQL → **"Variables"** → copie o `DATABASE_URL`

### 5. Deploy do Backend

1. No projeto, clique **"+ New"** → **"GitHub Repo"**
2. Selecione `holdus-backend`
3. Railway detecta o Dockerfile e faz build automaticamente
4. Vá em **"Variables"** e adicione:

```
DATABASE_URL       → (já é injetado automaticamente pelo PostgreSQL)
JWT_SECRET         → HoldusTriadeCapital2026SecretKeyMinimo32Caracteres!
JWT_ISSUER         → holdus.up.railway.app
JWT_AUDIENCE       → holdus-frontend
ASPNETCORE_ENVIRONMENT → Production
FRONTEND_URL       → (será preenchido depois, quando o frontend tiver URL)
```

5. Vá em **"Settings"** → **"Networking"** → **"Generate Domain"**
6. Anote a URL gerada (ex: `holdus-backend-production.up.railway.app`)

### 6. Deploy do Frontend

1. No projeto, clique **"+ New"** → **"GitHub Repo"**
2. Selecione `holdus-frontend`
3. Vá em **"Variables"** e adicione:

```
BACKEND_URL              → https://holdus-backend-production.up.railway.app
NEXT_PUBLIC_API_URL      → (deixe vazio — o proxy faz o roteamento)
```

4. Vá em **"Settings"** → **"Networking"** → **"Generate Domain"**
5. Anote a URL gerada (ex: `holdus-frontend-production.up.railway.app`)

### 7. Conectar CORS

1. Volte no serviço do **Backend** → **"Variables"**
2. Adicione/atualize:

```
FRONTEND_URL → https://holdus-frontend-production.up.railway.app
```

3. O backend vai redesploiar automaticamente

### 8. Testar

1. Acesse `https://holdus-backend-production.up.railway.app/swagger` → deve aparecer o Swagger
2. Acesse `https://holdus-frontend-production.up.railway.app` → deve aparecer o login
3. No Swagger, faça o **POST /api/auth/register** para criar o primeiro usuário
4. Logue no frontend com o e-mail e senha que cadastrou

### 9. Domínio customizado (opcional)

Quando quiser usar `holdus.com.br`:

1. Registre o domínio no Registro.br (~R$40/ano)
2. No Railway → serviço frontend → Settings → Custom Domain
3. Adicione `app.holdus.com.br`
4. Configure o DNS no Registro.br com o CNAME que o Railway fornecer

---

## Variáveis de ambiente — Resumo

### Backend
| Variável | Valor | Origem |
|----------|-------|--------|
| `DATABASE_URL` | postgresql://... | Auto (Railway PostgreSQL) |
| `JWT_SECRET` | string com 32+ chars | Você define |
| `JWT_ISSUER` | URL do backend | Você define |
| `JWT_AUDIENCE` | holdus-frontend | Você define |
| `FRONTEND_URL` | URL do frontend | Você define |
| `ASPNETCORE_ENVIRONMENT` | Production | Você define |

### Frontend
| Variável | Valor | Origem |
|----------|-------|--------|
| `BACKEND_URL` | URL do backend | Você define |

---

## Custos estimados

| Serviço | RAM | Custo/mês |
|---------|-----|-----------|
| PostgreSQL | ~100MB | ~$1-2 |
| Backend .NET | ~200MB | ~$3-4 |
| Frontend Next.js | ~150MB | ~$2-3 |
| Plano Hobby | — | $5 |
| **Total** | | **~$12-15 (~R$70)** |

---

## Próximas evoluções

- **Redis** → Adicionar como serviço no Railway quando precisar de cache
- **MinIO/S3** → Para armazenamento de documentos gerados (.docx)
- **Domínio próprio** → holdus.com.br
- **CI/CD** → Push no GitHub → deploy automático no Railway (já funciona)
