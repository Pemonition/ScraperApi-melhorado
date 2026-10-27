"""Microsservico de scraping exposto via gRPC (contrato em ../Protos/produto.proto).

Raspa o catalogo de https://books.toscrape.com, um site feito para treinar web scraping.
"""

import logging
import os
import sys
from concurrent import futures
from pathlib import Path
from urllib.parse import urljoin

import grpc
import requests
from bs4 import BeautifulSoup

AQUI = Path(__file__).resolve().parent
PROTO_DIR = Path(os.getenv("PROTO_DIR", AQUI.parent / "Protos"))


def gerar_stubs() -> None:
    """Gera produto_pb2.py / produto_pb2_grpc.py a partir do .proto compartilhado com a API .NET."""
    from grpc_tools import protoc

    resultado = protoc.main([
        "grpc_tools.protoc",
        f"-I{PROTO_DIR}",
        f"--python_out={AQUI}",
        f"--grpc_python_out={AQUI}",
        str(PROTO_DIR / "produto.proto"),
    ])
    if resultado != 0:
        raise RuntimeError("Falha ao gerar os stubs gRPC a partir do produto.proto")


if not (AQUI / "produto_pb2.py").exists():
    gerar_stubs()

sys.path.insert(0, str(AQUI))
import produto_pb2  # noqa: E402
import produto_pb2_grpc  # noqa: E402

URL_BASE = os.getenv("SCRAPER_URL", "https://books.toscrape.com/catalogue/page-1.html")
PAGINAS = int(os.getenv("SCRAPER_PAGINAS", "1"))
TIMEOUT = float(os.getenv("SCRAPER_TIMEOUT", "10"))
PORTA = os.getenv("GRPC_PORT", "50051")

log = logging.getLogger("scraper")


def raspar_produtos() -> list[dict]:
    produtos = []
    url = URL_BASE
    sessao = requests.Session()
    sessao.headers["User-Agent"] = "ScraperApi-melhorado/1.0 (+https://github.com/Pemonition)"

    for _ in range(PAGINAS):
        resposta = sessao.get(url, timeout=TIMEOUT)
        resposta.raise_for_status()
        resposta.encoding = "utf-8"
        sopa = BeautifulSoup(resposta.text, "html.parser")

        for livro in sopa.select("article.product_pod"):
            produtos.append({
                "titulo": livro.h3.a["title"].strip(),
                "preco": livro.select_one(".price_color").get_text(strip=True),
            })

        proxima = sopa.select_one("li.next a")
        if not proxima:
            break
        url = urljoin(url, proxima["href"])

    return produtos


class ScraperService(produto_pb2_grpc.ScraperServiceServicer):
    def ObterProdutos(self, request, context):
        try:
            produtos = raspar_produtos()
        except requests.RequestException as erro:
            log.exception("Falha ao raspar %s", URL_BASE)
            context.abort(grpc.StatusCode.UNAVAILABLE, f"Site de origem indisponivel: {erro}")

        log.info("Raspados %d produtos", len(produtos))
        return produto_pb2.ListaProdutosResponse(
            produtos=[produto_pb2.ProdutoDto(**p) for p in produtos]
        )


def main() -> None:
    logging.basicConfig(level=logging.INFO, format="%(asctime)s [scraper] %(levelname)s %(message)s")
    servidor = grpc.server(futures.ThreadPoolExecutor(max_workers=4))
    produto_pb2_grpc.add_ScraperServiceServicer_to_server(ScraperService(), servidor)
    servidor.add_insecure_port(f"0.0.0.0:{PORTA}")
    servidor.start()
    log.info("Servidor gRPC ouvindo na porta %s", PORTA)
    servidor.wait_for_termination()


if __name__ == "__main__":
    main()
