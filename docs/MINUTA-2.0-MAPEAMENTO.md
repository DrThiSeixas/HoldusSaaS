# Holdus — Mapeamento da Minuta 2.0
## Alteração Contratual Master — Integralização de Imóvel na Célula Cofre

---

## 1. Variáveis de Diagnóstico (9 campos obrigatórios + 2 confirmações)

| # | Variável | Tipo | Entidade Holdus |
|---|----------|------|-----------------|
| 1 | `empresa.nomeEmpresarial` | string | `Celula.RazaoSocial` ou `NomeCelula` |
| 2 | `empresa.cnpj` | string | `Celula.Cnpj` |
| 3 | `empresa.nire` | string | `Celula.Nire` |
| 4 | `empresa.juntaComercial` | string | Derivado de `Celula.EnderecoJson.uf` |
| 5 | `empresa.endereco` | object | `Celula.EnderecoJson` |
| 6 | `capitalAtual` | decimal | `Celula.CapitalSocialEfetivo` |
| 7 | `capitalNovo` | decimal | Calculado: capitalAtual + valorAumento |
| 8 | `valorAumento` | decimal | Input do usuário |
| 9 | `socioConferente` | ref | `SocioCelula` → `PessoaFisica` |
| 10 | `imovel` | object | `Bem` com `CelulaDestinoId` = esta célula |
| 11 | `tipoImovel` | enum | Urbano / Rural |
| **C1** | `haParcelaForaCapital` | bool | Confirmação expressa do advogado |
| **C2** | `sociedadeSemAtividadeOperacional` | bool | Confirmação expressa do advogado |

---

## 2. Placeholders

### 2.1 Sociedade

| Placeholder | Fonte |
|-------------|-------|
| `{{empresa.nomeEmpresarial}}` | `Celula.RazaoSocial` |
| `{{empresa.cnpj}}` | `Celula.Cnpj` |
| `{{empresa.nire}}` | `Celula.Nire` |
| `{{empresa.juntaUf}}` | Derivado: "São Paulo", "Minas Gerais"... |
| `{{empresa.endereco}}` | `Celula.EnderecoJson` (completo) |
| `{{empresa.capitalAtual}}` | `Celula.CapitalSocialEfetivo` |
| `{{empresa.capitalAtualExtenso}}` | Calculado |
| `{{empresa.capitalNovo}}` | capitalAtual + valorAumento |
| `{{empresa.capitalNovoExtenso}}` | Calculado |
| `{{empresa.clausulaCapitalNum}}` | Número da cláusula do capital no CS vigente |

### 2.2 Sócio Conferente

| Placeholder | Fonte |
|-------------|-------|
| `{{conferente.nome}}` | `PessoaFisica.Nome` |
| `{{conferente.qualificacao}}` | Montado (nacionalidade, EC, profissão, RG, CPF, endereço) |
| `{{conferente.valorIntegralizacao}}` | Input |
| `{{conferente.valorExtenso}}` | Calculado |

### 2.3 Imóvel

| Placeholder | Fonte | Obrigatório |
|-------------|-------|-------------|
| `{{imovel.descricao}}` | `Bem.Descricao` | Sim |
| `{{imovel.matricula}}` | `Bem.DadosEspecificosJson.matricula` | Sim |
| `{{imovel.cartorio}}` | `Bem.DadosEspecificosJson.cartorio` | Sim |
| `{{imovel.endereco}}` | `Bem.DadosEspecificosJson.endereco` | Sim |
| `{{imovel.area}}` | `Bem.DadosEspecificosJson.area` | Sim |
| `{{imovel.cadastroMunicipal}}` | `Bem.DadosEspecificosJson.inscricaoMunicipal` | Urbano |
| `{{imovel.tituloAquisitivo}}` | `Bem.DadosEspecificosJson.tituloAquisitivo` | Sim |
| `{{imovel.denominacao}}` | `Bem.DadosEspecificosJson.denominacao` | Rural |
| `{{imovel.ccir}}` | `Bem.DadosEspecificosJson.ccir` | Rural |
| `{{imovel.nirf}}` | `Bem.DadosEspecificosJson.nirf` | Rural |
| `{{imovel.valor}}` | `Bem.ValorMercado` ou declarado | Sim |
| `{{imovel.valorExtenso}}` | Calculado | Sim |

### 2.4 Quadro Societário Pós-Alteração (loop)

| Placeholder | Fonte |
|-------------|-------|
| `{{socio.nome}}` | `PessoaFisica.Nome` |
| `{{socio.quotas}}` | `SocioCelula.QuantidadeQuotas` (pós-aumento) |
| `{{socio.valorTotal}}` | quotas × valorPorQuota |
| `{{socio.percentual}}` | Recalculado sobre novo total |

---

## 3. Blocos Condicionais (3 blocos)

### BC-A1 — Imóvel Urbano vs Rural
**Condição:** `tipoImovel`

- **Urbano:** campos matrícula, cartório, endereço, área, cadastro municipal
- **Rural:** campos matrícula, cartório, denominação, área, município/UF, CCIR, NIRF/CAFIR

### BC-A2 — Cláusula de Reforço Narrativo
**Condição:** `cofre_puro == true && sociedadeSemAtividadeOperacional == true`

Injeta parágrafo adicional:
> "Os sócios declaram que a presente operação de aumento de capital insere-se na organização patrimonial da família, mantendo-se a sociedade como célula patrimonial-societária de preservação, sem desvio para atividade operacional própria."

### BC-A3 — Anuência Conjugal
**Condição:** `conferente.estadoCivil == Casado || conferente.uniaoEstavel` + integralização com imóvel

Injeta exigência de anuência ou documento apartado.

---

## 4. Regras de Validação — 4 Bloqueios Duros + 3 Alertas

| # | Regra | Severidade | Fundamento |
|---|-------|-----------|------------|
| B1 | Excedente fora do capital: `valor_bem != valor_aumento` | **BLOQUEIO** | Tema 796 STF |
| B2 | Desvio funcional: célula com atividade operacional + cofre puro | **BLOQUEIO** | CTN + Tema 1348 |
| B3 | Objeto social incompatível: contém atividade operacional estranha | **BLOQUEIO** | DREI |
| B4 | Descrição incompleta do imóvel: sem matrícula/cartório/localização | **BLOQUEIO** | DREI |
| A1 | Anuência conjugal necessária (art. 1.647 CC) | **ALERTA** | CC/2002 |
| A2 | Imóvel rural sem CCIR ou NIRF | **ALERTA** | Lei 4.947/66 |
| A3 | Percentuais pós-aumento não somam 100% | **BLOQUEIO** | Aritmético |

---

## 5. Fluxo do Motor

```
Entrada: Celula (já constituída) + Bem (a integralizar) + SocioConferente

Etapa A — Diagnóstico
    ├── Celula tem CNPJ e NIRE? (já constituída)
    ├── Tipo do imóvel? (urbano/rural)
    ├── Valor do bem vs valor do aumento
    ├── Conferente: estado civil, regime de bens
    ├── Cofre puro? Atividade operacional?
    └── Confirmações do advogado (C1, C2)

Etapa B — Validação
    ├── B1: valor_bem == valor_aumento? (Tema 796)
    ├── B2: desvio funcional?
    ├── B3: objeto social compatível?
    ├── B4: descrição completa?
    ├── A1..A3: alertas
    └── BLOQUEIO → não gera | OK → segue

Etapa C — Montagem
    ├── Monta preâmbulo com dados da sociedade
    ├── Cl.1: deliberação de aumento
    ├── Cl.2: integralização (BC-A1: urbano/rural)
    ├── Cl.3: nova redação do capital (quadro pós-aumento)
    ├── Cl.4: manutenção da natureza do Cofre
    │   └── BC-A2: reforço narrativo (opcional)
    ├── Cl.5: ratificação
    ├── Fecho + assinaturas
    └── Gera checklist registral
```
