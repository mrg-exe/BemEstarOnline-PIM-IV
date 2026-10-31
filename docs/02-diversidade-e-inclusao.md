# Etapa 3 — Responsabilidade social, diversidade e inclusão

| Item do manual | Como a solução atende | Onde está no sistema |
|---|---|---|
| Combate à discriminação | Campo **nome social** no cadastro (Decreto 8.727/2016): é o nome mostrado no site, no app, no painel e na entrega. Nenhum dado de raça, religião ou orientação é pedido. | `cadastro.html`, entidade `Usuario.NomeSocial`, coluna `usuarios.nome_social` |
| Valorização da diversidade | Linguagem simples e inclusiva em todas as telas; atendimento por texto e por receita fotografada para quem tem dificuldade de leitura; atendimento a cuidadores (assinatura em nome de outra pessoa, campo "nome do paciente"). | `assinatura.html`, `delivery.html` |
| Recursos tecnológicos inclusivos | Fonte **Atkinson Hyperlegible** (criada pelo Braille Institute para baixa visão); botões A / A+ / A++; **alto contraste** (preto/amarelo); tema escuro; **leitura em voz alta** da descrição do produto (site e app). | `css/estilo.css`, `js/app.js` (Acessibilidade), `AcessibilidadeServico.cs`, `ProdutoViewModel.OuvirAsync` |
| Ações de acessibilidade (WCAG 2.1) | Navegação por teclado com foco visível; link "Pular para o conteúdo"; textos alternativos; avisos lidos por leitor de tela (`aria-live`); botões com no mínimo 44–48 px; respeito a "reduzir movimento"; no app, `SemanticProperties` para TalkBack/Narrador e botões ampliados. | Todas as páginas e telas |
| Privacidade e autonomia | O próprio cliente exclui a conta no site ou no app, sem depender do suporte (LGPD, art. 18); ao sair da conta, nada dela fica no aparelho. | `conta.html` (Meus dados), Perfil do app, `AuthServico.ExcluirContaAsync` |
| Tecnologia para cidadania | Destaque e filtro do programa **Farmácia Popular** (remédios gratuitos para pressão, diabetes e asma); avisos de uso responsável ("leia a bula"); LGPD: consentimento explícito no cadastro, CPF mascarado na tela, senhas criptografadas. | Catálogo, `conta.html`, `SenhaHasher.cs` |

## Indicadores sugeridos
- % de clientes que usam alto contraste ou letra grande (medido no navegador, sem identificar a pessoa).
- Quantidade de pedidos com produtos do Farmácia Popular.
- Tempo médio de resposta às receitas enviadas.
