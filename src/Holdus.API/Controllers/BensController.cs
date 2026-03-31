#nullable enable
using Holdus.Application.DTOs;
using Holdus.Domain.Entities;
using Holdus.Domain.Enums;
using Holdus.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holdus.API.Controllers;

// ═══════════════════════════════════════════════════════════════
// BENS — CRUD dentro do contexto do projeto
// ═══════════════════════════════════════════════════════════════

[ApiController]
[Route("api/projetos/{projetoId:int}/bens")]
[Authorize]
public class BensController : ControllerBase
{
    private readonly AppDbContext _db;

    public BensController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar(int projetoId)
    {
        var participanteIds = await _db.Participantes
            .Where(p => p.ProjetoId == projetoId)
            .Select(p => p.PessoaFisicaId)
            .ToListAsync();

        var bens = await _db.Bens.AsNoTracking()
            .Include(b => b.Proprietario)
            .Include(b => b.CelulaDestino)
            .Where(b => participanteIds.Contains(b.ProprietarioId))
            .OrderBy(b => b.Tipo).ThenBy(b => b.Descricao)
            .Select(b => new BemResumoResponse(
                b.Id, b.Tipo, b.Descricao, b.ValorDeclaracaoIR, b.ValorMercado,
                b.Proprietario.Nome, b.CelulaDestinoId, b.CelulaDestino != null ? b.CelulaDestino.NomeCelula : null
            ))
            .ToListAsync();

        var totais = new
        {
            TotalDeclaradoIR = bens.Sum(b => b.ValorDeclaracaoIR),
            TotalMercado = bens.Sum(b => b.ValorMercado),
            DiferencaItbi = bens.Sum(b => b.ValorMercado - b.ValorDeclaracaoIR),
            TotalImoveis = bens.Count(b => b.Tipo == TipoBem.Imovel),
            TotalParticipacoes = bens.Count(b => b.Tipo == TipoBem.ParticipacaoSocietaria),
        };

        return Ok(new ApiResponse<object>(true, new { bens, totais }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalhe(int projetoId, int id)
    {
        var bem = await _db.Bens
            .Include(b => b.Proprietario)
            .Include(b => b.CelulaDestino)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bem == null) return NotFound(new ApiResponse<object>(false, null, "Bem não encontrado"));

        var dto = new BemDetalheResponse(
            bem.Id, bem.ProprietarioId, bem.Proprietario.Nome,
            bem.Tipo, bem.Descricao, bem.ValorDeclaracaoIR, bem.ValorMercado,
            bem.ValorMercado - bem.ValorDeclaracaoIR,
            bem.DataAvaliacao, bem.DadosEspecificosJson,
            bem.CelulaDestinoId, bem.CelulaDestino?.NomeCelula,
            bem.Observacoes, bem.CreatedAt
        );

        return Ok(new ApiResponse<BemDetalheResponse>(true, dto));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(int projetoId, [FromBody] BemCreateRequest req)
    {
        // Verificar se proprietário está no projeto
        var noProj = await _db.Participantes
            .AnyAsync(p => p.ProjetoId == projetoId && p.PessoaFisicaId == req.ProprietarioId);

        if (!noProj)
            return BadRequest(new ApiResponse<object>(false, null, "Proprietário não é participante deste projeto"));

        var bem = new Bem
        {
            ProprietarioId = req.ProprietarioId,
            Tipo = req.Tipo,
            Descricao = req.Descricao,
            ValorDeclaracaoIR = req.ValorDeclaracaoIR,
            ValorMercado = req.ValorMercado,
            DataAvaliacao = req.DataAvaliacao,
            DadosEspecificosJson = req.DadosEspecificosJson,
            CelulaDestinoId = req.CelulaDestinoId,
            Observacoes = req.Observacoes
        };

        _db.Bens.Add(bem);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Detalhe), new { projetoId, id = bem.Id },
            new ApiResponse<object>(true, new { bem.Id }, "Bem cadastrado"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int projetoId, int id, [FromBody] BemUpdateRequest req)
    {
        var bem = await _db.Bens.FindAsync(id);
        if (bem == null) return NotFound(new ApiResponse<object>(false, null, "Bem não encontrado"));

        bem.Descricao = req.Descricao;
        bem.ValorDeclaracaoIR = req.ValorDeclaracaoIR;
        bem.ValorMercado = req.ValorMercado;
        bem.DataAvaliacao = req.DataAvaliacao;
        bem.DadosEspecificosJson = req.DadosEspecificosJson;
        bem.CelulaDestinoId = req.CelulaDestinoId;
        bem.Observacoes = req.Observacoes;

        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, new { bem.Id }, "Bem atualizado"));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int projetoId, int id)
    {
        var bem = await _db.Bens.FindAsync(id);
        if (bem == null) return NotFound(new ApiResponse<object>(false, null, "Bem não encontrado"));

        bem.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, null, "Bem excluído"));
    }
}

// ═══════════════════════════════════════════════════════════════
// CÉLULAS — CRUD das holdings dentro do projeto
// ═══════════════════════════════════════════════════════════════

[ApiController]
[Route("api/projetos/{projetoId:int}/celulas")]
[Authorize]
public class CelulasController : ControllerBase
{
    private readonly AppDbContext _db;

    public CelulasController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar(int projetoId)
    {
        var celulas = await _db.Celulas.AsNoTracking()
            .Include(c => c.Socios)
            .Where(c => c.ProjetoId == projetoId)
            .OrderBy(c => c.Tipo)
            .Select(c => new CelulaResumoResponse(
                c.Id, c.Tipo, c.NomeCelula, c.Status, c.CapitalSocialPrevisto, c.Cnpj, c.Socios.Count
            ))
            .ToListAsync();

        return Ok(new ApiResponse<List<CelulaResumoResponse>>(true, celulas));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalhe(int projetoId, int id)
    {
        var c = await _db.Celulas
            .Include(x => x.Socios).ThenInclude(x => x.PessoaFisica)
            .Include(x => x.Administrador)
            .FirstOrDefaultAsync(x => x.Id == id && x.ProjetoId == projetoId);

        if (c == null) return NotFound(new ApiResponse<object>(false, null, "Célula não encontrada"));

        var dto = new CelulaDetalheResponse(
            c.Id, c.Tipo, c.NomeCelula, c.Status, c.CapitalSocialPrevisto, c.CapitalSocialEfetivo,
            c.Cnpj, c.RazaoSocial, c.Nire, c.DataRegistro, c.ObjetoSocial,
            c.AdministradorId, c.Administrador?.Nome, c.Observacoes,
            c.Socios.Select(s => new SocioCelulaResponse(
                s.Id, s.PessoaFisicaId, s.PessoaFisica.Nome, s.QuantidadeQuotas, s.ValorPorQuota,
                s.PercentualParticipacao, s.TipoQuota, s.PesoVoto, s.UsufrutoVitalicio
            )).ToList(),
            c.CreatedAt
        );

        return Ok(new ApiResponse<CelulaDetalheResponse>(true, dto));
    }

    [HttpPost]
    public async Task<IActionResult> Criar(int projetoId, [FromBody] CelulaCreateRequest req)
    {
        var projeto = await _db.Projetos.FindAsync(projetoId);
        if (projeto == null) return NotFound(new ApiResponse<object>(false, null, "Projeto não encontrado"));

        var celula = new Celula
        {
            ProjetoId = projetoId,
            Tipo = req.Tipo,
            NomeCelula = req.NomeCelula,
            ObjetoSocial = req.ObjetoSocial,
            CapitalSocialPrevisto = req.CapitalSocialPrevisto,
            AdministradorId = req.AdministradorId
        };

        _db.Celulas.Add(celula);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Detalhe), new { projetoId, id = celula.Id },
            new ApiResponse<object>(true, new { celula.Id }, "Célula criada"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int projetoId, int id, [FromBody] CelulaUpdateRequest req)
    {
        var celula = await _db.Celulas.FirstOrDefaultAsync(c => c.Id == id && c.ProjetoId == projetoId);
        if (celula == null) return NotFound(new ApiResponse<object>(false, null, "Célula não encontrada"));

        celula.NomeCelula = req.NomeCelula; celula.Status = req.Status;
        celula.Cnpj = req.Cnpj; celula.RazaoSocial = req.RazaoSocial;
        celula.Nire = req.Nire; celula.CapitalSocialEfetivo = req.CapitalSocialEfetivo;
        celula.DataRegistro = req.DataRegistro; celula.Observacoes = req.Observacoes;

        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, new { celula.Id }, "Célula atualizada"));
    }

    // ── Sócios da célula ─────────────────────────────────

    [HttpPost("{celulaId:int}/socios")]
    public async Task<IActionResult> AdicionarSocio(int projetoId, int celulaId, [FromBody] SocioCelulaAddRequest req)
    {
        var celula = await _db.Celulas.FirstOrDefaultAsync(c => c.Id == celulaId && c.ProjetoId == projetoId);
        if (celula == null) return NotFound(new ApiResponse<object>(false, null, "Célula não encontrada"));

        var socio = new SocioCelula
        {
            CelulaId = celulaId,
            PessoaFisicaId = req.PessoaFisicaId,
            QuantidadeQuotas = req.QuantidadeQuotas,
            ValorPorQuota = req.ValorPorQuota,
            PercentualParticipacao = req.PercentualParticipacao,
            TipoQuota = req.TipoQuota,
            PesoVoto = req.PesoVoto,
            UsufrutoVitalicio = req.UsufrutoVitalicio
        };

        _db.SociosCelulas.Add(socio);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Detalhe), new { projetoId, id = celulaId },
            new ApiResponse<object>(true, new { socio.Id }, "Sócio adicionado"));
    }

    [HttpDelete("{celulaId:int}/socios/{socioId:int}")]
    public async Task<IActionResult> RemoverSocio(int projetoId, int celulaId, int socioId)
    {
        var socio = await _db.SociosCelulas.FirstOrDefaultAsync(s => s.Id == socioId && s.CelulaId == celulaId);
        if (socio == null) return NotFound(new ApiResponse<object>(false, null, "Sócio não encontrado"));

        socio.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, null, "Sócio removido"));
    }
}
