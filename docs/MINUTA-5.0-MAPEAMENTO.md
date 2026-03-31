# Holdus — Mapeamento da Minuta 5.0
## Instrumento de Doação de Quotas — Célula Destino

**Código:** `DESTINO.DOACAO_QUOTAS.V1`

---

## 1. Tese

> O contrato social da Destino prepara a célula.
> Mas é este instrumento que começa a transferência patrimonial por quotas.

---

## 2. Variáveis de Diagnóstico (8 campos + 4 bloqueios)

| # | Variável | Tipo | Valores |
|---|----------|------|---------|
| 1 | `doadores[]` | array | Quem doa (ascendente, cônjuge, outro) |
| 2 | `donatarios[]` | array | Quem recebe (descendente, cônjuge, outro) |
| 3 | `naturezaDoacao` | enum | AdiantamentoLegitima / ParteDisponivel |
| 4 | `reservaUsufruto` | bool | Sim / Não |
| 5 | `direitosUsufruto` | enum | EconomicosPoliticos / EconomicosComVotoNuProp / Compartilhado |
| 6 | `clausulasRestritivas` | flags | Incomunicabilidade / Impenhorabilidade / Inalienabilidade / Reversão |
| 7 | `acordoQuotistasVinculado` | bool | Sim / Não |
| 8 | `alteracaoContratualCorrelata` | bool | Obrigatória |

### Bloqueios

| # | Regra | Severidade |
|---|-------|-----------|
| B1 | Parte disponível sem declaração expressa e validação de limite | **BLOQUEIO** |
| B2 | Doação compromete subsistência do doador | **BLOQUEIO** |
| B3 | Usufruto sem disciplina de voto e direitos econômicos | **BLOQUEIO** |
| B4 | Sem alteração contratual correlata da Destino | **BLOQUEIO** |

---

## 3. Blocos Condicionais (5 blocos)

### BC-DOA1 — Natureza da liberalidade
**Condição:** `naturezaDoacao`
- **Opção A (padrão):** adiantamento de legítima
- **Opção B:** parte disponível com dispensa de colação

### BC-DOA2 — Reserva de usufruto
**Condição:** `reservaUsufruto == true`
Injeta Cláusula 4 completa com disciplina de voto obrigatória.

### BC-DOA3 — Direitos do usufrutuário
**Condição:** `reservaUsufruto == true` → escolha obrigatória
- Econômicos + políticos (voto)
- Econômicos + voto do nu-proprietário
- Regime compartilhado (acordo específico)

### BC-DOA4 — Cláusulas restritivas
**Condição:** qualquer flag ativa em `clausulasRestritivas`
Injeta Cláusula 5 com os gravames selecionados.

### BC-DOA5 — Acordo de quotistas vinculado
**Condição:** `acordoQuotistasVinculado == true`
Reforça adesão na Cláusula 6.

---

## 4. Cláusulas (9 total)

| # | Cláusula | Condicional? |
|---|----------|-------------|
| 1 | Sociedade e quotas objeto da doação | Fixo |
| 2 | Natureza da liberalidade | BC-DOA1 |
| 3 | Aceitação | Fixo |
| 4 | Reserva de usufruto | BC-DOA2 + BC-DOA3 |
| 5 | Restrições sobre as quotas | BC-DOA4 |
| 6 | Governança e submissão | BC-DOA5 |
| 7 | Eficácia societária | Fixo |
| 8 | Declarações do doador | Fixo |
| 9 | Disposições finais / foro | Fixo |

---

## 5. Documentos vinculados obrigatórios

- Alteração contratual da Destino (reflete novo quadro societário)
- Checklist ITCMD
- Acordo de quotistas (se existente)
