using Grpc.Net.Client;

namespace ScraperApi.GrpcClient;
using Produto;

public record ProdutoDtoModel(string Titulo, string Preco);

public interface IPythonScraperClient
{
    Task<IEnumerable<ProdutoDtoModel>> BuscarProdutosDoScraperAsync();
}

public class PythonScraperClient : IPythonScraperClient
{
    private readonly IConfiguration _configuration;

    public PythonScraperClient(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task<IEnumerable<ProdutoDtoModel>> BuscarProdutosDoScraperAsync()
    {
        
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);

        var pythonUrl = _configuration["PythonGrpcSettings:Url"] ?? "http://localhost:50051";
        
        using var chanel = GrpcChannel.ForAddress(pythonUrl);
        var client = new ScraperService.ScraperServiceClient(chanel);

        var response = await client.ObterProdutosAsync(new VazioRequest());

        return response.Produtos.Select(p => new ProdutoDtoModel(p.Titulo, p.Preco));

    }
}