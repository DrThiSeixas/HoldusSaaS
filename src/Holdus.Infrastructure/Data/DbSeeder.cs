using Holdus.Domain.Entities;
using Holdus.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holdus.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Só faz seed se não tem nenhum tenant
        if (await db.Tenants.AnyAsync()) return;

        // ═══════════════════════════════════════════════════════
        // TENANT DEMO
        // ═══════════════════════════════════════════════════════

        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var tenant = new Tenant
        {
            Id = tenantId,
            Nome = "Thiago Seixas Advocacia Empresarial",
            Slug = "thiago-seixas",
            CnpjEscritorio = "00.000.000/0001-00",
            Plano = PlanoTenant.Pro,
            MaxUsuarios = 10,
            MaxProjetos = 50,
            StorageLimiteMb = 10240,
            RegimeTributario = RegimeTributario.SimplesNacional,
            IssAliquota = 5.0m,
            Municipio = "São João da Boa Vista",
            Uf = "SP",
            Ativo = true,
            ConfiguracoesJson = System.Text.Json.JsonSerializer.Serialize(GetDefaultConfigs()),
        };

        db.Tenants.Add(tenant);

        // ═══════════════════════════════════════════════════════
        // USUÁRIO ADMIN
        // ═══════════════════════════════════════════════════════

        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        db.Usuarios.Add(new Usuario
        {
            Id = userId,
            TenantId = tenantId,
            NomeCompleto = "Thiago Seixas",
            Email = "thiago@holdus.com.br",
            Role = RoleUsuario.Admin,
            OabNumero = "000.000",
            OabUf = "SP",
            IdentityUserId = "seed-admin-identity",
            Ativo = true,
        });

        // ═══════════════════════════════════════════════════════
        // TEMPLATES PADRÃO
        // ═══════════════════════════════════════════════════════

        var templatesSeed = new[]
        {
            new TemplateDocumento
            {
                TenantId = tenantId,
                Nome = "Contrato social — Holding patrimonial Ltda",
                Categoria = CategoriaTemplate.Constituicao,
                Versao = 1,
                IsPublico = false,
                ConteudoHtml = GetContratoSocialTemplate(),
                PlaceholdersJson = "[\"empresa.razaoSocial\",\"empresa.capitalSocial\",\"empresa.objetoSocial\",\"empresa.cidade\",\"empresa.uf\",\"socios.nome\",\"socios.cpf\",\"socios.rg\",\"socios.profissao\",\"socios.endereco\",\"socios.quotas\",\"socios.percentual\"]",
            },
            new TemplateDocumento
            {
                TenantId = tenantId,
                Nome = "Alteração contratual — Cessão de quotas",
                Categoria = CategoriaTemplate.Alteracao,
                Versao = 1,
                IsPublico = false,
                ConteudoHtml = "<p>Modelo de alteração contratual para cessão e transferência de quotas sociais.</p>",
            },
            new TemplateDocumento
            {
                TenantId = tenantId,
                Nome = "Ata de reunião de sócios",
                Categoria = CategoriaTemplate.Ata,
                Versao = 1,
                IsPublico = false,
                ConteudoHtml = "<p>Modelo de ata de reunião de sócios quotistas.</p>",
            },
            new TemplateDocumento
            {
                TenantId = tenantId,
                Nome = "Procuração societária ad judicia et extra",
                Categoria = CategoriaTemplate.Procuracao,
                Versao = 1,
                IsPublico = false,
                ConteudoHtml = "<p>Procuração para representação em atos societários.</p>",
            },
            new TemplateDocumento
            {
                TenantId = tenantId,
                Nome = "Contrato de honorários advocatícios",
                Categoria = CategoriaTemplate.Honorarios,
                Versao = 1,
                IsPublico = false,
                ConteudoHtml = "<p>Contrato de prestação de serviços advocatícios para constituição de holding.</p>",
            },
            new TemplateDocumento
            {
                TenantId = tenantId,
                Nome = "Instrumento de doação com reserva de usufruto",
                Categoria = CategoriaTemplate.Doacao,
                Versao = 1,
                IsPublico = false,
                ConteudoHtml = "<p>Instrumento particular de doação de quotas sociais com reserva de usufruto vitalício.</p>",
            },
        };

        db.Templates.AddRange(templatesSeed);

        // ═══════════════════════════════════════════════════════
        // CLÁUSULAS PADRÃO
        // ═══════════════════════════════════════════════════════

        var clausulasSeed = new[]
        {
            new Clausula { TenantId = tenantId, Titulo = "Inalienabilidade vitalícia das quotas", Categoria = CategoriaClausula.Sucessoria, TextoHtml = "<p>As quotas sociais são inalienáveis e impenhoráveis enquanto viver o(a) doador(a).</p>", Tags = ["inalienabilidade", "proteção"], IsPublica = false },
            new Clausula { TenantId = tenantId, Titulo = "Incomunicabilidade de quotas", Categoria = CategoriaClausula.Sucessoria, TextoHtml = "<p>As quotas sociais não se comunicam com o patrimônio do cônjuge ou companheiro(a).</p>", Tags = ["incomunicabilidade", "casamento"], IsPublica = false },
            new Clausula { TenantId = tenantId, Titulo = "Reserva de usufruto vitalício", Categoria = CategoriaClausula.Sucessoria, TextoHtml = "<p>Os doadores reservam para si o usufruto vitalício das quotas doadas.</p>", Tags = ["usufruto", "doação"], IsPublica = false },
            new Clausula { TenantId = tenantId, Titulo = "Administração exclusiva do constituinte", Categoria = CategoriaClausula.Governanca, TextoHtml = "<p>A administração da sociedade será exercida exclusivamente pelo(a) sócio(a) administrador(a).</p>", Tags = ["administração", "governança"], IsPublica = false },
            new Clausula { TenantId = tenantId, Titulo = "Distribuição desproporcional de lucros", Categoria = CategoriaClausula.DistribuicaoLucros, TextoHtml = "<p>Os lucros poderão ser distribuídos de forma desproporcional à participação societária.</p>", Tags = ["lucros", "distribuição"], IsPublica = false },
            new Clausula { TenantId = tenantId, Titulo = "Cláusula de call option", Categoria = CategoriaClausula.CallPut, TextoHtml = "<p>O sócio administrador terá direito de compra compulsória das quotas dos demais sócios.</p>", Tags = ["call", "opção", "compra"], IsPublica = false },
        };

        db.Clausulas.AddRange(clausulasSeed);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// 31 configurações padrão do escritório (tributárias, cartoriais, honorários).
    /// </summary>
    private static Dictionary<string, Dictionary<string, string>> GetDefaultConfigs() => new()
    {
        ["Tributario"] = new()
        {
            ["itcmd_aliquota_sp"] = "4.00",
            ["itcmd_aliquota_progressiva"] = "false",
            ["irpf_isencao_65anos"] = "true",
            ["ganho_capital_aliquota"] = "15.00",
            ["itbi_aliquota_media"] = "3.00",
            ["simples_anexo"] = "IV",
            ["cofins_aliquota"] = "3.00",
            ["pis_aliquota"] = "0.65",
            ["csll_presuncao"] = "32.00",
            ["irpj_presuncao"] = "32.00",
        },
        ["Cartorio"] = new()
        {
            ["custo_registro_contrato_social"] = "500.00",
            ["custo_alteracao_contratual"] = "350.00",
            ["custo_procuracao"] = "150.00",
            ["custo_certidao_negativa"] = "80.00",
            ["custo_autenticacao_por_folha"] = "8.50",
            ["custo_reconhecimento_firma"] = "12.00",
            ["custo_escritura_publica"] = "1200.00",
        },
        ["Honorarios"] = new()
        {
            ["honorario_minimo_basico"] = "15000.00",
            ["honorario_minimo_dupla"] = "25000.00",
            ["honorario_minimo_triade"] = "40000.00",
            ["percentual_sinal"] = "20.00",
            ["percentual_entrada"] = "30.00",
            ["percentual_minutas"] = "30.00",
            ["percentual_final"] = "20.00",
            ["desconto_resolvedores"] = "10.00",
            ["parcelas_maximo"] = "6",
        },
        ["Sistema"] = new()
        {
            ["moeda"] = "BRL",
            ["fuso_horario"] = "America/Sao_Paulo",
            ["formato_data"] = "dd/MM/yyyy",
            ["backup_automatico"] = "true",
            ["notificacao_vencimento_dias"] = "7",
        },
    };

    private static string GetContratoSocialTemplate() =>
        """
        <h1 style="text-align:center">CONTRATO SOCIAL</h1>
        <h2 style="text-align:center">{{empresa.razaoSocial}}</h2>
        <p>Pelo presente instrumento particular, os abaixo qualificados:</p>
        {{#each socios}}
        <p><strong>{{this.nome}}</strong>, {{this.nacionalidade}}, {{this.estadoCivil}}, {{this.profissao}}, portador(a) da Cédula de Identidade RG nº {{this.rg}}, inscrito(a) no CPF sob nº {{this.cpf}}, residente e domiciliado(a) em {{this.endereco}};</p>
        {{/each}}
        <p>resolvem constituir uma sociedade empresária limitada, que se regerá pelas cláusulas seguintes:</p>
        <h3>CLÁUSULA PRIMEIRA — DA DENOMINAÇÃO SOCIAL</h3>
        <p>A sociedade girará sob a denominação social de "{{empresa.razaoSocial}}", com sede na cidade de {{empresa.cidade}}, Estado de {{empresa.uf}}.</p>
        <h3>CLÁUSULA SEGUNDA — DO OBJETO SOCIAL</h3>
        <p>{{empresa.objetoSocial}}</p>
        <h3>CLÁUSULA TERCEIRA — DO CAPITAL SOCIAL</h3>
        <p>O capital social é de {{empresa.capitalSocial}}, dividido em {{empresa.totalQuotas}} quotas no valor de {{empresa.valorQuota}} cada uma, assim distribuídas:</p>
        {{#each socios}}
        <p>• {{this.nome}} — {{this.quotas}} quotas ({{this.percentual}}%)</p>
        {{/each}}
        <h3>CLÁUSULA QUARTA — DA ADMINISTRAÇÃO</h3>
        <p>{{clausula.administracao}}</p>
        <h3>CLÁUSULA QUINTA — DA DISTRIBUIÇÃO DE LUCROS</h3>
        <p>{{clausula.distribuicaoLucros}}</p>
        """;
}
