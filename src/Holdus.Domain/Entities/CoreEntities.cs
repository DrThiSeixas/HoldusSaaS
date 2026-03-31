using Holdus.Domain.Enums;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// PESSOA FÍSICA
// Cadastro completo para alimentar contratos e qualificações.
// Campos sensíveis (CPF, RG, filiação) criptografados via
// Data Protection no interceptor de persistência.
// ═══════════════════════════════════════════════════════════════

public class PessoaFisica : BaseEntity
{
    // Dados pessoais (CPF/RG criptografados em repouso)
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;          // [Encrypted]
    public string? Rg { get; set; }                           // [Encrypted]
    public string? RgOrgaoEmissor { get; set; }
    public string? RgUfEmissor { get; set; }
    public DateOnly? DataNascimento { get; set; }             // [Encrypted]
    public string? Naturalidade { get; set; }
    public string Nacionalidade { get; set; } = "Brasileira";
    public string? Profissao { get; set; }

    // Estado civil
    public EstadoCivil EstadoCivil { get; set; } = EstadoCivil.Solteiro;
    public RegimeBens? RegimeBens { get; set; }
    public int? ConjugeId { get; set; }

    // Filiação (criptografados)
    public string? NomePai { get; set; }                      // [Encrypted]
    public string? NomeMae { get; set; }                      // [Encrypted]

    // Endereço
    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }

    // Contato (criptografados)
    public string? Celular { get; set; }                      // [Encrypted]
    public string? Email { get; set; }                        // [Encrypted]
    public string? WhatsApp { get; set; }

    // Observações
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;

    // Navegação
    public PessoaFisica? Conjuge { get; set; }
    public ICollection<ParticipanteProjeto> Participacoes { get; set; } = [];
    public ICollection<Bem> Bens { get; set; } = [];
    public ICollection<SocioCelula> Sociedades { get; set; } = [];
}

// ═══════════════════════════════════════════════════════════════
// PROJETO TRÍADE
// Agrupador de uma família e suas células de holding.
// ═══════════════════════════════════════════════════════════════

public class ProjetoTriade : BaseEntity
{
    public string Codigo { get; set; } = string.Empty;  // Ex: PRJ-2026-001 (unique per tenant)
    public string NomeProjeto { get; set; } = string.Empty;
    public string NomeFamilia { get; set; } = string.Empty;
    public ModeloHolding ModeloEscolhido { get; set; } = ModeloHolding.TresCelulas;
    public StatusProjeto Status { get; set; } = StatusProjeto.Captacao;

    // Datas
    public DateTimeOffset? DataContratacao { get; set; }
    public DateTimeOffset? DataConclusao { get; set; }

    // Responsável
    public Guid? AdvogadoResponsavelId { get; set; }

    public string? Observacoes { get; set; }

    // Navegação
    public Usuario? AdvogadoResponsavel { get; set; }
    public ICollection<ParticipanteProjeto> Participantes { get; set; } = [];
    public ICollection<Celula> Celulas { get; set; } = [];
    public ICollection<FaseProjeto> Fases { get; set; } = [];
    public ContratoHonorarios? Contrato { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// PARTICIPANTE DO PROJETO
// ═══════════════════════════════════════════════════════════════

public class ParticipanteProjeto : BaseEntity
{
    public int ProjetoId { get; set; }
    public int PessoaFisicaId { get; set; }
    public PapelProjeto Papel { get; set; }
    public string? Observacoes { get; set; }

    // Navegação
    public ProjetoTriade Projeto { get; set; } = null!;
    public PessoaFisica PessoaFisica { get; set; } = null!;
}

// ═══════════════════════════════════════════════════════════════
// CÉLULA (HOLDING)
// Cofre, Destino ou Veículo — cada uma é uma empresa.
// ═══════════════════════════════════════════════════════════════

public class Celula : BaseEntity
{
    public int ProjetoId { get; set; }
    public TipoCelula Tipo { get; set; }
    public StatusCelula Status { get; set; } = StatusCelula.EmCriacao;

    // Dados previstos (em criação)
    public string NomeCelula { get; set; } = string.Empty;
    public string? ObjetoSocial { get; set; }
    public decimal CapitalSocialPrevisto { get; set; }

    // Dados efetivos (pós-registro)
    public string? Cnpj { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public decimal? CapitalSocialEfetivo { get; set; }
    public string? Nire { get; set; }
    public DateOnly? DataRegistro { get; set; }

    // Endereço (JSON flexível)
    public string? EnderecoJson { get; set; }

    // Administração
    public int? AdministradorId { get; set; }
    public string? Observacoes { get; set; }

    // Navegação
    public ProjetoTriade Projeto { get; set; } = null!;
    public PessoaFisica? Administrador { get; set; }
    public ICollection<SocioCelula> Socios { get; set; } = [];
    public ICollection<Bem> BensDestinados { get; set; } = [];
}

// ═══════════════════════════════════════════════════════════════
// SÓCIO DE CÉLULA
// Participação societária detalhada com quotas e votos.
// ═══════════════════════════════════════════════════════════════

public class SocioCelula : BaseEntity
{
    public int CelulaId { get; set; }
    public int PessoaFisicaId { get; set; }

    // Quotas
    public int QuantidadeQuotas { get; set; }
    public decimal ValorPorQuota { get; set; } = 1.00m;
    public decimal PercentualParticipacao { get; set; }

    // Tipo
    public TipoQuota TipoQuota { get; set; } = TipoQuota.Ordinaria;
    public int? PesoVoto { get; set; }
    public bool UsufrutoVitalicio { get; set; }

    // Navegação
    public Celula Celula { get; set; } = null!;
    public PessoaFisica PessoaFisica { get; set; } = null!;
}

// ═══════════════════════════════════════════════════════════════
// BEM PATRIMONIAL
// Imóvel, veículo, investimento vinculado a proprietário.
// Dados específicos em JSONB para flexibilidade.
// ═══════════════════════════════════════════════════════════════

public class Bem : BaseEntity
{
    public int ProprietarioId { get; set; }
    public TipoBem Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;

    // Valores (precisão jurídica)
    public decimal ValorDeclaracaoIR { get; set; }
    public decimal ValorMercado { get; set; }
    public DateOnly? DataAvaliacao { get; set; }

    // Dados específicos por tipo (JSONB)
    // Imóvel: matrícula, cartório, inscrição, IPTU, área, tipo
    // Veículo: placa, RENAVAM, chassi, FIPE
    // Investimento: instituição, tipo, CNPJ custodiante
    public string? DadosEspecificosJson { get; set; }

    // Destino na holding
    public int? CelulaDestinoId { get; set; }

    public string? Observacoes { get; set; }

    // Navegação
    public PessoaFisica Proprietario { get; set; } = null!;
    public Celula? CelulaDestino { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// FASE DO PROJETO
// ═══════════════════════════════════════════════════════════════

public class FaseProjeto : BaseEntity
{
    public int ProjetoId { get; set; }
    public int NumeroFase { get; set; }
    public string NomeFase { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendente";
    public DateTimeOffset? DataInicio { get; set; }
    public DateTimeOffset? DataConclusao { get; set; }

    // Navegação
    public ProjetoTriade Projeto { get; set; } = null!;
    public ICollection<PassoFase> Passos { get; set; } = [];
}

public class PassoFase : BaseEntity
{
    public int FaseId { get; set; }
    public int Ordem { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendente";
    public DateTimeOffset? DataConclusao { get; set; }
    public string? Observacoes { get; set; }

    public FaseProjeto Fase { get; set; } = null!;
}
