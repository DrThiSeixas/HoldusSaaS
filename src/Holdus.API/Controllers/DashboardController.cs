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
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// KPIs do dashboard — projetos, patrimônio, receita, docs.
    /// Query filters de tenant aplicados automaticamente.
    /// </summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis()
    {
        var projetos = await _db.Projetos.ToListAsync();
        var ativos = projetos.Count(p => p.Status != StatusProjeto.Concluido && p.Status != StatusProjeto.Cancelado);
        var concluidos = projetos.Count(p => p.Status == StatusProjeto.Concluido);

        var totalPessoas = await _db.PessoasFisicas.CountAsync();
        var totalBens = await _db.Bens.CountAsync();

        var patrimonioTotal = await _db.Bens.SumAsync(b => b.ValorMercado);

        var docsGerados = await _db.DocumentosGerados.CountAsync();

        var receitaMes = await _db.Parcelas
            .Where(p => p.Status == StatusParcela.Paga
                     && p.DataPagamento != null
                     && p.DataPagamento.Value.Month == DateTime.Today.Month
                     && p.DataPagamento.Value.Year == DateTime.Today.Year)
            .SumAsync(p => p.ValorPago ?? 0);

        return Ok(new ApiResponse<object>(true, new
        {
            projetosAtivos = ativos,
            projetosConcluidos = concluidos,
            totalPessoas,
            totalBens,
            patrimonioTotal,
            docsGerados,
            receitaMes,
        }));
    }

    /// <summary>
    /// Pipeline — projetos agrupados por status para o Kanban.
    /// </summary>
    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipeline()
    {
        var projetos = await _db.Projetos
            .Select(p => new
            {
                p.Id,
                p.NomeProjeto,
                p.NomeFamilia,
                p.Status,
                p.ModeloEscolhido,
                PatrimonioEstimado = p.Celulas.Sum(c => c.CapitalSocialPrevisto),
                TotalCelulas = p.Celulas.Count(),
                TotalParticipantes = p.Participantes.Count(),
            })
            .ToListAsync();

        var pipeline = Enum.GetValues<StatusProjeto>()
            .Where(s => s != StatusProjeto.Cancelado)
            .Select(status => new
            {
                status = status.ToString(),
                label = status switch
                {
                    StatusProjeto.Captacao => "Captação",
                    StatusProjeto.SessaoViabilidade => "Sessão",
                    StatusProjeto.CroquiElaborado => "Croqui elaborado",
                    StatusProjeto.CroquiApresentado => "Croqui apresentado",
                    StatusProjeto.Contratado => "Contratado",
                    StatusProjeto.EmExecucao => "Em execução",
                    StatusProjeto.Concluido => "Concluído",
                    _ => status.ToString()
                },
                projetos = projetos.Where(p => p.Status == status),
                count = projetos.Count(p => p.Status == status),
            });

        return Ok(new ApiResponse<object>(true, pipeline));
    }

    /// <summary>
    /// Próximos vencimentos financeiros.
    /// </summary>
    [HttpGet("vencimentos")]
    public async Task<IActionResult> GetVencimentos([FromQuery] int dias = 30)
    {
        var limite = DateOnly.FromDateTime(DateTime.Today.AddDays(dias));

        var parcelas = await _db.Parcelas
            .Where(p => p.Status == StatusParcela.Pendente || p.Status == StatusParcela.Vencida)
            .Where(p => p.DataVencimento <= limite)
            .Include(p => p.Contrato)
                .ThenInclude(c => c.Projeto)
            .OrderBy(p => p.DataVencimento)
            .Take(10)
            .Select(p => new
            {
                p.Id,
                p.Numero,
                p.Descricao,
                p.Valor,
                p.DataVencimento,
                p.Status,
                projeto = p.Contrato.Projeto.NomeProjeto,
            })
            .ToListAsync();

        return Ok(new ApiResponse<object>(true, parcelas));
    }
}
