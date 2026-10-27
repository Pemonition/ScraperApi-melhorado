using Microsoft.Extensions.Options;

namespace ScraperApi.GrpcClient;
using Produto;

public record ProdutoDtoModel(string Titulo, string Preco);

public class PythonGrpcSettings
{
    public string Url { get; set; } = "http://localhost:50051";
    public int TimeoutSegundos { get; set; } = 20;
}

public interface IPythonScraperClient
{
    Task<IReadOnlyList<ProdutoDtoModel>> BuscarProdutosDoScraperAsync(CancellationToken cancellationToken = default);
}

public class PythonScraperClient : IPythonScraperClient
{
    private readonly ScraperService.ScraperServiceClient _client;
    private readonly PythonGrpcSettings _settings;

    public PythonScraperClient(ScraperService.ScraperServiceClient client, IOptions<PythonGrpcSettings> settings)
    {
        _client = client;
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<ProdutoDtoModel>> BuscarProdutosDoScraperAsync(CancellationToken cancellationToken = default)
    {
        // Deadline evita que a requisicao fique presa se o scraper travar
        var deadline = DateTime.UtcNow.AddSeconds(_settings.TimeoutSegundos);

        var response = await _client.ObterProdutosAsync(new VazioRequest(), deadline: deadline, cancellationToken: cancellationToken);

        return response.Produtos.Select(p => new ProdutoDtoModel(p.Titulo, p.Preco)).ToList();
    }
}
