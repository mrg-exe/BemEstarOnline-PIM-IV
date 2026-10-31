using System.Security.Claims;
using System.Text;
using BemEstarOnline.Api.Aplicacao.Interfaces;
using BemEstarOnline.Api.Aplicacao.Servicos;
using BemEstarOnline.Api.Dominio.Interfaces;
using BemEstarOnline.Api.Infraestrutura.Arquivos;
using BemEstarOnline.Api.Infraestrutura.Automacoes;
using BemEstarOnline.Api.Infraestrutura.Dados;
using BemEstarOnline.Api.Infraestrutura.Email;
using BemEstarOnline.Api.Infraestrutura.Repositorios;
using BemEstarOnline.Api.Infraestrutura.Seguranca;
using BemEstarOnline.Api.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// =====================================================================
// 1) BANCO DE DADOS
//    UseInMemoryDatabase = true  -> modo fácil (nada para instalar, dados somem ao fechar)
//    UseInMemoryDatabase = false -> MySQL de verdade, com triggers e procedures
// =====================================================================
var usarMemoria = config.GetValue<bool>("UseInMemoryDatabase");
if (usarMemoria)
{
    builder.Services.AddDbContext<BemEstarDbContext>(o => o.UseInMemoryDatabase("BemEstarOnlineDb"));
    builder.Services.AddScoped<IAutomacoesBanco, AutomacoesEmMemoria>();
    builder.Services.AddScoped<IRelatorioRepositorio, RelatorioRepositorioEmMemoria>();
}
else
{
    var conexao = config.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection no appsettings.json.");
    // Versão fixa: assim a API não precisa conectar no MySQL só para descobrir a versão ao iniciar.
    builder.Services.AddDbContext<BemEstarDbContext>(o =>
        o.UseMySql(conexao, new MySqlServerVersion(new Version(8, 0, 36)),
                   mysql => mysql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null)));
    builder.Services.AddScoped<IAutomacoesBanco, AutomacoesViaTriggersMySql>();
    builder.Services.AddScoped<IRelatorioRepositorio, RelatorioRepositorioMySql>();
}

// =====================================================================
// 2) INJEÇÃO DE DEPENDÊNCIA (camadas)
// =====================================================================
// Infraestrutura
builder.Services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IEnderecoRepositorio, EnderecoRepositorio>();
builder.Services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
builder.Services.AddScoped<IProdutoRepositorio, ProdutoRepositorio>();
builder.Services.AddScoped<ICarrinhoRepositorio, CarrinhoRepositorio>();
builder.Services.AddScoped<ICupomRepositorio, CupomRepositorio>();
builder.Services.AddScoped<IPedidoRepositorio, PedidoRepositorio>();
builder.Services.AddScoped<IAssinaturaRepositorio, AssinaturaRepositorio>();
builder.Services.AddScoped<IDeliveryRepositorio, DeliveryRepositorio>();
builder.Services.AddScoped<IAlertaEstoqueRepositorio, AlertaEstoqueRepositorio>();
builder.Services.AddScoped<IOuvidoriaRepositorio, OuvidoriaRepositorio>();
builder.Services.AddScoped<IRedefinicaoSenhaRepositorio, RedefinicaoSenhaRepositorio>();
builder.Services.AddSingleton<ISenhaHasher, SenhaHasher>();
builder.Services.AddScoped<DadosIniciais>();

// Aplicação (casos de uso)
builder.Services.AddScoped<IAuthServico, AuthServico>();
builder.Services.AddScoped<ICatalogoServico, CatalogoServico>();
builder.Services.AddScoped<ICarrinhoServico, CarrinhoServico>();
builder.Services.AddScoped<IEnderecoServico, EnderecoServico>();
builder.Services.AddScoped<IPedidoServico, PedidoServico>();
builder.Services.AddScoped<IAssinaturaServico, AssinaturaServico>();
builder.Services.AddScoped<IDeliveryServico, DeliveryServico>();
builder.Services.AddScoped<IAdminServico, AdminServico>();
builder.Services.AddScoped<IEntregaServico, EntregaServico>();
builder.Services.AddScoped<IOuvidoriaServico, OuvidoriaServico>();
builder.Services.AddScoped<IEquipeServico, EquipeServico>();
builder.Services.AddScoped<IRecuperacaoSenhaServico, RecuperacaoSenhaServico>();

// E-mail (RF33): com a seção "Smtp" preenchida, envia de verdade; senão, salva em emails-enviados (demonstração).
var smtp = config.GetSection("Smtp").Get<SmtpOpcoes>() ?? new SmtpOpcoes();
if (string.IsNullOrWhiteSpace(smtp.Host)) builder.Services.AddSingleton<IEmailServico, EmailDemonstracao>();
else { builder.Services.AddSingleton(smtp); builder.Services.AddSingleton<IEmailServico, EmailSmtp>(); }
// Receitas enviadas (RF39): pasta privada, fora do wwwroot e do site (configurável em "PastaArquivosPrivados").
builder.Services.AddSingleton<IArquivosPrivados>(new ArquivosPrivados(
    config["PastaArquivosPrivados"] ?? Path.Combine(builder.Environment.ContentRootPath, "ArquivosPrivados")));
// Endereço do site usado nos links dos e-mails (nunca o endereço que veio na requisição).
builder.Services.AddSingleton(new ConfiguracaoSite(config["EnderecoDoSite"] ?? "http://localhost:5088"));

// =====================================================================
// 3) AUTENTICAÇÃO (JWT) E AUTORIZAÇÃO POR PERFIL
// =====================================================================
var jwt = config.GetSection("Jwt").Get<JwtOpcoes>() ?? new JwtOpcoes();
if (string.IsNullOrWhiteSpace(jwt.Chave) || jwt.Chave.Length < 32)
    throw new InvalidOperationException("A chave Jwt:Chave precisa ter pelo menos 32 caracteres.");
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<ITokenServico, TokenServico>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emissor,
            ValidateAudience = true,
            ValidAudience = jwt.Publico,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Chave)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

// =====================================================================
// 4) API, VALIDAÇÃO, CORS E SWAGGER
// =====================================================================
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        // Erros de validação no mesmo formato dos demais: { "mensagem": "..." }
        o.InvalidModelStateResponseFactory = contexto =>
        {
            var primeira = contexto.ModelState.Values.SelectMany(v => v.Errors)
                                   .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Dados inválidos." : e.ErrorMessage)
                                   .FirstOrDefault() ?? "Dados inválidos.";
            // Arquivo grande demais (ex.: receita do RF39): o ASP.NET para de ler e avisa em inglês.
            if (primeira.Contains("Request body too large", StringComparison.OrdinalIgnoreCase))
                primeira = "O arquivo é grande demais. A receita pode ter no máximo 5 MB.";
            return new BadRequestObjectResult(new { mensagem = primeira });
        };
    });

builder.Services.AddCors(o => o.AddPolicy("Clientes", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bem Estar Online API",
        Version = "v2",
        Description = "API da farmácia Bem Estar Online (PIM IV - UNIP). Use POST /api/auth/login, copie o token e clique em Authorize."
    });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Cole aqui somente o token (sem a palavra Bearer)."
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
    var xml = Path.Combine(AppContext.BaseDirectory, "BemEstarOnline.Api.xml");
    if (File.Exists(xml)) o.IncludeXmlComments(xml);
});

var app = builder.Build();

// =====================================================================
// 5) PREPARA O BANCO E CARREGA OS DADOS DE EXEMPLO
// =====================================================================
await PrepararBancoAsync(app, usarMemoria);

app.UseMiddleware<TratamentoDeErros>();
app.UseSwagger();
app.UseSwaggerUI(o => o.DocumentTitle = "Bem Estar Online API");
app.UseCors("Clientes");
app.UseStaticFiles(); // fotos dos produtos enviadas pelo painel (wwwroot/imagens/produtos)

// =====================================================================
// 6) SITE SERVIDO PELA PRÓPRIA API (RNF07 - "Desenvolvimento Web com .NET")
//    A pasta frontend/ abre em http://localhost:5088/ (Swagger continua em /swagger).
//    Abrir o index.html direto e o Nginx do Docker (porta 8080) continuam funcionando.
// =====================================================================
var pastaDoSite = LocalizarPastaDoSite(app);
if (pastaDoSite is not null)
{
    var arquivosDoSite = new PhysicalFileProvider(pastaDoSite);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = arquivosDoSite }); // "/" abre o index.html
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = arquivosDoSite,
        // O navegador sempre confere se o arquivo mudou (evita ver uma versão velha do site depois de atualizar).
        OnPrepareResponse = c => c.Context.Response.Headers.CacheControl = "no-cache"
    });

    // Aberto pela API, o site fala com a API no mesmo endereço (mesma origem).
    // Este endereço tem prioridade sobre o arquivo frontend/js/config.js, que continua
    // apontando para http://localhost:5088/api quando o site é aberto de outro jeito.
    app.MapGet("/js/config.js", () => Results.Content(
            "// Gerado pela API: o site foi aberto por ela, então a API está no mesmo endereço.\n" +
            "window.BEO_CONFIG = { apiBase: location.origin + '/api' };\n",
            "text/javascript; charset=utf-8"))
        .ExcludeFromDescription();

    app.Logger.LogInformation("Site servido pela API a partir de {Pasta}.", pastaDoSite);
}
else
{
    app.Logger.LogWarning("Pasta do site (frontend) não encontrada: a API vai mostrar só o Swagger.");
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseAuthentication();
app.UseMiddleware<VerificacaoDeConta>(); // conta desativada, perfil mudado ou senha provisória (RF35)
app.UseAuthorization();
app.MapControllers();

app.Run();

// Procura a pasta frontend: primeiro a configuração "PastaDoSite" (se existir), depois
// ../../frontend (rodando com dotnet run) e ./frontend (Docker e publicação no Azure).
static string? LocalizarPastaDoSite(WebApplication app)
{
    var raiz = app.Environment.ContentRootPath;
    var candidatas = new[]
    {
        app.Configuration["PastaDoSite"],
        Path.Combine(raiz, "..", "..", "frontend"),
        Path.Combine(raiz, "frontend"),
    };
    return candidatas
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Select(p => Path.GetFullPath(Path.Combine(raiz, p!)))
        .FirstOrDefault(p => File.Exists(Path.Combine(p, "index.html")));
}

static async Task PrepararBancoAsync(WebApplication app, bool usarMemoria)
{
    var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Inicializacao");
    const int maxTentativas = 12;

    // O MySQL do Docker pode demorar alguns segundos para aceitar conexões: tentamos algumas vezes.
    for (var tentativa = 1; tentativa <= maxTentativas; tentativa++)
    {
        try
        {
            using var escopo = app.Services.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<BemEstarDbContext>();

            if (usarMemoria)
                await db.Database.EnsureCreatedAsync();
            else
                await db.Categorias.AnyAsync(); // testa a conexão e se os scripts SQL já criaram as tabelas

            await escopo.ServiceProvider.GetRequiredService<DadosIniciais>().ExecutarAsync();
            log.LogInformation("Banco pronto ({Modo}).", usarMemoria ? "memória" : "MySQL");
            return;
        }
        catch (Exception ex) when (!usarMemoria)
        {
            if (tentativa == maxTentativas)
            {
                log.LogError(ex, "Não foi possível preparar o MySQL. Confira se ele está ligado e se os scripts da pasta database foram executados.");
                return; // a API sobe mesmo assim; /api/health vai mostrar "banco indisponível"
            }
            log.LogWarning("MySQL ainda não respondeu (tentativa {T}/{Max}): {Erro}", tentativa, maxTentativas, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
