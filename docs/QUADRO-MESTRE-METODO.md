# Holdus — Quadro Mestre do Método Tríade Capital®
## Versão definitiva — Thiago Seixas

---

## Regra canônica

> O PDF vira a espinha do workflow. As minutas viram a biblioteca documental.
> As validações jurídicas viram o freio. O sistema só avança quando a etapa
> estiver juridicamente fechada.
> PDF governa sequência. Lei governa validade.

---

## FASE 1 — Planejamento Sucessório — Célula Destino (6 passos)

| # | Passo | Doc. principal | Doc. reflexo | Validação | Trava |
|---|-------|---------------|-------------|-----------|-------|
| 1.1 | Constituir Destino | DESTINO.CONSTITUICAO.V1 | — | Sócios originários = donos patrimônio, capital reduzido, objeto sucessório | Sem CS → não abre conta |
| 1.2 | Abrir conta PJ | Checklist operacional | — | Dados bancários, titularidade, correspondência | Sem conta → capital não integraliza |
| 1.3 | Integralizar capital inicial | Comprovantes + checklist | — | Cada sócio aporta exato valor da participação (PIX) | Sem integralização → não doa |
| 1.4 | Doar quotas | DESTINO.DOACAO_QUOTAS.V1 | DESTINO.ALT_DOACAO.V1 | Quem doa, quem recebe, natureza, usufruto, restrições | Sem doação + alteração → não avança |
| 1.5 | Acordo de quotistas | DESTINO.ACORDO_QUOTISTAS.V1 | — | 16 perguntas da matriz, voto, frutos, circulação, eventos | Sem acordo → não fecha fase 1 |
| 1.6 | ITCMD | Checklist ITCMD + nota técnica | — | Base de cálculo, fluxo estadual, comprovante | Sem ITCMD → não entra Fase 2 |

## FASE 2 — Planejamento Patrimonial — Célula Cofre (5 passos)

| # | Passo | Doc. principal | Doc. reflexo | Validação | Trava |
|---|-------|---------------|-------------|-----------|-------|
| 2.1 | Constituir Cofre | COFRE.CONSTITUICAO.V1 | — | Objeto puro, sem atividade operacional, COFRE_001..071 | Sem CS → não apura ITBI |
| 2.2 | Apurar ITBI | COFRE.CHECKLIST_ITBI.V1 | — | Resposta prefeitura, base praticada, cenário municipal | Sem apuração → não preenche Cofre |
| 2.3 | Preencher Cofre com bens | ALT_CAPITAL_IMOVEL.V1 ou ALT_CAPITAL_PARTICIPACOES.V1 | ATO_REFLEXO_INVESTIDA.V1 | Descrição completa, valor, titular conferente | Sem integralização → não fecha |
| 2.4 | Vertente A (ITBI não cobrado) | Alteração contratual valor declarado IR | — | Aderência ao cenário apurado | Só habilita se prefeitura não cobra |
| 2.5 | Vertente B (ITBI cobrado) | Alteração contratual valor apurado + AVJ | — | Base municipal, valor total, coerência | Só habilita se prefeitura cobra |

## FASE 3 — Planejamento Tributário e Controle — Célula Veículo (9 passos)

| # | Passo | Doc. principal | Validação | Trava |
|---|-------|---------------|-----------|-------|
| 3.1 | Constituir Veículo | VEICULO.CONSTITUICAO.V1 | Regência supletiva, classes de quotas, função comando | Sem regência supletiva → bloqueia tudo |
| 3.2 | Estruturar ordinárias | Módulo interno CS Veículo | Mesma quantidade e distribuição da Destino | Espelhamento conferido |
| 3.3 | Estruturar preferencial | Módulo interno CS Veículo | Titular correto, valor nominal, direitos políticos | Sem definição completa → não fecha |
| 3.4 | Voto reforçado | Cláusula específica CS Veículo | Fórmula X+1 parametrizada | Sistema confere total ordinárias |
| 3.5 | Integralizar Veículo | Cláusula integralização | Valor = capital Cofre + preferencial | Sem vínculo → incoerente |
| 3.6 | Reserva de capital | VEICULO.MOD.RESERVA_CAPITAL.V1 (opcional) | Excedente + regência supletiva ativa | Não libera automaticamente |
| 3.7 | Call da preferencial | VEICULO.MOD.CALL_PREFERENCIAL.V1 | Evento, comprador, prazo, preço, pagamento | Sem parametrização → bloqueia |
| 3.8 | Alterar Cofre → Veículo | COFRE.ALT_TITULARIDADE_VEICULO.V1 | Causa jurídica, quotas, quadro novo | Sem causa expressa → bloqueia |
| 3.9 | Destino compra ordinárias | VEICULO.ALT_COMPRA_ORDINARIAS.V1 | Onerosa, preço nominal, preferencial fora | Preferencial na operação → bloqueia |

---

## Trava-mestra

> Nenhuma fase avança se a fase anterior não estiver fechada
> documentalmente e validada.
> - Não entra na Cofre sem a Destino fechada
> - Não entra na Veículo sem a Cofre fechada
> - Não fecha o método sem os atos reflexos da Fase 3
