# ERP atual — Regras de negócio implementadas

> Documento de apoio. Descreve **o que o ERP da distribuidora (fictício) já faz e como se comporta hoje**.
> Não são requisitos do novo sistema. São restrições e comportamentos que ele precisa respeitar ao integrar.
> Tabelas citadas: ver [[erp-tabelas]].

Formato: **RN-ERP-xx**, com a regra e, quando relevante, a consequência para o novo sistema.

---

## 1. Clientes

| Código | Regra |
|---|---|
| RN-ERP-01 | Todo cliente é identificado por **CPF ou CNPJ único**. |
| RN-ERP-02 | Cliente com situação `BLOQUEADO` (inadimplência, definida pelo financeiro) **não pode receber pedido novo**, e os pedidos abertos dele **não podem ser faturados**. |
| RN-ERP-03 | Cliente `INATIVO` não aparece para seleção no pedido. |
| RN-ERP-04 | Se o pedido não informar endereço de entrega, vale o **endereço do cadastro do cliente**. |
| RN-ERP-05 | O campo `regiao` é livre e opcional. **Não serve como base confiável** para agrupar entregas. |

## 2. Produtos

| Código | Regra |
|---|---|
| RN-ERP-06 | Todo produto é vendido e comprado **por caixa**. Não existe venda fracionada. |
| RN-ERP-07 | O código de barras do produto (EAN/DUN-14 do fornecedor) **identifica o produto, não a caixa**: caixas do mesmo produto têm o mesmo código. |
| RN-ERP-08 | Peso e volume da caixa são **opcionais**, e cerca de 30% dos produtos estão sem esses dados. Qualquer cálculo de capacidade de veículo fica impreciso. |
| RN-ERP-09 | Produto `INATIVO` não pode ser incluído em pedido de venda nem de compra. |

## 3. Pedido de venda

| Código | Regra |
|---|---|
| RN-ERP-10 | Só usuários com perfil `VENDEDOR` (ou `ADMIN`) criam e alteram pedidos de venda. |
| RN-ERP-11 | Os status do pedido no ERP são só **`ABERTO` → `FATURADO_PARCIAL` → `FATURADO`**, ou **`CANCELADO`**. O ERP **não sabe** se o pedido foi separado, carregado, saiu para entrega ou foi entregue. |
| RN-ERP-12 | Ao salvar, o ERP compara o valor do pedido com o **limite de crédito** do cliente. Se passar, o pedido fica com `bloqueado_credito = true` e **não pode ser faturado** até o financeiro liberar. |
| RN-ERP-13 | O pedido pode ser **alterado ou cancelado livremente enquanto está `ABERTO`**. Depois de um faturamento, mesmo parcial, só a **parte não faturada** pode ser alterada ou cancelada. |
| RN-ERP-14 | **Não existe campo de prioridade.** Os vendedores escrevem "URGENTE" na observação, e isso não tem efeito nenhum no sistema. |
| RN-ERP-15 | A data prevista de entrega é informada pelo vendedor e **não é validada** (pode estar no passado ou não ser preenchida). |

## 4. Faturamento (NF-e de saída)

| Código | Regra |
|---|---|
| RN-ERP-16 | O faturamento é feito pelo perfil `FATURAMENTO` (escritório) e gera a **NF-e de saída**. O DANFE impresso **acompanha a carga** no veículo. |
| RN-ERP-17 | O ERP permite **faturamento parcial**: um pedido pode gerar **mais de uma NF**, e cada uma atualiza `quantidade_faturada` dos itens. |
| RN-ERP-18 | Hoje o faturamento é feito **depois que a programadora monta a planilha de carregamento** e **antes da saída do veículo**. Isso é combinado entre as pessoas, porque o ERP não sabe o que é um carregamento. |
| RN-ERP-19 | A NF de saída **baixa o estoque na emissão**, não na saída física do veículo. |
| RN-ERP-20 | A NF-e só pode ser **cancelada até 24h depois de autorizada** (regra fiscal). Depois disso, mercadoria que volta precisa de uma **NF de `RETORNO`**, lançada manualmente pelo faturamento. |
| RN-ERP-21 | A NF de `RETORNO` **devolve as caixas ao estoque** e **não reabre o pedido**. Para entregar de novo, o faturamento precisa emitir outra NF de saída. |

## 5. Compras e recebimento (NF-e de entrada)

| Código | Regra |
|---|---|
| RN-ERP-22 | Só o perfil `COMPRAS` cria pedidos de compra. |
| RN-ERP-23 | A NF de entrada é **lançada manualmente** pelo escritório (digitada ou importada do XML), **a partir do papel da conferente**, normalmente horas depois da chegada. |
| RN-ERP-24 | O vínculo da NF de entrada com o pedido de compra é **opcional**. Por isso entra mercadoria que ninguém pediu e o pedido de compra fica `ABERTO` mesmo depois de recebido. |
| RN-ERP-25 | O ERP só registra a **quantidade que está na nota**. Ele **não tem campo para divergência** (falta, sobra, avaria). Isso fica no papel. |
| RN-ERP-26 | Caixa avariada que volta para o fornecedor exige uma **NF de `DEVOLUCAO`**, emitida pelo faturamento. |
| RN-ERP-27 | O lançamento da NF de entrada **soma o estoque** e, se houver vínculo, atualiza `quantidade_recebida` e o status do pedido de compra. |

## 6. Estoque

| Código | Regra |
|---|---|
| RN-ERP-28 | O estoque é só um **saldo de caixas por produto**. Não existe controle de caixa individual, endereço, lote ou validade. |
| RN-ERP-29 | O ERP **permite saldo negativo**. Foi configurado assim porque as entradas são lançadas com atraso e o faturamento não podia travar. |

## 7. Acesso

| Código | Regra |
|---|---|
| RN-ERP-30 | Perfis existentes: `VENDEDOR`, `COMPRAS`, `FATURAMENTO`, `FINANCEIRO`, `ADMIN`. |
| RN-ERP-31 | Conferentes, operadores, porteiro, programadora de cargas e motoristas **não têm acesso ao ERP**. A programadora recebe os pedidos por e-mail ou impressos. |
| RN-ERP-32 | O ERP **só funciona no computador** (desktop), sem versão mobile. |

---

## 8. Lacunas do ERP x problemas do contexto

| Problema (contexto) | Por que o ERP não resolve | Regras relacionadas |
|---|---|---|
| P1 — Entrada/saída no papel | Lançamento manual e atrasado, sem divergência, sem caixa individual | RN-ERP-07, 23, 25, 28 |
| P2 — Pedido sem acompanhamento | Status só vai até "faturado" | RN-ERP-11, 14 |
| P3 — Carregamento na planilha | O ERP não conhece carregamento, e peso/volume são incompletos | RN-ERP-08, 18 |
| P4 — Portaria não confere a carga | Portaria não usa o ERP | RN-ERP-31 |
| P5 — Ninguém sabe onde está o veículo | O ERP não tem veículo nem motorista | — |
| P6 — Retorno sem registro | A NF de retorno é manual e não reabre o pedido | RN-ERP-20, 21 |
