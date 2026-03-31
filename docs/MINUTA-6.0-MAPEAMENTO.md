# Holdus — Mapeamento da Minuta 6.0
## Alteração Contratual da Destino — Reflexo da Doação de Quotas

**Código:** `DESTINO.ALT_DOACAO.V1`

---

## 1. Relação com a Minuta 5.0

| Minuta 5.0 | Minuta 6.0 |
|------------|------------|
| Instrumento de Doação (civil) | Alteração Contratual (societário) |
| Formaliza a liberalidade | Reflete a nova titularidade no quadro |
| Gera obrigação de ato societário | Cumpre essa obrigação |
| DESTINO.DOACAO_QUOTAS.V1 | DESTINO.ALT_DOACAO.V1 |

**Modelo preferencial (diretriz dura do sistema):**
1. Instrumento de doação (Minuta 5.0)
2. Alteração contratual da Destino (Minuta 6.0) ← esta
3. ITCMD
4. Acordo de quotistas + usufruto (quando houver)

---

## 2. Variáveis de Diagnóstico

| # | Variável | Tipo |
|---|----------|------|
| 1 | `sociedade.*` | Dados da Destino (CNPJ, NIRE, sede) |
| 2 | `doadores[]` | Quem doou + qtd quotas cedidas |
| 3 | `donatarios[]` | Quem recebeu + qtd quotas recebidas |
| 4 | `naturezaDoacao` | AdiantamentoLegitima / ParteDisponivel |
| 5 | `reservaUsufruto` | bool |
| 6 | `clausulasRestritivas` | flags |
| 7 | `acordoQuotistasVinculado` | bool |
| 8 | `quadroPosAlteracao[]` | Novo quadro societário completo |
| 9 | `clausulaCapitalNumero` | int |
| 10 | `instrumentoDoacaoVinculado` | bool — obrigatório |

---

## 3. Blocos Condicionais (4 blocos)

| Bloco | Condição | Cláusula |
|-------|----------|----------|
| BC-AD1 | Natureza da liberalidade | Cl.2 — adiantamento vs parte disponível |
| BC-AD2 | Reserva de usufruto | Cl.5 — disciplina voto/frutos |
| BC-AD3 | Cláusulas restritivas | Cl.6 — gravames sobre quotas |
| BC-AD4 | Acordo vinculado | Cl.4 — adesão reforçada |

---

## 4. Bloqueios (4 travas)

| # | Trava | Severidade |
|---|-------|-----------|
| T1 | Sem instrumento de doação vinculado | **BLOQUEIO** |
| T2 | Usufruto sem disciplina de voto/frutos | **BLOQUEIO** |
| T3 | Parte disponível sem declaração expressa | **BLOQUEIO** |
| T4 | Donatários não declararam adesão ao CS e acordo | **BLOQUEIO** |

---

## 5. Cláusulas (8 total)

| # | Cláusula | Condicional? |
|---|----------|-------------|
| 1 | Doação e reorganização da titularidade | Fixo |
| 2 | Natureza sucessória | BC-AD1 |
| 3 | Nova redação da cláusula do capital | Fixo |
| 4 | Adesão dos novos quotistas | BC-AD4 |
| 5 | Usufruto sobre as quotas | BC-AD2 |
| 6 | Restrições sobre as quotas | BC-AD3 |
| 7 | Manutenção da finalidade da Destino | Fixo |
| 8 | Ratificação | Fixo |
