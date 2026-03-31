# Holdus — Mapeamento da Minuta 1.0
## Contrato Social Master — Célula Cofre

---

## 1. Variáveis-Mãe (15 variáveis de diagnóstico)

| # | Variável | Tipo | Valores | Entidade Holdus |
|---|----------|------|---------|-----------------|
| 1 | `tipo_sociedade` | enum | `Pluripessoal` / `Unipessoal` | Derivado de `qtd_socios` |
| 2 | `qtd_socios` | int | 1..N | `Celula.Socios.Count` |
| 3 | `estado_civil_socio` | enum[] | Solteiro/Casado/Divorciado/Viuvo/UniaoEstavel/Separado | `PessoaFisica.EstadoCivil` |
| 4 | `uniao_estavel` | bool[] | por sócio | `PessoaFisica.EstadoCivil == UniaoEstavel` |
| 5 | `regime_bens` | enum[] | Comunhão Total/Parcial/Separação Total/Obrigatória/Participação | `PessoaFisica.RegimeBens` |
| 6 | `ha_pacto_ou_contrato_convivencia` | bool[] | por sócio | Campo novo na PF ou em `DadosFamiliaJson` |
| 7 | `integralizacao_tipo` | enum[] | Dinheiro/ImovelUrbano/ImovelRural/Participacoes/BemMovel | `Bem.Tipo` + campo extra |
| 8 | `integralizacao_imovel_urbano` | bool | global | Derivado: existe Bem com Tipo=Imovel + TipoImovel=Urbano |
| 9 | `integralizacao_imovel_rural` | bool | global | Derivado: existe Bem com Tipo=Imovel + TipoImovel=Rural |
| 10 | `integralizacao_participacoes` | bool | global | Derivado: existe Bem com Tipo=ParticipacaoSocietaria |
| 11 | `ha_descendentes` | bool | global | Campo do ProjetoTriade ou Captação |
| 12 | `ha_usufruto_planejado` | bool | global | Campo do ProjetoTriade |
| 13 | `cofre_puro` | bool | global (default: true) | `Celula.Tipo == Cofre` + sem atividade operacional |
| 14 | `atividade_operacional_propria` | bool | global (default: false) | Trava: se true, sai do fluxo Cofre |
| 15 | `ha_anuencia_conjuge_companheiro` | bool[] | por sócio | Derivado de regime_bens + tipo integralização |

---

## 2. Campos de preenchimento (Placeholders)

### 2.1 Empresa (Célula)

| Placeholder | Entidade.Campo | Tipo |
|-------------|----------------|------|
| `{{empresa.nomeEmpresarial}}` | `Celula.NomeCelula` | string |
| `{{empresa.endereco}}` | `Celula.EnderecoJson` (parsed) | object |
| `{{empresa.municipio}}` | `Celula.EnderecoJson.cidade` | string |
| `{{empresa.uf}}` | `Celula.EnderecoJson.uf` | string |
| `{{empresa.comarca}}` | `Celula.EnderecoJson.comarca` ou `.cidade` | string |
| `{{empresa.capitalSocial}}` | `Celula.CapitalSocialPrevisto` | decimal |
| `{{empresa.capitalSocialExtenso}}` | Calculado (NumberToWords) | string |
| `{{empresa.totalQuotas}}` | Sum(`SocioCelula.QuantidadeQuotas`) | int |
| `{{empresa.totalQuotasExtenso}}` | Calculado | string |
| `{{empresa.valorQuota}}` | `SocioCelula[0].ValorPorQuota` | decimal |

### 2.2 Sócios (loop {{#each socios}})

| Placeholder | Entidade.Campo | Tipo |
|-------------|----------------|------|
| `{{socio.nome}}` | `PessoaFisica.Nome` | string |
| `{{socio.nacionalidade}}` | `PessoaFisica.Nacionalidade` | string |
| `{{socio.estadoCivil}}` | `PessoaFisica.EstadoCivil` (formatado) | string |
| `{{socio.uniaoEstavel}}` | se EstadoCivil == UniaoEstavel | bool |
| `{{socio.regimeBens}}` | `PessoaFisica.RegimeBens` (formatado) | string |
| `{{socio.profissao}}` | `PessoaFisica.Profissao` | string |
| `{{socio.rg}}` | `PessoaFisica.Rg` (decrypt) | string |
| `{{socio.rgOrgao}}` | `PessoaFisica.RgOrgaoEmissor` | string |
| `{{socio.cpf}}` | `PessoaFisica.Cpf` (decrypt) | string |
| `{{socio.endereco}}` | Concatenado de campos PF | string |
| `{{socio.quotas}}` | `SocioCelula.QuantidadeQuotas` | int |
| `{{socio.quotasExtenso}}` | Calculado | string |
| `{{socio.valorTotal}}` | `SocioCelula.QuantidadeQuotas * ValorPorQuota` | decimal |
| `{{socio.percentual}}` | `SocioCelula.PercentualParticipacao` | decimal |

### 2.3 Administrador

| Placeholder | Entidade.Campo |
|-------------|----------------|
| `{{admin.nome}}` | `Celula.Administrador.Nome` |
| `{{admin.qualificacao}}` | Montado a partir da PF do admin |
| `{{admin.eSocio}}` | bool (está na lista de sócios?) |
| `{{admin.prazo}}` | "indeterminado" (default Cofre) |

### 2.4 Bens para Integralização (loop {{#each bensIntegralizacao}})

| Placeholder | Entidade.Campo |
|-------------|----------------|
| `{{bem.tipo}}` | `Bem.Tipo` |
| `{{bem.descricao}}` | `Bem.Descricao` |
| `{{bem.valor}}` | `Bem.ValorDeclaracaoIR` ou `ValorMercado` |
| `{{bem.matricula}}` | `Bem.DadosEspecificosJson.matricula` |
| `{{bem.cartorio}}` | `Bem.DadosEspecificosJson.cartorio` |
| `{{bem.endereco}}` | `Bem.DadosEspecificosJson.endereco` |
| `{{bem.area}}` | `Bem.DadosEspecificosJson.area` |
| `{{bem.cadastroMunicipal}}` | `Bem.DadosEspecificosJson.inscricaoMunicipal` |
| `{{bem.ccir}}` | `Bem.DadosEspecificosJson.ccir` (rural) |
| `{{bem.nirf}}` | `Bem.DadosEspecificosJson.nirf` (rural) |
| `{{bem.denominacao}}` | `Bem.DadosEspecificosJson.denominacao` (rural) |
| `{{bem.socioConferente}}` | `Bem.Proprietario.Nome` |

### 2.5 Quóruns (configuráveis)

| Placeholder | Default | Configurável |
|-------------|---------|-------------|
| `{{quorum.deliberacaoEspecial}}` | 75 | Sim (Configuracao) |
| `{{quorum.cessaoTerceiros}}` | 75 | Sim |
| `{{quorum.alteracaoContratual}}` | 75 | Sim |
| `{{quorum.administracao}}` | 75 | Sim |

### 2.6 Apuração de haveres

| Placeholder | Default |
|-------------|---------|
| `{{haveres.parcelas}}` | 12 |
| `{{haveres.indice}}` | "IPCA" |
| `{{haveres.prazoInicio}}` | "90 (noventa)" |

---

## 3. Blocos Condicionais (7 blocos)

### BC1 — LTDA Unipessoal
**Condição:** `qtd_socios == 1`

**Afeta:**
- Preâmbulo: substitui "as partes abaixo qualificadas" → "o único sócio abaixo qualificado"
- Remove "têm entre si justo e contratado" → "constitui, por este instrumento"
- Cláusula 3: singular (1 sócio, 100% quotas)
- Cláusula 5: adapta solidariedade (não se aplica se unipessoal)
- Cláusula 7: "deliberação do sócio único" em vez de "reunião de sócios"
- Cláusula 9: mantém para futuro ingresso de sócio
- Assinaturas: 1 bloco de assinatura

### BC2 — Casamento / União Estável / Regime de Bens
**Condição:** `estado_civil_socio[i] == Casado || uniao_estavel[i] == true`

**Validações (bloqueios):**
- `regime_bens == ComunhaoUniversal` + ambos cônjuges como únicos sócios → **BLOQUEIO** (art. 977 CC)
- `regime_bens == SeparacaoObrigatoria` + ambos cônjuges como únicos sócios → **BLOQUEIO** (art. 977 CC)

**Afeta:**
- Qualificação: adiciona regime de bens e/ou indicação de união estável
- Cláusula condicional de ciência sobre restrições conjugais
- Se integralização com imóvel + casado → exige anuência conjugal (art. 1.647 CC)
- Gera campo de anuência ou documento apartado

### BC3 — Integralização com Imóvel Urbano
**Condição:** `integralizacao_imovel_urbano == true`

**Afeta:**
- Cláusula 4: injeta parágrafo de integralização com imóvel urbano
- Campos obrigatórios: matrícula, cartório, endereço, área, cadastro municipal
- **Validação Tema 796:** se `valor_bem > capital_integralizado` → alerta vermelho
- Cláusula de coerência (BC7) é ativada automaticamente

### BC4 — Integralização com Imóvel Rural
**Condição:** `integralizacao_imovel_rural == true`

**Afeta:**
- Cláusula 4: injeta parágrafo específico com campos rurais
- Campos obrigatórios: matrícula, cartório, denominação, área, município/UF, CCIR, NIRF/CAFIR
- Mesma validação Tema 796 do BC3
- Cláusula de coerência (BC7) é ativada automaticamente

### BC5 — Integralização com Quotas/Ações
**Condição:** `integralizacao_participacoes == true`

**Afeta:**
- Cláusula 4: injeta parágrafo de integralização com participações
- Campos obrigatórios: nome da investida, CNPJ, quantidade quotas/ações, percentual, valor atribuído
- Validação: necessidade de alteração reflexa na investida

### BC6 — Trava de Função do Cofre
**Condição:** `cofre_puro == true` (default ativo)

**Afeta:**
- Cláusula 2: injeta vedações operacionais (locação, compra/venda, serviços)
- Cláusula 14: injeta declaração de finalidade e coerência
- **Trava de sistema:** se `atividade_operacional_propria == true`, bloqueia e exige revisão
- CNAE padrão: 6462-0/00 (holding não financeira)

### BC7 — Cláusula de Coerência da Integralização
**Condição:** `integralizacao_imovel_urbano == true || integralizacao_imovel_rural == true`

**Afeta:**
- Injeta declaração final vinculando: finalidade societária + estrutura do aporte + atividade exercida
- Reforço contra autossabotagem documental

---

## 4. Validações do Motor (Etapa B)

| # | Regra | Severidade | Ação |
|---|-------|-----------|------|
| V1 | Art. 977 CC: cônjuges em comunhão universal como únicos sócios | **BLOQUEIO** | Impede geração |
| V2 | Art. 977 CC: cônjuges em separação obrigatória como únicos sócios | **BLOQUEIO** | Impede geração |
| V3 | Art. 1.647 CC: integralização imóvel + casado sem anuência | **ALERTA** | Exige anuência |
| V4 | Tema 796 STF: valor do bem > capital integralizado | **ALERTA VERMELHO** | Força revisão |
| V5 | Desvio funcional: atividade operacional + cofre puro | **BLOQUEIO** | Sai do fluxo Cofre |
| V6 | Capital social = 0 ou negativo | **BLOQUEIO** | Impede geração |
| V7 | Sócio sem CPF preenchido | **BLOQUEIO** | Impede geração |
| V8 | Imóvel sem matrícula | **ALERTA** | Exige preenchimento |
| V9 | Imóvel rural sem CCIR/NIRF | **ALERTA** | Exige preenchimento |
| V10 | Percentuais dos sócios ≠ 100% | **BLOQUEIO** | Impede geração |

---

## 5. Fluxo do Motor Documental

```
Etapa A — Diagnóstico
    ├── Celula.Tipo == Cofre?
    ├── Quantos sócios? → tipo_sociedade
    ├── Estado civil de cada sócio?
    ├── Regime de bens? → validar art. 977
    ├── Tipo de integralização?
    │   ├── Dinheiro
    │   ├── Imóvel urbano → BC3
    │   ├── Imóvel rural → BC4
    │   └── Participações → BC5
    ├── Cofre puro? → BC6
    └── Há imóvel? → BC7

Etapa B — Validação
    ├── V1..V10 executadas em sequência
    ├── BLOQUEIO → retorna erros, não gera
    ├── ALERTA → retorna warnings, permite gerar com ciência
    └── OK → segue para montagem

Etapa C — Montagem
    ├── Carrega minuta-base (14 cláusulas fixas)
    ├── Resolve tipo_sociedade (pluripessoal/unipessoal) → BC1
    ├── Para cada sócio:
    │   ├── Monta qualificação com regime de bens → BC2
    │   └── Monta bloco de integralização → BC3/BC4/BC5
    ├── Injeta trava do Cofre → BC6
    ├── Injeta coerência de integralização → BC7
    ├── Resolve quóruns configuráveis
    ├── Resolve apuração de haveres
    ├── Gera checklist registral
    └── Retorna DocumentoGerado (HTML + PDF + DOCX)
```

---

## 6. O que NÃO entra neste contrato

- Doação de quotas com reserva de usufruto → instrumento próprio
- Protocolo familiar detalhado → documento separado
- Governança sucessória fina → Acordo de Quotistas
- Regras de herdeiros por ramo → Acordo de Quotistas
- Política de voto e impasse → Acordo de Quotistas
- Confidencialidade ampla → cláusula em Acordo de Quotistas

O contrato social deve ser **forte, mas não obeso**. O DREI cobra clareza e registrabilidade.
