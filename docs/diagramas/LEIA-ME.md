# Diagramas UML (para o trabalho escrito)

| Arquivo | Diagrama | Onde entra no trabalho |
|---|---|---|
| uc-01-cliente.png | Casos de uso: Visitante e Cliente | Etapa 4/5 (requisitos) |
| uc-02-equipe.png | Casos de uso: Atendente e Administrador | Etapa 4 (estrutura administrativa) |
| uc-03-entregador.png | Casos de uso: Entregador (com «extend» da receita) | Etapa 5 |
| classes-dominio.png | Classes do domínio (entidades, atributos, métodos e multiplicidades) | Etapa 6 (orientação a objetos) |
| classes-camadas.png | Classes da arquitetura: interfaces e implementações por camada | Etapa 6 (camadas, polimorfismo, SOLID) |
| seq-compra.png | Sequência: finalizar compra (com os triggers) | Etapas 4 e 7 |
| seq-entrega.png | Sequência: separação, aviso ao entregador e rastreio em tempo real | Etapas 4 e 5 |
| mer-banco-de-dados.png | MER/diagrama físico do banco MySQL (17 tabelas, chaves e relacionamentos), gerado pelo MySQL Workbench (Reverse Engineer do banco do Docker) | Etapa 7 (banco de dados) |
| mer-banco-de-dados.mwb | Modelo do MySQL Workbench do mesmo diagrama (abrir no Workbench para editar) | Etapa 7 |

As fontes editáveis estão em `fonte/` (Mermaid `.mmd` e SVG). Se o sistema mudar, o Claude Code
avisa no arquivo de alterações e os diagramas são atualizados no chat.
