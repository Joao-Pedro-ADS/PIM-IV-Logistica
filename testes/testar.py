"""Roda os checklists dos passos 06 a 12 contra o site em http://localhost:5080."""
import html, re, http.cookiejar, urllib.request, urllib.parse, urllib.error, datetime

BASE = "http://localhost:5080"
ok = falhas = 0


class SemRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k):
        return None


class Sessao:
    def __init__(self, login):
        self.jar = http.cookiejar.CookieJar()
        self.op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar), SemRedirect())
        self.post("/Conta/Login", {"login": login, "senha": "123456"}, de="/Conta/Login")

    def req(self, url, dados=None):
        try:
            r = self.op.open(BASE + url, dados)
            return r.status, html.unescape(r.read().decode(errors="ignore")), r.headers.get("Location")
        except urllib.error.HTTPError as e:
            corpo = e.read().decode(errors="ignore")
            if e.code in (301, 302):
                return e.code, corpo, e.headers.get("Location")
            return e.code, html.unescape(corpo), None

    def get(self, url):
        status, corpo, loc = self.req(url)
        if status == 302 and loc:   # segue um redirect
            status, corpo, loc = self.req(loc.replace(BASE, ""))
        return corpo

    def post(self, url, campos, de):
        pagina = self.req(de)[1]
        token = re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', pagina) or \
            re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', pagina)
        itens = list(campos.items()) if isinstance(campos, dict) else list(campos)
        itens.append(("__RequestVerificationToken", token.group(1)))
        status, corpo, loc = self.req(url, urllib.parse.urlencode(itens).encode())
        if status == 302 and loc:
            corpo = self.get(loc.replace(BASE, ""))
        return corpo


def checa(nome, cond):
    global ok, falhas
    cond = bool(cond)
    print(("  OK   " if cond else "  FALHA ") + nome)
    ok += cond
    falhas += not cond


def texto(h):
    return re.sub(r"\s+", " ", re.sub(r"<[^>]+>", " ", h))


g = Sessao("gerente")
prog = Sessao("programador")
com = Sessao("comercial")

print("06 Veículos")
t = texto(g.get("/Veiculos"))
checa("3 veículos, Truck em EmManutencao", all(p in t for p in ["ABC1D23", "DEF4G56", "GHI7J89"]) and "EmManutencao" in t)
r = texto(g.post("/Veiculos/Criar", {"Placa": "ABC1D23", "Modelo": "X", "LimiteCaixas": "10"}, de="/Veiculos/Criar"))
checa("placa repetida dá erro", "Já existe um veículo com essa placa." in r)
r = g.post("/Veiculos/Criar", {"Placa": "", "Modelo": "", "LimiteCaixas": ""}, de="/Veiculos/Criar")
checa("formulário vazio mostra validação", "field-validation-error" in r)
r = texto(g.post("/Veiculos/Criar", {"Placa": "XYZ9K87", "Modelo": "Fiorino", "LimiteCaixas": "40"}, de="/Veiculos/Criar"))
checa("placa nova cadastrada", "XYZ9K87" in r)
idnovo = re.search(r'/Veiculos/Editar/(\d+)"[^<]*</a>(?:(?!/Veiculos/Editar/).)*$', g.get("/Veiculos"), re.S)
ids = re.findall(r"/Veiculos/AlternarManutencao/(\d+)", g.get("/Veiculos"))
vid = max(map(int, ids)) if ids else None
if vid:
    antes = texto(g.get("/Veiculos"))
    g.post(f"/Veiculos/AlternarManutencao/{vid}", {}, de="/Veiculos")
    depois = texto(g.get("/Veiculos"))
    g.post(f"/Veiculos/AlternarManutencao/{vid}", {}, de="/Veiculos")
    checa("alternar manutenção muda a situação", depois.count("EmManutencao") == antes.count("EmManutencao") + 1)
else:
    checa("botão de manutenção encontrado", False)
status, corpo, _ = g.req("/Veiculos/QrCode/1")
checa("QR Code gera imagem", status == 200)
checa("Etiqueta abre", "ABC1D23" in g.get("/Veiculos/Etiqueta/1"))
t = prog.get("/Veiculos")
checa("programador vê lista sem botões", "ABC1D23" in t and "AlternarManutencao" not in t and "/Veiculos/Criar" not in t)

print("07 Motoristas")
t = texto(g.get("/Motoristas"))
checa("Pedro Vencida, João Vence em 20 dias", "Vencida" in t and "Vence em 20 dias" in t)
checa("CPF mascarado", "***.456.789-**" in t)
venc = (datetime.date.today() + datetime.timedelta(days=400)).isoformat()
r = texto(g.post("/Motoristas/Criar", {"Nome": "Teste", "Cpf": "12345678901", "NumeroCnh": "999", "ValidadeCnh": venc, "UsuarioId": ""}, de="/Motoristas/Criar"))
checa("CPF repetido dá erro", "CPF" in r and "/Motoristas/Criar" not in "" and ("Já existe" in r or "já" in r.lower()))
pedro = g.req("/Motoristas/Editar/2")[1]
campos = {"Nome": "Pedro Costa", "Cpf": "34567890123",
          "NumeroCnh": re.search(r'id="NumeroCnh"[^>]*value="([^"]*)"', pedro).group(1) if re.search(r'id="NumeroCnh"[^>]*value="([^"]*)"', pedro) else re.search(r'value="([^"]*)"[^>]*id="NumeroCnh"', pedro).group(1),
          "ValidadeCnh": venc, "UsuarioId": ""}
t = texto(g.post("/Motoristas/Editar/2", campos, de="/Motoristas/Editar/2"))
checa("editar validade deixa CNH Válida", "Válida" in t)
# volta Pedro para vencida (o passo 09 testa RN18 com ele)
campos["ValidadeCnh"] = (datetime.date.today() - datetime.timedelta(days=10)).isoformat()
g.post("/Motoristas/Editar/2", campos, de="/Motoristas/Editar/2")
status, _, loc = prog.req("/Motoristas")
checa("programador recebe Acesso negado", status == 302 and "AcessoNegado" in (loc or ""))

print("08 Pedidos")
t = texto(g.get("/Pedidos"))
checa("5 pedidos Aberto", all(str(n) in t for n in range(5001, 5006)) and t.count("Aberto") >= 5)
t = texto(g.get("/Pedidos?busca=Padaria"))
checa("busca 'Padaria' filtra", "Padaria" in t and t.count("Detalhes") < 5)
t = texto(g.get("/Pedidos?busca=5001"))
checa("busca '5001' filtra", "5001" in t and "5002" not in t)
t = texto(g.get("/Pedidos?status=Cancelado"))
checa("filtro por status", "5001" not in t)
t = texto(g.get("/Pedidos/Detalhes/1"))
checa("detalhes com itens e total de caixas", "5001" in t and "caixas" in t.lower())
checa("comercial sem botão cancelar", "Cancelar" not in com.get("/Pedidos/Detalhes/5"))
t = texto(g.post("/Pedidos/Cancelar/5", {}, de="/Pedidos/Detalhes/5"))
t2 = g.get("/Pedidos/Detalhes/5")
checa("gerente cancela 5005 e botão some", "Cancelado" in texto(t2) and "/Pedidos/Cancelar/5" not in t2)

print("09 Carregamentos")
hoje = datetime.date.today().isoformat()
casos = [
    (3, 3, [1], "RN17"), (1, 2, [1], "RN18"), (1, 3, [1, 3], "RN09"),
    (1, 3, [], "RN08"), (1, 3, [1], "criado"), (1, 1, [2], "RN19: este veículo"),
    (2, 3, [2], "RN19: este motorista"),
]
for v, m, peds, esperado in casos:
    campos = [("veiculoId", v), ("motoristaId", m), ("dataPrevista", hoje)] + [("pedidoIds", p) for p in peds]
    t = texto(prog.post("/Carregamentos/Criar", campos, de="/Carregamentos/Criar"))
    checa(f"veículo {v}, motorista {m}, pedidos {peds} -> {esperado}", esperado.lower() in t.lower())
checa("5001 aparece EmCarregamento", "EmCarregamento" in texto(g.get("/Pedidos?busca=5001")))
g.post("/Pedidos/Cancelar/1", {}, de="/Pedidos/Detalhes/1")
t = texto(g.get("/Carregamentos"))
checa("cancelar 5001 cancela o carregamento", "Cancelado" in t)
t = texto(prog.post("/Carregamentos/Criar", [("veiculoId", 1), ("motoristaId", 3), ("dataPrevista", hoje), ("pedidoIds", 2)], de="/Carregamentos/Criar"))
checa("van liberada: novo carregamento com 5002 (HasTrigger ok)", "criado" in t.lower())

print("10 Recebimentos")
t = texto(g.get("/Recebimentos"))
checa("4521 Finalizado e 7788 AguardandoAprovacao", "4521" in t and "Finalizado" in t and "7788" in t and "AguardandoAprovacao" in t)
t = texto(g.get("/Recebimentos/Detalhes/2"))
checa("7788 mostra Produto D com -2", "Produto D" in t and "-2" in t)
t = g.post("/Recebimentos/Aprovar/2", {"justificativa": "Duas caixas avariadas"}, de="/Recebimentos/Detalhes/2")
t = g.get("/Recebimentos/Detalhes/2")
checa("aprovação finaliza e mostra quem aprovou", "Finalizado" in texto(t) and "/Recebimentos/Aprovar" not in t)

print("11 Painel e rastreio")
t = texto(g.get("/"))
checa("painel inicial com números", "Pedidos" in t and re.search(r"\d", t) is not None)
t = texto(g.get("/Caixas?codigo=cx00000001"))
checa("cx00000001 mostra Produto A, 4521 e Juliana", "Produto A" in t and "4521" in t and "Juliana" in t)
checa("XX não encontrada", "Caixa não encontrada." in texto(g.get("/Caixas?codigo=XX")))

print(f"\n{ok} OK, {falhas} falha(s)")
