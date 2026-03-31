using Holdus.Application.DTOs;
using Holdus.Domain.Enums;
using Holdus.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holdus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TributarioController : ControllerBase
{
    private readonly AppDbContext _db;

    public TributarioController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Calcula impostos nos dois regimes para um faturamento dado.
    /// Usa configurações fiscais do tenant (ISS, regime).
    /// </summary>
    [HttpGet("simular")]
    public async Task<IActionResult> Simular(
        [FromQuery] decimal faturamentoMes,
        [FromQuery] decimal? rbt12 = null,
        [FromQuery] decimal? issAliquota = null)
    {
        // Buscar config fiscal do tenant
        var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
            return Unauthorized();

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null) return NotFound();

        var iss = issAliquota ?? tenant.IssAliquota;
        var rbt = rbt12 ?? faturamentoMes * 12;

        // Simples Nacional — Anexo IV
        var simples = CalcularSimples(faturamentoMes, rbt);

        // Lucro Presumido
        var presumido = CalcularPresumido(faturamentoMes, iss);

        var economia = simples.ImpostoMensal.HasValue
            ? (presumido.Total - simples.ImpostoMensal.Value) * 12
            : (decimal?)null;

        return Ok(new ApiResponse<object>(true, new
        {
            simplesNacional = simples,
            lucroPresumido = presumido,
            economiaAnual = economia,
            melhorRegime = economia > 0 ? "SimplesNacional" : "LucroPresumido",
            configTenant = new { tenant.RegimeTributario, tenant.IssAliquota, tenant.Municipio },
        }));
    }

    /// <summary>
    /// Retorna faturamento mensal real das NFS-e emitidas (últimos 12 meses).
    /// </summary>
    [HttpGet("faturamento")]
    public async Task<IActionResult> GetFaturamento()
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        var inicio = hoje.AddMonths(-12);

        var notas = await _db.NotasFiscais
            .Where(n => n.Status == StatusNotaFiscal.Emitida)
            .Where(n => n.EmitidaEm != null)
            .Select(n => new { n.ValorServico, Mes = n.EmitidaEm!.Value.Month, Ano = n.EmitidaEm!.Value.Year })
            .ToListAsync();

        var meses = Enumerable.Range(0, 12)
            .Select(i => hoje.AddMonths(-11 + i))
            .Select(d => new
            {
                mes = d.Month,
                ano = d.Year,
                label = d.ToString("MMM/yy"),
                faturamento = notas
                    .Where(n => n.Mes == d.Month && n.Ano == d.Year)
                    .Sum(n => n.ValorServico),
            });

        var rbt12 = meses.Sum(m => m.faturamento);

        return Ok(new ApiResponse<object>(true, new { meses, rbt12 }));
    }

    // ═══════════════════════════════════════════════════════════
    // CÁLCULOS (Simples Nacional Anexo IV + Lucro Presumido)
    // ═══════════════════════════════════════════════════════════

    private record SimplesResult(
        decimal? AliquotaEfetiva,
        decimal? ImpostoMensal,
        string? Faixa,
        decimal? AliquotaNominal,
        decimal? Deducao,
        bool ExcedeuLimite
    );

    private static readonly (decimal ate, decimal aliq, decimal ded)[] TabelaAnexoIV =
    [
        (180_000m,   4.50m, 0m),
        (360_000m,   9.00m, 8_100m),
        (720_000m,   10.70m, 14_220m),
        (1_800_000m, 14.30m, 39_780m),
        (3_600_000m, 19.00m, 183_780m),
        (4_800_000m, 22.90m, 324_540m),
    ];

    private static SimplesResult CalcularSimples(decimal fatMes, decimal rbt12)
    {
        if (rbt12 > 4_800_000m)
            return new(null, null, null, null, null, true);

        if (rbt12 <= 0)
            return new(0, 0, "1ª Faixa", 4.50m, 0, false);

        var faixa = TabelaAnexoIV.First(f => rbt12 <= f.ate);
        var ae = ((rbt12 * faixa.aliq / 100m) - faixa.ded) / rbt12 * 100m;
        var imposto = fatMes * ae / 100m;

        return new(
            Math.Round(ae, 4),
            Math.Round(imposto, 2),
            $"Até {faixa.ate:N0}",
            faixa.aliq,
            faixa.ded,
            false
        );
    }

    private record PresumidoResult(
        decimal Irpj, decimal AdicionalIrpj, decimal Csll,
        decimal Pis, decimal Cofins, decimal Iss,
        decimal Total, decimal AliquotaEfetiva, decimal BasePresumida
    );

    private static PresumidoResult CalcularPresumido(decimal fatMes, decimal issAliq)
    {
        var bp = fatMes * 0.32m;
        var irpj = bp * 0.15m;
        var adicIrpj = bp > 20_000m ? (bp - 20_000m) * 0.10m : 0m;
        var csll = bp * 0.09m;
        var pis = fatMes * 0.0065m;
        var cofins = fatMes * 0.03m;
        var iss = fatMes * issAliq / 100m;
        var total = irpj + adicIrpj + csll + pis + cofins + iss;
        var ae = fatMes > 0 ? total / fatMes * 100m : 0m;

        return new(
            Math.Round(irpj, 2), Math.Round(adicIrpj, 2), Math.Round(csll, 2),
            Math.Round(pis, 2), Math.Round(cofins, 2), Math.Round(iss, 2),
            Math.Round(total, 2), Math.Round(ae, 4), Math.Round(bp, 2)
        );
    }
}
