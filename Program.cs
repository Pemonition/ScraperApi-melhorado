using ScraperApi.GrpcClients;
using ScraperApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<IPythonScraperClient, PythonScraperClient>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();