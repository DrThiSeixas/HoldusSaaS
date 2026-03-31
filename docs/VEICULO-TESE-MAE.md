# Holdus — Mapeamento da Célula Veículo
## Tese-mãe + Pilares Jurídicos + Arquitetura de Domínio

---

## 1. Tese-mãe

> A célula Veículo é a sociedade limitada de comando e reorganização de
> controle, estruturada para concentrar a titularidade societária da célula
> Cofre e permitir a separação técnica entre direitos econômicos e direitos
> políticos, mediante classes de quotas compatíveis com a limitada de
> regência supletiva pela Lei das S.A., preservando o núcleo decisório com
> os instituidores da estrutura e deslocando a titularidade econômica
> ordinária para a célula Destino.

---

## 2. O que a Veículo É vs NÃO É

| É | NÃO É |
|---|-------|
| Célula de comando | Célula sucessória principal |
| Ponte societária Cofre ↔ Destino | Preservação patrimonial pura |
| Separação voto × titularidade econômica | Operação imobiliária |
| Organização do controle | Carta branca para importar S.A. |

---

## 3. Função sistêmica no Método Tríade

```
Destino → organiza a transferência patrimonial
Cofre   → concentra e preserva o patrimônio
Veículo → segura o comando
```

---

## 4. Cinco pilares jurídicos

### Pilar 1 — Regência supletiva da Lei das S.A.
- Art. 1.053, parágrafo único, CC
- DREI: aceita expressa ou presumida pela adoção de institutos de S.A. compatíveis

### Pilar 2 — Classes de quotas
- Ordinárias: espelham distribuição da Destino
- Preferenciais: R$ 1.000, peso de voto X+1, vinculadas aos donos do Cofre
- DREI: admite quotas preferenciais como instituto compatível com a limitada

### Pilar 3 — Reserva de capital
- Excedente sobre valor nominal → reserva de capital
- Art. 13, §2º, Lei 6.404/76
- Só coerente com regência supletiva expressa

### Pilar 4 — Cláusula de call da quota preferencial
- Ativada por falecimento
- Herdeiros como compradores
- Continuidade do controle

### Pilar 5 — Atos reflexos
- Alteração na Cofre: quotas passam à Veículo
- Alteração na Veículo: Destino compra ordinárias

---

## 5. Fluxo operacional (do fluxograma)

```
1. Veículo constituída em nome dos detentores do patrimônio
2. Capital = valor da Destino + R$ 1.000
3. Quotas ordinárias espelhando Destino
4. Quota preferencial R$ 1.000 → donos do Cofre → voto X+1
5. Sócios integralizam valor correspondente ao capital do Cofre
6. Excedente → reserva de capital
7. Cláusula de call da preferencial (falecimento)
8. Cofre alterada → quotas passam à Veículo
9. Destino compra ordinárias da Veículo pelo valor nominal
10. Controle preservado com instituidores via preferenciais
```

---

## 6. Regra-mãe do sistema

> A célula Veículo só deve entrar no sistema quando a tese estiver
> absolutamente limpa. Nada de sair direto para minuta sem antes
> fechar as regras.

---

## 7. Travas específicas da Veículo

| # | Trava | Fundamento |
|---|-------|-----------|
| T1 | Sem regência supletiva expressa no contrato social → bloqueio | Art. 1.053, §único, CC |
| T2 | Quotas preferenciais sem direitos bem definidos → bloqueio | DREI |
| T3 | Reserva de capital sem base contratual → bloqueio | Art. 13, §2º, Lei 6.404 |
| T4 | Cláusula de call sem evento-gatilho definido → bloqueio | Contratual |
| T5 | Sem ato reflexo na Cofre → operação incompleta | DREI |
| T6 | Destino comprando ordinárias sem contrato de compra → incompleto | Contratual |

---

## 8. Status de implementação

- ○ VEICULO.CONSTITUICAO.V1 — Contrato Social Master (próximo)
- ○ VEICULO.MODULO_OPERACIONAL.V1 — Módulo de comando/controle

**Nota:** a Veículo é a última célula a ser implementada por ser a mais sensível.
Cofre e Destino precisam estar sólidos antes.
