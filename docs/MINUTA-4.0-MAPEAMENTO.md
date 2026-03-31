# Holdus — Mapeamento da Minuta 4.0
## Contrato Social Master — Célula Destino

---

## 1. Tese-mãe

> A célula Destino é a sociedade vocacionada à organização da transmissão
> intergeracional do patrimônio familiar por meio de quotas, servindo como
> plataforma societária de doação, acomodação dos sucessores e implementação
> das regras de governança familiar e societária.

---

## 2. O que entra no contrato social

- Denominação, sede, prazo e capital
- Objeto social com vocação sucessória
- Administração com restrições específicas
- Deliberações com referência a acordo de quotistas
- Cessão/transmissão de quotas (doação, sucessão, liberalidade)
- Cláusula de vocação sucessória (finalidade estrutural)
- Referência a instrumentos complementares

## 3. O que fica FORA

- Doação em si → instrumento próprio
- Reserva de usufruto → instrumento próprio
- Cláusulas de colação/dispensa → instrumento próprio
- Incomunicabilidade, inalienabilidade, impenhorabilidade → instrumento próprio
- Regras de voto nu-proprietário vs usufrutuário → acordo de quotistas
- Acordo de quotistas completo → documento separado

---

## 4. Diferenças da Destino vs Cofre

| Aspecto | Cofre | Destino |
|---------|-------|---------|
| Função | Concentração patrimonial | Transmissão intergeracional |
| Objeto social | Holding pura (participações) | Plataforma sucessória |
| CNAE | 6462-0/00 (holding) | 6462-0/00 (pode ser o mesmo) |
| Trava funcional | Veda atividade operacional | Veda desvio da função sucessória |
| Blocos condicionais | 7 (regime bens, imóvel, rural, participações, trava, coerência) | 3 (unipessoal, regime bens, vocação sucessória) |
| Integralização | Imóvel, participações, dinheiro | Geralmente dinheiro (quotas recebem patrimônio via doação depois) |
| Tema 796 | Crítico (integraliza imóvel) | Geralmente não se aplica (integralização em dinheiro) |

---

## 5. Variáveis de Diagnóstico (8 campos)

| # | Variável | Tipo | Fonte |
|---|----------|------|-------|
| 1 | `nomeEmpresarial` | string | Input |
| 2 | `endereco` | object | Input |
| 3 | `comarca` | string | Input |
| 4 | `capitalSocial` | decimal | Input |
| 5 | `valorPorQuota` | decimal | Input (default 1.00) |
| 6 | `socios[]` | array | Qualificação completa |
| 7 | `administrador` | ref | Índice do sócio admin |
| 8 | `tipoSociedade` | enum | Pluripessoal / Unipessoal |

---

## 6. Blocos Condicionais (3 blocos)

### BC-D1 — Unipessoal
**Condição:** `qtd_socios == 1`
Adapta preâmbulo, cláusulas de deliberação e assinaturas.

### BC-D2 — Regime de bens (qualificação)
**Condição:** `socio.estadoCivil == Casado || socio.uniaoEstavel`
Inclui regime de bens na qualificação. Mesma lógica do BC2 do Cofre.

### BC-D3 — Referência a instrumentos complementares
**Condição:** sempre ativo (a Destino por natureza prevê doação/usufruto/acordo futuros)
Cláusula 4 (finalidade estrutural) e Cláusula 10 (instrumentos complementares) sempre presentes.

---

## 7. Validações (4 regras)

| # | Regra | Severidade |
|---|-------|-----------|
| V1 | Art. 977 CC — cônjuges como únicos sócios em regime vedado | **BLOQUEIO** |
| V2 | Capital social <= 0 | **BLOQUEIO** |
| V3 | Sócio sem CPF | **BLOQUEIO** |
| V4 | Percentuais != 100% | **BLOQUEIO** |

---

## 8. Cláusulas (13 total)

| # | Cláusula | Conteúdo |
|---|----------|----------|
| 1 | Denominação, tipo, sede e prazo | Padrão LTDA + prazo indeterminado |
| 2 | Objeto social | 4 incisos + vocação sucessória + não é célula operacional |
| 3 | Capital social | Distribuição por sócio + integralização em dinheiro |
| 4 | Finalidade estrutural da célula Destino | Vocação de transferência patrimonial |
| 5 | Responsabilidade dos sócios | Art. 1.052 CC |
| 6 | Administração | Com restrições (não desvirtuar função sucessória) |
| 7 | Deliberações sociais | Com referência a acordo de quotistas |
| 8 | Cessão, transmissão e reorganização de quotas | Doação, sucessão, liberalidade |
| 9 | Falecimento, incapacidade e reflexos familiares | Não dissolve + instrumento próprio |
| 10 | Acordo de quotistas e instrumentos complementares | Doação, usufruto, protocolo familiar |
| 11 | Exercício social e resultados | 31/dez + instrumentos complementares |
| 12 | Dissolução e liquidação | Preservar coerência sucessória |
| 13 | Foro | Comarca + cláusula arbitral eventual |

---

## 9. Código do módulo

`DESTINO.CONSTITUICAO.V1`
