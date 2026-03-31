#nullable enable
using System.Collections.Generic;
using System.Linq;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// VALIDADOR DA CÉLULA VEÍCULO
// Códigos VEI_001..060
//
// "A Veículo só deve entrar no sistema quando a tese
//  estiver absolutamente limpa."
// ═══════════════════════════════════════════════════════════════

public sealed class VeiculoProfileValidator
{
    public ValidationResult Validate(VeiculoProfile profile)
    {
        var result = new ValidationResult();

        ValidateIdentity(profile, result);
        ValidatePilar1_RegenciaSupletiva(profile, result);
        ValidatePilar2_ClassesQuotas(profile, result);
        ValidateSocios(profile, result);
        ValidatePilar3_ReservaCapital(profile, result);
        ValidatePilar4_ClausulaCall(profile, result);
        ValidatePilar5_AtosReflexos(profile, result);
        ValidateAdministracao(profile, result);

        UpdateStatus(profile, result);
        return result;
    }

    // ── VEI_001..005: Identidade ─────────────────────────

    private static void ValidateIdentity(VeiculoProfile profile, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(profile.NomeEmpresarial))
            result.AddError("VEI_001", "Nome empresarial da Veículo é obrigatório.");

        if (string.IsNullOrWhiteSpace(profile.UfJunta))
            result.AddError("VEI_002", "UF da Junta é obrigatória.");

        if (profile.IsAlteracao)
        {
            if (string.IsNullOrWhiteSpace(profile.Cnpj))
                result.AddError("VEI_003", "Em alteração, CNPJ é obrigatório.");
            if (string.IsNullOrWhiteSpace(profile.Nire))
                result.AddError("VEI_004", "Em alteração, NIRE é obrigatório.");
        }

        if (profile.CapitalSocial <= 0)
            result.AddError("VEI_005", "Capital social deve ser maior que zero.");
    }

    // ── VEI_010..011: Pilar 1 — Regência supletiva ──────

    private static void ValidatePilar1_RegenciaSupletiva(VeiculoProfile profile, ValidationResult result)
    {
        if (!profile.RegenciaSupletivaExpressa)
        {
            result.AddError("VEI_010",
                "BLOQUEIO PILAR 1: A Veículo exige regência supletiva expressa da Lei das S.A. " +
                "no contrato social (art. 1.053, parágrafo único, CC). Sem isso, quotas preferenciais " +
                "e reserva de capital não têm base jurídica defensável.");
        }

        if (profile.CnaePrincipal != "6462-0/00" && profile.RegenciaSupletivaExpressa)
            result.AddWarning("VEI_011", "CNAE diferente de 6462-0/00 com regência supletiva. Verificar coerência.");
    }

    // ── VEI_020..026: Pilar 2 — Classes de quotas ────────

    private static void ValidatePilar2_ClassesQuotas(VeiculoProfile profile, ValidationResult result)
    {
        if (!profile.ClassesQuotas.Any())
        {
            result.AddError("VEI_020", "A Veículo deve ter ao menos uma classe de quotas definida.");
            return;
        }

        if (!profile.TemPreferenciais)
            result.AddError("VEI_021",
                "BLOQUEIO PILAR 2: A Veículo deve ter quotas preferenciais para funcionar como " +
                "célula de comando. Sem elas, não há separação voto × titularidade econômica.");

        foreach (var classe in profile.ClassesQuotas)
        {
            if (classe.Quantidade <= 0)
                result.AddError("VEI_022", $"Classe '{classe.Descricao}': quantidade deve ser maior que zero.");

            if (classe.ValorNominalUnitario <= 0)
                result.AddError("VEI_023", $"Classe '{classe.Descricao}': valor nominal deve ser maior que zero.");

            if (classe.Classe == ClasseQuota.Preferencial)
            {
                if (classe.PesoVoto <= 1)
                    result.AddWarning("VEI_024",
                        $"Quota preferencial '{classe.Descricao}' com peso de voto ≤ 1. " +
                        "No fluxograma, o peso esperado é X+1 para garantir o comando.");

                if (string.IsNullOrWhiteSpace(classe.VinculacaoDescricao))
                    result.AddWarning("VEI_025",
                        $"Quota preferencial '{classe.Descricao}' sem descrição de vinculação. " +
                        "Deve estar vinculada aos donos do Cofre.");
            }
        }

        // Verificar se ordinárias espelham Destino
        if (profile.TotalQuotasOrdinarias <= 0)
            result.AddError("VEI_026", "A Veículo deve ter quotas ordinárias espelhando a distribuição da Destino.");

        // Modo de voto das ordinárias
        if (profile.VotoOrdinarias == VotoOrdinariaMode.NaoDefinido)
            result.AddError("VEI_027", "O modo de voto das quotas ordinárias deve ser definido (residual, limitado, ou sem voto em controle).");
    }

    // ── VEI_030..033: Sócios ─────────────────────────────

    private static void ValidateSocios(VeiculoProfile profile, ValidationResult result)
    {
        if (!profile.Socios.Any())
        {
            result.AddError("VEI_030", "Deve haver ao menos um sócio.");
            return;
        }

        foreach (var socio in profile.Socios)
        {
            if (string.IsNullOrWhiteSpace(socio.Cpf))
                result.AddError("VEI_031", $"Sócio {socio.NomeCompleto} sem CPF.");
        }

        // Deve haver ao menos um detentor do Cofre com preferenciais
        if (!profile.Socios.Any(s => s.EhDetentorCofre && s.QuotasPreferenciais > 0))
            result.AddError("VEI_032",
                "Nenhum sócio é detentor do Cofre com quotas preferenciais. " +
                "O comando deve estar com os instituidores.");

        var totalPct = profile.Socios.Sum(s => s.ParticipacaoPercentual);
        if (profile.Socios.Any() && System.Math.Abs(totalPct - 100m) > 0.01m)
            result.AddWarning("VEI_033", $"Percentuais somam {totalPct:F2}%, devem somar 100%.");
    }

    // ── VEI_040..042: Pilar 3 — Reserva de capital ───────

    private static void ValidatePilar3_ReservaCapital(VeiculoProfile profile, ValidationResult result)
    {
        if (!profile.TemReservaCapital)
            return;

        if (!profile.RegenciaSupletivaExpressa)
            result.AddError("VEI_040",
                "BLOQUEIO PILAR 3: Reserva de capital sem regência supletiva expressa. " +
                "Art. 13, §2º, Lei 6.404 só se aplica com base contratual técnica.");

        if (profile.ValorReservaCapital <= 0)
            result.AddError("VEI_041", "Valor da reserva de capital deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(profile.DescricaoReserva))
            result.AddWarning("VEI_042", "Reserva de capital sem descrição. Documentar a origem do excedente.");
    }

    // ── VEI_050..053: Pilar 4 — Cláusula de call ─────────

    private static void ValidatePilar4_ClausulaCall(VeiculoProfile profile, ValidationResult result)
    {
        if (!profile.TemClausulaCall)
        {
            result.AddWarning("VEI_050",
                "Veículo sem cláusula de call. No fluxograma, o call é essencial para " +
                "continuidade do controle em caso de falecimento.");
            return;
        }

        var call = profile.ClausulaCall;
        if (call == null)
        {
            result.AddError("VEI_051", "Cláusula de call marcada mas não configurada.");
            return;
        }

        if (call.EventoGatilho == CallEventoGatilho.NaoDefinido)
            result.AddError("VEI_052",
                "BLOQUEIO PILAR 4: Cláusula de call sem evento-gatilho definido.");

        if (call.EventoGatilho == CallEventoGatilho.Personalizado &&
            string.IsNullOrWhiteSpace(call.DescricaoEventoPersonalizado))
            result.AddError("VEI_053", "Evento personalizado sem descrição.");
    }

    // ── VEI_055..058: Pilar 5 — Atos reflexos ──────────

    private static void ValidatePilar5_AtosReflexos(VeiculoProfile profile, ValidationResult result)
    {
        if (!profile.AtoReflexoCofre)
            result.AddError("VEI_055",
                "BLOQUEIO PILAR 5: A Veículo exige alteração reflexa na Cofre " +
                "para deslocar a titularidade das quotas. Sem isso, a estrutura fica incompleta.");

        if (!profile.CompraOrdinariasPelaDestino)
            result.AddWarning("VEI_056",
                "Compra de ordinárias pela Destino não marcada. No fluxograma, " +
                "essa é a etapa que desloca a titularidade econômica.");

        // Trava 4: coerência da compra
        if (profile.CompraOrdinariasPelaDestino && profile.CompraOrdinariosInfo != null)
        {
            if (!profile.CompraOrdinariosInfo.PrecoCoerente)
                result.AddError("VEI_056B",
                    "TRAVA: Compra de ordinárias pela Destino sem critério de preço definido.");
        }

        if (profile.CofreId == null)
            result.AddWarning("VEI_057", "Veículo sem referência ao Cofre vinculado.");

        if (profile.DestinoId == null)
            result.AddWarning("VEI_058", "Veículo sem referência à Destino vinculada.");
    }

    // ── VEI_060..062: Administração + Matérias reservadas ─

    private static void ValidateAdministracao(VeiculoProfile profile, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(profile.AdministradorNome))
            result.AddError("VEI_060", "A Veículo precisa de administrador definido.");

        // Trava 2: direitos da preferencial definidos
        if (profile.TemPreferenciais && !profile.MateriasReservadasPreferencial.Any())
            result.AddError("VEI_061",
                "BLOQUEIO TRAVA 2: A Veículo tem quotas preferenciais mas nenhuma " +
                "matéria reservada à preferencial foi definida. Sem isso, o contrato não fecha.");

        // Regra de ouro: ordinária = econômico, preferencial = político
        var preferenciais = profile.ClassesQuotas.Where(c => c.Classe == ClasseQuota.Preferencial);
        foreach (var pref in preferenciais)
        {
            if (pref.PesoVoto <= 1)
                result.AddWarning("VEI_062",
                    $"Quota preferencial '{pref.Descricao}' com peso de voto ≤ 1. " +
                    "A regra de ouro: preferencial = núcleo político de controle.");
        }

        // Trava 5: incoerência estrutural — economia × comando
        if (profile.TemPreferenciais && profile.VotoOrdinarias == VotoOrdinariaMode.NaoDefinido)
            result.AddError("VEI_063",
                "TRAVA 5: Sem matriz clara entre economia e comando. " +
                "Defina o modo de voto das ordinárias para que a separação fique coerente.");
    }

    // ── Status ───────────────────────────────────────────

    private static void UpdateStatus(VeiculoProfile profile, ValidationResult result)
    {
        profile.BlockedBy = result.Messages
            .Where(x => x.Severity == ValidationSeverity.Error)
            .Select(x => x.Code)
            .ToList();

        profile.Status = result.HasErrors
            ? VeiculoStatus.Bloqueado
            : VeiculoStatus.AptoParaGeracao;
    }
}
