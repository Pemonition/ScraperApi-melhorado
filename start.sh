#!/bin/sh
# Sobe o scraper gRPC em segundo plano e a API .NET em primeiro plano.
# Se qualquer um dos dois cair, o container encerra e a plataforma reinicia.
set -e

/opt/venv/bin/python /app/scraper/server.py &
SCRAPER_PID=$!

dotnet /app/api/ScraperApi.dll &
API_PID=$!

trap 'kill $SCRAPER_PID $API_PID 2>/dev/null' TERM INT

while kill -0 $SCRAPER_PID 2>/dev/null && kill -0 $API_PID 2>/dev/null; do
  sleep 2
done

echo "Um dos processos parou; encerrando o container."
kill $SCRAPER_PID $API_PID 2>/dev/null || true
exit 1
