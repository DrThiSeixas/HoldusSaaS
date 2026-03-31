#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// VALIDADOR JURÍDICO-OPERACIONAL DO COFRE
// "Esse é o núcleo. Ele trava o que precisa travar."
// Códigos COFRE_001..071
// ═══════════════════════════════════════════════════════════════

public sealed class ValidationMessage
{
    public ValidationSeverity Severity { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Field { get; init; }
}

public sealed class ValidationResult
{
    public List<ValidationMessage> Messages { get; } = new();

    public bool HasErrors => Messages.Any(x => x.Severity == ValidationSeverity.Error);
    public bool HasWarnings => Messages.Any(x => x.Severity == ValidationSeverity.Warning);

    public bool CanGenerateDocuments => !HasErrors;

    public void AddError(string code, string message, string? field = null) =>
        Messages.Add(new ValidationMessage
        {
            Severity = ValidationSeverity.Error,
            Code = code,
            Message = message,
            Field = field
        });

    public void AddWarning(string code, string message, string? field = null) =>
        Messages.Add(new ValidationMessage
        {
            Severity = ValidationSeverity.Warning,
            Code = code,
            Message = message,
            Field = field
        });

    public void AddInfo(string code, string message, string? field = null) =>
        Messages.Add(new ValidationMessage
        {
            Severity = ValidationSeverity.Info,
            Code = code,
            Message = message,
            Field = field
        });
}

public sealed class CofreProfileValidator
{
    public ValidationResult Validate(CofreProfile profile)
    {
        var result = new ValidationResult();

        ValidateIdentity(profile, result);
        ValidateCofrePurity(profile, result);
        ValidatePartners(profile, result);
        ValidateAdministrators(profile, result);
        ValidateCapital(profile, result);
        ValidatePropertyContribution(profile, result);
        ValidateEquityContribution(profile, result);
        ValidateChecklists(profile, result);

        UpdateStatus(profile, result);
        return result;
    }

    // ═══════════════════════════════════════════════════════════
    // A/B. IDENTIDADE E DADOS REGISTRAIS
    // ═══════════════════════════════════════════════════════════

    private static void ValidateIdentity(CofreProfile profile, ValidationResult result)
    {
        if (profile.NaturezaCelula != "COFRE")
            result.AddError("COFRE_001", "A natureza da célula deve ser COFRE.", nameof(profile.NaturezaCelula));

        if (string.IsNullOrWhiteSpace(profile.NomeEmpresarial))
            result.AddError("COFRE_002", "O nome empresarial é obrigatório.", nameof(profile.NomeEmpresarial));

        if (string.IsNullOrWhiteSpace(profile.UfJunta))
            result.AddError("COFRE_003", "A UF da Junta é obrigatória.", nameof(profile.UfJunta));

        if (profile.IsAlteracao)
        {
            if (string.IsNullOrWhiteSpace(profile.Cnpj))
                result.AddError("COFRE_004", "Em alteração contratual, o CNPJ é obrigatório.", nameof(profile.Cnpj));

            if (string.IsNullOrWhiteSpace(profile.Nire))
                result.AddError("COFRE_005", "Em alteração contratual, o NIRE é obrigatório.", nameof(profile.Nire));
        }

        if (profile.EnderecoSede is null)
        {
            result.AddError("COFRE_006", "O endereço da sede deve ser informado.", nameof(profile.EnderecoSede));
            return;
        }

        if (string.IsNullOrWhiteSpace(profile.EnderecoSede.Cidade))
            result.AddError("COFRE_007", "A cidade da sede é obrigatória.", "EnderecoSede.Cidade");

        if (string.IsNullOrWhiteSpace(profile.EnderecoSede.Uf))
            result.AddError("COFRE_008", "A UF da sede é obrigatória.", "EnderecoSede.Uf");
    }

    // ═══════════════════════════════════════════════════════════
    // C. PUREZA DO COFRE
    // ═══════════════════════════════════════════════════════════

    private static void ValidateCofrePurity(CofreProfile profile, ValidationResult result)
    {
        var desvioFuncional =
            profile.AtividadeOperacionalPropria ||
            profile.LocacaoImoveisProprios ||
            profile.CompraVendaImoveis ||
            profile.PrestacaoServicos ||
            profile.AdministraTerceiros;

        if (desvioFuncional)
        {
            profile.CofrePuro = false;
            profile.HaRiscoAtividadePreponderante = true;

            result.AddError(
                "COFRE_010",
                "A célula Cofre não pode seguir no fluxo padrão se houver atividade operacional própria, locação, compra e venda de imóveis, prestação de serviços ou administração de terceiros.");
        }

        if (profile.CnaePrincipal != "6462-0/00" && profile.CofrePuro)
        {
            result.AddWarning(
                "COFRE_011",
                "Para Cofre puro, o CNAE-base esperado é 6462-0/00.",
                nameof(profile.CnaePrincipal));
        }
    }

    // ═══════════════════════════════════════════════════════════
    // D. SÓCIOS
    // ═══════════════════════════════════════════════════════════

    private static void ValidatePartners(CofreProfile profile, ValidationResult result)
    {
        if (profile.Socios.Count == 0)
            result.AddError("COFRE_020", "A célula Cofre deve possuir ao menos um sócio.", nameof(profile.Socios));

        if (profile.TipoSociedade == CompanyType.LtdaUnipessoal && profile.Socios.Count != 1)
            result.AddError("COFRE_021", "LTDA unipessoal deve possuir exatamente um sócio.", nameof(profile.Socios));

        if (profile.TipoSociedade == CompanyType.Ltda && profile.Socios.Count < 1)
            result.AddError("COFRE_022", "LTDA deve possuir ao menos um sócio.", nameof(profile.Socios));

        foreach (var socio in profile.Socios)
        {
            if (string.IsNullOrWhiteSpace(socio.NomeCompleto))
                result.AddError("COFRE_023", "Nome do sócio é obrigatório.", $"Socio:{socio.Id}:NomeCompleto");

            if (string.IsNullOrWhiteSpace(socio.Cpf))
                result.AddError("COFRE_024", "CPF do sócio é obrigatório.", $"Socio:{socio.Id}:Cpf");

            if (socio.RequiresSpousalConsent && !socio.SpousalConsentOk)
            {
                profile.HaAnuenciaConjugalPendente = true;
                result.AddError(
                    "COFRE_025",
                    $"Há anuência conjugal/convivencial pendente para o sócio {socio.NomeCompleto}.",
                    $"Socio:{socio.Id}:SpousalConsentOk");
            }

            if (socio.IsSpouseOrPartnerOfAnotherPartnerInThisCompany &&
                (socio.RegimeBens == MaritalRegime.ComunhaoUniversal ||
                 socio.RegimeBens == MaritalRegime.SeparacaoObrigatoria))
            {
                profile.HaBloqueioRegimeBens = true;
                result.AddError(
                    "COFRE_026",
                    $"Há potencial bloqueio jurídico pela composição conjugal/convivencial do sócio {socio.NomeCompleto}.",
                    $"Socio:{socio.Id}:RegimeBens");
            }
        }

        var somaQuotas = profile.Socios.Sum(x => x.QuantidadeQuotas);
        if (somaQuotas < 0)
            result.AddError("COFRE_027", "A soma das quotas não pode ser negativa.");

        var somaPercentuais = profile.Socios.Sum(x => x.ParticipacaoPercentual);
        if (somaPercentuais > 0 && decimal.Round(somaPercentuais, 2) != 100m)
            result.AddWarning("COFRE_028", "A soma dos percentuais dos sócios é diferente de 100%.");
    }

    // ═══════════════════════════════════════════════════════════
    // E. ADMINISTRADORES
    // ═══════════════════════════════════════════════════════════

    private static void ValidateAdministrators(CofreProfile profile, ValidationResult result)
    {
        if (profile.Administradores.Count == 0)
            result.AddError("COFRE_030", "A célula Cofre deve possuir ao menos um administrador.", nameof(profile.Administradores));

        foreach (var adm in profile.Administradores)
        {
            if (string.IsNullOrWhiteSpace(adm.NomeCompleto))
                result.AddError("COFRE_031", "Nome do administrador é obrigatório.", $"Administrador:{adm.Id}:NomeCompleto");

            if (string.IsNullOrWhiteSpace(adm.Cpf))
                result.AddError("COFRE_032", "CPF do administrador é obrigatório.", $"Administrador:{adm.Id}:Cpf");

            if (!adm.DeclaracaoDesimpedimento)
                result.AddError("COFRE_033", $"Administrador {adm.NomeCompleto} sem declaração de desimpedimento.", $"Administrador:{adm.Id}:DeclaracaoDesimpedimento");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // F/G. CAPITAL E INTEGRALIZAÇÃO
    // ═══════════════════════════════════════════════════════════

    private static void ValidateCapital(CofreProfile profile, ValidationResult result)
    {
        if (profile.CapitalSocialPosAto < profile.CapitalSocialAtual)
            result.AddWarning("COFRE_040", "O capital social pós-ato está menor que o capital atual. Verifique se o ato é mesmo de aumento.");

        if (profile.TipoIntegralizacao == ContributionType.Nenhuma && profile.Integralizacao.ValorDestinadoCapital > 0)
            result.AddWarning("COFRE_041", "Há valor destinado ao capital sem tipo de integralização definido.");

        if (profile.Integralizacao.ValorTotalAporte != profile.Integralizacao.ValorDestinadoCapital)
        {
            profile.Integralizacao.HaParcelaForaCapital = true;
            profile.HaRiscoTema796 = true;

            result.AddError(
                "COFRE_042",
                "O valor total do aporte é diferente do valor destinado ao capital. A operação sai do fluxo seguro do Cofre e entra em risco Tema 796.",
                nameof(profile.Integralizacao));
        }
    }

    // ═══════════════════════════════════════════════════════════
    // H. IMÓVEIS
    // ═══════════════════════════════════════════════════════════

    private static void ValidatePropertyContribution(CofreProfile profile, ValidationResult result)
    {
        if (!profile.TemIntegralizacaoImovel)
            return;

        if (profile.ImoveisAportados.Count == 0)
        {
            result.AddError("COFRE_050", "Há integralização com imóvel, mas nenhum imóvel foi cadastrado.", nameof(profile.ImoveisAportados));
            return;
        }

        foreach (var item in profile.ImoveisAportados)
        {
            if (string.IsNullOrWhiteSpace(item.Matricula))
                result.AddError("COFRE_051", "Matrícula do imóvel é obrigatória.", $"Imovel:{item.Id}:Matricula");

            if (string.IsNullOrWhiteSpace(item.Cartorio))
                result.AddError("COFRE_052", "Cartório do imóvel é obrigatório.", $"Imovel:{item.Id}:Cartorio");

            if (string.IsNullOrWhiteSpace(item.Municipio))
                result.AddError("COFRE_053", "Município do imóvel é obrigatório.", $"Imovel:{item.Id}:Municipio");

            if (string.IsNullOrWhiteSpace(item.Uf))
                result.AddError("COFRE_054", "UF do imóvel é obrigatória.", $"Imovel:{item.Id}:Uf");

            if (string.IsNullOrWhiteSpace(item.TituloAquisitivo))
                result.AddError("COFRE_055", "Título aquisitivo do imóvel é obrigatório.", $"Imovel:{item.Id}:TituloAquisitivo");

            if (item.ValorAtribuidoNoAto <= 0)
                result.AddError("COFRE_056", "Valor atribuído ao imóvel no ato deve ser maior que zero.", $"Imovel:{item.Id}:ValorAtribuidoNoAto");

            if (item.ValorIntegralizadoEmCapital <= 0)
                result.AddError("COFRE_057", "Valor integralizado em capital deve ser maior que zero.", $"Imovel:{item.Id}:ValorIntegralizadoEmCapital");

            if (item.ValorAtribuidoNoAto != item.ValorIntegralizadoEmCapital)
            {
                profile.HaRiscoTema796 = true;
                result.AddError(
                    "COFRE_058",
                    "No imóvel aportado, o valor atribuído no ato é diferente do valor integralizado em capital. Risco Tema 796.",
                    $"Imovel:{item.Id}");
            }

            if (!item.DocumentacaoOk)
                result.AddError("COFRE_059", "A documentação do imóvel não está marcada como completa.", $"Imovel:{item.Id}:DocumentacaoOk");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // I. PARTICIPAÇÕES
    // ═══════════════════════════════════════════════════════════

    private static void ValidateEquityContribution(CofreProfile profile, ValidationResult result)
    {
        if (!profile.TemIntegralizacaoParticipacoes)
            return;

        if (profile.ParticipacoesAportadas.Count == 0)
        {
            result.AddError("COFRE_060", "Há integralização com participações, mas nenhuma participação foi cadastrada.", nameof(profile.ParticipacoesAportadas));
            return;
        }

        foreach (var item in profile.ParticipacoesAportadas)
        {
            if (string.IsNullOrWhiteSpace(item.SociedadeInvestidaNome))
                result.AddError("COFRE_061", "O nome da sociedade investida é obrigatório.", $"Participacao:{item.Id}:SociedadeInvestidaNome");

            if (string.IsNullOrWhiteSpace(item.SociedadeInvestidaCnpj))
                result.AddError("COFRE_062", "O CNPJ da sociedade investida é obrigatório.", $"Participacao:{item.Id}:SociedadeInvestidaCnpj");

            if (item.Quantidade <= 0)
                result.AddError("COFRE_063", "A quantidade da participação deve ser maior que zero.", $"Participacao:{item.Id}:Quantidade");

            if (item.ValorAtribuido <= 0)
                result.AddError("COFRE_064", "O valor atribuído à participação deve ser maior que zero.", $"Participacao:{item.Id}:ValorAtribuido");

            if (!item.CapitalDaInvestidaTotalmenteIntegralizado)
                result.AddError("COFRE_065", "O capital da investida deve estar totalmente integralizado para este tipo de operação.", $"Participacao:{item.Id}:CapitalDaInvestidaTotalmenteIntegralizado");

            if (item.RestricaoContratualTransferencia)
                result.AddWarning("COFRE_066", "Há restrição contratual à transferência na investida. Revisão jurídica necessária.", $"Participacao:{item.Id}:RestricaoContratualTransferencia");

            if (!item.DocumentacaoOk)
                result.AddError("COFRE_067", "A documentação da participação não está marcada como completa.", $"Participacao:{item.Id}:DocumentacaoOk");
        }

        if (!profile.HaAtoReflexo || profile.AtosReflexos.Count == 0)
            result.AddError("COFRE_068", "Integralização com participações exige ato reflexo na investida.", nameof(profile.AtosReflexos));

        foreach (var ato in profile.AtosReflexos)
        {
            if (!ato.Gerado)
                result.AddError("COFRE_069", $"O ato reflexo da sociedade {ato.SociedadeAfetada} ainda não foi gerado.", $"AtoReflexo:{ato.Id}:Gerado");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CHECKLISTS
    // ═══════════════════════════════════════════════════════════

    private static void ValidateChecklists(CofreProfile profile, ValidationResult result)
    {
        if (!profile.ChecklistRegistralOk)
            result.AddError("COFRE_070", "O checklist registral precisa estar concluído.", nameof(profile.ChecklistRegistralOk));

        if (!profile.ChecklistTributarioOk)
            result.AddWarning("COFRE_071", "O checklist tributário ainda não foi concluído.", nameof(profile.ChecklistTributarioOk));
    }

    // ═══════════════════════════════════════════════════════════
    // STATUS
    // ═══════════════════════════════════════════════════════════

    private static void UpdateStatus(CofreProfile profile, ValidationResult result)
    {
        if (result.HasErrors)
        {
            profile.StatusCelula = CellStatus.Bloqueada;
            return;
        }

        profile.StatusCelula = CellStatus.AptaParaGeracao;
    }
}
