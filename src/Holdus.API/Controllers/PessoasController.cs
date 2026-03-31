#nullable enable
using Holdus.Application.DTOs;
using Holdus.Domain.Entities;
using Holdus.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holdus.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PessoasController : ControllerBase
{
    private readonly AppDbContext _db;

    public PessoasController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busca, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = _db.PessoasFisicas.AsNoTracking().Where(p => p.Ativo);

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(p => p.Nome.Contains(busca) || p.Cpf.Contains(busca));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Nome)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PessoaResponse(
                p.Id, p.Nome, p.Cpf, p.Email, p.Celular, p.DataNascimento,
                p.Profissao, p.Nacionalidade, p.EstadoCivil, p.RegimeBens,
                p.ConjugeId, p.Conjuge != null ? p.Conjuge.Nome : null,
                p.Cidade, p.Uf, p.Ativo,
                p.Participacoes.Count,
                p.CreatedAt
            ))
            .ToListAsync();

        return Ok(new ApiResponse<PagedResponse<PessoaResponse>>(true,
            new PagedResponse<PessoaResponse>(items, total, page, pageSize)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detalhe(int id)
    {
        var p = await _db.PessoasFisicas
            .Include(x => x.Conjuge)
            .Include(x => x.Participacoes).ThenInclude(x => x.Projeto)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return NotFound(new ApiResponse<object>(false, null, "Pessoa não encontrada"));

        var dto = new PessoaDetalheResponse(
            p.Id, p.Nome, p.Cpf, p.Rg, p.RgOrgaoEmissor,
            p.Email, p.Celular, p.DataNascimento, p.Profissao, p.Nacionalidade,
            p.EstadoCivil, p.RegimeBens, p.ConjugeId,
            p.Conjuge?.Nome, p.NomePai, p.NomeMae,
            p.Cep, p.Logradouro, p.Numero, p.Complemento, p.Bairro, p.Cidade, p.Uf,
            p.Observacoes, p.Ativo,
            p.Participacoes.Select(x => new ParticipacaoResumoResponse(
                x.ProjetoId, x.Projeto.NomeProjeto, x.Projeto.NomeFamilia, x.Papel
            )).ToList(),
            p.CreatedAt
        );

        return Ok(new ApiResponse<PessoaDetalheResponse>(true, dto));
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] PessoaCreateRequest req)
    {
        if (await _db.PessoasFisicas.AnyAsync(p => p.Cpf == req.Cpf))
            return Conflict(new ApiResponse<object>(false, null, "CPF já cadastrado"));

        var pessoa = new PessoaFisica
        {
            Nome = req.Nome, Cpf = req.Cpf, Email = req.Email, Celular = req.Celular,
            DataNascimento = req.DataNascimento, Profissao = req.Profissao,
            EstadoCivil = req.EstadoCivil, RegimeBens = req.RegimeBens, ConjugeId = req.ConjugeId,
            NomePai = req.NomePai, NomeMae = req.NomeMae,
            Cep = req.Cep, Logradouro = req.Logradouro, Numero = req.Numero,
            Complemento = req.Complemento, Bairro = req.Bairro, Cidade = req.Cidade, Uf = req.Uf,
            Observacoes = req.Observacoes
        };

        _db.PessoasFisicas.Add(pessoa);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Detalhe), new { id = pessoa.Id },
            new ApiResponse<object>(true, new { pessoa.Id }, "Pessoa criada"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] PessoaUpdateRequest req)
    {
        var pessoa = await _db.PessoasFisicas.FindAsync(id);
        if (pessoa == null) return NotFound(new ApiResponse<object>(false, null, "Pessoa não encontrada"));

        pessoa.Nome = req.Nome; pessoa.Email = req.Email; pessoa.Celular = req.Celular;
        pessoa.DataNascimento = req.DataNascimento; pessoa.Profissao = req.Profissao;
        pessoa.EstadoCivil = req.EstadoCivil; pessoa.RegimeBens = req.RegimeBens;
        pessoa.ConjugeId = req.ConjugeId;
        pessoa.NomePai = req.NomePai; pessoa.NomeMae = req.NomeMae;
        pessoa.Cep = req.Cep; pessoa.Logradouro = req.Logradouro; pessoa.Numero = req.Numero;
        pessoa.Complemento = req.Complemento; pessoa.Bairro = req.Bairro;
        pessoa.Cidade = req.Cidade; pessoa.Uf = req.Uf;
        pessoa.Observacoes = req.Observacoes;

        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, new { pessoa.Id }, "Pessoa atualizada"));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var pessoa = await _db.PessoasFisicas.FindAsync(id);
        if (pessoa == null) return NotFound(new ApiResponse<object>(false, null, "Pessoa não encontrada"));

        pessoa.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new ApiResponse<object>(true, null, "Pessoa excluída"));
    }
}
