# Regras de negócio

> Referência: [[contexto-original]].

## Processo (com o sistema)

1. **Entrada:** o conferente bipa cada caixa que chega do fornecedor e o sistema compara com a nota.
2. **Pedido:** o pedido vem do ERP e o sistema mostra em que etapa ele está.
3. **Carregamento:** o programador escolhe veículo, motorista e pedidos. O operador bipa as caixas ao subir no caminhão.
4. **Saída:** o porteiro bipa o veículo, que só sai se o carregamento estiver completo.
5. **Entrega:** o motorista marca cada pedido como entregue ou não entregue.
6. **Retorno:** as caixas que voltam são bipadas e o pedido fica disponível de novo.
7. **Frota:** cadastro de veículos e motoristas com a situação de cada um.

## Regras

| Código | Regra |
|---|---|
| RN01 | Toda caixa recebe uma etiqueta com código único na entrada. |
| RN02 | Só entra caixa que está na nota fiscal. |
| RN03 | Se as caixas bipadas não baterem com a nota, só o gerente pode aceitar a entrada. |
| RN04 | Os pedidos vêm do ERP. O sistema não cria pedidos. |
| RN05 | Status do pedido: Aberto → Em carregamento → Em rota → Entregue ou Não entregue. |
| RN06 | Um pedido vai inteiro em um único carregamento. |
| RN07 | O pedido só pode ser cancelado antes de o veículo sair. |
| RN08 | Um carregamento tem veículo, motorista, data e pedidos. |
| RN09 | A quantidade de caixas não pode passar do limite do veículo. |
| RN10 | Caixa bipada que não pertence ao carregamento é recusada. |
| RN11 | O veículo só sai se todas as caixas do carregamento foram bipadas. |
| RN12 | Na saída e na volta, o sistema registra veículo, motorista e horário. |
| RN13 | Pedido não entregue precisa de motivo. |
| RN14 | Caixas que voltam são bipadas e o pedido volta para Aberto. |
| RN15 | O carregamento termina quando todos os pedidos têm entrega registrada. |
| RN16 | Situação do veículo: Disponível, Em rota ou Em manutenção. |
| RN17 | Veículo em manutenção não pode ser usado em carregamento. |
| RN18 | Motorista com CNH vencida não pode ser escalado. |
| RN19 | Um veículo ou motorista só pode estar em um carregamento por vez. |
