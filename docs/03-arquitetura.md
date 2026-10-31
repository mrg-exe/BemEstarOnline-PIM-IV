# Etapas 4, 5 e 6 — Arquitetura da solução

## Visão geral
```
   [Site HTML/CSS/JS]      [App .NET MAUI]        [Painel admin (site)]
            \                    |                     /
             \___________ HTTP/JSON + JWT ____________/
                                 |
                    [API ASP.NET Core 8 (C#)]
         Controllers -> Aplicação (serviços) -> Domínio
                                 |
                       Infraestrutura (EF Core)
                                 |
                 [MySQL 8: tabelas + triggers + procedures]
```
O site, o app e o painel usam **a mesma API**. O app guarda uma cópia do catálogo, o carrinho e
os pedidos feitos sem internet no **SQLite** do celular e sincroniza depois.

## Camadas da API (`src/BemEstarOnline.Api`)
| Pasta | Responsabilidade | Exemplos |
|---|---|---|
| `Dominio/` | Regras do negócio, sem saber de banco ou HTTP | `Pedido.AlterarStatus()`, `Cupom.Aplicar()`, `StatusPedido` (transições permitidas), interfaces dos repositórios |
| `Aplicacao/` | Casos de uso e DTOs | `PedidoServico.CriarAsync()`, `CatalogoServico`, `AuthServico` |
| `Infraestrutura/` | Banco, segurança e detalhes técnicos | `BemEstarDbContext`, repositórios, `SenhaHasher` (PBKDF2), `TokenServico` (JWT), automações/relatórios |
| `Controllers/` | Entrada HTTP (REST) | `PedidosController`, `AdminController` |
| `Middlewares/` | Tratamento único de erros | `TratamentoDeErros` → `{ "mensagem": "..." }` |

## Orientação a objetos e SOLID
- **Encapsulamento:** o pedido só muda de status pelo método `AlterarStatus`, que valida o fluxo
  (PAGO → EM_SEPARACAO → SAIU_PARA_ENTREGA → ENTREGUE) e estorna o pagamento ao cancelar.
- **Polimorfismo / inversão de dependência (D):** `IAutomacoesBanco` tem duas implementações:
  `AutomacoesViaTriggersMySql` (o MySQL faz a baixa de estoque) e `AutomacoesEmMemoria`
  (o C# faz a mesma regra no modo demonstração). O `PedidoServico` não sabe qual está em uso.
  O mesmo vale para `IRelatorioRepositorio` (procedures × LINQ).
- **Responsabilidade única (S):** cada serviço cuida de um assunto; repositórios só acessam dados.
- **Aberto/fechado (O):** novos tipos de cupom entram em `Cupom.Aplicar` sem mudar o serviço.

## Segurança
- Login com **JWT** (8 h) e perfis `CLIENTE`, `ATENDENTE`, `ADMIN` (`[Authorize(Roles=...)]`).
- O id do cliente vem do token, nunca do corpo da requisição (ninguém vê pedido de outra pessoa).
- Senhas com **PBKDF2-SHA256**, 100 mil iterações e sal aleatório.
- Preços sempre recalculados no servidor; validação de CPF; mensagens de erro sem detalhes internos.

## Principais rotas REST
| Método | Rota | Acesso |
|---|---|---|
| POST | /api/auth/login, /api/auth/registrar | público |
| GET | /api/categorias, /api/produtos, /api/produtos/{idOuSlug} | público |
| POST | /api/pedidos/simular, /api/cupons/validar | público |
| GET/PUT/DELETE | /api/carrinho | cliente |
| GET/POST/DELETE | /api/enderecos | cliente |
| GET/POST | /api/pedidos, /api/pedidos/{id}, /api/pedidos/{id}/cancelar | cliente |
| GET/POST/PATCH | /api/assinaturas | cliente |
| POST | /api/delivery | público |
| GET | /api/admin/painel, /pedidos, /clientes, /alertas, /assinaturas, /delivery | equipe |
| PATCH | /api/admin/pedidos/{id}/status | equipe |
| POST/PUT/PATCH | /api/admin/produtos, /api/admin/produtos/{id}/imagem (foto) | admin (estoque: equipe) |
| GET | /api/pedidos/{id}/rastreio | cliente dono do pedido ou equipe |
| POST | /api/auth/excluir-conta | cliente (pede a senha) |
| GET | /api/entregador/pedidos | entregador |
| POST | /api/entregador/pedidos/{id}/aceitar, /sair, /entregar | entregador |
| PUT | /api/entregador/pedidos/{id}/localizacao | entregador |

## Aplicativo móvel (`src/BemEstarOnline.Mobile`) — MVVM
- **Telas:** Login, Produtos, Produto, Carrinho, Pedidos, Acompanhar pedido, Perfil.
- **Fluxo:** Login (ou "ver sem entrar") → abas Produtos / Carrinho / Pedidos / Perfil.
- **Autenticação:** mesmo JWT da API, salvo no aparelho.
- **Sincronização:** catálogo em cache, carrinho local copiado para a API (o mesmo carrinho do site)
  e fila de pedidos offline enviada quando a internet volta (`SincronizacaoServico`). Cada pedido
  offline guarda o id do dono e só é enviado com a conta dele.
- **Dois modos no mesmo app:** o perfil do token decide as abas. Cliente: Produtos, Carrinho,
  Pedidos e Perfil. Entregador: Entregas e Perfil.

## Entrega com localização em tempo real
```
App do entregador --PUT /localizacao (a cada 10 s: GPS; ou "Simular trajeto")--> API --> pedidos.entregador_latitude/longitude
                                                                                   └─ trigger grava o trajeto (trajeto_entregas)
Site/App do cliente --GET /pedidos/{id}/rastreio (a cada 8 s)--> mapa Leaflet + OpenStreetMap (sem chave de API)
```
1. A farmácia muda o pedido para **EM_SEPARACAO** no painel.
2. O app do entregador consulta a lista a cada 20 s e **avisa** (alerta + vibração) quando chega
   pedido novo, informando se **exige receita**.
3. Entregador pega o pedido (`aceitar`) e toca "Saí da farmácia" (`sair` → SAIU_PARA_ENTREGA).
4. O entregador tem a localização do cliente: o endereço tem latitude/longitude (GPS do navegador
   ou busca no OpenStreetMap) e o botão "Abrir rota" abre o app de mapas do celular.
5. O cliente vê o ponto do entregador andando no mapa, com distância e horário da última posição.
6. Para confirmar a entrega de pedido com tarja vermelha, a API exige `receitaConferida = true`.

## Privacidade (LGPD)
- Exclusão da conta pelo próprio cliente: apaga carrinho e endereços não usados, cancela
  assinaturas e anonimiza o cadastro (`Usuario.Anonimizar`). Pedidos antigos ficam sem dados pessoais.
- Ao sair da conta, o site e o app apagam o carrinho e os dados em cache daquele usuário.
