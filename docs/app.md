# App mobile (Flutter)

> Referências: [[api]], [[requisitos-funcionais]], [[regras-de-negocio]].
> Código em `app/`. Fala com a `Pim.Api` seguindo o contrato de [[api]].

## Rodar

```fish
# 1. banco e API no ar (o Web também, para criar carregamentos)
docker start pim-sql
cd ~/dev/studies/university/PIM04/src
dotnet run --project Pim.Api

# 2. app no emulador ou no celular (USB com depuração ativada)
cd ~/dev/studies/university/PIM04/app
fish_add_path ~/development/flutter/bin
flutter run
```

- **Emulador:** o servidor padrão já é `http://10.0.2.2:5090`.
- **Celular:** na tela de login, abra **Servidor** e coloque `http://IP-do-computador:5090` (o IP aparece em `ip addr`). Celular e computador precisam estar na mesma rede Wi-Fi.
- APK para instalar direto: `flutter build apk` gera `build/app/outputs/flutter-apk/app-release.apk`.

## Telas por perfil

| Perfil | Tela | O que faz | RF / RN |
|---|---|---|---|
| Todos | Login | Login e endereço do servidor. Gerente, frota etc. são recusados ("use o Web") | RF01 |
| Conferente | Nova entrada | Bipa o DANFE, bipa as caixas uma atrás da outra e mostra a **etiqueta gerada** e a conferência produto a produto. Finaliza a entrada | RF06, RF07, RN01–RN03 |
| Conferente | Continuar entrada | Volta para a entrada em andamento, mesmo depois de fechar o app | RF06 |
| Conferente | Caixas que voltaram | Bipa as caixas de pedidos não entregues e avisa quando o pedido volta para Aberto | RF15, RN14 |
| Operador | Carregar veículo | Lê o QR do veículo e mostra o progresso (12 de 20) e **o que falta subir** por pedido. Bipa em sequência | RF11, RN10 |
| Porteiro | Chegada de fornecedor | Registra a placa | RF05 |
| Porteiro | Saída / Volta | Lê o QR do veículo. Na saída recusada mostra o motivo em destaque (RN11). Mostra veículo, motorista e horário | RF12, RF14, RN11, RN12 |
| Motorista | Minhas entregas | Lista os pedidos com cliente e endereço. **Entregue** ou **Não entregue** com motivo obrigatório (motivos prontos ou texto livre) | RF13, RN13, RF16 |

## Organização do código

```
app/lib/
├── main.dart                 tema e troca login ↔ início
├── sessao.dart               servidor, token e entrada em andamento (shared_preferences)
├── api/
│   ├── api_client.dart       HTTP + JWT + erros (400 regra, 401 sessão, 403 perfil, sem rede)
│   ├── pim_api.dart          uma função por rota do docs/api.md
│   └── modelos.dart          classes das respostas (fromJson)
├── widgets/
│   ├── leitor_codigo.dart    câmera (mobile_scanner) + campo para digitar o código
│   └── comuns.dart           avisos, botão grande, barra de progresso
└── telas/                    uma tela (ou par de telas) por função
```

- **Sem gerenciador de estado:** `StatefulWidget` + `setState`, e a `Sessao` é um `ChangeNotifier` que troca login ↔ início sozinho. Quando o token vence (401), o app volta para o login.
- **Leitor:** a câmera lê código de barras e QR Code. Sempre dá para **digitar o código**, para quando a etiqueta está rasgada ou a câmera não funciona. Nas telas de bipagem o leitor **reabre sozinho** depois de cada caixa e para no primeiro erro, para a pessoa ler o motivo.

## Acessibilidade e inclusão (Etapa 3 do manual)

- Botões grandes (64 px de altura) e texto a partir de 16–18 px, para usar com uma mão ou de luva.
- **Vibração diferente** para sucesso e erro, assim o operador percebe sem olhar a tela.
- Cores de sucesso e erro **sempre acompanhadas de texto e ícone**, para não depender só da cor (daltonismo).
- Tema claro e escuro conforme o celular. A barra de progresso tem rótulo para leitor de tela (`Semantics`).
- O app acompanha o tamanho de fonte do sistema.

## Testes

```fish
cd ~/dev/studies/university/PIM04/app
flutter test test/              # unidade: modelos e cliente HTTP (sem servidor)
flutter test integration_test -d linux --dart-define=SERVIDOR=http://localhost:5090
```

O teste de integração percorre um pedido inteiro pelas telas, falando com a API de verdade:
login recusado do gerente → entrada da nota 9010 (RN02, etiquetas, finalizar) → chegada de fornecedor e saída barrada (RN11) → carregamento de 20 caixas (RN10) → saída → entregue / não entregue com motivo (RN13) → volta e encerramento (RF16) → retorno das caixas, com o pedido de novo Aberto (RN14).

**Antes de rodar:** banco zerado, Web e API no ar, e um carregamento criado no Web com **DEF4G56 + Marcos Silva + pedidos 5001 e 5002**.

## Limites (para citar no texto do PIM)

- **Sem fila offline:** cada bipagem precisa de conexão. Sem sinal, o app avisa "Sem conexão com o servidor" e nada é gravado pela metade. Uma melhoria futura seria guardar as bipagens no celular e enviar quando o sinal voltar.
- **Etiqueta:** o app mostra o código gerado (`CX...`) para a pessoa escrever ou colar na caixa. Impressão por impressora Bluetooth fica como evolução.
