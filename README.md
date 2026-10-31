# Bem Estar Online — PIM IV (UNIP, ADS)

[![CI-CD](https://github.com/mrg-exe/BemEstarOnline-PIM-IV/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/mrg-exe/BemEstarOnline-PIM-IV/actions/workflows/ci-cd.yml)

Farmácia virtual de bairro (Cajamar/SP): continuação do projeto do PIM III, agora com
API em camadas, banco MySQL com triggers e procedures, site repaginado com painel
administrativo, aplicativo .NET MAUI com modo offline, Docker e pipeline CI/CD.

> Para rodar passo a passo, leia **GUIA-SIMPLES-DE-EXECUCAO.txt**.

## Estrutura
| Pasta | Conteúdo |
|---|---|
| `src/BemEstarOnline.Api` | API REST em ASP.NET Core 8 (Domínio, Aplicação, Infraestrutura, Controllers), JWT, Swagger |
| `src/BemEstarOnline.Mobile` | App .NET MAUI 10 (Android e Windows), MVVM, SQLite offline, modo cliente e modo entregador com mapa em tempo real |
| `frontend` | Site (HTML, CSS, JS) + painel da farmácia (`admin.html`) |
| `database` | Scripts MySQL: `01_ddl`, `02_triggers`, `03_procedures`, `04_seed` |
| `tests/BemEstarOnline.Testes` | Testes automáticos (xUnit) das regras de negócio: `dotnet test tests/BemEstarOnline.Testes` |
| `docs` | Material das Etapas 2 a 9 (empreendedorismo, diversidade, arquitetura, banco, nuvem, Scrum) |
| `docker-compose.yml` | MySQL + API + site com um comando |
| `.github/workflows/ci-cd.yml` | Pipeline de integração e entrega contínua |

## Rodar rápido
```powershell
# Modo fácil (banco em memória)
cd src/BemEstarOnline.Api
dotnet run            # site em http://localhost:5088 (servido pela API) e Swagger em /swagger
# o site também abre direto pelo arquivo frontend/index.html

# Modo completo (MySQL), com o Docker Desktop aberto
docker compose up --build   # site http://localhost:5088 (API) ou http://localhost:8080 (Nginx)
```

## Contas de teste
| Perfil | E-mail | Senha |
|---|---|---|
| Cliente | cliente@bemestaronline.com | Cliente@123 |
| Entregador (app) | entregador@bemestaronline.com | Entregador@123 |
| Atendente | atendente@bemestaronline.com | Atendente@123 |
| Administrador | admin@bemestaronline.com | Admin@123 |

## Onde cada etapa do manual do PIM IV está
| Etapa | Onde |
|---|---|
| 1 Caracterização | Trabalho escrito (vem do PIM III) |
| 2 Planejamento / empreendedorismo | `docs/01-empreendedorismo.md` |
| 3 Diversidade e inclusão | `docs/02-diversidade-e-inclusao.md` + nome social, acessibilidade, VLibras, ouvidoria contra discriminação, Farmácia Popular, LGPD (`frontend/privacidade.html`) |
| 4 Web | `frontend/` + `src/BemEstarOnline.Api` (REST, JWT, perfis, painel admin) |
| 5 Mobile | `src/BemEstarOnline.Mobile` |
| 6 Arquitetura | `docs/03-arquitetura.md` |
| 7 Banco de dados | `database/` + `docs/04-banco-de-dados.md` |
| 8 Nuvem e DevOps | `src/BemEstarOnline.Api/Dockerfile`, `docker-compose.yml`, `.github/workflows` (compila, testa e monta a imagem), `docs/05-nuvem-e-devops.md` |
| 9 Gestão ágil | `docs/scrum/` |

Projeto acadêmico: empresa, pessoas, produtos e pagamentos são fictícios.
