namespace Holdus.Domain.Enums;

// ═══════════════════════════════════════════════════════════════
// TENANT & USUÁRIO
// ═══════════════════════════════════════════════════════════════

public enum PlanoTenant
{
    Free = 0,
    Pro = 1,
    Enterprise = 2
}

public enum RoleUsuario
{
    Admin = 0,
    Advogado = 1,
    Assistente = 2,
    Estagiario = 3
}

// ═══════════════════════════════════════════════════════════════
// PESSOA FÍSICA
// ═══════════════════════════════════════════════════════════════

public enum EstadoCivil
{
    Solteiro = 0,
    Casado = 1,
    Divorciado = 2,
    Viuvo = 3,
    UniaoEstavel = 4,
    SeparadoJudicialmente = 5
}

public enum RegimeBens
{
    ComunhaoTotal = 0,
    ComunhaoParcial = 1,
    SeparacaoTotal = 2,
    SeparacaoObrigatoria = 3,
    ParticipacaoFinalAquestos = 4
}

// ═══════════════════════════════════════════════════════════════
// PROJETO TRÍADE
// ═══════════════════════════════════════════════════════════════

public enum ModeloHolding
{
    Basico = 0,         // 1 célula (Cofre)
    DuasCelulas = 1,    // Cofre + Destino
    TresCelulas = 2     // Cofre + Destino + Veículo (Tríade)
}

public enum StatusProjeto
{
    Captacao = 0,
    SessaoViabilidade = 1,
    CroquiElaborado = 2,
    CroquiApresentado = 3,
    Contratado = 4,
    EmExecucao = 5,
    Concluido = 6,
    Cancelado = 7
}

public enum PapelProjeto
{
    Constituinte = 0,   // Dono do patrimônio
    Donatario = 1       // Herdeiro / beneficiário
}

// ═══════════════════════════════════════════════════════════════
// CÉLULA (HOLDING)
// ═══════════════════════════════════════════════════════════════

public enum TipoCelula
{
    Cofre = 0,      // Capital Ltda
    Destino = 1,    // Estrutura Patrimonial Ltda
    Veiculo = 2     // Gestão Ltda
}

public enum StatusCelula
{
    EmCriacao = 0,
    Constituida = 1,
    EmAlteracao = 2,
    Ativa = 3,
    Inativa = 4
}

public enum TipoQuota
{
    Ordinaria = 0,
    Preferencial = 1
}

// ═══════════════════════════════════════════════════════════════
// BEM PATRIMONIAL
// ═══════════════════════════════════════════════════════════════

public enum TipoBem
{
    Imovel = 0,
    Veiculo = 1,
    ParticipacaoSocietaria = 2,
    Investimento = 3,
    BemMovel = 4,
    Outros = 5
}

// ═══════════════════════════════════════════════════════════════
// DOCUMENTOS
// ═══════════════════════════════════════════════════════════════

public enum CategoriaTemplate
{
    Constituicao = 0,
    Alteracao = 1,
    Ata = 2,
    Procuracao = 3,
    Honorarios = 4,
    Doacao = 5,
    Outro = 6
}

public enum StatusDocumento
{
    Rascunho = 0,
    EmRevisao = 1,
    Final = 2,
    Assinado = 3
}

public enum CategoriaClausula
{
    Sucessoria = 0,
    Governanca = 1,
    Administracao = 2,
    DistribuicaoLucros = 3,
    CallPut = 4,
    Restricoes = 5,
    Geral = 6
}

// ═══════════════════════════════════════════════════════════════
// FINANCEIRO
// ═══════════════════════════════════════════════════════════════

public enum StatusContrato
{
    Vigente = 0,
    Encerrado = 1,
    Cancelado = 2
}

public enum StatusParcela
{
    Pendente = 0,
    Paga = 1,
    Vencida = 2,
    Cancelada = 3
}

public enum FormaPagamento
{
    Pix = 0,
    Boleto = 1,
    Transferencia = 2,
    CartaoCredito = 3,
    Dinheiro = 4
}

public enum StatusNotaFiscal
{
    Pendente = 0,
    Emitida = 1,
    Cancelada = 2,
    Erro = 3
}

// ═══════════════════════════════════════════════════════════════
// LGPD
// ═══════════════════════════════════════════════════════════════

public enum AcaoAuditoria
{
    Create = 0,
    Read = 1,
    Update = 2,
    Delete = 3,
    Export = 4,
    Anonymize = 5,
    Login = 6,
    Logout = 7
}

public enum BaseLegalLgpd
{
    Consentimento = 0,          // Art. 7º, I
    ExecucaoContrato = 1,       // Art. 7º, V
    ExercicioRegularDireitos = 2, // Art. 7º, VI
    LegitimoInteresse = 3       // Art. 7º, IX
}

// ═══════════════════════════════════════════════════════════════
// CAPTAÇÃO
// ═══════════════════════════════════════════════════════════════

public enum FaixaPatrimonio
{
    Ate500Mil = 0,
    De500MilA1Mi = 1,
    De1MiA3Mi = 2,
    De3MiA5Mi = 3,
    De5MiA10Mi = 4,
    Acima10Mi = 5
}

public enum NaturezaBens
{
    SomenteImoveis = 0,
    ImoveisEVeiculos = 1,
    ImoveisEInvestimentos = 2,
    Diversificado = 3,
    SomenteInvestimentos = 4,
    Outros = 5
}

public enum StatusCaptacao
{
    EmPreenchimento = 0,
    Concluida = 1,
    ConvertidaProjeto = 2,
    Descartada = 3
}

public enum ComoConheceu
{
    Indicacao = 0,
    PalestraResolvedores = 1,
    RedesSociais = 2,
    Google = 3,
    Outro = 4
}

// ═══════════════════════════════════════════════════════════════
// TRIBUTÁRIO
// ═══════════════════════════════════════════════════════════════

public enum RegimeTributario
{
    SimplesNacional = 0,
    LucroPresumido = 1
}
