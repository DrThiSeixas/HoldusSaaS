#nullable enable
using Holdus.Application.DTOs;
using Holdus.Domain.Entities;
using Holdus.Domain.Enums;
using Holdus.Infrastructure.Data;
using Holdus.Infrastructure.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holdus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjetosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WorkflowInitializationService _wfInit;

    public ProjetosController(AppDbContext db, WorkflowInitializationService wfInit)
    {
        _db = db;
        _wfInit = wfInit;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.Projetos.AsNoTracking()
            .Include(p => p.Participantes)
            .Include(p => p.Celulas)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(p => p.NomeProjeto.Contains(busca) || p.NomeFamilia.Contains(busca));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProjetoListResponse(
                p.Id, p.Codigo, p.NomeProjeto, p.NomeFamilia,
                p.ModeloEscolhido, p.Status,
                p.Participantes.Count,
                p.Celulas.Count,
                _db.Bens.Count(b => b.Proprietario.Participacoes.Any(pp => pp.ProjetoId == p.Id)),
                _db.Bens.Where(b => b.Proprietario.Participacoes.Any(pp => pp.ProjetoId == p.Id))
                    .Sum(b => (decimal?)b.ValorMercado) ?? 0,
                null, null,
                p.UpdatedAt,
                p.CreatedAt
            ))
            .ToListAsync();

        return Ok(new ApiResponse<PagedResponse<ProjetoListResponse>>(true,
            new PagedResponse<ProjetoListResponse>(items, total, page, pageSize)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalhe(int id)
    {
        var p = await _db.Projetos
            .Include(x => x.Participantes).ThenInclude(x => x.PessoaFisica)
            .Include(x => x.Celulas).ThenInclude(x => x.Socios)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return NotFound(new ApiResponse<object>(false, null, "Projeto não encontrado"));

        // Buscar bens dos participantes
        var participanteIds = p.Participantes.Select(x => x.PessoaFisicaId).ToList();
        var bens = await _db.Bens.AsNoTracking()
            .Include(b => b.Proprietario)
            .Include(b => b.CelulaDestino)
            .Where(b => participanteIds.Contains(b.ProprietarioId))
            .ToListAsync();

        // Buscar workflow
        var wf = await _db.WorkflowInstances.AsNoTracking()
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w => w.ProjetoId == (Guid)(object)p.Id); // Pode ser null

        WorkflowResumoResponse? wfResumo = null;
        // Workflow resumo será montado no WorkflowController

        var dto = new ProjetoDetalheResponse(
            p.Id, p.Codigo, p.NomeProjeto, p.NomeFamilia,
            p.ModeloEscolhido, p.Status,
            p.DataContratacao, p.DataConclusao, p.Observacoes,
            p.Participantes.Select(x => new ParticipanteResponse(
                x.Id, x.PessoaFisicaId, x.PessoaFisica.Nome, x.PessoaFisica.Cpf, x.Papel, x.Observacoes
            )).ToList(),
            p.Celulas.Select(c => new CelulaResumoResponse(
                c.Id, c.Tipo, c.NomeCelula, c.Status, c.CapitalSocialPrevisto, c.Cnpj, c.Socios.Count
            )).ToList(),
            bens.Select(b => new BemResumoResponse(
                b.Id, b.Tipo, b.Descricao, b.ValorDeclaracaoIR, b.ValorMercado,
                b.Proprietario.Nome, b.CelulaDestinoId, b.CelulaDestino?.NomeCelula
            )).ToList(),
            wfResumo,
            p.CreatedAt
        );

        return Ok(new ApiResponse<ProjetoDetalheResponse>(true, dto));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] ProjetoCreateRequest req)
    {
        // Gerar código sequencial
        var count = await _db.Projetos.CountAsync() + 1;
        var codigo = $"PRJ-{DateTime.Now:yyyy}-{count:D3}";

        var projeto = new ProjetoTriade
        {
            Codigo = codigo,
            NomeProjeto = req.NomeProjeto,
            NomeFamilia = req.NomeFamilia,
            ModeloEscolhido = req.ModeloEscolhido,
            Status = StatusProjeto.Captacao,
            Observacoes = req.Observacoes
        };

        _db.Projetos.Add(projeto);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Detalhe), new { id = projeto.Id },
            new ApiResponse<object>(true, new { projeto.Id, projeto.Codigo }, "Projeto criado"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] ProjetoUpdateRequest req)
    {
        var projeto = await _db.Projetos.FindAsync(id);
        if (projeto == null) return NotFound(new ApiResponse<object>(false, null, "Projeto não encontrado"));

        projeto.NomeProjeto = req.NomeProjeto;
        projeto.NomeFamilia = req.NomeFamilia;
        projeto.Status = req.Status;
        projeto.Observacoes = req.Observacoes;

        if (req.Status == StatusProjeto.Contratado && !projeto.DataContratacao.HasValue)
            projeto.DataContratacao = DateTimeOffset.UtcNow;

        if (req.Status == StatusProjeto.Concluido && !projeto.DataConclusao.HasValue)
            projeto.DataConclusao = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, new { projeto.Id }, "Projeto atualizado"));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var projeto = await _db.Projetos.FindAsync(id);
        if (projeto == null) return NotFound(new ApiResponse<object>(false, null, "Projeto não encontrado"));

        projeto.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, null, "Projeto excluído"));
    }

    // ═══════════════════════════════════════════════════════════
    // PARTICIPANTES
    // ═══════════════════════════════════════════════════════════

    [HttpPost("{projetoId:int}/participantes")]
    public async Task<IActionResult> AdicionarParticipante(int projetoId, [FromBody] ParticipanteAddRequest req)
    {
        var projeto = await _db.Projetos.FindAsync(projetoId);
        if (projeto == null) return NotFound(new ApiResponse<object>(false, null, "Projeto não encontrado"));

        var pessoa = await _db.PessoasFisicas.FindAsync(req.PessoaFisicaId);
        if (pessoa == null) return NotFound(new ApiResponse<object>(false, null, "Pessoa não encontrada"));

        if (await _db.Participantes.AnyAsync(p => p.ProjetoId == projetoId && p.PessoaFisicaId == req.PessoaFisicaId))
            return Conflict(new ApiResponse<object>(false, null, "Pessoa já participa deste projeto"));

        var participante = new ParticipanteProjeto
        {
            ProjetoId = projetoId,
            PessoaFisicaId = req.PessoaFisicaId,
            Papel = req.Papel,
            Observacoes = req.Observacoes
        };

        _db.Participantes.Add(participante);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Detalhe), new { id = projetoId },
            new ApiResponse<object>(true, new { participante.Id }, "Participante adicionado"));
    }

    [HttpDelete("{projetoId:int}/participantes/{participanteId:int}")]
    public async Task<IActionResult> RemoverParticipante(int projetoId, int participanteId)
    {
        var p = await _db.Participantes
            .FirstOrDefaultAsync(x => x.ProjetoId == projetoId && x.Id == participanteId);
        if (p == null) return NotFound(new ApiResponse<object>(false, null, "Participante não encontrado"));

        p.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, null, "Participante removido"));
    }
}
