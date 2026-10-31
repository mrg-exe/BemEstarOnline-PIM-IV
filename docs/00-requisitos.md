# Documento de Requisitos — Bem Estar Online (PIM IV)

Responsável: documentação (Claude no chat). Implementação: Claude Code.
Situação: **Feito** (implementado, ainda não testado em máquina real), **Pendente**, **Futuro**.

## 1. Atores
| Ator | Descrição |
|---|---|
| Visitante | Navega no catálogo sem conta |
| Cliente | Compra, assina, acompanha entregas, gerencia a própria conta |
| Atendente | Opera o painel: pedidos, estoque, receitas, assinaturas |
| Administrador | Tudo do atendente + cadastro de produtos e fotos |
| Entregador | Usa o app: pega pedidos, envia localização, confirma entrega |

## 2. Requisitos funcionais
| ID | Requisito | Ator | Etapa PIM | Situação |
|---|---|---|---|---|
| RF01 | Listar produtos por categoria, com busca sem acento e filtros (Farmácia Popular, uso contínuo, sem receita, disponíveis) | Visitante | 4 | Feito |
| RF02 | Ver ficha do produto (princípio ativo, dosagem, Anvisa, validade) com leitura em voz alta | Visitante | 3, 4 | Feito |
| RF03 | Criar conta com CPF validado, nome social opcional e consentimento LGPD | Visitante | 3, 4 | Feito |
| RF04 | Entrar com e-mail e senha (token JWT, 8 h) | Todos | 4 | Feito |
| RF05 | Carrinho salvo no aparelho e sincronizado com a conta (mesmo carrinho no site e no app) | Cliente | 4, 5 | Feito |
| RF06 | Calcular subtotal, cupom e frete (grátis acima de R$ 99) no servidor | Cliente | 4 | Feito |
| RF07 | Finalizar compra com endereço salvo ou novo (CEP automático, GPS opcional) e pagamento simulado (Pix, cartão, dinheiro) | Cliente | 4 | Feito |
| RF08 | Impedir venda sem estoque (validação na API + trigger) | Sistema | 7 | Feito |
| RF09 | Baixar estoque e gerar alerta de reposição automaticamente (trigger) | Sistema | 7 | Feito |
| RF10 | Registrar histórico de status do pedido (trigger) e mostrar linha do tempo | Cliente | 4, 7 | Feito |
| RF11 | Cancelar pedido antes da separação, devolvendo o estoque (trigger) | Cliente | 4, 7 | Feito |
| RF12 | Filtrar meus pedidos por situação, período e número | Cliente | 4 | Feito |
| RF13 | Assinatura de uso contínuo (15/30/60/90 dias, 10% de desconto), pausar/retomar/cancelar | Cliente | 2, 4 | Feito |
| RF14 | Enviar pedido por texto/foto da receita sem precisar de conta | Visitante | 4 | Feito |
| RF15 | Gerenciar endereços (com coordenadas para o mapa) | Cliente | 4 | Feito |
| RF16 | Excluir a própria conta (senha + confirmação), anonimizando os dados | Cliente | 3 | Feito |
| RF17 | Ao sair da conta, apagar carrinho e dados da conta no aparelho | Cliente | 3, 4 | Feito |
| RF18 | Painel com vendas (procedure), pedidos em aberto, estoque baixo e validade (procedures) | Equipe | 4, 7 | Feito |
| RF19 | Avançar status do pedido seguindo o fluxo permitido | Equipe | 4 | Feito |
| RF20 | Cadastrar/editar produtos, ajustar estoque e enviar foto | Admin | 4 | Feito |
| RF21 | Listar e filtrar clientes e equipe (nome, perfil, compras) | Equipe | 4 | Feito |
| RF22 | Ver assinaturas e receitas recebidas, mudando a situação | Equipe | 4 | Feito |
| RF23 | App: catálogo, carrinho, compra, pedidos e perfil | Cliente | 5 | Feito |
| RF24 | App: catálogo e pedidos funcionam sem internet; pedido offline é enviado depois, só com a conta de quem o fez | Cliente | 5 | Feito |
| RF25 | App entregador: lista de pedidos em separação e aviso (alerta + vibração) de pedido novo, informando se exige receita | Entregador | 5 | Feito |
| RF26 | App entregador: pegar pedido, ver a localização do cliente e abrir a rota no app de mapas | Entregador | 5 | Feito |
| RF27 | App entregador: enviar posição (GPS a cada 10 s ou trajeto simulado) | Entregador | 5 | Feito |
| RF28 | Cliente vê o entregador no mapa em tempo real (site e app, atualização a cada 8 s) | Cliente | 4, 5 | Feito |
| RF29 | Confirmar entrega exigindo conferência da receita quando houver tarja vermelha | Entregador | 5 | Feito |
| RF30 | **Ouvidoria e canal de denúncia de discriminação** (ver especificação abaixo) | Todos | 3 | **Pendente** |
| RF33 | **Esqueci minha senha** por e-mail, com link de uso único válido por 30 minutos | Todos | 4 | **Pendente** |
| RF34 | **Alterar senha** estando logado (site e app) | Todos | 4 | **Pendente** |
| RF35 | **Gestão da equipe** no painel: admin cadastra atendente/entregador/admin, desativa e reativa contas, redefine senha | Admin | 4 | **Pendente** |
| RF36 | App: **criar conta** (CPF validado, nome social, consentimento LGPD) | Visitante | 5 | **Pendente** |
| RF37 | App: **endereços** (listar, adicionar com CEP automático e GPS do celular, remover) | Cliente | 5 | **Pendente** |
| RF38 | App: **assinatura** de uso contínuo (criar, listar, pausar, retomar, cancelar) | Cliente | 5 | **Pendente** |
| RF39 | App e site: **enviar foto da receita** de verdade (câmera ou galeria no app; arquivo no site), guardada em pasta privada | Cliente | 3, 5 | **Pendente** |
| RF40 | Site: **VLibras** (tradutor de Libras do governo federal) em todas as páginas | Todos | 3 | **Pendente** |
| RF41 | Site e app: **Política de Privacidade e Termos de Uso** (texto em `docs/textos/`) | Todos | 3 | **Pendente** |
| RF31 | Notificação push com o app fechado | Entregador/Cliente | 5 | Futuro |
| RF32 | Pagamento real (gateway) e geração automática dos pedidos das assinaturas | Sistema | 2 | Futuro |

### RF30 — Ouvidoria e canal de denúncia (especificação para o Claude Code)
Objetivo: dar ao cliente um canal seguro para relatar discriminação (racial, religiosa, de gênero,
por deficiência, idade ou outra) e outros problemas de atendimento, reforçando a Etapa 3
(Relações étnico-raciais) e o Estatuto da Igualdade Racial (Lei 12.288/2010).
- **Banco:** tabela `ouvidoria` (id_ouvidoria, id_usuario NULL, tipo ENUM('DISCRIMINACAO','ATENDIMENTO','ENTREGA','SUGESTAO','ELOGIO'),
  subtipo_discriminacao NULL (RACIAL, RELIGIOSA, GENERO, DEFICIENCIA, IDADE, OUTRA), descricao TEXT, anonimo BOOLEAN,
  contato VARCHAR NULL, protocolo VARCHAR(20) único, status ENUM('ABERTA','EM_ANALISE','RESPONDIDA','ENCERRADA'),
  resposta TEXT NULL, criado_em, respondido_em NULL). Incluir no `01_ddl.sql`.
- **API:** `POST /api/ouvidoria` (público; se logado e não anônimo, guarda o id do usuário) devolve o protocolo
  (ex.: OUV-2026-000123); `GET /api/ouvidoria/{protocolo}` mostra situação e resposta;
  `GET /api/admin/ouvidoria` e `PATCH /api/admin/ouvidoria/{id}` (equipe) para responder.
  Relato anônimo **não** guarda id do usuário nem contato.
- **Site:** página `ouvidoria.html` (link no rodapé e em Minha conta) com formulário simples, opção
  "quero ficar anônimo", texto explicando que discriminação é crime (Lei 7.716/1989) e consulta por protocolo.
  No painel: seção "Ouvidoria" com contador de abertas e destaque para denúncias de discriminação.
- **Critério de aceite:** registrar anônimo e identificado, consultar pelo protocolo, equipe responder e o
  cliente ver a resposta; denúncias de discriminação aparecem primeiro no painel.

### RF33 e RF34 — Esqueci minha senha / Alterar senha
- **Banco:** tabela `redefinicoes_senha` (id_redefinicao, id_usuario FK, token_hash CHAR(64) único,
  expira_em DATETIME, usado_em DATETIME NULL, criado_em). Guardar só o **SHA-256 do token**, nunca o token.
- **API:**
  - `POST /api/auth/esqueci-senha { email }` → sempre responde a mesma mensagem ("Se o e-mail existir, enviamos
    um link"), para não revelar quem tem conta. Máximo 3 pedidos por e-mail por hora.
  - `POST /api/auth/redefinir-senha { token, novaSenha }` → valida token (não usado e não expirado), troca a senha,
    marca como usado.
  - `POST /api/auth/alterar-senha { senhaAtual, novaSenha }` (logado).
- **E-mail:** interface `IEmailServico` com duas implementações: `EmailSmtp` (usa seção `Smtp` do appsettings,
  se preenchida) e `EmailDemonstracao` (padrão), que salva cada e-mail como arquivo `.html` em
  `src/BemEstarOnline.Api/emails-enviados/` e escreve o link no log da API. Assim funciona sem servidor de e-mail.
- **Site:** link "Esqueci minha senha" no login → `esqueci-senha.html`; o link do e-mail abre
  `redefinir-senha.html?token=...`. Em Minha conta > Meus dados: "Alterar senha".
- **App:** "Esqueci minha senha" na tela de login (pede o e-mail e chama a mesma rota) e "Alterar senha" no Perfil.
- **Aceite:** e-mail inexistente recebe a mesma resposta; token usado ou vencido é recusado; senha antiga deixa de funcionar.

### RF35 — Gestão da equipe (painel)
- **Banco:** coluna `usuarios.trocar_senha BOOLEAN DEFAULT FALSE` (força troca no primeiro acesso).
- **API (só ADMIN):** `GET /api/admin/equipe`; `POST /api/admin/equipe` (nome, e-mail, CPF, celular, perfil
  ATENDENTE/ENTREGADOR/ADMIN, senha inicial → `trocar_senha = true`); `PATCH /api/admin/equipe/{id}`
  (perfil e ativo); `POST /api/admin/equipe/{id}/redefinir-senha` (nova senha provisória).
  Regras: não pode desativar a si mesmo nem o último ADMIN ativo; cliente não vira equipe por aqui.
  Login de conta desativada é recusado.
- **Login:** se `trocar_senha` for true, a resposta do login informa isso e o site/app abre a tela de nova senha.
- **Painel:** seção "Equipe" (só para ADMIN) com lista, botão "Nova pessoa", ativar/desativar e redefinir senha.
- **Aceite:** admin cria um entregador, o entregador entra no app, é obrigado a trocar a senha e vê a aba Entregas.

### RF36 a RF38 — App completo
- **Criar conta:** tela "Criar conta" a partir do login, com os mesmos campos e validações do site (CPF com
  dígitos verificadores, data DD/MM/AAAA, nome social com explicação, caixa de consentimento LGPD com link
  para a Política de Privacidade). Usa `POST /api/auth/registrar` e já entra logado.
- **Endereços:** tela no Perfil (listar, adicionar, remover) e botão "Novo endereço" no carrinho.
  CEP preenche rua/bairro/cidade (ViaCEP); botão "Usar minha localização" grava latitude/longitude do GPS.
- **Assinatura:** tela "Assinaturas" (no Perfil e um atalho no produto de uso contínuo), igual à do site:
  escolher remédio de uso contínuo, paciente, caixas, intervalo (15/30/60/90), ver o valor com 10% de desconto;
  listar, pausar, retomar e cancelar.

### RF39 — Foto da receita de verdade
- **API:** `POST /api/delivery` passa a aceitar **multipart** (campos atuais + `receita` arquivo JPG/PNG/PDF até 5 MB).
  O arquivo vai para `src/BemEstarOnline.Api/ArquivosPrivados/receitas/` (**fora do wwwroot**, não é público),
  com nome aleatório (GUID). `GET /api/admin/delivery/{id}/receita` (só equipe) devolve o arquivo.
  Receita é dado de saúde (LGPD, dado sensível): nunca expor por URL pública. Docker: volume para essa pasta.
- **Site:** `delivery.html` envia o arquivo de verdade; no painel, "Ver receita" abre o arquivo.
- **App:** tela "Enviar receita" com **"Tirar foto"** (`MediaPicker.CapturePhotoAsync`) e "Escolher da galeria";
  mostra a prévia antes de enviar. Android: permissão de câmera no manifest.

### RF40 — VLibras
- Incluir o widget oficial do VLibras (vlibras.gov.br) em todas as páginas do site, pelo `app.js` ou no modelo
  das páginas. Precisa de internet; se não carregar, o site segue normal.

### RF41 — Política de Privacidade e Termos de Uso
- Textos prontos em `docs/textos/politica-de-privacidade.md` e `docs/textos/termos-de-uso.md` (escritos pelo chat).
- Site: páginas `privacidade.html` e `termos.html`, links no rodapé, no cadastro (caixa LGPD) e na exclusão de conta.
- App: links no Perfil e na tela de criar conta (abrir as páginas do site ou mostrar o texto).

## 3. Requisitos não funcionais
| ID | Requisito | Situação |
|---|---|---|
| RNF01 | Senhas com PBKDF2-SHA256 (100 mil iterações, sal); JWT com perfis; id do usuário sempre do token | Feito |
| RNF02 | Acessibilidade WCAG 2.1: fonte para baixa visão, A/A+/A++, alto contraste, tema escuro, teclado, leitor de tela | Feito |
| RNF03 | Site responsivo (celular, tablet, computador) | Feito |
| RNF04 | LGPD: consentimento, coleta mínima, CPF mascarado, exclusão da conta pelo titular | Feito |
| RNF05 | Mesmo comportamento nos modos MySQL (triggers) e memória (C#) | Feito |
| RNF06 | Executar com um comando (`docker compose up`) e pipeline de CI no GitHub Actions | Feito |
| RNF07 | **O site deve ser servido pela aplicação ASP.NET Core** (disciplina "Desenvolvimento Web com .NET"), mantendo o modo de abrir o HTML direto | **Pendente** |
| RNF08 | App em .NET MAUI 10 (versão com suporte da Microsoft) | Feito |
| RNF09 | Mensagens de erro claras para o usuário, sem detalhes técnicos | Feito |

## 4. Prints necessários para o trabalho escrito (salvar em `docs/prints/`)
| Arquivo | Conteúdo |
|---|---|
| 01-site-inicio.png | Página inicial com o carrossel |
| 02-site-catalogo.png | Catálogo com filtros |
| 03-site-produto.png | Página de produto (com selo de receita) |
| 04-site-carrinho-checkout.png | Carrinho com cupom e entrega/pagamento |
| 05-site-confirmacao.png | Pedido confirmado com linha do tempo |
| 06-site-mapa-entregador.png | Minha conta com o mapa do entregador em movimento |
| 07-site-cadastro-nome-social.png | Cadastro mostrando o nome social |
| 08-site-acessibilidade.png | Alto contraste + letra grande |
| 09-site-excluir-conta.png | Exclusão de conta |
| 10-painel-visao-geral.png | Painel com gráfico, estoque baixo e validade |
| 11-painel-pedido.png | Pedido aberto no painel com botões de status |
| 12-painel-produtos.png | Produtos e estoque (com fotos) |
| 13-painel-reposicao.png | Alertas de reposição gerados pelo trigger |
| 14-painel-ouvidoria.png | Ouvidoria (RF30) |
| 15-swagger.png | Swagger com o botão Authorize |
| 16-app-login.png ... 22-app-perfil.png | App cliente: login, produtos, produto, carrinho, pedidos, mapa, perfil/alto contraste |
| 23-app-entregas-aviso.png | App entregador: aviso de pedido novo com "EXIGE RECEITA" |
| 24-app-entrega-mapa.png | App entregador: mapa e botões |
| 25-app-confirmar-receita.png | Confirmação exigindo conferência da receita |
| 26-mysql-triggers.png | MySQL Workbench mostrando triggers e procedures |
| 27-mysql-mer.png | Diagrama (Database > Reverse Engineer) |
| 28-docker-compose.png | Os 3 containers rodando no Docker Desktop |
| 29-github-actions.png | Pipeline verde no GitHub (se enviado ao GitHub) |
| 30-site-esqueci-senha.png | Esqueci minha senha + e-mail gerado (pasta emails-enviados) |
| 31-painel-equipe.png | Gestão da equipe |
| 32-site-vlibras.png | VLibras aberto no site |
| 33-site-privacidade.png | Política de Privacidade |
| 34-app-criar-conta.png | App: criar conta |
| 35-app-enderecos.png | App: endereços com GPS |
| 36-app-assinatura.png | App: assinatura |
| 37-app-foto-receita.png | App: foto da receita antes de enviar |
| 38-painel-ver-receita.png | Painel: receita recebida aberta |
