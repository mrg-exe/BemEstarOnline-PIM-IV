# Alterações feitas no código (para atualizar a documentação)

> Preenchido pelo **Claude Code** a cada tarefa concluída. O Filipe leva este arquivo
> (e a pasta `docs/prints/`) para o chat do Claude, que atualiza o trabalho escrito.

Modelo de anotação:

```
## AAAA-MM-DD — título curto
- O que mudou: ...
- Arquivos: ...
- Rotas/tabelas/telas novas ou alteradas: ...
- Afeta qual etapa do PIM: ...
- Prints gerados: docs/prints/...
- Problemas encontrados e como foram resolvidos: ...
```

---

## 2026-10-30 — Tarefa 1: API compilada e testada no modo fácil (banco em memória)
- O que mudou: **nenhuma linha de código precisou ser alterada.** A API compilou na primeira
  tentativa (0 erros, 0 avisos) e subiu em http://localhost:5088/swagger.
- Ambiente do teste: Windows 11, só o **.NET SDK 10** instalado (10.0.403). A API é .NET 8 e rodou
  no .NET 10 graças ao `RollForward=Major` do `.csproj`, como planejado.
- Ao iniciar, a API criou sozinha: 45 produtos em 9 categorias, os usuários de teste
  (cliente, atendente, admin, entregador) e 30 pedidos de exemplo.
- Arquivos: nenhum alterado.
- Rotas/tabelas/telas novas ou alteradas: nenhuma. Swagger lista 38 rotas.
- Afeta qual etapa do PIM: Etapa 4 (web/API) e Etapa 7 (regras do banco no modo memória);
  pode mudar a situação "ainda não testado em máquina real" no `docs/00-requisitos.md`
  para "testado no modo memória" nos itens abaixo.
- Testes feitos (todos passaram), chamando a API de verdade:
  - **Login** dos 4 perfis; senha errada recusada (400); `/api/auth/eu`.
  - **Catálogo**: categorias, produtos, busca "dipirona" e busca sem acento ("analgesico"),
    produto por slug e por id.
  - **Carrinho**: salvar, obter e limpar.
  - **Cupons**: BEMESTAR10, SAUDE15 e FRETEGRATIS válidos; código em minúsculas também aceito;
    cupom não aplicado quando o subtotal está abaixo do mínimo.
  - **Pedido** com endereço novo, Pix, item com receita: estoque baixou sozinho (RF09),
    histórico gravado (RF10), pedido acima do estoque recusado (RF08), outro usuário não vê
    o pedido (403).
  - **Cancelamento** pelo cliente devolveu o estoque (RF11).
  - **Painel**: atendente acessa painel, pedidos, clientes, alertas, assinaturas, delivery e
    produtos; cliente é barrado (403). Mudança de status segue o fluxo; pulo de status recusado.
  - **Entregador** (RF25 a RF29): vê o pedido em separação marcado "exige receita", aceita,
    envio de localização antes de sair é recusado, sai para entrega, envia GPS; entregar sem
    conferir a receita é recusado; com receita, vira ENTREGUE.
    Linha do tempo final: PAGO > EM_SEPARACAO > SAIU_PARA_ENTREGA > ENTREGUE.
  - **Rastreio** `/api/pedidos/{id}/rastreio` (RF28): cliente e equipe veem a posição do
    entregador e o nome dele.
  - **Cadastro** com nome social (RF03); CPF inválido recusado. **Assinatura** criada com 10%
    de desconto (RF13). **Delivery** sem conta (RF14).
  - **Foto do produto** (`POST /api/admin/produtos/{id}/imagem`): funciona, e o Swagger mostra
    o botão de escolher arquivo.
- Prints gerados: nenhum ainda (ficam para a tarefa 7).
- Problemas encontrados e como foram resolvidos: nenhum na API. Observações:
  - No modo memória, a foto enviada pelo painel fica salva em `wwwroot/imagens/produtos/`, mas
    o produto "esquece" a foto quando a API é desligada, porque o banco em memória é apagado.
    No MySQL isso não acontece. É o comportamento esperado do modo demonstração.
  - O projeto veio num `.zip`; foi extraído para `Documents\BemEstarOnline` para poder compilar.

## 2026-10-30 — Tarefa 2: modo completo (Docker + MySQL) testado
- O que mudou: **nenhuma linha de código precisou ser alterada.** `docker compose up --build`
  subiu os 3 containers na primeira tentativa: `banco` (MySQL 8.0.46, porta 3307),
  `api` (porta 5088) e `site` (Nginx, porta 8080).
- Ambiente: Docker Desktop 29.8.1 / Compose v5.5.1 no Windows 11.
- Arquivos: nenhum alterado.
- Rotas/tabelas/telas novas ou alteradas: nenhuma.
- Afeta qual etapa do PIM: Etapa 7 (banco: triggers e procedures), Etapa 4 (web) e
  nuvem/DevOps (RNF06: sobe com um comando). RNF05 (mesmo comportamento nos dois modos)
  confirmado: o mesmo roteiro de testes da tarefa 1 passou inteiro também no MySQL.
- Conferido dentro do MySQL:
  - Os 4 scripts rodaram em ordem (`01_ddl`, `02_triggers`, `03_procedures`, `04_seed`).
    A pasta `database/completo/` é ignorada pelo Docker (de propósito: é uma cópia de tudo junto).
  - **5 triggers** criadas: `trg_pedido_itens_bi_valida_estoque`, `trg_pedido_itens_ai_baixa_estoque`,
    `trg_pedidos_ai_historico`, `trg_pedidos_au_status`, `trg_pedidos_au_trajeto`.
  - **4 procedures** criadas: `sp_relatorio_vendas`, `sp_produtos_estoque_baixo`,
    `sp_produtos_vencendo`, `sp_proximas_entregas_assinatura`. O painel admin funciona
    chamando as procedures (mostra "MySQL (triggers e procedures ativos)").
  - Dados iniciais: 45 produtos, 9 categorias, 4 cupons, usuários de teste e 30 pedidos de
    exemplo (criados pela API, passando pelas triggers).
  - Acentos gravados corretamente em utf8mb4.
- Provas de que as **triggers** fazem o trabalho (e não o C#):
  - Pedido pela API: a linha do tempo completa (PAGO > EM_SEPARACAO > SAIU_PARA_ENTREGA >
    ENTREGUE) foi gravada em `pedido_historico_status`; o estoque da Água Micelar caiu de 4
    para 2 e a trigger criou sozinha o **alerta de reposição** em `alertas_estoque`
    (mínimo 8); 3 pontos de GPS gravados em `trajeto_entregas`; o cancelamento devolveu o estoque.
  - **Compra pelo site** (http://localhost:8080, conta cliente, 3× Dipirona 500 mg, Pix):
    pedido nº 33, canal SITE, total R$ 22,40 (R$ 13,50 + R$ 8,90 de frete). Estoque da
    Dipirona no MySQL: **178 → 175**; histórico ganhou a linha "NULL → PAGO".
    Sem nenhum erro de JavaScript no navegador.
- Pontos de atenção do `CLAUDE.md` que foram resolvidos sem mudança:
  - Pomelo + triggers: inserir pedido com itens (3 triggers disparando) funciona sem erro do EF.
  - `RelatorioRepositorioMySql` chamando as procedures: funciona.
- Prints gerados: nenhum ainda (ficam para a tarefa 7).
- Problemas encontrados e como foram resolvidos: nenhum defeito. Observações:
  - O console do navegador mostra vários avisos "404" de `img/produtos/<produto>.jpg/.png/.webp`.
    Não é erro: o site procura uma foto real e, como a pasta `frontend/img/produtos/` ainda não
    tem fotos, desenha a embalagem. Some quando as fotos forem colocadas lá.
  - O MySQL mostra "�gua" na tela do PowerShell, mas está gravado certo ("Água"); é só a
    codificação do terminal. No MySQL Workbench aparece normal.

## 2026-10-30 — Tarefa 3: site servido pela própria API ASP.NET Core (RNF07)
- O que mudou: a API passou a entregar o site (pasta `frontend/`), além das rotas `/api`.
  Agora **http://localhost:5088** abre a página inicial e **http://localhost:5088/swagger**
  continua mostrando a documentação. Isso atende a disciplina "Desenvolvimento Web com .NET":
  o mesmo servidor ASP.NET Core entrega as páginas e os dados.
  - Como funciona (para o texto de arquitetura): no `Program.cs`, o middleware
    `UseDefaultFiles` + `UseStaticFiles` (com um `PhysicalFileProvider` apontando para a pasta
    `frontend/`) serve os arquivos HTML, CSS e JS. Uma rota da API, `GET /js/config.js`, entrega
    uma versão do arquivo de configuração que diz ao site "a API está no mesmo endereço"
    (`location.origin + '/api'`). Assim o site e a API ficam na **mesma origem** e o navegador
    nem precisa da liberação de CORS (some a chamada extra "OPTIONS" antes de cada requisição).
  - Os jeitos antigos continuam funcionando sem mudança: abrir `frontend/index.html` direto e
    o site do Nginx no Docker (http://localhost:8080) usam o arquivo `frontend/js/config.js`,
    que continua apontando para `http://localhost:5088/api`.
  - A API procura a pasta do site em `../../frontend` (rodando com `dotnet run`), em
    `./frontend` (Docker e Azure) ou no endereço da configuração `PastaDoSite`. Se não achar,
    avisa no log e abre só o Swagger, como antes.
- Arquivos:
  - `src/BemEstarOnline.Api/Program.cs`: nova seção "6) SITE SERVIDO PELA PRÓPRIA API" e a função
    `LocalizarPastaDoSite`. Saiu o redirecionamento de "/" para o Swagger (agora "/" é o site).
  - `src/BemEstarOnline.Api/Properties/launchSettings.json`: o Visual Studio abre o site, e não mais o Swagger.
  - `docker-compose.yml`: a API recebe a pasta `frontend` (volume somente leitura em `/app/frontend`).
  - `.github/workflows/ci-cd.yml`: o job "Compilar API" copia `frontend/` para dentro do pacote
    publicado, então no Azure o site também é servido pela API.
  - `frontend/js/config.js`: só o comentário mudou (explica os dois casos).
  - `GUIA-SIMPLES-DE-EXECUCAO.txt`, `README.md` e `CLAUDE.md`: novo endereço do site (5088). No
    guia também foram corrigidos: a pasta do projeto (`Documents`, não `Downloads`) e a versão do
    .NET (8 ou mais nova; testado com o .NET 10).
- Rotas/tabelas/telas novas ou alteradas:
  - `GET /` → página inicial do site (antes redirecionava para o Swagger).
  - `GET /js/config.js` → configuração do site gerada pela API (não aparece no Swagger).
  - Todas as páginas (`/index.html`, `/admin.html`, `/carrinho.html`...) também em http://localhost:5088.
  - Nenhuma tabela e nenhum DTO mudaram; o app MAUI não é afetado.
- Afeta qual etapa do PIM: Etapa 4 (web, RNF07 passa a "Feito"), Etapa 6 (arquitetura: o site agora
  é servido pela camada de apresentação ASP.NET Core) e Etapa 8 (Docker/Azure).
- Testes feitos (todos passaram):
  - Modo fácil (API rodando à parte na porta 5099, porque o Docker ocupava a 5088): site, painel,
    CSS, JS, Swagger e `/api` respondem; o config gerado aponta para a própria API; login e
    **compra pelo site** (pedido nº 31) com todas as chamadas indo para o mesmo endereço do site.
  - Docker: http://localhost:5088 (site pela API, banco MySQL) mostra os produtos, e o
    **painel do admin** abriu com o gráfico, "Banco: MySQL (triggers e procedures ativos)" e o
    alerta de reposição. http://localhost:8080 (Nginx) continua funcionando, falando com a API na 5088.
  - Nenhum erro de JavaScript nem de CORS no navegador.
- Prints gerados: nenhum ainda (ficam para a tarefa 7).
- Problemas encontrados e como foram resolvidos: nenhum. Observação: o teste de abrir o
  `index.html` direto do disco não pôde ser feito pelo navegador de testes, mas esse caminho usa
  o mesmo `config.js` do Nginx (testado) e o arquivo não mudou.

## 2026-10-30 — Tarefa 4: app .NET MAUI compilado e testado no Windows
- O que mudou: o app compilou na primeira tentativa para **Windows e Android** (0 erros). Ao testar
  no Windows (contra a API do Docker, com MySQL), apareceram 4 defeitos, todos corrigidos:
  1. **Mapa bloqueado pelo OpenStreetMap.** O app montava a página do mapa dentro do próprio
     WebView (HTML "sem endereço"), e o OpenStreetMap recusa mapas que não dizem de qual site vieram
     (aparecia "Access blocked"). Agora a página do mapa é um arquivo do site, `frontend/mapa-app.html`,
     entregue pela API (graças à tarefa 3), e o app abre o endereço
     `http://<servidor>/mapa-app.html?loja=...&destino=...&entregador=...`. O entregador continua
     andando sem recarregar a página (o app chama `atualizar(lat, lng)` dentro do mapa).
  2. **Pergunta de localização repetida.** Se a pessoa negasse a localização, o app perguntaria de
     novo a cada 10 segundos. Agora pergunta uma vez; se negar, o app usa o "Simular trajeto".
  3. **"Categoria" invisível** no catálogo (título da caixa de categorias em branco sobre fundo claro).
  4. **Leitor de tela na lista de pedidos** lia um texto técnico ("ItemTemplateContext"). Agora lê
     "Pedido nº 34, 30/10/2026 18:00, 2 item(ns), R$ 43,82, Entregue" (RNF02, acessibilidade).
  - Também: pacote nativo do SQLite atualizado para a 2.1.14 (a versão anterior tinha a falha de
    segurança GHSA-2m69-gcr7-jv3q, aviso NU1903) e troca de duas funções que o MAUI 10 marcou como
    antigas (`DisplayAlert` → `DisplayAlertAsync`; `MainPage` → `CreateWindow`).
- Arquivos:
  - Novo: `frontend/mapa-app.html` (página do mapa usada pelo app).
  - `src/BemEstarOnline.Mobile/Modelos/Entrega.cs`: `MapaHtml.Gerar` (HTML) virou `MapaHtml.Endereco` (URL).
  - `src/BemEstarOnline.Mobile/Views/EntregaDetalhePage.xaml.cs` e `PedidoDetalhePage.xaml.cs`:
    `HtmlWebViewSource` → `UrlWebViewSource`.
  - `src/BemEstarOnline.Mobile/ViewModels/EntregaDetalheViewModel.cs`: localização pedida uma vez só.
  - `src/BemEstarOnline.Mobile/ViewModels/PedidoDetalheViewModel.cs`: usa o novo endereço do mapa.
  - `src/BemEstarOnline.Mobile/ViewModels/BaseViewModel.cs`, `PerfilViewModel.cs`, `App.xaml.cs`: funções novas do MAUI 10.
  - `src/BemEstarOnline.Mobile/Modelos/Modelos.cs` (`PedidoResumo.DescricaoAcessivel`) e
    `Views/PedidosPage.xaml`: texto para o leitor de tela.
  - `src/BemEstarOnline.Mobile/Resources/Styles/Styles.xaml`: cor do título das caixas de escolha.
  - `src/BemEstarOnline.Mobile/BemEstarOnline.Mobile.csproj`: versões do SQLite.
  - `GUIA-SIMPLES-DE-EXECUCAO.txt`: aviso da pergunta de localização do Windows e do mapa precisar da API.
- Rotas/tabelas/telas novas ou alteradas: nova página `/mapa-app.html` (só para o app). Nenhuma rota de
  API, tabela ou DTO mudou.
- Afeta qual etapa do PIM: Etapa 5 (mobile; RF23 a RF29 testados de verdade), Etapa 3 (acessibilidade)
  e Etapa 6 (arquitetura: o mapa do app agora vem do servidor web).
- Testes feitos no app do Windows (todos passaram), com os dados conferidos no MySQL:
  - **Cliente:** login com a conta de teste, catálogo com os selos "Exige receita / Farmácia Popular /
    Uso contínuo", carrinho com o aviso de tarja vermelha, cupom BEMESTAR10 (R$ 38,80 − 10% + R$ 8,90 =
    R$ 43,82), endereço e Pix → **pedido nº 34, canal APP**; as triggers baixaram o estoque
    (Amoxicilina 39 → 38; Álcool 70% 127 → 126). Lista "Meus pedidos" e mapa do cliente
    ("Marcos Entregador está a caminho. Faltam cerca de 284 m") funcionando.
  - **Entregador:** aba Entregas; **aviso de pedido novo** apareceu sozinho com o app aberto:
    "Novo pedido para entregar! Nº 35: EXIGE RECEITA" (RF25); pegar o pedido (com alerta de receita);
    "Saí da farmácia" (status SAIU_PARA_ENTREGA); GPS do Windows; **"Simular trajeto"** (o ponto andou
    no app e no site do cliente ao mesmo tempo, de "983 m" para "616 m"); **confirmar entrega**:
    respondendo "Ainda não" à receita o pedido **não** é entregue; com "Sim, conferi" vira ENTREGUE com
    `receita_conferida = 1` (RF29). Linha do tempo gravada pelas triggers: PAGO > EM_SEPARACAO >
    SAIU_PARA_ENTREGA > ENTREGUE; 22 pontos em `trajeto_entregas`.
- Não testado: **emulador Android** (não há emulador instalado neste computador; criar um exige baixar
  alguns GB). A versão Android compila com 0 erros e 0 avisos.
- Avisos que ficaram (de propósito): 51 avisos MVVMTK0045 só na versão Windows. A ferramenta sugere outro
  jeito de escrever as propriedades, que só importa para compilação AOT (não usada no projeto).
- Prints gerados: nenhum na pasta `docs/prints` ainda (ficam para a tarefa 7). As telas acima foram vistas
  durante o teste.

## 2026-10-30 — Tarefa 5: Ouvidoria e canal de denúncia de discriminação (RF30)
- O que mudou: RF30 implementado (pode passar de "Pendente" para "Feito" no `docs/00-requisitos.md`).
  Qualquer pessoa, com ou sem conta, registra uma manifestação (denúncia de discriminação, reclamação de
  atendimento, problema na entrega, sugestão ou elogio), pode ficar **anônima**, recebe um **protocolo** e
  acompanha a resposta por ele. A equipe vê tudo no painel, com as denúncias de discriminação em primeiro
  lugar e destacadas, e responde.
- **Diferença em relação à especificação (de propósito):** o protocolo **não** é sequencial (OUV-2026-000123).
  Ele tem 8 letras e números sorteados, sem as letras que confundem (0/O, 1/I/L): ex.: `OUV-2026-7K3M9QXA`.
  Motivo: como a consulta pelo protocolo é pública (não pede login), um número em sequência deixaria qualquer
  pessoa adivinhar os protocolos dos outros (000122, 000121...) e ler as respostas de denúncias de
  discriminação. Com o sorteio, só quem tem o protocolo consulta. Isso protege a vítima (LGPD).
- Regras de privacidade (LGPD), garantidas no código e no banco:
  - Relato anônimo não guarda a conta (`id_usuario`) nem o `contato`, **mesmo se a pessoa estiver logada
    ou preencher o contato** (a API descarta). No banco, a CHECK `ck_ouvidoria_anonimo` recusa contato em
    relato anônimo.
  - A consulta pública pelo protocolo mostra só tipo, situação, datas e resposta: **nunca** o relato nem o contato.
  - Só a equipe (ADMIN/ATENDENTE) vê o relato completo.
- Banco (MER):
  - Nova tabela `ouvidoria` (em `01_ddl.sql` e no script completo): `id_ouvidoria` (PK), `id_usuario` (FK
    → usuarios, NULL, ON DELETE SET NULL), `tipo` ENUM('DISCRIMINACAO','ATENDIMENTO','ENTREGA','SUGESTAO','ELOGIO'),
    `subtipo_discriminacao` ENUM('RACIAL','RELIGIOSA','GENERO','DEFICIENCIA','IDADE','OUTRA') NULL, `descricao`
    TEXT, `anonimo` BOOLEAN, `contato` VARCHAR(150) NULL, `protocolo` VARCHAR(20) UNIQUE, `status`
    ENUM('ABERTA','EM_ANALISE','RESPONDIDA','ENCERRADA'), `resposta` TEXT NULL, `criado_em`, `respondido_em` NULL.
    Relacionamento: usuarios 1 — 0..N ouvidoria (opcional).
  - Restrições CHECK: `ck_ouvidoria_anonimo` (anônimo sem contato) e `ck_ouvidoria_subtipo` (subtipo só em
    denúncia de discriminação). Índice `idx_ouvidoria_status (status, tipo)`.
  - Para bancos que já existiam: `database/atualizacoes/2026-10-30-ouvidoria.sql` (`CREATE TABLE IF NOT EXISTS`,
    não apaga nada). Foi aplicado no banco do Docker deste computador.
  - Não precisou de trigger: as regras da ouvidoria ficam na entidade `Ouvidoria` (C#), então o comportamento
    é igual nos modos MySQL e memória.
- API (rotas novas):
  - `POST /api/ouvidoria` (público) → devolve `{ protocolo, tipo, status, criadoEm, resposta, respondidoEm }`.
    Se a pessoa estiver logada e não for anônima, o id da conta vem do token (regra 7 do projeto).
  - `GET /api/ouvidoria/{protocolo}` (público) → situação e resposta (aceita minúsculas).
  - `GET /api/admin/ouvidoria` (equipe) → todas, na ordem: em aberto primeiro; entre elas, discriminação primeiro;
    depois as mais novas.
  - `PATCH /api/admin/ouvidoria/{id}` (equipe) `{ status?, resposta? }` → responder já marca "Respondida";
    "Respondida" sem resposta é recusado; manifestação "Encerrada" não pode ser reaberta.
- Arquivos novos: `Dominio/Entidades/Ouvidoria.cs`, `Aplicacao/Servicos/OuvidoriaServico.cs`,
  `Controllers/OuvidoriaController.cs`, `frontend/ouvidoria.html`, `frontend/js/paginas/ouvidoria.js`,
  `database/atualizacoes/2026-10-30-ouvidoria.sql`.
- Arquivos alterados: `Dominio/Constantes.cs` (tipos, subtipos e situações), `Dominio/Interfaces/IRepositorios.cs`,
  `Infraestrutura/Repositorios/Repositorios.cs`, `Infraestrutura/Dados/BemEstarDbContext.cs`,
  `Infraestrutura/Dados/DadosIniciais.cs` (3 manifestações de exemplo: denúncia racial anônima, reclamação já
  respondida e elogio), `Aplicacao/Dtos/Dtos.cs` (DTOs novos; nenhum DTO antigo mudou), `Aplicacao/Mapeamentos.cs`,
  `Aplicacao/Interfaces/IServicos.cs`, `Controllers/AdminController.cs`, `Program.cs`, `database/01_ddl.sql`,
  `database/completo/script_completo_bemestar.sql`, `frontend/js/app.js` (nomes dos tipos e link no rodapé),
  `frontend/js/paginas/admin.js` e `frontend/admin.html` (seção Ouvidoria), `frontend/conta.html` (botão),
  `frontend/css/estilo.css`, `GUIA-SIMPLES-DE-EXECUCAO.txt`, `CLAUDE.md`.
- Telas novas:
  - **`ouvidoria.html`** (link no rodapé de todas as páginas, coluna Atendimento, e botão em Minha conta):
    formulário com tipo, tipo de discriminação (só aparece quando é denúncia), relato com contador de letras,
    "Quero ficar anônimo" (esconde e apaga o contato) e contato opcional; texto explicando que discriminação é
    crime (Lei 7.716/1989), o Estatuto da Igualdade Racial (Lei 12.288/2010), o 190 e o Disque 100; protocolo com
    botão "Copiar"; consulta pelo protocolo (também pelo link `ouvidoria.html?protocolo=...`).
  - **Painel > Ouvidoria**: contador de manifestações em aberto no menu; faixa de alerta com o número de denúncias
    de discriminação em aberto; filtros "Em aberto" e "Todas"; cada manifestação mostra protocolo, data, quem enviou
    (ou "Anônimo"), contato, relato e resposta; campo de resposta e troca de situação.
- Afeta qual etapa do PIM: **Etapa 3** (Relações étnico-raciais e diversidade: canal de denúncia, Lei 7.716/1989,
  Lei 12.288/2010), Etapa 4 (web), Etapa 7 (banco: tabela, FK e CHECKs novas no MER) e LGPD (anonimato real).
- Outras correções feitas nesta tarefa:
  - **Painel no celular:** o painel inteiro ficava cortado na direita em telas pequenas (a fileira de botões
    do menu esticava a coluna para ~1100 px). Corrigido no CSS (`minmax(0, 1fr)`); agora o menu rola de lado e o
    conteúdo cabe na tela (RNF03, site responsivo).
  - **Versão velha do site no navegador:** depois de atualizar os arquivos, o navegador às vezes continuava usando
    o `app.js` antigo (guardado em cache). A API agora manda o cabeçalho `Cache-Control: no-cache` nos arquivos do
    site, então o navegador sempre confere se há versão nova.
  - Campos escondidos (`hidden`) dentro de `.campo` apareciam mesmo assim por causa do CSS; corrigido.
- Testes feitos (todos passaram), nos **dois modos** (memória e Docker/MySQL), 22 verificações em cada:
  registrar anônimo, identificado (logado) e visitante sem login; recusar discriminação sem tipo, texto curto e tipo
  inválido; consultar pelo protocolo (com minúsculas) sem mostrar relato nem contato; protocolo inexistente (404);
  cliente barrado na lista da equipe (403); discriminação em primeiro na lista; anônimo gravado sem conta e sem
  contato (conferido também direto no MySQL); equipe muda para "Em análise", responde (vira "Respondida") e o
  cliente vê a resposta; "Respondida" sem resposta recusada; encerrar; encerrada não reabre.
  No navegador: formulário (erros, campo de discriminação aparecendo só quando precisa, anonimato apagando o contato,
  contador), protocolo na tela, painel com a faixa de alerta e a denúncia em primeiro, resposta pelo painel e a
  pessoa vendo a resposta pelo protocolo. Página e painel conferidos também na largura de celular (375 px).
- Prints gerados: nenhum ainda (ficam para a tarefa 7: `14-painel-ouvidoria.png`).

## 2026-10-30 — Tarefa 6: roteiro completo da apresentação (Passo 5 do guia), do começo ao fim
- O que foi feito: os 12 passos do roteiro foram executados em sequência, como na apresentação, no
  **modo completo** (Docker + MySQL), com o site no navegador e o app do Windows ao mesmo tempo.
  **Todos os 12 passos funcionaram.** No caminho apareceram 7 detalhes, todos corrigidos e testados de novo.
- Resultado de cada passo:
  1. Página inicial, busca "dor" (10 produtos, com sugestões enquanto digita), produto, "Ouvir a descrição em
     voz alta", letra grande, alto contraste e tema escuro: OK.
  2. Cadastro com **nome social** ("Carlos Eduardo Teste" / "Carla Teste"): o site passa a chamar de "Olá, Carla";
     em Meus dados o CPF aparece mascarado (578.\*\*\*.\*\*\*-30): OK.
  3. Compra de 2 produtos (Amoxicilina, com receita, e Dipirona), cupom **BEMESTAR10** (R$ 29,40 − R$ 2,94 + frete
     R$ 8,90 = **R$ 35,36**), endereço novo com **CEP automático** (cidade e estado preenchidos sozinhos) e Pix:
     pedido nº 36: OK.
  4. Linha do tempo do pedido na confirmação: OK.
  5. Painel do admin: gráfico, estoque no mínimo e validade (procedures, "Banco: MySQL"); "Começar separação": OK.
  6. App do entregador: **aviso "Nº 36: EXIGE RECEITA"** chegou sozinho; pegar o pedido, "Saí da farmácia",
     "Simular trajeto": OK.
  7. Site da cliente (Minha conta > pedido > Ver no mapa): o entregador anda e a distância diminui: OK.
  8. App: "Confirmar entrega" exigindo a receita → ENTREGUE, `receita_conferida = 1`, linha do tempo completa
     gravada pelas triggers e 22 pontos de trajeto: OK.
  9. Produtos e estoque: Amoxicilina **37 → 36** e Dipirona **175 → 174** (trigger); Reposição com o alerta
     automático: OK.
  10. Excluir minha conta (senha + "EXCLUIR"): a conta foi **anonimizada** ("Conta excluída", sem CPF, telefone e
      e-mail reais), o login antigo passou a ser recusado, o pedido nº 36 continua guardado (obrigação fiscal) e o
      endereço usado no pedido ficou só com bairro/cidade ("Endereço removido"): OK.
  11. Swagger: Authorize com o token e chamada de `/api/auth/eu` respondendo os dados da conta: OK.
  12. Ouvidoria: página e link do rodapé OK no Docker (o fluxo completo foi testado na tarefa 5).
  - Extra: entrega de um pedido **pago em dinheiro** (nº 35): o app pede a receita e pergunta "Recebeu R$ 33,80 em
    dinheiro do cliente?"; ao confirmar, o pagamento vira APROVADO.
- Problemas encontrados e corrigidos:
  1. **"Simular trajeto" não saía da farmácia** quando o GPS do computador estava liberado: o motoboy começava na
     posição do computador (às vezes ao lado do cliente) e quase não andava no mapa. Agora a simulação sempre
     começa na farmácia (`EntregaDetalheViewModel.cs`, posição da farmácia num lugar só: `Farmacia`).
  2. **Rodapé ilegível no tema escuro automático** (computador em modo escuro): fundo claro com letra clara.
     Corrigido no `estilo.css` (RNF02, contraste).
  3. **Leitura em voz alta com ponto repetido** ("febre.. Preço"). Corrigido em `js/paginas/produto.js`.
  4. **Botões iguais no painel** ("Abrir", "Estoque", "Editar" em todas as linhas): o leitor de tela não sabia qual
     era qual. Agora cada um tem nome completo para o leitor de tela ("Abrir pedido nº 36", "Editar Dipirona Sódica
     500mg"), sem mudar o que aparece na tela (`js/paginas/admin.js`, RNF02).
  5. **"Observação:" vazio** na tela de entrega do app. Agora só aparece quando o cliente escreveu algo
     (`Modelos/Entrega.cs` e `Views/EntregaDetalhePage.xaml`).
  6. **Versão velha do site no navegador** também no Nginx (porta 8080): novo arquivo `docker/nginx-site.conf`
     (ligado no `docker-compose.yml`) com `Cache-Control: no-cache`, igual ao que a API já faz.
  7. **Painel ignorava o endereço com #seção** quando já estava aberto (ex.: colar `admin.html#ouvidoria`). Agora troca
     a seção.
- Arquivos alterados: `src/BemEstarOnline.Mobile/ViewModels/EntregaDetalheViewModel.cs`,
  `src/BemEstarOnline.Mobile/Modelos/Entrega.cs`, `src/BemEstarOnline.Mobile/Views/EntregaDetalhePage.xaml`,
  `frontend/css/estilo.css`, `frontend/js/paginas/produto.js`, `frontend/js/paginas/admin.js`, `docker-compose.yml`,
  novo `docker/nginx-site.conf`, `GUIA-SIMPLES-DE-EXECUCAO.txt` (roteiro: deixar o app do entregador aberto antes do
  passo 5; usar 5088 para o cliente e 8080 para o admin ao mesmo tempo; Ctrl+F5 se o site parecer velho).
- Rotas/tabelas/telas novas: nenhuma.
- Afeta qual etapa do PIM: 3 (acessibilidade), 4 (web), 5 (mobile), 7 (triggers comprovadas de novo) e 8 (Docker).
- Observações (não são defeitos):
  - Por um instante, antes do desenho da embalagem aparecer, a foto do produto pode piscar como "imagem quebrada": o
    site tenta achar uma foto real (.jpg, .png, .webp) primeiro. Some quando houver fotos na pasta `frontend/img/produtos/`.
  - Na busca, "dor" também traz "Desodorante" (contém as letras "dor"). É o comportamento normal de busca por texto.
  - Os alertas de reposição só nascem quando uma **venda** deixa o produto no mínimo (trigger). Produtos que já
    começam abaixo do mínimo no seed (Glibenclamida, Butilbrometo) aparecem em "Estoque no mínimo", mas não em "Reposição".
- Prints gerados: nenhum ainda (tarefa 7).

## 2026-10-30 — Tarefa 7: prints das telas para o trabalho escrito
- O que foi feito: **28 dos 29 prints** possíveis hoje estão em `docs/prints/` (só falta o 29, do GitHub), com os nomes da seção "Prints" do
  `docs/00-requisitos.md`. Todos no modo completo (Docker + MySQL), com os dados de demonstração.
  - **Site e painel (01 a 15):** tirados com o Microsoft Edge em modo invisível, controlado por script, na
    resolução de notebook (1366 × 900), tema claro. Alguns são a página inteira (04, 05, 09, 10).
  - **App (16 a 25):** janela do app no Windows no formato de celular (em pé). Cliente: login, produtos,
    produto, carrinho com cupom, pedidos, mapa do entregador, perfil com alto contraste. Entregador: aviso
    "Novo pedido para entregar! Nº 37: EXIGE RECEITA", mapa com "Simulando trajeto: 50%" e a pergunta
    "Receita obrigatória".
  - **Docker (28):** Docker Desktop com o projeto `bemestaronline` e os 3 containers rodando
    (site-1 Nginx 8080, api-1 5088, banco-1 MySQL 3307).
  - A lista do que existe e do que falta ficou em `docs/prints/LEIA-ME.txt`.
  - **MySQL Workbench (26 e 27)**, tirados depois, com o computador livre:
    - 26: consulta que lista as **5 triggers** (com o momento: BEFORE/AFTER INSERT/UPDATE e a tabela) e as
      **4 procedures**, mais o painel Navigator com as procedures abertas.
    - 27: diagrama gerado por **Database > Reverse Engineer** do banco do Docker (17 tabelas, chaves primárias,
      chaves estrangeiras e relacionamentos, incluindo a tabela nova `ouvidoria`).
    - **Novo para o trabalho:** `docs/diagramas/mer-banco-de-dados.png` (diagrama inteiro em alta resolução,
      1886 × 1172, exportado pelo Workbench) e `docs/diagramas/mer-banco-de-dados.mwb` (modelo editável).
      Bom para a figura do MER na Etapa 7.
    - Nada foi salvo nas configurações do Workbench (conexão aberta direto, sem guardar senha).
- Faltam:
  - **29 (GitHub Actions):** depende de enviar o projeto ao GitHub (tarefa 10).
  - **30 a 38:** telas das melhorias da tarefa 8, ainda não implementadas.
- Problemas encontrados nos prints e corrigidos:
  1. **Gráfico do painel cortava o "R"** de "R$ 472,28" no eixo da esquerda: espaço do eixo aumentado
     (`js/paginas/admin.js`, função `grafico`).
  2. **Busca de produtos do painel em LETRAS MAIÚSCULAS** (herdava o estilo do campo de cupom): corrigido
     (`js/paginas/admin.js`).
  3. **App, alto contraste: o botão liga/desliga desligado ficava invisível** (bolinha cinza escura no fundo
     preto). Agora a bolinha do Switch usa a cor nova `CorBolinhaChave` (branca no alto contraste)
     (`Resources/Styles/Colors.xaml`, `Resources/Styles/Styles.xaml`, `Servicos/AcessibilidadeServico.cs`). RNF02.
- Ajustes nos dados de demonstração (não apagam nada): dois relatos de teste da ouvidoria estavam com acento
  quebrado ("candomblÃ©") e foram corrigidos; o endereço de teste "Rua Teste, 10" virou "Rua das Acácias, 45".
- Arquivos alterados: `frontend/js/paginas/admin.js`, `src/BemEstarOnline.Mobile/Resources/Styles/Colors.xaml`,
  `src/BemEstarOnline.Mobile/Resources/Styles/Styles.xaml`, `src/BemEstarOnline.Mobile/Servicos/AcessibilidadeServico.cs`,
  `GUIA-SIMPLES-DE-EXECUCAO.txt` (seção "PRINTS DO MYSQL WORKBENCH", para refazer os prints 26 e 27 se o banco
  mudar), `docs/prints/LEIA-ME.txt`, `docs/diagramas/LEIA-ME.md`, os 28 arquivos `.png` de prints e os dois
  arquivos novos do MER em `docs/diagramas/`.
- Afeta qual etapa do PIM: todas as que usam figuras (3, 4, 5, 7 e 8).

## 2026-10-31 — Tarefa 8, parte 1: RF35 Gestão da equipe (+ "Alterar senha" do RF34)
- O que mudou: **RF35 implementado** (pode passar para "Feito"). O administrador cadastra atendentes,
  entregadores e administradores pelo painel, muda o perfil, desativa/reativa contas e redefine senhas.
  Quem é cadastrado (ou tem a senha redefinida) recebe uma **senha provisória** e é **obrigado a criar a própria
  senha no primeiro acesso**, no site e no app. Para isso, a parte **"Alterar senha" do RF34** também ficou pronta
  (site e app). Falta do RF33/RF34 só o **"Esqueci minha senha"** (próxima parte).
- **Critério de aceite cumprido** (Docker + MySQL): o admin cadastrou a entregadora "Renata Alves de Souza" pelo
  painel → ela entrou no **app** com a senha provisória → o app abriu direto a tela obrigatória "Alterar senha" →
  depois de criar a senha nova, apareceu a aba **Entregas** ("Olá, Renata"). No banco, `trocar_senha` voltou a 0.
- Regras (todas testadas):
  - Só **ADMIN** acessa a gestão da equipe (atendente e cliente recebem 403).
  - Cadastro valida CPF (dígitos verificadores), e-mail e CPF únicos; perfil só ATENDENTE, ENTREGADOR ou ADMIN
    (**cliente não vira equipe por aqui**).
  - O administrador **não pode desativar a própria conta, nem mudar o próprio perfil, nem redefinir a própria senha**
    pela gestão (usa "Alterar senha"). A farmácia **nunca fica sem um ADMIN ativo** (regra conferida no código; na
    prática ela já é garantida pela regra anterior, porque quem faz a mudança é sempre um admin ativo).
  - **Login de conta desativada é recusado** ("E-mail ou senha incorretos", sem revelar o motivo).
  - **Segurança extra (novo middleware `VerificacaoDeConta`):** o token vale 8 horas, mas agora a API confere a conta
    em toda requisição. Conta **desativada** → sai do sistema **na hora** (401). **Perfil mudado** → precisa entrar de
    novo (401). **Senha provisória** ainda não trocada → tudo bloqueado (403), menos "alterar senha" e "quem sou eu".
  - Alterar senha pede a senha atual, exige 6+ caracteres e recusa senha nova igual à atual. A senha antiga deixa de
    funcionar na hora.
- Banco (MER):
  - Nova coluna **`usuarios.trocar_senha BOOLEAN NOT NULL DEFAULT FALSE`** (em `01_ddl.sql` e no script completo).
  - Para bancos que já existiam: `database/atualizacoes/2026-10-31-equipe-trocar-senha.sql` (confere antes de criar,
    pode rodar mais de uma vez, não apaga nada). Já aplicado no Docker deste computador.
- API (rotas novas):
  - `GET /api/admin/equipe` (ADMIN): funcionários, ativos primeiro.
  - `POST /api/admin/equipe` (ADMIN) `{ nome, email, cpf, celular, perfil, senhaInicial }` → senha provisória.
  - `PATCH /api/admin/equipe/{id}` (ADMIN) `{ perfil?, ativo? }`.
  - `POST /api/admin/equipe/{id}/redefinir-senha` (ADMIN) `{ novaSenha }` → nova senha provisória.
  - `POST /api/auth/alterar-senha` (logado) `{ senhaAtual, novaSenha }` → devolve o usuário atualizado.
  - **Contrato JSON alterado:** `UsuarioDto` (resposta do login e de `/api/auth/eu`) ganhou o campo **`trocarSenha`**.
    Site e app foram ajustados.
- Arquivos novos: `Aplicacao/Servicos/EquipeServico.cs`, `Controllers/EquipeController.cs`,
  `Middlewares/VerificacaoDeConta.cs`, `frontend/alterar-senha.html`, `frontend/js/paginas/alterar-senha.js`,
  `src/BemEstarOnline.Mobile/ViewModels/AlterarSenhaViewModel.cs`, `Views/AlterarSenhaPage.xaml(.cs)`,
  `database/atualizacoes/2026-10-31-equipe-trocar-senha.sql`.
- Arquivos alterados: `Dominio/Entidades/Usuario.cs` (`TrocarSenha`, `DefinirSenhaProvisoria`, `DefinirNovaSenha`),
  `Dominio/Constantes.cs` (`Perfis.Funcionarios`), `Aplicacao/Dtos/Dtos.cs`, `Aplicacao/Mapeamentos.cs`,
  `Aplicacao/Interfaces/IServicos.cs`, `Aplicacao/Servicos/AuthServico.cs`, `Controllers/AuthController.cs`,
  `Program.cs`, `database/01_ddl.sql`, `database/completo/script_completo_bemestar.sql`, `frontend/js/app.js`
  (trava global da senha provisória, mensagem da API quando a conta é desativada), `frontend/js/paginas/login.js`
  (senha provisória; e o **entregador não é mais mandado para o painel**, que é só de atendente/admin — era um
  defeito antigo), `frontend/js/paginas/conta.js` (botão "Alterar senha" em Meus dados), `frontend/admin.html` e
  `frontend/js/paginas/admin.js` (seção "Gestão da equipe"), `frontend/css/estilo.css`, app: `Modelos/Modelos.cs`,
  `Servicos/SessaoServico.cs`, `ViewModels/LoginViewModel.cs`, `ViewModels/PerfilViewModel.cs`,
  `Views/PerfilPage.xaml` (botão "Alterar senha"), `AppShell.xaml.cs`, `MauiProgram.cs`.
- Telas novas:
  - **Painel > Gestão da equipe** (só aparece para o ADMIN): lista com perfil, situação e "Senha provisória"; botão
    "Nova pessoa" (com senha provisória gerada automaticamente, sem letras que confundem, como 0/O e 1/l); trocar
    perfil; Desativar/Reativar (pede confirmação); Redefinir senha. A senha provisória aparece uma única vez, para o
    admin passar à pessoa.
  - **Site: `alterar-senha.html`** (Minha conta > Meus dados > "Alterar senha"). No modo obrigatório mostra o aviso
    "Sua senha é provisória", e **qualquer outra página do site redireciona para ela** até a troca.
  - **App: tela "Alterar senha"** (Perfil > "Alterar senha"). No modo obrigatório aparece logo depois do login, com
    aviso vermelho, sem botão "Voltar" (só "Sair da conta").
- Testes: 33 verificações da API nos **dois modos** (memória e MySQL), todas OK; fluxo no site (admin cadastra
  atendente → atendente entra → é levado à troca de senha → painel sem a "Gestão da equipe"); fluxo no app (aceite acima).
- Afeta qual etapa do PIM: Etapa 4 (web: perfis e estrutura administrativa), Etapa 5 (app), Etapa 6 (arquitetura:
  middleware de segurança), Etapa 7 (coluna nova no MER) e LGPD/segurança.
- Prints gerados: `docs/prints/31-painel-equipe.png`.

## 2026-10-31 — Tarefa 8, parte 2: RF33 Esqueci minha senha (RF33 e RF34 completos)
- O que mudou: **RF33 implementado** e, com a parte anterior, **RF34 também completo** (os dois podem passar para
  "Feito"). No login do site e do app há "Esqueci minha senha": a pessoa digita o e-mail e recebe um link para criar
  uma senha nova. O link abre `redefinir-senha.html` (no site, também quando o pedido foi feito pelo app).
- **Critérios de aceite cumpridos** (testados nos modos memória e MySQL):
  - E-mail **inexistente recebe exatamente a mesma resposta** ("Se o e-mail estiver cadastrado, enviamos um link...")
    e nenhum e-mail é gerado.
  - Link **usado** e link **vencido** (mais de 30 minutos) são **recusados**.
  - Depois de criar a senha nova, a **senha antiga deixa de funcionar**.
- Outras regras (todas testadas):
  - **No máximo 3 pedidos por e-mail por hora** (acima disso, a resposta é a mesma, mas nada é enviado).
  - **Um pedido novo anula o link anterior** (só o mais recente funciona).
  - Conta desativada não recebe link. Falha no envio do e-mail também não muda a resposta (não revela se a conta existe).
  - O link tem um código aleatório de 32 bytes (impossível de adivinhar). **No banco fica só o SHA-256 do código**
    (conferido direto no MySQL: o código "puro" não aparece em lugar nenhum), igual à senha, que fica só como hash.
  - **Segurança contra links falsos:** o endereço do link vem da configuração `EnderecoDoSite` do `appsettings.json`,
    nunca do endereço que chega na requisição. Isso evita o ataque "Host header injection", em que alguém faria a API
    mandar um link apontando para outro site para roubar o código.
  - Ao abrir o link, a página **tira o código da barra de endereço** (não fica no histórico do navegador).
- E-mail (como pede a especificação): interface **`IEmailServico`** com duas implementações:
  - **`EmailDemonstracao`** (padrão): salva cada e-mail como `.html` em `src/BemEstarOnline.Api/emails-enviados/` e
    escreve o link no log da API. Funciona sem servidor de e-mail. No Docker, essa pasta aparece no computador (volume).
    A pasta está no `.gitignore` (os e-mails têm links válidos) e no `.dockerignore`.
  - **`EmailSmtp`**: envia de verdade quando a seção `Smtp` do `appsettings.json` está preenchida (Host, Porta,
    Usuario, Senha, Remetente). A escolha é automática no `Program.cs`.
- Banco (MER):
  - Nova tabela **`redefinicoes_senha`** (`id_redefinicao` PK, `id_usuario` FK → usuarios ON DELETE CASCADE,
    `token_hash CHAR(64)` UNIQUE, `expira_em`, `usado_em` NULL, `criado_em`; índice `(id_usuario, criado_em)`).
    Relacionamento: usuarios 1 — 0..N redefinicoes_senha. Em `01_ddl.sql` e no script completo.
  - Para bancos que já existiam: `database/atualizacoes/2026-10-31-redefinicoes-senha.sql` (IF NOT EXISTS, não apaga
    nada). Já aplicado no Docker deste computador.
- API (rotas novas, públicas):
  - `POST /api/auth/esqueci-senha` `{ email }` → sempre `{ mensagem: "Se o e-mail estiver cadastrado..." }`.
  - `POST /api/auth/redefinir-senha` `{ token, novaSenha }` → cria a senha nova (link de uso único, 30 minutos).
  - Nenhum DTO antigo mudou.
- Arquivos novos: `Dominio/Entidades/RedefinicaoSenha.cs`, `Aplicacao/Servicos/RecuperacaoSenhaServico.cs`,
  `Infraestrutura/Email/Email.cs` (`SmtpOpcoes`, `EmailSmtp`, `EmailDemonstracao`), `frontend/esqueci-senha.html`,
  `frontend/redefinir-senha.html`, `frontend/js/paginas/esqueci-senha.js`, `frontend/js/paginas/redefinir-senha.js`,
  `database/atualizacoes/2026-10-31-redefinicoes-senha.sql`.
- Arquivos alterados: `Aplicacao/Interfaces/IServicos.cs` (`IEmailServico`, `MensagemEmail`, `IRecuperacaoSenhaServico`),
  `Aplicacao/Dtos/Dtos.cs`, `Controllers/AuthController.cs`, `Dominio/Interfaces/IRepositorios.cs`,
  `Infraestrutura/Repositorios/Repositorios.cs`, `Infraestrutura/Dados/BemEstarDbContext.cs`, `Program.cs`,
  `appsettings.json` (`EnderecoDoSite` e seção `Smtp`), `database/01_ddl.sql`, script completo, `docker-compose.yml`
  (volume de `emails-enviados`), `.gitignore`, `.dockerignore`, `frontend/login.html` (link "Esqueci minha senha"),
  app: `ViewModels/LoginViewModel.cs` (comando `EsqueciSenha`), `Views/LoginPage.xaml` (botão), `Modelos/Modelos.cs`
  (`RespostaMensagem`).
- Testes: 14 verificações no modo memória e 16 no MySQL (inclui link vencido e "só o hash no banco"), todas OK;
  página `redefinir-senha.html` testada de ponta a ponta no navegador; botão do app testado (o e-mail foi gerado).
- Observação: no celular de verdade ou no emulador, o link do e-mail precisa apontar para um endereço que o celular
  enxergue. Para isso, troque `EnderecoDoSite` no `appsettings.json` (ex.: o IP do computador na rede Wi-Fi).
- Afeta qual etapa do PIM: Etapa 4 (web), Etapa 5 (app), Etapa 6 (arquitetura: interface `IEmailServico` com duas
  implementações, exemplo de polimorfismo e inversão de dependência), Etapa 7 (tabela nova no MER) e segurança/LGPD.
- Prints gerados: `docs/prints/30-site-esqueci-senha.png` (página depois de pedir o link) e
  `docs/prints/30-email-gerado.png` (o e-mail salvo na pasta `emails-enviados`).

## 2026-10-31 — Tarefa 8, parte 3: RF36, RF37 e RF38 (app completo: criar conta, endereços e assinaturas)
- O que mudou: o app agora faz sozinho o que antes só dava para fazer no site. **RF36, RF37 e RF38 podem passar
  para "Feito".** A API não mudou (o app usa as mesmas rotas do site) e nenhum DTO mudou.
- **RF36 — Criar conta** (botão "Criar conta" na tela de login):
  - Mesmos campos e validações do site: nome e sobrenome, **nome social** (com a explicação "é assim que vamos te
    chamar no site, no app e na entrega"), data de nascimento **DD/MM/AAAA** (não aceita data inexistente, antes de
    1900 ou no futuro), **CPF com os dígitos verificadores**, celular, e-mail, senha (mínimo 6) repetida.
  - Caixa de **consentimento da LGPD** obrigatória, com o botão "Ler a Política de Privacidade" (abre a página
    `privacidade.html` do site no navegador; essa página será criada no RF41).
  - CPF, data e celular ganham a máscara sozinhos quando a pessoa termina de digitar (529.982.247-25, 20/05/1998,
    (11) 99123-4567).
  - Usa `POST /api/auth/registrar` e **já entra logado** (vai para os produtos e o app chama a pessoa pelo nome social).
- **RF37 — Endereços** (Perfil > "Meus endereços" e botão **"Novo endereço" no carrinho**):
  - Lista, adiciona e remove (`GET`, `POST` e `DELETE /api/enderecos`). O primeiro endereço já vem marcado como principal.
  - **CEP automático (ViaCEP):** ao digitar os 8 números, preenche rua, bairro, cidade e estado. CEP que não existe
    mostra "CEP não encontrado. Preencha o endereço à mão."
  - **"Usar minha localização atual":** grava a latitude e a longitude do **GPS do celular** (pede permissão).
    Sem GPS, o app procura o ponto no mapa pelo próprio endereço (OpenStreetMap/Nominatim), como o site já fazia.
    Os endereços com ponto no mapa mostram "Com localização no mapa" (usado no mapa da entrega).
  - Pelo carrinho: salva e **volta sozinho para o carrinho com o endereço novo já escolhido**. Antes, sem endereço,
    o app mandava a pessoa cadastrar no site; agora pergunta "Cadastrar um agora?".
- **RF38 — Assinaturas** (Perfil > "Assinaturas" e botão **"Assinar com 10% de desconto"** nos produtos de uso contínuo):
  - Igual ao site: escolher o remédio de uso contínuo (o atalho do produto já abre com ele escolhido), o nome de quem
    vai tomar, 1 a 4 caixas e o intervalo (15, 30, 60 ou 90 dias). Mostra o **preço normal riscado, o valor por entrega
    com 10% de desconto e quanto a pessoa economiza**.
  - Lista as assinaturas (ativas primeiro) com **Pausar, Retomar e Cancelar** (`PATCH /api/assinaturas/{id}/status`;
    cancelar pede confirmação).
  - Sem login, a tela pede para entrar.
- Arquitetura (para a Etapa 6): telas novas no padrão MVVM (`ViewModel` + `Page`), registradas na injeção de dependência
  (`MauiProgram.cs`) e nas rotas do Shell (`AppShell.xaml.cs`: `criar-conta`, `enderecos`, `assinaturas`). As telas
  avisam umas às outras quando os dados mudam (`WeakReferenceMessenger`, "mensageiro" do CommunityToolkit.Mvvm): se
  "Assinaturas" estiver aberta em duas abas, as duas ficam iguais.
- Arquivos novos (app): `ViewModels/CriarContaViewModel.cs`, `ViewModels/EnderecosViewModel.cs`,
  `ViewModels/AssinaturasViewModel.cs`, `Views/CriarContaPage.xaml(.cs)`, `Views/EnderecosPage.xaml(.cs)`,
  `Views/AssinaturasPage.xaml(.cs)`, `Servicos/Validacao.cs` (CPF, data, e-mail e máscaras),
  `Servicos/LocalizacaoServico.cs` (ViaCEP, GPS e Nominatim), `Servicos/Mensagens.cs`.
- Arquivos alterados (app): `Servicos/ApiServico.cs` (PATCH e DELETE), `Modelos/Modelos.cs` (`Assinatura`; campos de
  tela em `Endereco` e `Produto`), `ViewModels/LoginViewModel.cs` + `Views/LoginPage.xaml` (botão "Criar conta"),
  `ViewModels/PerfilViewModel.cs` + `Views/PerfilPage.xaml` (cartão "Minha conta", só para clientes),
  `ViewModels/ProdutoViewModel.cs` + `Views/ProdutoPage.xaml` (atalho da assinatura),
  `ViewModels/CarrinhoViewModel.cs` + `Views/CarrinhoPage.xaml` ("Novo endereço"), `AppShell.xaml.cs`, `MauiProgram.cs`
  (também deixa a caixa de marcar do Windows do tamanho certo).
- Testes (no app do Windows, contra o Docker/MySQL, com uma conta nova criada pelo próprio app):
  - Criar conta: CPF inválido recusado ("CPF inválido. Confira os números."); com CPF válido, a conta foi criada e o app
    entrou como "Ana" (nome social).
  - Endereços: CEP preencheu rua/bairro/cidade/estado; GPS gravou a posição (conferido no MySQL); o segundo endereço,
    sem GPS, ganhou a posição pelo endereço; CEP inexistente avisado; "Novo endereço" pelo carrinho voltou com o endereço
    escolhido; remover funcionou.
  - Assinaturas: criada pelo atalho do produto com o remédio já escolhido (2 caixas: R$ 25,80 → R$ 23,22); pausar,
    retomar e cancelar conferidos no MySQL (`ATIVA`, `PAUSADA`, `CANCELADA`).
  - A versão Android também compila (0 erros). Emulador continua não testado (não instalado).
- Afeta qual etapa do PIM: Etapa 5 (app completo), Etapa 3 (nome social e LGPD no app) e Etapa 6 (MVVM, injeção de
  dependência, mensageiro entre telas).
- Prints gerados: `docs/prints/34-app-criar-conta.png`, `docs/prints/35-app-enderecos.png` (CEP preenchido sozinho) e
  `docs/prints/36-app-assinatura.png` (valor com desconto e as assinaturas ativa, pausada e cancelada).

## 2026-10-31 — Tarefa 8, parte 4: RF39 Foto da receita de verdade (site, painel e app)
- O que mudou: **RF39 pode passar para "Feito".** Antes o site mandava só o **nome** do arquivo da receita; agora
  a foto (ou o PDF) vai de verdade, fica guardada numa **pasta privada** e só a equipe logada consegue abrir.
- **API:**
  - `POST /api/delivery` agora recebe **formulário com arquivo** (multipart/form-data): os campos de antes
    (`nomeCliente`, `telefone`, `enderecoTexto`, `observacao`) + o arquivo **`receita`** (opcional).
  - Aceita **JPG, PNG ou PDF de até 5 MB**. O tipo é conferido pelos **primeiros bytes do arquivo** (a "assinatura"
    do formato), não só pelo nome: um texto ou programa renomeado para ".jpg" é recusado. Acima de 6 MB a API para de
    ler o envio na hora (não gasta memória) e responde em português.
  - O arquivo vai para **`src/BemEstarOnline.Api/ArquivosPrivados/receitas/`** com **nome aleatório (GUID)**, fora do
    `wwwroot` e da pasta do site: **não existe endereço público** para ele (testado nas portas 5088 e 8080).
  - Rota nova **`GET /api/admin/delivery/{id}/receita`** (só ADMIN/ATENDENTE): devolve o arquivo com
    `Cache-Control: no-store` (o navegador não guarda cópia). Sem login = 401; cliente = 403.
  - Solicitações antigas (que só tinham o nome do arquivo) respondem 404 com a explicação.
  - **LGPD:** receita é **dado de saúde (dado sensível)**. Ao **excluir a conta**, as solicitações da pessoa ficam sem
    dados pessoais ("Conta excluída") e os **arquivos das receitas são apagados**.
  - Arquitetura: interface **`IArquivosPrivados`** (camada de aplicação) com a implementação `ArquivosPrivados`
    (infraestrutura); a aplicação não depende do `IFormFile` do ASP.NET (usa `ArquivoRecebido`).
  - **Contrato mudou:** `DeliveryCriarDto` perdeu o campo `ReceitaArquivo` (o arquivo vem no formulário) e a rota não
    aceita mais JSON. `DeliveryDto.ReceitaArquivo` continua existindo, agora com o nome guardado (GUID). O site foi ajustado.
  - Banco: **nenhuma tabela mudou** (a coluna `delivery_solicitacoes.receita_arquivo` já existia).
- **Docker:** volume novo `./src/BemEstarOnline.Api/ArquivosPrivados:/app/ArquivosPrivados` (as receitas não se perdem
  ao recriar o container). A pasta está no `.gitignore` (**nunca vai para o GitHub**) e no `.dockerignore`.
- **Site:** `delivery.html` envia o arquivo de verdade e avisa antes de enviar se o tipo não for JPG/PNG/PDF ou se passar
  de 5 MB. **Painel > Receitas recebidas:** botão **"Ver receita"**, que abre a foto (ou o PDF) dentro do próprio cartão,
  com "Abrir em tela cheia".
- **App:** tela nova **"Enviar receita"** (botão no alto de **Produtos** e no **Perfil**), funciona com ou sem login:
  - **"Tirar foto"** (câmera, `MediaPicker.CapturePhotoAsync`) e **"Escolher da galeria"**, com **prévia** da foto antes
    de enviar, "Tirar outra" e "Remover foto".
  - A foto é **reduzida para até 2000 px** e **perde os dados escondidos da imagem** (como a localização GPS de onde foi
    tirada): mais leve e mais privada.
  - Quem está logado já vem com nome, celular e endereço principal preenchidos.
  - A foto fica só num arquivo temporário do app e é **apagada depois do envio** (e sobras antigas são apagadas ao abrir a tela).
  - Android: permissão de **câmera** no `AndroidManifest.xml`.
- Arquivos novos: `Infraestrutura/Arquivos/ArquivosPrivados.cs`; app `ViewModels/EnviarReceitaViewModel.cs`,
  `Views/EnviarReceitaPage.xaml(.cs)`.
- Arquivos alterados: API `Aplicacao/Interfaces/IServicos.cs`, `Aplicacao/Servicos/DeliveryServico.cs`,
  `Aplicacao/Servicos/AuthServico.cs` (exclusão de conta), `Aplicacao/Dtos/Dtos.cs`, `Controllers/ClienteControllers.cs`,
  `Controllers/AdminController.cs`, `Dominio/Entidades/Servicos.cs` (`DeliverySolicitacao.Anonimizar`),
  `Dominio/Interfaces/IRepositorios.cs`, `Infraestrutura/Repositorios/Repositorios.cs`, `Program.cs` (registro da pasta
  privada; mensagem em português para arquivo grande demais); `docker-compose.yml`, `.gitignore`, `.dockerignore`;
  site `js/app.js` (envio de formulário e download protegido), `js/paginas/delivery.js`, `js/paginas/admin.js`,
  `delivery.html`, `css/estilo.css`; app `Servicos/ApiServico.cs`, `Modelos/Modelos.cs`, `Views/CatalogoPage.xaml`,
  `ViewModels/CatalogoViewModel.cs`, `Views/PerfilPage.xaml`, `ViewModels/PerfilViewModel.cs`, `AppShell.xaml.cs`,
  `MauiProgram.cs`, `Platforms/Android/AndroidManifest.xml`.
- Testes:
  - Script com 26 verificações, **todas OK nos dois modos** (memória e Docker/MySQL): envio sem receita, JPG, PNG e PDF
    aceitos; nome aleatório; texto disfarçado de .jpg, ".exe", arquivo de 5,5 MB e de 8 MB recusados (sem sobrar arquivo);
    401/403/200 na rota da equipe; arquivo devolvido idêntico ao enviado; sem endereço público; exclusão de conta apaga a receita.
  - Site e painel no navegador: envio com a receita de exemplo, avisos de tipo e tamanho, "Ver receita" com foto e com PDF.
  - App no Windows: campos preenchidos sozinhos, "Escolher da galeria" com prévia, envio (solicitação ligada à conta pelo
    login, conferido no MySQL) e foto temporária apagada. **"Tirar foto" não abriu a câmera neste PC** (só há a webcam
    virtual "Iriun"); o app mostrou "Não deu para abrir a câmera. Use 'Escolher da galeria'". No celular Android a câmera
    é a do aparelho (não testado: emulador não instalado). A versão Android compila sem erros.
  - Para os prints foi usada uma **receita fictícia** com o aviso "EXEMPLO - PROJETO ACADÊMICO (PIM IV) - SEM VALOR".
- Afeta qual etapa do PIM: Etapa 5 (app com câmera/galeria), Etapa 4 (site e painel), Etapa 6 (arquitetura: interface
  `IArquivosPrivados`), Etapa 3 e LGPD (dado sensível de saúde: pasta privada, só equipe, apagado ao excluir a conta).
- Prints gerados: `docs/prints/37-app-foto-receita.png` (prévia da foto antes de enviar) e
  `docs/prints/38-painel-ver-receita.png` (receita recebida aberta no painel).

## 2026-10-31 — Tarefa 8, parte 5: RF41 Política de Privacidade e Termos de Uso (site e app)
- O que mudou: **RF41 pode passar para "Feito".** Os textos escritos pelo chat (`docs/textos/politica-de-privacidade.md`
  e `docs/textos/termos-de-uso.md`) agora aparecem no site e no app. **O texto não foi alterado.**
- **Site:**
  - Páginas novas **`privacidade.html`** (com índice "Nesta página" que leva a cada seção) e **`termos.html`**, no mesmo
    visual do site (cabeçalho, rodapé, letra maior, alto contraste e tema escuro funcionam). Abrem também sem a API
    (arquivo direto) e pela API (http://localhost:5088/privacidade.html).
  - Links: **rodapé de todas as páginas** ("Política de Privacidade · Termos de Uso"); **cadastro**, na caixa da LGPD
    ("Li a Política de Privacidade e os Termos de Uso", abrem em outra aba para não perder o que foi digitado);
    **Minha conta > Meus dados** (seção Privacidade e o quadro de **exclusão de conta**, com link para "Seus direitos").
  - O texto da exclusão de conta (site e app) agora diz que as **receitas enviadas também são apagadas** (é o que o
    sistema faz desde o RF39).
- **App:** tela nova **"Privacidade" / "Termos de Uso"** que mostra o texto **dentro do app, sem internet**.
  - O app leva junto os próprios arquivos `docs/textos/*.md` (no `.csproj`, como `MauiAsset`) e os transforma em títulos,
    parágrafos e listas com negrito e itálico. **Se o chat mudar o texto em `docs/textos`, o app muda sozinho** na próxima
    compilação. (O site tem uma cópia em HTML: aí precisa atualizar `privacidade.html` e `termos.html` também.)
  - Acessibilidade: os títulos são marcados como cabeçalhos para o leitor de tela (TalkBack/Narrador) e as "bolinhas"
    da lista não são lidas em voz alta.
  - Links: **Perfil > cartão "Privacidade"** (para todos, até para quem não entrou e para o entregador) e tela
    **Criar conta** (botões "Política de Privacidade" e "Termos de Uso" ao lado da caixa da LGPD; ao voltar, o que foi
    digitado continua lá). Antes, o botão da tela Criar conta abria o navegador numa página que ainda não existia.
- Arquivos novos: `frontend/privacidade.html`, `frontend/termos.html`, app `ViewModels/TextoLegalViewModel.cs`,
  `Views/TextoLegalPage.xaml(.cs)`.
- Arquivos alterados: `frontend/js/app.js` (rodapé), `frontend/cadastro.html`, `frontend/js/paginas/conta.js`,
  `frontend/css/estilo.css`; app `BemEstarOnline.Mobile.csproj`, `Views/PerfilPage.xaml`, `ViewModels/PerfilViewModel.cs`,
  `Views/CriarContaPage.xaml`, `ViewModels/CriarContaViewModel.cs`, `AppShell.xaml.cs` (rota `texto-legal`), `MauiProgram.cs`.
  API e banco: nenhuma mudança.
- Testes: no navegador, as duas páginas (10 e 9 títulos, nenhum link quebrado no índice), links do rodapé e do cadastro, e a
  página aberta direto do arquivo; no app do Windows, Perfil > Política de Privacidade e Termos de Uso, links da tela Criar
  conta. App compila para Windows e Android.
- **Pontos para o chat conferir no texto da política** (o código faz assim hoje; o texto não foi mexido):
  1. O texto diz "Não usamos cookies de propaganda nem rastreadores de terceiros" (correto), mas o site e o app usam
     **serviços de terceiros** que recebem o endereço IP de quem acessa: Google Fonts (letras do site), mapas do
     OpenStreetMap, ViaCEP (busca do CEP), Nominatim (posição do endereço no mapa) e, no RF40, o VLibras (governo federal).
     Talvez valha uma frase na seção 3 ("Com quem compartilhamos").
  2. "Receitas: pelo prazo exigido pelas normas sanitárias; depois são apagadas": hoje o sistema apaga a receita **quando
     a pessoa exclui a conta**; ainda **não existe** a limpeza automática por prazo. Ou o texto fala só da exclusão, ou
     isso entra como trabalho futuro.
- Afeta qual etapa do PIM: Etapa 3 (LGPD e acessibilidade), Etapa 4 (site), Etapa 5 (app).
- Prints gerados: `docs/prints/33-site-privacidade.png` e, a mais, `docs/prints/33-app-privacidade.png` (a mesma política
  no app).

## 2026-10-31 — Tarefa 8, parte 6: RF40 VLibras no site (tarefa 8 concluída)
- O que mudou: **RF40 pode passar para "Feito".** O **VLibras**, tradutor de Libras oficial do governo federal
  (vlibras.gov.br), está em **todas as 17 páginas do site**, inclusive no painel da farmácia. Com isso, **todas as melhorias
  aprovadas da tarefa 8 estão feitas** (RF33 a RF41).
- Como funciona para quem usa: aparece um **botão azul no meio do lado direito da tela**. Ao clicar, abre a janela do
  VLibras com um avatar (Ícaro, Hosana ou Guga) que faz os sinais. Depois é só **clicar em qualquer texto do site** para o
  avatar traduzir para Libras. A janela tem velocidade, legenda e tela cheia.
- Como foi feito: uma parte nova no `frontend/js/app.js` (`TradutorLibras`) carrega o script oficial
  `https://vlibras.gov.br/app/vlibras-plugin.js` em todas as páginas (todas usam o `app.js`). O script é pequeno; o avatar
  só é baixado quando a pessoa clica no botão, então o site não fica mais lento.
- **Precisa de internet:** sem acesso ao VLibras, o site funciona normal, só sem o botão (testado bloqueando o endereço).
  Funciona pela API (porta 5088), pelo Nginx (porta 8080) e abrindo o arquivo direto.
- O app não recebe o VLibras (o RF40 é do site). A página do mapa usada dentro do app (`mapa-app.html`) não usa o `app.js`,
  então o botão não aparece lá.
- Privacidade: o VLibras é um serviço de terceiros (governo federal). Ele entra na observação 1 do RF41 para o chat
  (serviços de terceiros que recebem o IP de quem acessa).
- Arquivos alterados: só `frontend/js/app.js`. API, banco e app: nenhuma mudança.
- Testes no navegador: botão presente em início, medicamentos, privacidade, painel e login (mesma posição em todos); o
  avatar abriu e traduziu o texto "Entrega em até 2 horas" clicado na página; com o VLibras bloqueado o site seguiu normal.
- Afeta qual etapa do PIM: **Etapa 3 (acessibilidade e inclusão da comunidade surda)** e Etapa 4 (site).
- Print gerado: `docs/prints/32-site-vlibras.png` (avatar traduzindo um texto da página inicial).

## 2026-10-31 — Tarefa 9: testes automáticos (xUnit) das regras de negócio
- O que mudou: projeto novo **`tests/BemEstarOnline.Testes`** (xUnit, .NET 8) com **69 testes automáticos**, todos
  aprovados. Eles conferem as regras sozinhos em menos de 1 segundo, sem banco e sem a API ligada.
  Comando (na pasta do projeto): `dotnet test tests\BemEstarOnline.Testes`.
- O que é testado (as 4 regras pedidas na tarefa 9 + a foto da receita do RF39):
  - **`Pedido.AlterarStatus`** (`PedidoTestes`, 24 casos): o fluxo permitido (aguardando → pago → em separação → saiu →
    entregue; cancelar só antes de sair) e o que é proibido (entregue não volta, cancelado não reabre, não pula etapas,
    status inexistente); cancelar pedido pago **estorna** o pagamento; entregar pedido pago na entrega **aprova** o
    pagamento; cliente só cancela antes da separação; dois entregadores não pegam o mesmo pedido.
  - **`Cupom.Aplicar`** e validade do cupom (`CupomTestes`, 11 casos): percentual com arredondamento em centavos, valor
    fixo que nunca desconta mais do que a compra, frete grátis; cupom desativado, vencido, que ainda não começou, que
    atingiu o limite de usos e compra abaixo do mínimo.
  - **`ValidadorCpf`** (`ValidadorCpfTestes`, 15 casos): CPFs válidos com e sem pontuação; dígito errado, todos os números
    iguais, tamanho errado, vazio e letras são recusados; formatação 000.000.000-00.
  - **`EntregaServico.ConfirmarEntregaAsync`** (`EntregaServicoTestes`, 7 casos): **pedido com receita não é entregue sem o
    entregador conferir a receita** (nada é gravado), com a receita conferida é entregue e avisa o histórico; pedido sem
    receita é entregue normalmente; entregador não confirma pedido de outro; pedido que não saiu para entrega não pode ser
    entregue; pedido inexistente.
  - **Foto da receita, RF39** (`ReceitaDeliveryTestes`, 12 casos): JPG, PNG e PDF aceitos com nome aleatório; texto
    disfarçado de .jpg, .exe/.gif/sem extensão, arquivo vazio e acima de 5 MB recusados; a equipe abre a receita guardada.
- Como: os testes do serviço de entrega e do delivery usam **"dublês"** (`Duble.cs`): versões de mentira do repositório,
  da unidade de trabalho, das automações e da pasta privada. Isso só é possível porque a API foi feita em camadas com
  **interfaces** e **injeção de dependência** (o serviço recebe as interfaces, não o MySQL): bom exemplo para a Etapa 6.
- Prova de que os testes pegam erros de verdade: a regra da receita foi desligada de propósito e o teste
  `Pedido_com_receita_nao_e_entregue_sem_conferir_a_receita` falhou na hora; depois a regra voltou e os 69 passaram.
- **CI/DevOps:** o GitHub Actions (`.github/workflows/ci-cd.yml`, job "Compilar API") agora roda os testes a cada envio;
  se algum falhar, o pipeline fica vermelho e a imagem Docker não é montada.
- Arquivos novos: `tests/BemEstarOnline.Testes/` (`BemEstarOnline.Testes.csproj`, `PedidoTestes.cs`, `CupomTestes.cs`,
  `ValidadorCpfTestes.cs`, `EntregaServicoTestes.cs`, `ReceitaDeliveryTestes.cs`, `Duble.cs`). Alterados:
  `BemEstarOnline.sln` (projeto incluído na pasta "tests"), `.github/workflows/ci-cd.yml`, `GUIA-SIMPLES-DE-EXECUCAO.txt`
  (seção "Testes automáticos"). Nenhuma regra do sistema mudou.
- Afeta qual etapa do PIM: Etapa 6 (qualidade, camadas e interfaces), DevOps (testes no pipeline) e Scrum (critério de
  "pronto": a história só está pronta quando os testes passam).

## 2026-10-31 — Tarefa 10: projeto enviado ao GitHub (repositório novo) e pipeline verde
- O que mudou: o PIM IV está no GitHub, num **repositório novo e público**:
  **https://github.com/mrg-exe/BemEstarOnline-PIM-IV** (branch `main`). O repositório antigo do PIM III
  (github.com/Kodaiulou/BemestarOnline) **não foi mexido**.
- O que foi enviado: 278 arquivos (API, app, site, banco, testes, documentação, prints, diagramas, Docker e pipeline).
- O que **não** foi enviado (de propósito, pelo `.gitignore`): arquivos gerados pela compilação (`bin/`, `obj/`), as
  **receitas enviadas** (`ArquivosPrivados/`, dado de saúde), os **e-mails de teste** (`emails-enviados/`, têm links de
  senha) e as instruções para o Claude Code (`CLAUDE.md`, `PRIMEIRA-MENSAGEM-PARA-O-CLAUDE-CODE.txt`), que ficam só no
  computador do Filipe. Antes do envio foi feita uma busca por senhas e chaves: só há as de demonstração do projeto
  (MySQL local do Docker, contas de teste e a chave de exemplo do JWT, que já se chama "troque-esta-chave-em-producao").
- Limpeza antes do envio (autorizada pelo Filipe): apagada a foto de teste da Dipirona (`wwwroot/imagens/produtos`, também
  dentro do volume do Docker) e a pasta vazia `.gstack`.
- **GitHub Actions (DevOps):** a primeira execução do pipeline `CI-CD` ficou **verde** (1 min 41 s):
  compilar a API **e rodar os 69 testes**, testar os scripts num MySQL de verdade, conferir o JavaScript do site e montar a
  imagem Docker. "Compilar app Android" (manual) e "Publicar no Azure" (desligado) foram pulados, como planejado.
  Link: https://github.com/mrg-exe/BemEstarOnline-PIM-IV/actions
- O README ganhou o **selo do pipeline** (mostra "passing" quando está tudo verde), a pasta de testes e os recursos de
  inclusão (VLibras, ouvidoria, LGPD).
- Atenção para o futuro: se um dia publicar no Azure de verdade, troque a chave do JWT por uma configuração do próprio
  Azure (`Jwt__Chave`), porque a do `appsettings.json` agora é pública.
- Afeta qual etapa do PIM: Etapa 8 (nuvem e DevOps: versionamento e integração contínua) e Etapa 9 (Scrum: entrega).
- Print gerado: `docs/prints/29-github-actions.png` (pipeline verde).

## 2026-11-01 — App: entrar como entregador, botão Voltar, visual novo e app no celular de verdade
- Pedidos do Filipe: (1) como rodar o app num celular de verdade; (2) a Política de Privacidade não tinha botão de
  voltar; (3) deixar o app mais agradável; (4) não havia como entrar como entregador na tela de login.
- **Entrar como entregador:** a tela de entrada ganhou o cartão **"Contas de teste"** com os botões **"🛒 Cliente"** e
  **"🛵 Entregador"**: um toque preenche e entra. O entregador cai direto na aba **Entregas** (conta Marcos Entregador,
  entregador@bemestaronline.com / Entregador@123). Antes só havia "Usar conta de teste (cliente)".
- **Botão Voltar:** no Windows a seta de voltar fica na barra de título, pequena e cinza-clara (quase invisível). Agora
  as telas internas têm um botão **"← Voltar"** bem visível no topo: produto, acompanhar pedido, entrega, criar conta,
  meus endereços, assinaturas, enviar receita, alterar senha (só quando a troca não é obrigatória) e Privacidade/Termos
  (que também ganhou "Entendi, voltar" no fim do texto). No Android esse botão fica escondido, porque lá a seta da barra
  verde e o botão de voltar do celular já funcionam. Componente reutilizável: `Views/Componentes/BotaoVoltar.cs`.
- **Visual novo:**
  - Tela de entrada com o **logo** num círculo branco, cartão "Entrar", "Esqueci minha senha" e "Configurar endereço do
    servidor" como links, e o cartão de contas de teste.
  - **Perfil** reorganizado: cabeçalho com um **círculo com as iniciais**, nome, e-mail e tipo de conta (Cliente,
    Entregador...); grupos "Minha conta", "Acessibilidade", "Privacidade" e "Pedidos feitos sem internet" com ícones;
    "Excluir minha conta" virou um link vermelho discreto no fim. O entregador também pode "Alterar senha".
  - Política de Privacidade e Termos dentro de um cartão branco.
  - No Windows, a cor de destaque (linha do campo em uso, bolinha das opções) passou do **roxo padrão para o verde** da
    farmácia (`Platforms/Windows/App.xaml`).
  - Estilos novos em `Resources/Styles/Styles.xaml`: `BotaoVoltar`, `BotaoLink`, `TituloSecao`.
- **App no celular de verdade (Android):** novo **PASSO 4B** no `GUIA-SIMPLES-DE-EXECUCAO.txt`. Resumo: o PC roda o modo
  completo (Docker), o celular fica na mesma rede, instala o arquivo **`publicado/android/br.com.bemestaronline.app-Signed.apk`**
  (gerado nesta data, 30,6 MB) e, no app, "Configurar endereço do servidor" = `http://IP-DO-PC:5088/api/`
  (neste PC: 192.168.18.7). Também explica o jeito com cabo (Depuração USB + Visual Studio) e os erros comuns
  (rede pública/privada do Windows, IP que muda, link do "Esqueci minha senha"). O Firewall do Windows já libera o
  Docker para a rede; a API respondeu pelo IP da rede.
- Arquivos novos: `Views/Componentes/BotaoVoltar.cs`, `Resources/Images/logo_bem_estar.png`.
  Alterados: `Views/LoginPage.xaml`, `ViewModels/LoginViewModel.cs` (`EntrarComo`), `Views/PerfilPage.xaml`,
  `ViewModels/PerfilViewModel.cs`, `Views/TextoLegalPage.xaml`, `ViewModels/TextoLegalViewModel.cs`, as 8 telas internas
  (botão Voltar), `Resources/Styles/Styles.xaml`, `Platforms/Windows/App.xaml`, `BemEstarOnline.Mobile.csproj`,
  `GUIA-SIMPLES-DE-EXECUCAO.txt`. API, site e banco: nenhuma mudança.
- Testes: app do Windows compilado e testado (botão "Entregador" entra na aba Entregas; Perfil do entregador e do
  cliente; Privacidade com "← Voltar" funcionando). APK Android gerado sem erros. **Não testado:** o APK num celular de
  verdade (fica para o Filipe, seguindo o PASSO 4B) e, na tela, a cor verde nova do Windows.
- Afeta qual etapa do PIM: Etapa 5 (app: usabilidade, instalação no celular) e Etapa 3 (acessibilidade: botão de voltar
  grande e visível).
- **Correção (mesmo dia): "o app não consegue falar com a API" no celular de verdade.** No PC estava tudo certo (Docker
  ligado, API respondendo pelo IP da rede 192.168.18.7, APK com permissão de internet e de HTTP). A causa provável é o
  endereço do servidor: o app vinha com o endereço do **emulador** (`http://10.0.2.2:5088/api/`), que não funciona num
  celular de verdade, e pequenos erros de digitação faziam o app se perder. Agora:
  - no celular de verdade com o endereço do emulador, o cartão **"Endereço da API" já abre sozinho com um aviso**;
  - o app **completa o endereço**: digitar só `192.168.18.7` vira `http://192.168.18.7:5088/api/`;
  - botão **"Testar conexão"**, que diz em português o que está errado (sem Wi-Fi, ninguém respondeu no IP, API
    desligada na porta, "localhost" no celular) e "Tudo certo!" em verde quando funciona;
  - a mensagem de erro do login manda usar o "Testar conexão".
  Arquivos: `Servicos/SessaoServico.cs` (`NormalizarUrl`, `PrecisaConfigurarServidor`), `Servicos/ApiServico.cs`
  (`DiagnosticarAsync`), `ViewModels/LoginViewModel.cs`, `Views/LoginPage.xaml`, guia (PASSO 4B). APK novo gerado
  (30,3 MB). **Falta o Filipe confirmar no celular.**
