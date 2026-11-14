"""Mede a latência da bipagem de entrada (POST /recebimentos/{id}/caixas), o caminho mais usado do app.

Faz um aquecimento, depois N bipagens em sequência (como um conferente bipando) e mostra média e percentis.
Uso: ./rodar.sh latencia   (base zerada)   ou   python3 medir_latencia.py  (API já no ar em 5090)
"""
import json, os, statistics, time, urllib.request

API = "http://localhost:5090"
N = int(os.environ.get("N", "500"))
CHAVE = "35261111222333000181550010000090101000090100"   # nota 9010 dos dados de teste
PRODUTO = "7891000200206"                                # Produto B, presente na nota


def post(url, corpo, token=None):
    req = urllib.request.Request(API + url, method="POST", data=json.dumps(corpo).encode())
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    with urllib.request.urlopen(req) as r:
        return json.loads(r.read())


token = post("/auth/login", {"login": "conferente", "senha": "123456"})["token"]
rid = post("/recebimentos", {"chaveNota": CHAVE}, token)["id"]

for _ in range(20):  # aquecimento: JIT e conexões
    post(f"/recebimentos/{rid}/caixas", {"codigoProduto": PRODUTO}, token)

tempos = []
for _ in range(N):
    t0 = time.perf_counter()
    post(f"/recebimentos/{rid}/caixas", {"codigoProduto": PRODUTO}, token)
    tempos.append((time.perf_counter() - t0) * 1000)

tempos.sort()
pct = lambda p: tempos[min(len(tempos) - 1, int(round(p / 100 * len(tempos))) - 1)]
print(f"{N} bipagens | média {statistics.mean(tempos):.1f} ms | p50 {pct(50):.1f} | "
      f"p95 {pct(95):.1f} | p99 {pct(99):.1f} | máx {tempos[-1]:.1f} ms")
