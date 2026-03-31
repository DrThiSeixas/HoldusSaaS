# Holdus — Mapeamento da Minuta 3.0
## Alteração Contratual — Integralização com Participações Societárias na Célula Cofre

---

## 1. Visão Geral

Esta operação gera **até 3 atos simultâneos**:

| Ato | Documento | Obrigatório |
|-----|-----------|-------------|
| Ato 1 | Alteração contratual da **investida** (reflete saída do sócio e ingresso do Cofre) | Sim (se Ltda) |
| Ato 2 | Alteração contratual do **Cofre** (aumento de capital com participações) | Sim |
| Ato 3 | Averbação nos livros societários da **S.A.** | Sim (se ações) |

O sistema deve gerar Ato 1 + Ato 2 como par inseparável quando a investida for Ltda.

---

## 2. Variáveis de Diagnóstico (12 campos + 4 confirmações)

| # | Variável | Tipo | Fonte |
|---|----------|------|-------|
| 1 | `cofre.*` | object | Dados da Célula Cofre (CNPJ, NIRE, sede, capital) |
| 2 | `investida.nomeEmpresarial` | string | Input |
| 3 | `investida.cnpj` | string | Input |
| 4 | `investida.nire` | string | Input (se Ltda) |
| 5 | `investida.juntaUf` | string | Input |
| 6 | `investida.sede` | string | Input |
| 7 | `conferente.*` | object | PessoaFisica (qualificação completa) |
| 8 | `tipoAtivo` | enum | Quotas / Ações |
| 9 | `quantidade` | int | Nº de quotas ou ações |
| 10 | `percentualInvestida` | decimal | % do capital da investida |
| 11 | `valorAtribuido` | decimal | Valor para integralização |
| 12 | `usoTotalOuParcial` | enum | Total / Parcial |
| **C1** | `capitalInvestidaIntegralizado` | bool | Confirmação expressa |
| **C2** | `cofreSemAtividadeOperacional` | bool | Confirmação expressa |
| **C3** | `semRestricoesTransferencia` | bool | Confirmação expressa |
| **C4** | `mesmaUf` | bool | Derivado (cofre.uf == investida.uf) |

### Campos extras para Ações (S.A.)

| Campo | Descrição |
|-------|-----------|
| `acoes.especie` | Ordinária / Preferencial |
| `acoes.classe` | Classe A, B, etc. (se houver) |
| `acoes.forma` | Nominativa / Escritural |
| `acoes.valorNominal` | Se houver |

### Campos extras para uso parcial

| Campo | Descrição |
|-------|-----------|
| `conferente.quotasAntes` | Quotas do conferente na investida antes |
| `conferente.quotasDepois` | Quotas restantes após o aporte |

---

## 3. Blocos Condicionais (4 blocos)

### BC-P1 — Tipo de ativo: Quotas vs Ações
**Condição:** `tipoAtivo`

- **Quotas (Ltda):** gera Ato 1 (investida) + Ato 2 (Cofre)
- **Ações (S.A.):** gera Ato 2 (Cofre) com cláusula adaptada + alerta de averbação nos livros

### BC-P2 — Uso total vs parcial da participação
**Condição:** `usoTotalOuParcial`

- **Total:** conferente sai integralmente do quadro da investida, Cofre assume 100% das quotas cedidas
- **Parcial:** conferente reduz participação, Cofre ingressa com a parcela cedida

### BC-P3 — Mesma UF (tramitação conjunta)
**Condição:** `cofre.uf == investida.uf`

- Se mesma UF → alerta de tramitação conjunta obrigatória na Junta

### BC-P4 — Reforço narrativo do Cofre
**Condição:** `cofrePuro == true`

- Injeta parágrafo reafirmando a natureza de holding de participações

---

## 4. Regras de Validação — 4 Bloqueios + 3 Alertas

| # | Regra | Severidade | Fundamento |
|---|-------|-----------|------------|
| B1 | Capital da investida não totalmente integralizado | **BLOQUEIO** | DREI |
| B2 | Ausência do ato reflexo (investida é Ltda e não gerou Ato 1) | **BLOQUEIO** | DREI |
| B3 | Desvio funcional do Cofre (atividade operacional + cofre puro) | **BLOQUEIO** | CNAE 6462-0/00 |
| B4 | Dados incompletos da investida (sem CNPJ ou sem NIRE) | **BLOQUEIO** | DREI |
| A1 | Mesma UF — tramitação conjunta obrigatória | **ALERTA** | DREI |
| A2 | Restrições contratuais à cessão na investida | **ALERTA** | Contrato da investida |
| A3 | Percentuais pós-aumento não somam 100% | **BLOQUEIO** | Aritmético |

---

## 5. Documentos Gerados

### Ato 1 — Alteração da Investida (se Ltda)

```
Título: Alteração Contratual da [Investida] LTDA.
├── Preâmbulo (dados da investida)
├── Cláusula única:
│   ├── Se uso TOTAL:
│   │   ├── Retira-se [conferente] do quadro
│   │   └── Ingressa [Cofre] LTDA. como titular das quotas
│   └── Se uso PARCIAL:
│       ├── Reduz-se participação de [conferente] de X para Y quotas
│       └── Ingressa [Cofre] LTDA. com Z quotas
├── Nova redação da cláusula do capital (quadro pós-alteração)
├── Ratificação
└── Fecho + assinaturas
```

### Ato 2 — Alteração do Cofre

```
Título: Alteração Contratual da [Cofre] LTDA.
├── Preâmbulo (dados do Cofre)
├── Cláusula 1 — Aumento de capital
├── Cláusula 2 — Integralização com participações
│   ├── Qualificação do conferente
│   ├── BC-P1: quotas ou ações (cláusula adaptada)
│   ├── §1 — valor integralmente destinado ao capital
│   ├── §2 — livre de ônus
│   ├── §3 — capital totalmente integralizado
│   └── §4 — ato reflexo na investida
├── Cláusula 3 — Nova redação do capital
├── Cláusula 4 — Manutenção da natureza do Cofre
├── Cláusula 5 — Ratificação
└── Fecho + assinaturas
```

### Ato 3 — Averbação S.A. (checklist apenas)

```
Checklist:
├── Averbação no Livro de Registro de Ações Nominativas
├── Averbação no Livro de Transferência de Ações Nominativas
├── Espécie, classe, forma e valor nominal das ações
└── Comunicação à administração da companhia
```

---

## 6. Fluxo do Motor

```
Entrada: Cofre + Investida + Conferente + Tipo de ativo

Etapa A — Diagnóstico
    ├── Tipo: quotas ou ações?
    ├── Uso total ou parcial?
    ├── Mesma UF?
    ├── Capital integralizado?
    └── Confirmações do advogado (C1..C4)

Etapa B — Validação
    ├── B1..B4 bloqueios
    ├── A1..A3 alertas
    └── BLOQUEIO → não gera | OK → segue

Etapa C — Montagem
    ├── Se quotas de Ltda:
    │   ├── Gera Ato 1 (investida) com BC-P2 (total/parcial)
    │   └── Gera Ato 2 (Cofre) com BC-P1 (quotas)
    ├── Se ações de S.A.:
    │   ├── Gera Ato 2 (Cofre) com BC-P1 (ações) — cláusula adaptada
    │   └── Gera checklist Ato 3 (averbação livros)
    └── Gera checklist registral
```
