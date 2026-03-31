#nullable enable
using System.Collections.Generic;
using System.Linq;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// VALIDAÇÃO + RESOLVER DO ACORDO DA DESTINO
// Versão definitiva — Thiago Seixas
// Códigos DAG_001..102
// ═══════════════════════════════════════════════════════════════

// ── TIPOS DE VALIDAÇÃO ───────────────────────────────────────

public enum AgreementValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public sealed class AgreementValidationMessage
{
    public AgreementValidationSeverity Severity { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? Field { get; init; }
}

public sealed class AgreementValidationResult
{
    public List<AgreementValidationMessage> Messages { get; } = new();
    public bool HasErrors => Messages.Any(x => x.Severity == AgreementValidationSeverity.Error);
    public bool CanGenerate => !HasErrors;

    public void AddError(string code, string message, string? field = null) =>
        Messages.Add(new AgreementValidationMessage
        {
            Severity = AgreementValidationSeverity.Error,
            Code = code,
            Message = message,
            Field = field
        });

    public void AddWarning(string code, string message, string? field = null) =>
        Messages.Add(new AgreementValidationMessage
        {
            Severity = AgreementValidationSeverity.Warning,
            Code = code,
            Message = message,
            Field = field
        });

    public void AddInfo(string code, string message, string? field = null) =>
        Messages.Add(new AgreementValidationMessage
        {
            Severity = AgreementValidationSeverity.Info,
            Code = code,
            Message = message,
            Field = field
        });
}

// ═══════════════════════════════════════════════════════════════
// VALIDADOR — DAG_001..102
// ═══════════════════════════════════════════════════════════════

public sealed class DestinoAgreementValidator
{
    public AgreementValidationResult Validate(DestinoAgreementProfile profile)
    {
        var result = new AgreementValidationResult();

        ValidateIdentity(profile, result);
        ValidateParties(profile, result);
        ValidateGiftMode(profile, result);
        ValidateUsufruct(profile, result);
        ValidateTransferRestrictions(profile, result);
        ValidateReservedMatters(profile, result);
        ValidateFamilyEvents(profile, result);
        ValidateExitRule(profile, result);
        ValidatePenaltyRule(profile, result);
        ValidateArchiving(profile, result);
        ValidateDispute(profile, result);

        UpdateStatus(profile, result);

        return result;
    }

    // ── DAG_001..002: Identidade ─────────────────────────

    private static void ValidateIdentity(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(profile.NomeSociedade))
            result.AddError("DAG_001", "O nome da sociedade é obrigatório.", nameof(profile.NomeSociedade));

        if (!profile.HasSignatories)
            result.AddError("DAG_002", "O acordo precisa ter ao menos um signatário.", nameof(profile.Parties));
    }

    // ── DAG_010..013: Partes ─────────────────────────────

    private static void ValidateParties(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (profile.Parties.Count == 0)
            result.AddError("DAG_010", "Nenhuma parte foi cadastrada no acordo.", nameof(profile.Parties));

        foreach (var party in profile.Parties)
        {
            if (string.IsNullOrWhiteSpace(party.NomeCompleto))
                result.AddError("DAG_011", "Nome da parte é obrigatório.", $"Party:{party.Id}:NomeCompleto");

            if (string.IsNullOrWhiteSpace(party.Cpf))
                result.AddError("DAG_012", "CPF da parte é obrigatório.", $"Party:{party.Id}:Cpf");

            if (party.Signatario && party.DataAdesao is null)
                result.AddWarning("DAG_013", "Signatário sem data de adesão registrada.", $"Party:{party.Id}:DataAdesao");
        }
    }

    // ── DAG_020..022: Doação ─────────────────────────────

    private static void ValidateGiftMode(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (profile.HaDoacao && profile.GiftSuccessionMode == GiftSuccessionMode.NaoAplicavel)
            result.AddError("DAG_020", "Havendo doação, a natureza sucessória da liberalidade deve ser definida.", nameof(profile.GiftSuccessionMode));

        if (profile.HasGiftToDescendant &&
            profile.GiftSuccessionMode == GiftSuccessionMode.NaoAplicavel)
        {
            result.AddError("DAG_021", "Doação para descendente exige definição expressa do modo sucessório.", nameof(profile.GiftSuccessionMode));
        }

        if (profile.HasGiftToDescendant &&
            profile.GiftSuccessionMode == GiftSuccessionMode.ParteDisponivelComDispensaColacao)
        {
            result.AddWarning("DAG_022", "Modo 'parte disponível com dispensa de colação' exige validação patrimonial interna.", nameof(profile.GiftSuccessionMode));
        }
    }

    // ── DAG_030..033: Usufruto ───────────────────────────

    private static void ValidateUsufruct(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (!profile.HaUsufruto)
            return;

        if (profile.PoliticalRightsMode == PoliticalRightsMode.NaoDefinido)
            result.AddError("DAG_030", "Havendo usufruto, o modo de direitos políticos deve ser definido.", nameof(profile.PoliticalRightsMode));

        if (profile.EconomicRightsMode == EconomicRightsMode.NaoDefinido)
            result.AddError("DAG_031", "Havendo usufruto, o modo de direitos econômicos deve ser definido.", nameof(profile.EconomicRightsMode));

        var hasUsufrutuary = profile.Parties.Any(x => x.Papel == AgreementPartyRole.Usufrutuario);
        var hasBareOwner = profile.Parties.Any(x => x.Papel == AgreementPartyRole.NuProprietario);

        if (!hasUsufrutuary)
            result.AddError("DAG_032", "O acordo indica usufruto, mas não há usufrutuário cadastrado.", nameof(profile.Parties));

        if (!hasBareOwner)
            result.AddError("DAG_033", "O acordo indica usufruto, mas não há nu-proprietário cadastrado.", nameof(profile.Parties));
    }

    // ── DAG_040: Circulação ──────────────────────────────

    private static void ValidateTransferRestrictions(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (profile.TransferRestrictionMode == TransferRestrictionMode.NaoDefinido)
            result.AddError("DAG_040", "O modo de restrição à circulação de quotas deve ser definido.", nameof(profile.TransferRestrictionMode));
    }

    // ── DAG_050..052: Matérias reservadas ────────────────

    private static void ValidateReservedMatters(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (profile.ReservedMatters.Count == 0)
            result.AddError("DAG_050", "O acordo deve conter matérias reservadas.", nameof(profile.ReservedMatters));

        foreach (var matter in profile.ReservedMatters)
        {
            if (string.IsNullOrWhiteSpace(matter.Descricao))
                result.AddError("DAG_051", "Matéria reservada sem descrição.", $"ReservedMatter:{matter.Id}:Descricao");

            if (matter.Quorum == ReservedMatterQuorumMode.NaoDefinido)
                result.AddError("DAG_052", "Matéria reservada sem quórum definido.", $"ReservedMatter:{matter.Id}:Quorum");
        }
    }

    // ── DAG_060..061: Eventos familiares ─────────────────

    private static void ValidateFamilyEvents(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (profile.FamilyEventRules.Count == 0)
            result.AddError("DAG_060", "O acordo deve conter regras para eventos familiares críticos.", nameof(profile.FamilyEventRules));

        foreach (var rule in profile.FamilyEventRules)
        {
            if (string.IsNullOrWhiteSpace(rule.ConsequenciaJuridica))
                result.AddError("DAG_061", "Evento familiar sem consequência jurídica definida.", $"FamilyEvent:{rule.Id}:Consequence");
        }
    }

    // ── DAG_070..072: Saída ──────────────────────────────

    private static void ValidateExitRule(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (profile.ExitRule.ValuationMode == ValuationMode.NaoDefinido)
            result.AddError("DAG_070", "A regra de saída precisa de critério de valuation.", nameof(profile.ExitRule.ValuationMode));

        if (profile.ExitRule.ParcelasPagamento is < 0)
            result.AddError("DAG_071", "A quantidade de parcelas não pode ser negativa.", nameof(profile.ExitRule.ParcelasPagamento));

        if (profile.ExitRule.DescontoIliquidez && (!profile.ExitRule.PercentualDescontoIliquidez.HasValue || profile.ExitRule.PercentualDescontoIliquidez <= 0))
            result.AddError("DAG_072", "Se houver desconto por iliquidez, o percentual deve ser informado.", nameof(profile.ExitRule.PercentualDescontoIliquidez));
    }

    // ── DAG_080: Sanções ─────────────────────────────────

    private static void ValidatePenaltyRule(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        var hasAnyConsequence =
            profile.PenaltyRule.MultaValor.HasValue ||
            profile.PenaltyRule.PerdasEDanos ||
            profile.PenaltyRule.TutelaEspecifica ||
            profile.PenaltyRule.ObrigacaoFazerNaoFazer;

        if (!hasAnyConsequence)
            result.AddError("DAG_080", "O acordo deve prever ao menos uma consequência para descumprimento.", nameof(profile.PenaltyRule));
    }

    // ── DAG_090..091: Arquivamento ───────────────────────

    private static void ValidateArchiving(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (profile.ArchivingRule.Mode == ArchivingMode.NaoDefinido)
            result.AddError("DAG_090", "O modo de arquivamento deve ser definido.", nameof(profile.ArchivingRule.Mode));

        if (profile.HaUsufruto &&
            profile.ArchivingRule.RegulaUsufrutoEmInstrumentoParassocial &&
            profile.ArchivingRule.Mode == ArchivingMode.NaoArquivar)
        {
            result.AddWarning("DAG_091", "Há usufruto regulado em instrumento parassocial sem arquivamento previsto. Rever eficácia perante terceiros.", nameof(profile.ArchivingRule.Mode));
        }
    }

    // ── DAG_100..102: Controvérsias ──────────────────────

    private static void ValidateDispute(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        if (profile.DisputeRule.Mode == DisputeResolutionMode.NaoDefinido)
            result.AddError("DAG_100", "O modo de solução de controvérsias deve ser definido.", nameof(profile.DisputeRule.Mode));

        if ((profile.DisputeRule.Mode == DisputeResolutionMode.Foro ||
             profile.DisputeRule.Mode == DisputeResolutionMode.MediacaoEForo) &&
            string.IsNullOrWhiteSpace(profile.DisputeRule.ForoComarca))
        {
            result.AddError("DAG_101", "É necessário informar a comarca do foro.", nameof(profile.DisputeRule.ForoComarca));
        }

        if ((profile.DisputeRule.Mode == DisputeResolutionMode.Arbitragem ||
             profile.DisputeRule.Mode == DisputeResolutionMode.MediacaoEArbitragem) &&
            string.IsNullOrWhiteSpace(profile.DisputeRule.CamaraArbitral))
        {
            result.AddWarning("DAG_102", "Arbitragem sem câmara definida. Revisar antes da geração final.", nameof(profile.DisputeRule.CamaraArbitral));
        }
    }

    // ── Status ───────────────────────────────────────────

    private static void UpdateStatus(DestinoAgreementProfile profile, AgreementValidationResult result)
    {
        profile.BlockedBy = result.Messages
            .Where(x => x.Severity == AgreementValidationSeverity.Error)
            .Select(x => x.Code)
            .ToList();

        profile.Status = result.HasErrors
            ? AgreementStatus.Bloqueado
            : AgreementStatus.AptoParaGeracao;
    }
}

// ═══════════════════════════════════════════════════════════════
// RESOLVER DE MÓDULOS E CLÁUSULAS
// ═══════════════════════════════════════════════════════════════

public static class DestinoAgreementClauseResolver
{
    public static IReadOnlyList<string> ResolveModules(DestinoAgreementProfile profile)
    {
        var modules = new List<string>
        {
            "DESTINO.ACQ.MOD.01", // finalidade e vinculação
            "DESTINO.ACQ.MOD.02", // adesão obrigatória
            "DESTINO.ACQ.MOD.07", // administração e matérias reservadas
            "DESTINO.ACQ.MOD.08", // circulação de quotas
            "DESTINO.ACQ.MOD.09", // eventos familiares
            "DESTINO.ACQ.MOD.11", // saída e valuation
            "DESTINO.ACQ.MOD.12", // sanções
            "DESTINO.ACQ.MOD.13", // confidencialidade
            "DESTINO.ACQ.MOD.14", // arquivamento
            "DESTINO.ACQ.MOD.15"  // solução de controvérsias
        };

        if (profile.HaDoacao)
            modules.Add("DESTINO.ACQ.MOD.03");

        if (profile.HaUsufruto)
        {
            modules.Add("DESTINO.ACQ.MOD.04");
            modules.Add("DESTINO.ACQ.MOD.05");
            modules.Add("DESTINO.ACQ.MOD.06");
        }

        if (profile.RestrictiveClauses.Count > 0)
            modules.Add("DESTINO.ACQ.MOD.10");

        return modules;
    }
}
