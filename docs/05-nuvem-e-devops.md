# Etapa 8 — Infraestrutura em nuvem e DevOps (plano de execução)

## Arquitetura na nuvem (Microsoft Azure)
```
 Usuários ──HTTPS──> Azure Front Door (CDN + certificado)
                       ├──> Azure Static Web Apps ........ site (pasta frontend)
                       └──> Azure App Service (Linux, container) .. API
                                  │   ├── Azure Key Vault (senha do banco, chave JWT)
                                  │   └── Application Insights (logs e métricas)
                                  └──> Azure Database for MySQL – Flexible Server
 App Android/Windows ──HTTPS──> mesma API
 GitHub Actions ──> Azure Container Registry ──> App Service
```

## Serviços utilizados
| Serviço | Para quê |
|---|---|
| Azure Static Web Apps | Hospedar o site (HTML/CSS/JS) com HTTPS grátis |
| Azure App Service (Linux) | Rodar o container da API, com escala automática |
| Azure Database for MySQL Flexible Server | Banco gerenciado com backup automático (7 a 35 dias) |
| Azure Container Registry | Guardar as imagens Docker geradas pelo pipeline |
| Azure Key Vault | Guardar segredos fora do código |
| Application Insights / Azure Monitor | Logs, tempo de resposta, erros e alertas |

## Containers
- `src/BemEstarOnline.Api/Dockerfile`: build em duas etapas (SDK → runtime), roda com usuário sem privilégios.
- `docker-compose.yml`: ambiente local completo (MySQL + API + site Nginx) com um comando.

## Pipeline CI/CD (`.github/workflows/ci-cd.yml`)
1. **Compilar API** (restore, build, publish).
2. **Testar banco**: sobe um MySQL 8 de verdade e executa os 4 scripts; confere triggers e procedures.
3. **Conferir site**: verificação de sintaxe dos arquivos JavaScript.
4. **Imagem Docker** da API.
5. **Deploy** no Azure App Service (só na branch main, quando `DEPLOY_AZURE=true`).
6. **App Android** sob demanda (botão "Run workflow").

## Monitoramento
- Endpoint `GET /api/health` (verifica a API e a conexão com o banco), usado pelo Health Check do App Service.
- Application Insights: alertas para taxa de erro acima de 2% e tempo de resposta acima de 2 s.
- Painel administrativo mostra estoque baixo, validade e pedidos em aberto (monitoramento do negócio).

## Escalabilidade
- App Service com autoescala de 1 a 3 instâncias por uso de CPU (> 70%) — a API não guarda estado
  em memória (sessão via JWT), então pode rodar em várias instâncias.
- MySQL Flexible Server pode aumentar CPU/memória sem mudar o código; réplica de leitura para relatórios.
- Site em CDN (arquivos estáticos).

## Segurança
- HTTPS obrigatório; CORS restrito ao domínio do site em produção.
- Segredos no Key Vault (a chave JWT e a senha do `appsettings.json` são só para desenvolvimento).
- Banco sem acesso público (rede privada/VNet); backups automáticos.
- Senhas PBKDF2, tokens com validade de 8 h, perfis de acesso por rota.
- LGPD: coleta mínima de dados e consentimento no cadastro.
