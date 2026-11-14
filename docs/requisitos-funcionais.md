# Requisitos funcionais

> Referências: [[contexto-original]], [[regras-de-negocio]].

| Código | Requisito | Quem usa | Regras |
|---|---|---|---|
| RF01 | Fazer login, com acesso conforme o perfil do usuário | Todos | — |
| RF02 | Cadastrar veículos (placa, limite de caixas, situação) e gerar o QR Code de cada um | Responsável pela frota | RN09, RN16 |
| RF03 | Cadastrar motoristas (nome, CNH e validade) | Responsável pela frota | RN18 |
| RF04 | Importar do ERP clientes, fornecedores, produtos, notas, pedidos de compra e pedidos de venda (simulado) | Sistema | RN04 |
| RF05 | Registrar a chegada do veículo do fornecedor | Porteiro | — |
| RF06 | Bipar as caixas da entrada, conferindo cada bip com a nota e ligando cada caixa a uma etiqueta única | Conferente | RN01, RN02 |
| RF07 | Finalizar a entrada mostrando as diferenças com a nota e pedindo aprovação do gerente quando houver | Conferente, gerente | RN03 |
| RF08 | Consultar os pedidos de venda e o status de cada um | Comercial, gerente | RN05 |
| RF09 | Cancelar um pedido de venda, bloqueando o cancelamento se o veículo já saiu | Gerente | RN07 |
| RF10 | Criar um carregamento escolhendo veículo, motorista, data e pedidos | Programador de cargas | RN06, RN08, RN09, RN17, RN18, RN19 |
| RF11 | Bipar o QR Code do veículo e as caixas que sobem nele, recusando caixas de outro carregamento | Operador | RN10 |
| RF12 | Registrar a saída do veículo pelo QR Code, liberando só se todas as caixas foram bipadas | Porteiro | RN11, RN12 |
| RF13 | Registrar cada pedido como entregue ou não entregue (com motivo) | Motorista | RN13 |
| RF14 | Registrar a volta do veículo pelo QR Code | Porteiro | RN12 |
| RF15 | Bipar as caixas que voltaram e devolver o pedido para Aberto | Conferente | RN14 |
| RF16 | Encerrar o carregamento quando todos os pedidos tiverem a entrega registrada | Sistema | RN15 |
| RF17 | Colocar e tirar um veículo de manutenção | Responsável pela frota | RN16, RN17 |
| RF18 | Consultar a situação de cada veículo (disponível, em rota, em manutenção) | Gerente, programador | RN16 |
| RF19 | Consultar o histórico de uma caixa pelo código: quando entrou, em qual carregamento foi e se foi entregue | Gerente | RN01 |
