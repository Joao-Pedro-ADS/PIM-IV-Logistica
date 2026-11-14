"""Ciclo completo do app pela API (5090), com o carregamento criado pelo Web (5080)."""
import json, os, re, subprocess, urllib.request, urllib.error, datetime

exec(open(__file__.replace("testar_api.py", "testar.py")).read().split('g = Sessao("gerente")')[0])  # Sessao, checa, texto

API = "http://localhost:5090"


def api(metodo, url, token=None, corpo=None):
    req = urllib.request.Request(API + url, method=metodo,
                                 data=json.dumps(corpo).encode() if corpo is not None else None)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req) as r:
            return r.status, json.loads(r.read() or b"null")
    except urllib.error.HTTPError as e:
        bruto = e.read()
        try:
            return e.code, json.loads(bruto or b"null")
        except ValueError:
            return e.code, bruto.decode(errors="ignore")


def sql(q):
    return subprocess.run(["docker", "exec", "pim-sql", "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa",
                           "-P", "Pim@2026forte", "-C", "-d", os.environ.get("PIM_DB", "PimDb"), "-h", "-1", "-W", "-Q", "SET NOCOUNT ON; " + q],
                          capture_output=True, text=True).stdout.split()


def erro(resp):
    return resp[1].get("erro", "") if isinstance(resp[1], dict) else str(resp[1])


def login(u):
    s, r = api("POST", "/auth/login", corpo={"login": u, "senha": "123456"})
    return r["token"] if s == 200 else None


print("Login")
tk = {u: login(u) for u in ["conferente", "operador", "porteiro", "motorista"]}
checa("token para os 4 perfis do app", all(tk.values()))
r = api("POST", "/auth/login", corpo={"login": "gerente", "senha": "123456"})
checa("gerente recusado no app", r[0] == 400 and "Web" in erro(r))
r = api("POST", "/auth/login", corpo={"login": "porteiro", "senha": "x"})
checa("senha errada", r[0] == 400 and "inválidos" in erro(r))
checa("sem token -> 401", api("GET", "/entregas")[0] == 401)
checa("perfil errado -> 403", api("GET", "/entregas", tk["operador"])[0] == 403)

print("RF05 chegada do fornecedor")
r = api("POST", "/portaria/chegada-fornecedor", tk["porteiro"], {"placa": "xyz-1a23"})
checa("registrada com placa XYZ1A23", r[0] == 200 and r[1]["placaFornecedor"] == "XYZ1A23")
checa("placa inválida", "7 caracteres" in erro(api("POST", "/portaria/chegada-fornecedor", tk["porteiro"], {"placa": "AB"})))

print("RF06 / RF07 entrada")
chave = "35261111222333000181550010000090101000090100"   # nota 9010: 5 caixas do Produto B
r = api("POST", "/recebimentos", tk["conferente"], {"chaveNota": chave})
checa("entrada iniciada (201)", r[0] == 201 and r[1]["notaNumero"] == 9010)
rid = r[1]["id"]
checa("mesma nota de novo recusada", "já tem" in erro(api("POST", "/recebimentos", tk["conferente"], {"chaveNota": chave})))
checa("nota inexistente", "não encontrada" in erro(api("POST", "/recebimentos", tk["conferente"], {"chaveNota": "123"})))
checa("RN02: Produto A fora da nota", "RN02" in erro(api("POST", f"/recebimentos/{rid}/caixas", tk["conferente"], {"codigoProduto": "7891000100103"})))
codigos = [api("POST", f"/recebimentos/{rid}/caixas", tk["conferente"], {"codigoProduto": "7891000200206"})[1]["codigoCaixa"] for _ in range(5)]
checa(f"RN01: 5 códigos novos ({codigos[0]}..{codigos[-1]})", codigos == [f"CX{n:08d}" for n in range(39, 44)])
r = api("POST", f"/recebimentos/{rid}/finalizar", tk["conferente"])
checa("sem diferença -> Finalizado", r[1]["status"] == "Finalizado" and r[1]["linhas"][0]["diferenca"] == 0)
checa("bipar depois de finalizar recusa", "finalizada" in erro(api("POST", f"/recebimentos/{rid}/caixas", tk["conferente"], {"codigoProduto": "7891000200206"})))
# nota extra com falta: 3 caixas do Produto C, só 2 chegam
chave2 = "35261199999999000199550010000099991000099990"
sql(f"INSERT INTO NotasFiscais (Numero, Chave, FornecedorId, DataEmissao, QuantidadeVolumes) VALUES (9999, '{chave2}', 1, GETDATE(), 3);"
    "INSERT INTO NotaFiscalItens (NotaFiscalId, ProdutoId, Quantidade) VALUES (SCOPE_IDENTITY(), 3, 3)")
rid2 = api("POST", "/recebimentos", tk["conferente"], {"chaveNota": chave2})[1]["id"]
for _ in range(2):
    api("POST", f"/recebimentos/{rid2}/caixas", tk["conferente"], {"codigoProduto": "7891000300309"})
r = api("POST", f"/recebimentos/{rid2}/finalizar", tk["conferente"])
checa("com falta -> AguardandoAprovacao (diferença -1)", r[1]["status"] == "AguardandoAprovacao" and r[1]["linhas"][0]["diferenca"] == -1)
g = Sessao("gerente")
checa("Web mostra a nota 9999 aguardando o gerente", "9999" in texto(g.get("/Recebimentos")))

print("Carregamentos (criados pelo Web)")
prog = Sessao("programador")
hoje = datetime.date.today().isoformat()
t = texto(prog.post("/Carregamentos/Criar", [("veiculoId", 2), ("motoristaId", 3), ("dataPrevista", hoje), ("pedidoIds", 1), ("pedidoIds", 2)], de="/Carregamentos/Criar"))
checa("DEF4G56 + Marcos + 5001/5002 criado", "criado" in t.lower())
t = texto(prog.post("/Carregamentos/Criar", [("veiculoId", 1), ("motoristaId", 1), ("dataPrevista", hoje), ("pedidoIds", 3)], de="/Carregamentos/Criar"))
checa("ABC1D23 + João + 5003 criado", "criado" in t.lower())

print("RF11 / RF12 carregamento e saída")
op, pt = tk["operador"], tk["porteiro"]
r = api("GET", "/carregamentos/veiculo?qr=VEICULO:2", op)
checa("QR do veículo: 20 caixas, 0 bipadas", r[0] == 200 and r[1]["totalCaixas"] == 20 and r[1]["caixasBipadas"] == 0)
cid = r[1]["id"]
checa("QR inválido", "inválido" in erro(api("GET", "/carregamentos/veiculo?qr=ABC", op)))
checa("RN11: saída antes de bipar", "RN11" in erro(api("POST", "/portaria/saida", pt, {"qr": "VEICULO:2"})))
checa("RN10: caixa de Produto D não pertence", "RN10" in erro(api("POST", f"/carregamentos/{cid}/caixas", op, {"codigoCaixa": "CX00000037"})))
checa("caixa inexistente", "não encontrada" in erro(api("POST", f"/carregamentos/{cid}/caixas", op, {"codigoCaixa": "CX99"})))
# 5001: A8 B4 / 5002: A5 C3  ->  A 1..13, B 21..24, C 31..33
for n in list(range(1, 14)) + list(range(21, 25)) + list(range(31, 34)):
    r = api("POST", f"/carregamentos/{cid}/caixas", op, {"codigoCaixa": f"cx{n:08d}"})
    if r[0] != 200:
        print("    bip falhou", n, r)
checa("20 de 20 bipadas, nada faltando", r[1]["caixasBipadas"] == 20 and r[1]["faltando"] == [])
checa("bipar de novo: já bipada", "já foi bipada" in erro(api("POST", f"/carregamentos/{cid}/caixas", op, {"codigoCaixa": "CX00000001"})))
cid2 = api("GET", "/carregamentos/veiculo?qr=VEICULO:1", op)[1]["id"]
checa("RN10: caixa de outro carregamento", "outro carregamento" in erro(api("POST", f"/carregamentos/{cid2}/caixas", op, {"codigoCaixa": "CX00000021"})))
checa("RN10: caixa a mais do mesmo produto", "RN10" in erro(api("POST", f"/carregamentos/{cid}/caixas", op, {"codigoCaixa": "CX00000014"})))
r = api("POST", "/portaria/saida", pt, {"qr": "VEICULO:2"})
checa("saída liberada -> EmRota", r[0] == 200 and r[1]["status"] == "EmRota")
checa("Web: 5001 EmRota", "EmRota" in texto(g.get("/Pedidos?busca=5001")))
checa("Web: DEF4G56 EmRota", re.search(r"DEF4G56.{0,80}EmRota", texto(g.get("/Veiculos"))) is not None)
checa("RN12: registro de saída", sql(f"SELECT COUNT(*) FROM RegistrosPortaria WHERE Tipo = 2 AND CarregamentoId = {cid}") == ["1"])
checa("saída de novo recusada", "não tem carregamento" in erro(api("POST", "/portaria/saida", pt, {"qr": "VEICULO:2"})))

print("RF13 entregas")
mt = tk["motorista"]
r = api("GET", "/entregas", mt)
checa("motorista vê os 2 pedidos com endereço", r[0] == 200 and len(r[1]["pedidos"]) == 2 and r[1]["pedidos"][0]["endereco"])
ent = {p["pedidoNumero"]: p["id"] for p in r[1]["pedidos"]}
checa("RN13: não entregue sem motivo", "RN13" in erro(api("POST", f"/entregas/{ent[5002]}", mt, {"entregue": False, "motivo": " "})))
api("POST", f"/entregas/{ent[5001]}", mt, {"entregue": True})
r = api("POST", f"/entregas/{ent[5002]}", mt, {"entregue": False, "motivo": "Cliente fechado"})
checa("5001 Entregue e 5002 NaoEntregue", {p["pedidoNumero"]: p["resultado"] for p in r[1]["pedidos"]} == {5001: "Entregue", 5002: "NaoEntregue"})
checa("registrar de novo recusado", "já foi registrada" in erro(api("POST", f"/entregas/{ent[5001]}", mt, {"entregue": True})))
checa("ainda EmRota até o veículo voltar", r[1]["status"] == "EmRota")
cx5002 = sql("SELECT Codigo FROM Caixas WHERE PedidoVendaId = 2 ORDER BY Codigo")
checa("retorno antes da volta recusado", "ainda não voltou" in erro(api("POST", "/retornos/caixas", tk["conferente"], {"codigoCaixa": cx5002[0]})))

print("RF14 / RF16 volta e encerramento")
r = api("POST", "/portaria/volta", pt, {"qr": "VEICULO:2"})
checa("volta -> carregamento Encerrado", r[0] == 200 and r[1]["status"] == "Encerrado")
checa("Web: DEF4G56 Disponivel", re.search(r"DEF4G56.{0,80}Disponivel", texto(g.get("/Veiculos"))) is not None)
checa("volta de novo recusada", "não está em rota" in erro(api("POST", "/portaria/volta", pt, {"qr": "VEICULO:2"})))
checa("motorista sem carregamento em rota", "não tem carregamento" in erro(api("GET", "/entregas", mt)))

print("RF15 retorno")
cf = tk["conferente"]
checa("caixa entregue não volta", "não está em rota" in erro(api("POST", "/retornos/caixas", cf, {"codigoCaixa": "CX00000001"})))
for c in cx5002:
    r = api("POST", "/retornos/caixas", cf, {"codigoCaixa": c})
checa(f"{len(cx5002)} caixas do 5002 voltaram; última reabre o pedido", r[0] == 200 and r[1]["pedidoReaberto"] and r[1]["caixasFaltando"] == 0)
checa("Web: 5002 Aberto de novo", "Aberto" in texto(g.get("/Pedidos?busca=5002")))
checa("caixa do retorno está livre no armazém", sql(f"SELECT Status FROM Caixas WHERE Codigo = '{cx5002[0]}'") == ["0"])

print("Rastreio no Web")
t = texto(g.get("/Caixas?codigo=CX00000001"))
checa("CX00000001: Entrada, Carregamento, Saida, Entrega", all(x in t for x in ["Entrada", "Carregamento", "Saida", "Entrega"]))
t = texto(g.get(f"/Caixas?codigo={cx5002[0]}"))
checa(f"{cx5002[0]}: termina em Retorno", "Retorno" in t)

print(f"\n{ok} OK, {falhas} falha(s)")
