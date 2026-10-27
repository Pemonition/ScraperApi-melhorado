using ScraperApi.GrpcClient;

namespace ScraperApi.Services;

public record CatalogoResponse(
    string Mensagem,
    int Quantidade,
    DateTime ObtidoEm,
    bool DoCache,
    IReadOnlyList<ProdutoDtoModel> Itens);

public interface IProdutoService
{
    Task<CatalogoResponse> ObterCatalogoComScrapingAsync(bool forcarAtualizacao = false, CancellationToken cancellationToken = default);
}
