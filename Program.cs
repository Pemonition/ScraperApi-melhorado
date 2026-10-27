using Produto;
using ScraperApi.GrpcClient;
using ScraperApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Plataformas como Render/Koyeb/Cloud Run informam a porta pela variavel PORT
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.Configure<PythonGrpcSettings>(builder.Configuration.GetSection("PythonGrpcSettings"));

// Client factory: reaproveita o canal HTTP/2 em vez de abrir um canal novo a cada requisicao
builder.Services.AddGrpcClient<ScraperService.ScraperServiceClient>(options =>
{
    var url = builder.Configuration["PythonGrpcSettings:Url"] ?? "http://localhost:50051";
    options.Address = new Uri(url);
});

builder.Services.AddScoped<IPythonScraperClient, PythonScraperClient>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();

var app = builder.Build();

// Swagger fica ligado tambem em producao para facilitar testar a API na nuvem
app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    // Na nuvem o HTTPS e terminado pelo proxy da plataforma
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
