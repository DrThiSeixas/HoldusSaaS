#nullable enable
using System.Collections.Generic;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// RESOLVER DE DOCUMENTOS DO COFRE
// Dado o estado do CofreProfile, determina quais documentos
// devem ser gerados.
//
// "Primeiro define qual célula. Depois qual ato."
// ═══════════════════════════════════════════════════════════════

public static class CofreDocumentResolver
{
    public static IReadOnlyList<GeneratedDocument> Resolve(CofreProfile profile)
    {
        var docs = new List<GeneratedDocument>();

        // ── Constituição (se não é alteração) ────────────
        if (!profile.IsAlteracao)
        {
            docs.Add(New("COFRE.CONSTITUICAO.V1", "Contrato Social Master da Célula Cofre"));
        }

        // ── Integralização com imóvel ────────────────────
        if (profile.TemIntegralizacaoImovel)
        {
            docs.Add(New("COFRE.ALT_CAPITAL_IMOVEL.V1", "Alteração Contratual de Aumento de Capital por Imóvel"));
            docs.Add(New("COFRE.CHECKLIST_REGISTRO.V1", "Checklist Registral do Cofre"));
            docs.Add(New("COFRE.NOTA_ITBI.V1", "Nota Técnica Interna de ITBI"));
        }

        // ── Integralização com participações ─────────────
        if (profile.TemIntegralizacaoParticipacoes)
        {
            docs.Add(New("COFRE.ALT_CAPITAL_PARTICIPACOES.V1", "Alteração Contratual de Integralização de Participações"));
            docs.Add(New("COFRE.ATO_REFLEXO_INVESTIDA.V1", "Ato Reflexo da Sociedade Investida"));
            docs.Add(New("COFRE.CHECKLIST_REGISTRO.V1", "Checklist Registral do Cofre"));
        }

        return docs;
    }

    private static GeneratedDocument New(string codigo, string nome)
    {
        return new GeneratedDocument
        {
            CodigoDocumento = codigo,
            NomeDocumento = nome,
            Gerado = false,
            Versao = "1.0"
        };
    }
}
