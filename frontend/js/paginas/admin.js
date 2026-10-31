// Área administrativa: painel, pedidos, produtos/estoque, reposição, assinaturas, receitas, ouvidoria e clientes.
// Só abre para os perfis ADMIN e ATENDENTE (a API também confere isso em cada chamada).
window.iniciarPagina = async function (online) {
  const conteudo = document.getElementById('admin-conteudo');
  const menu = document.getElementById('menu-admin');
  const dialogo = document.getElementById('dialogo');
  const $ = id => document.getElementById(id);

  if (!Sessao.exigirLogin()) return;
  if (!Sessao.ehEquipe()) {
    conteudo.innerHTML = '<div class="vazio"><strong>Área exclusiva da equipe da farmácia.</strong><a href="index.html">Voltar ao site</a></div>';
    menu.hidden = true;
    return;
  }
  if (!online) {
    conteudo.innerHTML = '<div class="vazio"><strong>A API está desligada.</strong>Ligue a API para usar o painel (veja o guia).</div>';
    return;
  }

  const ehAdmin = Sessao.ehAdmin();
  if (ehAdmin) menu.querySelector('[data-secao="equipe"]').hidden = false; // gestão da equipe: só ADMIN (RF35)
  let categorias = [];

  // ------------------------------------------------------------ utilidades
  function abrirDialogo(titulo, html) {
    $('dialogo-titulo').textContent = titulo;
    $('dialogo-corpo').innerHTML = html;
    if (!dialogo.open) dialogo.showModal();
  }
  $('fechar-dialogo').addEventListener('click', () => dialogo.close());

  function erro(e) { Interface.aviso(e.message || 'Algo deu errado.', 'erro'); }

  function cabecalho(titulo, extra = '') {
    return `<header><h1>${esc(titulo)}</h1><div>${extra}</div></header>`;
  }

  const emAberto = o => o.status === 'ABERTA' || o.status === 'EM_ANALISE';

  async function atualizarContadores() {
    try {
      const [painel, alertas, ouvidoria] = await Promise.all([Api.get('/admin/painel'), Api.get('/admin/alertas'), Api.get('/admin/ouvidoria')]);
      const c1 = $('cont-abertos'), c2 = $('cont-alertas'), c3 = $('cont-ouvidoria');
      c1.textContent = painel.pedidosEmAberto; c1.dataset.zero = String(painel.pedidosEmAberto === 0);
      c2.textContent = alertas.length; c2.dataset.zero = String(alertas.length === 0);
      const abertas = ouvidoria.filter(emAberto).length;
      c3.textContent = abertas; c3.dataset.zero = String(abertas === 0);
    } catch { /* os contadores são só um atalho visual */ }
  }

  // ------------------------------------------------------------ gráfico de vendas (SVG)
  function grafico(vendas) {
    // esq: 72 cabe "R$ 1.234,56" no eixo sem cortar o "R".
    const largura = 640, altura = 220, margem = { topo: 16, dir: 8, base: 30, esq: 72 };
    const maximo = Math.max(100, ...vendas.map(v => v.faturamento));
    const passo = (largura - margem.esq - margem.dir) / vendas.length;
    const escala = v => (altura - margem.topo - margem.base) * (v / maximo);
    const linhasGrade = [0, 0.5, 1].map(f => {
      const y = altura - margem.base - (altura - margem.topo - margem.base) * f;
      return `<line class="grade" x1="${margem.esq}" x2="${largura - margem.dir}" y1="${y}" y2="${y}"/>
              <text x="${margem.esq - 6}" y="${y + 4}" text-anchor="end">${Formatar.moeda(maximo * f).replace(',00', '')}</text>`;
    }).join('');
    const barras = vendas.map((v, i) => {
      const h = escala(v.faturamento);
      const x = margem.esq + i * passo + passo * 0.18;
      const y = altura - margem.base - h;
      return `<rect class="barra" x="${x}" y="${y}" width="${passo * 0.64}" height="${Math.max(h, 1)}" rx="3">
                <title>${Formatar.data(v.dia)}: ${Formatar.moeda(v.faturamento)} em ${v.quantidadePedidos} pedido(s)</title></rect>
              ${i % 2 === 0 || vendas.length <= 8 ? `<text x="${x + passo * 0.32}" y="${altura - 10}" text-anchor="middle">${Formatar.diaCurto(v.dia)}</text>` : ''}`;
    }).join('');
    const total = vendas.reduce((s, v) => s + v.faturamento, 0);
    return `<figure class="grafico" style="margin:0">
      <svg viewBox="0 0 ${largura} ${altura}" role="img" aria-label="Faturamento dos últimos 14 dias: ${Formatar.moeda(total)} no total">${linhasGrade}${barras}</svg>
      <figcaption style="color:var(--tinta-suave);font-size:.9rem">Total no período: ${Formatar.moeda(total)}. Passe o mouse nas barras para ver cada dia.</figcaption>
    </figure>`;
  }

  // ------------------------------------------------------------ seções
  const secoes = {
    async painel() {
      const p = await Api.get('/admin/painel');
      conteudo.innerHTML = `
        ${cabecalho('Visão geral', `<small style="color:var(--tinta-suave)">Banco: ${esc(p.modoBanco)}</small>`)}
        <div class="indicadores">
          <div class="indicador indicador--destaque"><span>Vendas hoje</span><strong>${Formatar.moeda(p.vendasHoje)}</strong></div>
          <div class="indicador"><span>Vendas no mês</span><strong>${Formatar.moeda(p.vendasMes)}</strong></div>
          <div class="indicador"><span>Pedidos hoje</span><strong>${p.pedidosHoje}</strong></div>
          <div class="indicador ${p.pedidosEmAberto ? 'indicador--alerta' : ''}"><span>Pedidos em aberto</span><strong>${p.pedidosEmAberto}</strong></div>
          <div class="indicador"><span>Clientes</span><strong>${p.totalClientes}</strong></div>
          <div class="indicador"><span>Assinaturas ativas</span><strong>${p.assinaturasAtivas}</strong></div>
          <div class="indicador ${p.solicitacoesAbertas ? 'indicador--alerta' : ''}"><span>Receitas para analisar</span><strong>${p.solicitacoesAbertas}</strong></div>
        </div>
        <div class="bloco" style="margin-bottom:1rem"><h2>Faturamento dos últimos 14 dias</h2>${grafico(p.vendasPorDia)}</div>
        <div class="duas-colunas">
          <div class="bloco"><h2>Estoque no mínimo</h2>
            ${p.estoqueBaixo.length ? `<div class="tabela-rolagem"><table><thead><tr><th>Produto</th><th class="numero">Tem</th><th class="numero">Mínimo</th></tr></thead>
              <tbody>${p.estoqueBaixo.map(e => `<tr><td>${esc(e.nome)}</td><td class="numero estoque-baixo">${e.quantidade}</td><td class="numero">${e.quantidadeMinima}</td></tr>`).join('')}</tbody></table></div>`
              : '<p>Nenhum produto abaixo do mínimo.</p>'}
          </div>
          <div class="bloco"><h2>Validade nos próximos 60 dias</h2>
            ${p.produtosVencendo.length ? `<div class="tabela-rolagem"><table><thead><tr><th>Produto</th><th>Validade</th><th class="numero">Dias</th></tr></thead>
              <tbody>${p.produtosVencendo.map(v => `<tr><td>${esc(v.nome)}</td><td>${Formatar.data(v.validadeLote)}</td>
                <td class="numero ${v.diasRestantes <= 30 ? 'estoque-baixo' : ''}">${v.diasRestantes}</td></tr>`).join('')}</tbody></table></div>`
              : '<p>Nenhum lote vencendo em breve.</p>'}
          </div>
        </div>`;
    },

    async pedidos(filtro = 'ABERTOS') {
      const opcoes = [['ABERTOS', 'Em aberto'], ['', 'Todos'], ...['AGUARDANDO_PAGAMENTO', 'PAGO', 'EM_SEPARACAO', 'SAIU_PARA_ENTREGA', 'ENTREGUE', 'CANCELADO']
        .map(s => [s, Formatar.status[s]])];
      const lista = await Api.get('/admin/pedidos' + (filtro ? `?status=${filtro}` : ''));
      conteudo.innerHTML = `
        ${cabecalho('Pedidos')}
        <div class="filtros-admin" role="group" aria-label="Filtrar pedidos">
          ${opcoes.map(([v, r]) => `<button type="button" data-filtro="${v}" aria-pressed="${v === filtro}">${esc(r)}</button>`).join('')}
        </div>
        ${lista.length ? `<div class="tabela-rolagem"><table>
          <thead><tr><th>Nº</th><th>Data</th><th>Cliente</th><th>Canal</th><th class="numero">Itens</th><th class="numero">Total</th><th>Situação</th><th></th></tr></thead>
          <tbody>${lista.map(p => `<tr>
            <td>${p.idPedido}</td><td>${Formatar.dataHora(p.criadoEm)}</td><td>${esc(p.cliente || '')}</td>
            <td>${p.canal === 'APP' ? 'App' : 'Site'}</td><td class="numero">${p.quantidadeItens}</td>
            <td class="numero">${Formatar.moeda(p.total)}</td><td>${Formatar.situacao(p.status)}</td>
            <td><button class="botao botao--pequeno botao--contorno" type="button" data-abrir="${p.idPedido}" aria-label="Abrir pedido nº ${p.idPedido}">Abrir</button></td></tr>`).join('')}
          </tbody></table></div>` : '<div class="vazio"><strong>Nenhum pedido neste filtro.</strong></div>'}`;

      conteudo.querySelectorAll('[data-filtro]').forEach(b => b.addEventListener('click', () => secoes.pedidos(b.dataset.filtro).catch(erro)));
      conteudo.querySelectorAll('[data-abrir]').forEach(b => b.addEventListener('click', () => abrirPedido(b.dataset.abrir, filtro)));
    },

    async produtos(busca = '') {
      if (!categorias.length) categorias = await Api.get('/categorias');
      const lista = await Api.get('/admin/produtos' + (busca ? `?busca=${encodeURIComponent(busca)}` : ''));
      Interface.lembrarProdutos(lista);
      conteudo.innerHTML = `
        ${cabecalho('Produtos e estoque', ehAdmin ? '<button class="botao" type="button" id="novo-produto">Novo produto</button>' : '')}
        <form id="busca-admin" class="cupom-linha" style="max-width:520px;margin-bottom:1rem" role="search">
          <label for="busca-produto" class="sr-only">Buscar produto</label>
          <input class="entrada" id="busca-produto" type="search" placeholder="Buscar por nome ou princípio ativo" value="${esc(busca)}"
            style="text-transform:none">
          <button class="botao botao--contorno" type="submit">Buscar</button>
        </form>
        <div class="tabela-rolagem"><table>
          <thead><tr><th>Produto</th><th>Categoria</th><th class="numero">Preço</th><th class="numero">Estoque</th><th class="numero">Mínimo</th><th>Situação</th><th></th></tr></thead>
          <tbody>${lista.map(p => `<tr>
            <td><div style="display:flex;gap:.6rem;align-items:center">${Interface.foto(p, 'mini')}
              <div><strong>${esc(p.nome)}</strong><br><small style="color:var(--tinta-suave)">${esc(p.apresentacao || '')}</small></div></div></td>
            <td>${esc(p.categoria)}</td><td class="numero">${Formatar.moeda(p.preco)}</td>
            <td class="numero ${p.estoque <= p.estoqueMinimo ? 'estoque-baixo' : ''}">${p.estoque}</td><td class="numero">${p.estoqueMinimo}</td>
            <td>${p.ativo ? 'À venda' : 'Desativado'}</td>
            <td style="white-space:nowrap">
              <button class="botao botao--pequeno botao--contorno" type="button" data-estoque="${p.idMedicamento}" aria-label="Ajustar estoque de ${esc(p.nome)}">Estoque</button>
              ${ehAdmin ? `<button class="botao botao--pequeno botao--contorno" type="button" data-editar="${p.idMedicamento}" aria-label="Editar ${esc(p.nome)}">Editar</button>` : ''}
            </td></tr>`).join('')}
          </tbody></table></div>`;

      $('busca-admin').addEventListener('submit', ev => { ev.preventDefault(); secoes.produtos($('busca-produto').value.trim()).catch(erro); });
      $('novo-produto')?.addEventListener('click', () => formularioProduto(null));
      conteudo.querySelectorAll('[data-estoque]').forEach(b => b.addEventListener('click', () =>
        ajustarEstoque(lista.find(p => String(p.idMedicamento) === b.dataset.estoque), () => secoes.produtos(busca))));
      conteudo.querySelectorAll('[data-editar]').forEach(b => b.addEventListener('click', () =>
        formularioProduto(lista.find(p => String(p.idMedicamento) === b.dataset.editar))));
    },

    async alertas() {
      const lista = await Api.get('/admin/alertas');
      conteudo.innerHTML = `
        ${cabecalho('Reposição de estoque')}
        <p>Os alertas são criados automaticamente quando uma venda deixa o produto no estoque mínimo (trigger do banco). Ao repor o estoque, o alerta se encerra sozinho.</p>
        ${lista.length ? `<div class="tabela-rolagem"><table>
          <thead><tr><th>Produto</th><th class="numero">Tinha no alerta</th><th class="numero">Tem agora</th><th class="numero">Mínimo</th><th>Desde</th><th></th></tr></thead>
          <tbody>${lista.map(a => `<tr><td>${esc(a.medicamento)}</td><td class="numero">${a.quantidadeAtual}</td>
            <td class="numero estoque-baixo">${a.estoqueAgora}</td><td class="numero">${a.quantidadeMinima}</td><td>${Formatar.dataHora(a.criadoEm)}</td>
            <td style="white-space:nowrap"><button class="botao botao--pequeno" type="button" data-repor="${a.idMedicamento}">Repor</button>
              <button class="botao botao--pequeno botao--contorno" type="button" data-resolver="${a.idAlerta}">Dispensar</button></td></tr>`).join('')}
          </tbody></table></div>` : '<div class="vazio"><strong>Nenhum alerta em aberto.</strong>Estoque em dia.</div>'}`;

      conteudo.querySelectorAll('[data-resolver]').forEach(b => b.addEventListener('click', async () => {
        try { await Api.post(`/admin/alertas/${b.dataset.resolver}/resolver`); Interface.aviso('Alerta dispensado.'); secoes.alertas(); atualizarContadores(); }
        catch (e) { erro(e); }
      }));
      conteudo.querySelectorAll('[data-repor]').forEach(b => b.addEventListener('click', async () => {
        try { const p = await Api.get(`/produtos/${b.dataset.repor}`); ajustarEstoque(p, () => { secoes.alertas(); atualizarContadores(); }); }
        catch (e) { erro(e); }
      }));
    },

    async assinaturas() {
      const lista = await Api.get('/admin/assinaturas');
      conteudo.innerHTML = `
        ${cabecalho('Assinaturas de uso contínuo')}
        ${lista.length ? `<div class="tabela-rolagem"><table>
          <thead><tr><th>Cliente</th><th>Paciente</th><th>Medicamento</th><th class="numero">Qtd.</th><th>Intervalo</th><th class="numero">Valor</th><th>Próxima entrega</th><th>Situação</th><th></th></tr></thead>
          <tbody>${lista.map(a => `<tr><td>${esc(a.cliente || '')}</td><td>${esc(a.nomePaciente)}</td><td>${esc(a.medicamento)}</td>
            <td class="numero">${a.quantidade}</td><td>${a.periodicidadeDias} dias</td><td class="numero">${Formatar.moeda(a.valorPorEntrega)}</td>
            <td>${a.status === 'ATIVA' ? Formatar.data(a.proximaEntrega) : '—'}</td><td>${Formatar.situacao(a.status)}</td>
            <td style="white-space:nowrap">
              ${a.status === 'ATIVA' ? `<button class="link-botao" data-id="${a.idAssinatura}" data-status="PAUSADA">Pausar</button>` : ''}
              ${a.status === 'PAUSADA' ? `<button class="link-botao" data-id="${a.idAssinatura}" data-status="ATIVA">Retomar</button>` : ''}
            </td></tr>`).join('')}</tbody></table></div>` : '<div class="vazio"><strong>Nenhuma assinatura ainda.</strong></div>'}`;
      conteudo.querySelectorAll('[data-status]').forEach(b => b.addEventListener('click', async () => {
        try { await Api.patch(`/assinaturas/${b.dataset.id}/status`, { status: b.dataset.status }); secoes.assinaturas(); }
        catch (e) { erro(e); }
      }));
    },

    async delivery() {
      const lista = await Api.get('/admin/delivery');
      const situacoes = ['RECEBIDO', 'EM_ANALISE', 'EM_ROTA', 'CONCLUIDO', 'CANCELADO'];
      conteudo.innerHTML = `
        ${cabecalho('Receitas e pedidos por mensagem')}
        ${lista.length ? `<ul class="lista-cartoes">${lista.map(d => `
          <li class="cartao-linha">
            <div>
              <h3>Nº ${d.idDelivery}, ${esc(d.nomeCliente)} <small style="font-weight:400;color:var(--tinta-suave)">${esc(d.telefone)}</small></h3>
              <p>${esc(d.observacao || '')}</p>
              <p>${esc(d.enderecoTexto)}</p>
              <p><small>${Formatar.dataHora(d.criadoEm)}</small></p>
              ${d.receitaArquivo ? `<button class="botao botao--contorno" type="button" data-receita="${d.idDelivery}"
                  aria-expanded="false" aria-controls="receita-${d.idDelivery}">Ver receita</button>
                <div id="receita-${d.idDelivery}" class="receita-aberta" hidden></div>` : ''}
            </div>
            <label>Situação
              <select class="entrada" data-delivery="${d.idDelivery}" style="min-width:170px">
                ${situacoes.map(s => `<option value="${s}" ${s === d.status ? 'selected' : ''}>${Formatar.status[s]}</option>`).join('')}
              </select></label>
          </li>`).join('')}</ul>` : '<div class="vazio"><strong>Nenhuma solicitação recebida.</strong></div>'}`;
      conteudo.querySelectorAll('[data-delivery]').forEach(s => s.addEventListener('change', async () => {
        try { await Api.patch(`/admin/delivery/${s.dataset.delivery}/status`, { status: s.value }); Interface.aviso('Situação atualizada.', 'sucesso'); }
        catch (e) { erro(e); }
      }));
      // Receita (RF39): o arquivo não tem endereço público; vem da API com o login da equipe e abre aqui mesmo.
      conteudo.querySelectorAll('[data-receita]').forEach(b => b.addEventListener('click', async () => {
        const area = document.getElementById(`receita-${b.dataset.receita}`);
        if (!area.hidden) { area.hidden = true; b.setAttribute('aria-expanded', 'false'); b.textContent = 'Ver receita'; return; }
        b.disabled = true;
        try {
          const arquivo = await Api.arquivo(`/admin/delivery/${b.dataset.receita}/receita`);
          if (area.dataset.url) URL.revokeObjectURL(area.dataset.url);
          const url = area.dataset.url = URL.createObjectURL(arquivo);
          area.innerHTML = (arquivo.type === 'application/pdf'
            ? `<iframe src="${url}" title="Receita da solicitação ${b.dataset.receita}"></iframe>`
            : `<img src="${url}" alt="Receita enviada na solicitação ${b.dataset.receita}">`)
            + `<p><a href="${url}" target="_blank" rel="noopener">Abrir em tela cheia</a></p>`;
          area.hidden = false;
          b.setAttribute('aria-expanded', 'true');
          b.textContent = 'Fechar receita';
        } catch (e) { erro(e); }
        finally { b.disabled = false; }
      }));
    },

    // Ouvidoria (RF30): a API já manda em aberto primeiro e, entre elas, discriminação primeiro.
    async ouvidoria(filtro = 'ABERTAS') {
      const todas = await Api.get('/admin/ouvidoria');
      const lista = filtro === 'ABERTAS' ? todas.filter(emAberto) : todas;
      const denunciasAbertas = todas.filter(o => emAberto(o) && o.tipo === 'DISCRIMINACAO').length;
      const situacoes = ['ABERTA', 'EM_ANALISE', 'RESPONDIDA', 'ENCERRADA'];
      conteudo.innerHTML = `
        ${cabecalho('Ouvidoria e denúncias')}
        ${denunciasAbertas ? `<div class="aviso-receita" style="margin-bottom:1rem">${icone('receita', 24)}
          <p style="margin:0"><strong>${denunciasAbertas} denúncia(s) de discriminação em aberto.</strong>
          Discriminação é crime (Lei 7.716/1989): trate com prioridade e responda a pessoa.</p></div>` : ''}
        <div class="filtros-admin" role="group" aria-label="Filtrar manifestações">
          <button type="button" data-filtro="ABERTAS" aria-pressed="${filtro === 'ABERTAS'}">Em aberto (${todas.filter(emAberto).length})</button>
          <button type="button" data-filtro="" aria-pressed="${filtro === ''}">Todas (${todas.length})</button>
        </div>
        ${lista.length ? `<ul class="lista-cartoes">${lista.map(o => `
          <li class="cartao-linha cartao-ouvidoria ${o.tipo === 'DISCRIMINACAO' ? 'cartao-ouvidoria--discriminacao' : ''}">
            <div>
              <h3>${o.tipo === 'DISCRIMINACAO' ? '<span class="selo-discriminacao">Discriminação</span>' : ''}${esc(Formatar.ouvidoria[o.tipo] || o.tipo)}${
                o.subtipoDiscriminacao ? `: ${esc(Formatar.discriminacao[o.subtipoDiscriminacao] || o.subtipoDiscriminacao)}` : ''}
                ${Formatar.situacao(o.status)}</h3>
              <p><small>Protocolo ${esc(o.protocolo)}, ${Formatar.dataHora(o.criadoEm)}.
                ${o.anonimo ? 'Anônimo.' : `${esc(o.cliente || 'Visitante sem conta')}${o.contato ? `, contato: ${esc(o.contato)}` : ', sem contato'}.`}</small></p>
              <p class="relato">${esc(o.descricao)}</p>
              ${o.resposta ? `<div class="resposta-ouvidoria"><strong>Resposta enviada</strong> <small>(${Formatar.dataHora(o.respondidoEm)})</small>
                <p style="margin:.3rem 0 0;white-space:pre-line">${esc(o.resposta)}</p></div>` : ''}
            </div>
            ${o.status === 'ENCERRADA' ? '' : `<form data-ouvidoria="${o.idOuvidoria}">
              <label for="resp-${o.idOuvidoria}">${o.resposta ? 'Nova resposta (substitui a anterior)' : 'Resposta para a pessoa'}</label>
              <textarea class="entrada" id="resp-${o.idOuvidoria}" rows="3" maxlength="4000"
                        placeholder="A pessoa lê esta resposta pelo protocolo."></textarea>
              <div class="linha-acoes">
                <label>Situação
                  <select class="entrada" name="status" style="min-width:170px">
                    ${situacoes.map(s => `<option value="${s}" ${s === o.status ? 'selected' : ''}>${Formatar.status[s]}</option>`).join('')}
                  </select></label>
                <button class="botao botao--pequeno" type="submit">Salvar</button>
              </div>
            </form>`}
          </li>`).join('')}</ul>` : '<div class="vazio"><strong>Nenhuma manifestação neste filtro.</strong></div>'}`;

      conteudo.querySelectorAll('[data-filtro]').forEach(b => b.addEventListener('click', () => secoes.ouvidoria(b.dataset.filtro).catch(erro)));
      conteudo.querySelectorAll('[data-ouvidoria]').forEach(f => f.addEventListener('submit', async ev => {
        ev.preventDefault();
        const id = f.dataset.ouvidoria;
        const resposta = f.querySelector('textarea').value.trim();
        const status = f.querySelector('select').value;
        // Escreveu a resposta e deixou a situação como estava: a API marca "Respondida".
        const original = todas.find(o => String(o.idOuvidoria) === id);
        const corpo = { resposta: resposta || null, status: resposta && status === original.status ? null : status };
        try {
          await Api.patch(`/admin/ouvidoria/${id}`, corpo);
          Interface.aviso(resposta ? 'Resposta enviada.' : 'Situação atualizada.', 'sucesso');
          await secoes.ouvidoria(filtro);
          atualizarContadores();
        } catch (e) { erro(e); }
      }));
    },

    // Gestão da equipe (RF35): só o administrador. Cadastra com senha provisória, muda o perfil,
    // desativa/reativa e redefine a senha. A API impede mexer na própria conta e ficar sem administrador.
    async equipe() {
      if (!ehAdmin) { conteudo.innerHTML = '<div class="vazio"><strong>Só o administrador gerencia a equipe.</strong></div>'; return; }
      const lista = await Api.get('/admin/equipe');
      const eu = Sessao.usuario()?.idUsuario;
      conteudo.innerHTML = `
        ${cabecalho('Gestão da equipe', '<button class="botao" type="button" id="nova-pessoa">Nova pessoa</button>')}
        <p style="color:var(--tinta-suave);margin-top:0">Quem é cadastrado aqui recebe uma <strong>senha provisória</strong> e
          cria a própria senha no primeiro acesso. Conta desativada não entra mais, nem no site nem no app.</p>
        <div class="tabela-rolagem"><table>
          <thead><tr><th>Pessoa</th><th>E-mail</th><th>Perfil</th><th>Situação</th><th></th></tr></thead>
          <tbody>${lista.map(p => `<tr>
            <td><strong>${esc(p.nomeExibicao)}</strong>${p.idUsuario === eu ? ' <small>(você)</small>' : ''}
              <br><small style="color:var(--tinta-suave)">${esc(p.telefone || '')}</small></td>
            <td>${esc(p.email)}</td>
            <td>${p.idUsuario === eu ? esc(PERFIS_EQUIPE[p.perfil] || p.perfil) : `
              <label class="sr-only" for="perfil-${p.idUsuario}">Perfil de ${esc(p.nomeExibicao)}</label>
              <select class="entrada" id="perfil-${p.idUsuario}" data-perfil="${p.idUsuario}" style="min-width:150px">
                ${Object.entries(PERFIS_EQUIPE).map(([v, r]) => `<option value="${v}" ${v === p.perfil ? 'selected' : ''}>${r}</option>`).join('')}
              </select>`}</td>
            <td>${p.ativo ? '<span class="situacao situacao--ATIVA">Ativa</span>' : '<span class="situacao situacao--CANCELADA">Desativada</span>'}
              ${p.trocarSenha ? '<br><small style="color:var(--tinta-suave)">Senha provisória</small>' : ''}</td>
            <td style="white-space:nowrap">${p.idUsuario === eu ? '' : `
              <button class="botao botao--pequeno botao--contorno" type="button" data-ativo="${p.idUsuario}" data-valor="${!p.ativo}"
                aria-label="${p.ativo ? 'Desativar' : 'Reativar'} ${esc(p.nomeExibicao)}">${p.ativo ? 'Desativar' : 'Reativar'}</button>
              <button class="botao botao--pequeno botao--contorno" type="button" data-senha="${p.idUsuario}"
                aria-label="Redefinir a senha de ${esc(p.nomeExibicao)}">Redefinir senha</button>`}</td>
          </tr>`).join('')}</tbody></table></div>`;

      const pessoa = id => lista.find(p => String(p.idUsuario) === String(id));
      $('nova-pessoa').addEventListener('click', () => novaPessoa());
      conteudo.querySelectorAll('[data-perfil]').forEach(s => s.addEventListener('change', async () => {
        try {
          await Api.patch(`/admin/equipe/${s.dataset.perfil}`, { perfil: s.value });
          Interface.aviso(`${pessoa(s.dataset.perfil).nomeExibicao} agora é ${PERFIS_EQUIPE[s.value]}.`, 'sucesso');
        } catch (e) { erro(e); }
        secoes.equipe().catch(erro);
      }));
      conteudo.querySelectorAll('[data-ativo]').forEach(b => b.addEventListener('click', async () => {
        const ativar = b.dataset.valor === 'true';
        const nome = pessoa(b.dataset.ativo).nomeExibicao;
        if (!ativar && !confirm(`Desativar a conta de ${nome}? A pessoa sai do sistema na hora e não consegue mais entrar.`)) return;
        try {
          await Api.patch(`/admin/equipe/${b.dataset.ativo}`, { ativo: ativar });
          Interface.aviso(ativar ? `Conta de ${nome} reativada.` : `Conta de ${nome} desativada.`, 'sucesso');
          secoes.equipe().catch(erro);
        } catch (e) { erro(e); }
      }));
      conteudo.querySelectorAll('[data-senha]').forEach(b => b.addEventListener('click', () => redefinirSenha(pessoa(b.dataset.senha))));
    },

    async clientes() {
      const lista = await Api.get('/admin/clientes');
      const perfis = { CLIENTE: 'Cliente', ATENDENTE: 'Atendente', ENTREGADOR: 'Entregador', ADMIN: 'Administrador' };
      conteudo.innerHTML = `
        ${cabecalho('Clientes e equipe')}
        <div class="grade-campos" style="max-width:820px">
          <div class="campo"><label for="f-cli-busca">Buscar</label>
            <input class="entrada" id="f-cli-busca" type="search" placeholder="Nome, e-mail ou celular"></div>
          <div class="campo"><label for="f-cli-perfil">Perfil</label>
            <select class="entrada" id="f-cli-perfil"><option value="">Todos</option>
              ${Object.entries(perfis).map(([v, r]) => `<option value="${v}">${r}</option>`).join('')}</select></div>
          <div class="campo"><label for="f-cli-compras">Compras</label>
            <select class="entrada" id="f-cli-compras"><option value="">Todos</option><option value="com">Já compraram</option>
              <option value="sem">Nunca compraram</option><option value="inativos">Contas excluídas</option></select></div>
        </div>
        <p id="cont-clientes" role="status" style="color:var(--tinta-suave)"></p>
        <div class="tabela-rolagem"><table>
          <thead><tr><th>Nome</th><th>E-mail</th><th>Celular</th><th>Perfil</th><th class="numero">Pedidos</th><th class="numero">Total comprado</th><th>Desde</th></tr></thead>
          <tbody id="corpo-clientes"></tbody></table></div>`;
      const filtrar = () => {
        const termo = normalizar($('f-cli-busca').value);
        const perfil = $('f-cli-perfil').value;
        const compras = $('f-cli-compras').value;
        const filtrados = lista.filter(c =>
          (!termo || normalizar(`${c.nomeExibicao} ${c.email} ${c.telefone || ''}`).includes(termo)) &&
          (!perfil || c.perfil === perfil) &&
          (compras !== 'com' || c.totalPedidos > 0) && (compras !== 'sem' || c.totalPedidos === 0) &&
          (compras === 'inativos' ? !c.ativo : compras === '' ? true : c.ativo));
        $('cont-clientes').textContent = `${filtrados.length} de ${lista.length} cadastro(s)`;
        $('corpo-clientes').innerHTML = filtrados.map(c => `<tr><td>${esc(c.nomeExibicao)}</td><td>${esc(c.email)}</td><td>${esc(c.telefone || '—')}</td>
          <td>${perfis[c.perfil] || c.perfil}${c.ativo ? '' : ' (excluída)'}</td><td class="numero">${c.totalPedidos}</td><td class="numero">${Formatar.moeda(c.totalGasto)}</td>
          <td>${Formatar.data(c.criadoEm)}</td></tr>`).join('') || '<tr><td colspan="7">Nenhum cadastro com esses filtros.</td></tr>';
      };
      ['f-cli-busca', 'f-cli-perfil', 'f-cli-compras'].forEach(id => $(id).addEventListener(id === 'f-cli-busca' ? 'input' : 'change', filtrar));
      filtrar();
    },
  };

  // ------------------------------------------------------------ diálogos
  async function abrirPedido(id, filtroAtual) {
    try {
      const p = await Api.get(`/pedidos/${id}`);
      const nomesAcao = { PAGO: 'Confirmar pagamento', EM_SEPARACAO: 'Começar separação', SAIU_PARA_ENTREGA: 'Saiu para entrega', ENTREGUE: 'Marcar como entregue', CANCELADO: 'Cancelar pedido' };
      abrirDialogo(`Pedido nº ${p.idPedido}`, `
        <p><strong>${esc(p.cliente)}</strong>${p.telefoneCliente ? `, ${esc(p.telefoneCliente)}` : ''}. Feito pelo ${p.canal === 'APP' ? 'app' : 'site'} em ${Formatar.dataHora(p.criadoEm)}.</p>
        <p>Situação: ${Formatar.situacao(p.status)}${p.entregador ? ` &nbsp; Entregador: <strong>${esc(p.entregador)}</strong>` : ''}</p>
        ${p.status === 'SAIU_PARA_ENTREGA' ? '<div id="mapa-admin"></div>' : ''}
        ${p.status === 'EM_SEPARACAO' && !p.entregador ? '<p class="mapa-legenda">Aguardando um entregador pegar este pedido no app.</p>' : ''}
        ${p.proximosStatus.length ? `<div class="linha-compra" style="margin:1rem 0">${p.proximosStatus.map(s =>
          `<button class="botao ${s === 'CANCELADO' ? 'botao--perigo' : ''} botao--pequeno" type="button" data-mudar="${s}">${esc(nomesAcao[s] || s)}</button>`).join('')}</div>` : ''}
        <div class="duas-colunas"><div><h3>Linha do tempo</h3>${Interface.linhaDoTempo(p)}</div><div><h3>Itens</h3>${Interface.resumoPedido(p)}</div></div>`);

      if ($('mapa-admin')) Rastreio.acompanhar(p.idPedido, $('mapa-admin'), () => abrirPedido(id, filtroAtual));
      $('dialogo-corpo').querySelectorAll('[data-mudar]').forEach(b => b.addEventListener('click', async () => {
        if (b.dataset.mudar === 'CANCELADO' && !confirm('Cancelar este pedido? Os itens voltam para o estoque.')) return;
        try {
          await Api.patch(`/admin/pedidos/${id}/status`, { status: b.dataset.mudar });
          Interface.aviso(`Pedido nº ${id}: ${Formatar.status[b.dataset.mudar]}.`, 'sucesso');
          await abrirPedido(id, filtroAtual);
          secoes.pedidos(filtroAtual).catch(erro);
          atualizarContadores();
        } catch (e) { erro(e); }
      }));
    } catch (e) { erro(e); }
  }

  // ------------------------------------------------------------ equipe (RF35)
  const PERFIS_EQUIPE = { ATENDENTE: 'Atendente', ENTREGADOR: 'Entregador', ADMIN: 'Administrador' };

  /** Senha provisória fácil de ditar: 8 letras/números sem os que confundem (0/O, 1/l/I). */
  function senhaProvisoria() {
    const letras = 'ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789';
    const sorteio = crypto.getRandomValues(new Uint32Array(8));
    return [...sorteio].map(n => letras[n % letras.length]).join('');
  }

  function mostrarSenhaCriada(titulo, nome, senha) {
    abrirDialogo(titulo, `
      <p>Passe esta <strong>senha provisória</strong> para ${esc(nome)} (pessoalmente ou por mensagem).
         No primeiro acesso, o site ou o app pede para criar uma senha nova.</p>
      <p class="protocolo" style="text-align:center;margin:1rem 0">${esc(senha)}</p>
      <p style="color:var(--tinta-suave)">Por segurança, ela não aparece de novo depois que esta janela fecha.</p>
      <button class="botao" type="button" id="fechar-senha">Entendi</button>`);
    $('fechar-senha').addEventListener('click', () => dialogo.close());
  }

  function novaPessoa() {
    abrirDialogo('Nova pessoa na equipe', `
      <form id="form-equipe" novalidate>
        <div class="campo"><label for="eq-nome">Nome completo</label><input class="entrada" id="eq-nome" autocomplete="off"></div>
        <div class="grade-campos">
          <div class="campo"><label for="eq-cpf">CPF</label><input class="entrada" id="eq-cpf" inputmode="numeric" placeholder="000.000.000-00"></div>
          <div class="campo"><label for="eq-celular">Celular</label><input class="entrada" id="eq-celular" type="tel" placeholder="(11) 90000-0000"></div>
        </div>
        <div class="campo"><label for="eq-email">E-mail (usado para entrar)</label><input class="entrada" id="eq-email" type="email" autocomplete="off"></div>
        <div class="campo"><label for="eq-perfil">Perfil</label>
          <select class="entrada" id="eq-perfil">
            <option value="">Escolha</option>
            ${Object.entries(PERFIS_EQUIPE).map(([v, r]) => `<option value="${v}">${r}</option>`).join('')}
          </select>
          <small>Atendente: painel (pedidos, estoque, receitas). Entregador: só o app. Administrador: tudo, inclusive a equipe.</small></div>
        <div class="campo"><label for="eq-senha">Senha provisória</label>
          <div class="cupom-linha"><input class="entrada" id="eq-senha" autocomplete="off" style="text-transform:none">
            <button class="botao botao--contorno" type="button" id="eq-gerar">Gerar outra</button></div></div>
        <button class="botao" type="submit">Cadastrar</button>
      </form>`);
    Mascara.aplicar($('eq-cpf'), 'cpf');
    Mascara.aplicar($('eq-celular'), 'telefone');
    $('eq-senha').value = senhaProvisoria();
    $('eq-gerar').addEventListener('click', () => { $('eq-senha').value = senhaProvisoria(); });
    $('form-equipe').addEventListener('submit', async ev => {
      ev.preventDefault();
      const regras = [
        [$('eq-nome'), $('eq-nome').value.trim().length >= 3 ? '' : 'Digite o nome completo.'],
        [$('eq-cpf'), $('eq-cpf').value.replace(/\D/g, '').length === 11 ? '' : 'Digite os 11 números do CPF.'],
        [$('eq-email'), /\S+@\S+\.\S+/.test($('eq-email').value) ? '' : 'Digite um e-mail válido.'],
        [$('eq-perfil'), $('eq-perfil').value ? '' : 'Escolha o perfil.'],
        [$('eq-senha'), $('eq-senha').value.length >= 6 ? '' : 'A senha precisa ter pelo menos 6 caracteres.'],
      ];
      regras.forEach(([c, m]) => Interface.erroCampo(c, m));
      const falha = regras.find(([, m]) => m);
      if (falha) { falha[0].focus(); return; }
      const senha = $('eq-senha').value;
      try {
        const p = await Api.post('/admin/equipe', {
          nome: $('eq-nome').value.trim(), cpf: $('eq-cpf').value, celular: $('eq-celular').value || null,
          email: $('eq-email').value.trim(), perfil: $('eq-perfil').value, senhaInicial: senha,
        });
        Interface.aviso(`${p.nomeExibicao} cadastrado(a) como ${PERFIS_EQUIPE[p.perfil]}.`, 'sucesso');
        mostrarSenhaCriada('Pessoa cadastrada', p.nomeExibicao, senha);
        secoes.equipe().catch(erro);
      } catch (e) { erro(e); }
    });
  }

  function redefinirSenha(p) {
    abrirDialogo(`Redefinir senha: ${p.nomeExibicao}`, `
      <form id="form-redefinir" novalidate>
        <p>A senha atual de ${esc(p.nomeExibicao)} deixa de valer. A pessoa entra com a senha provisória abaixo
           e cria uma nova no primeiro acesso.</p>
        <div class="campo"><label for="rd-senha">Senha provisória</label>
          <div class="cupom-linha"><input class="entrada" id="rd-senha" autocomplete="off" style="text-transform:none">
            <button class="botao botao--contorno" type="button" id="rd-gerar">Gerar outra</button></div></div>
        <button class="botao" type="submit">Redefinir senha</button>
      </form>`);
    $('rd-senha').value = senhaProvisoria();
    $('rd-gerar').addEventListener('click', () => { $('rd-senha').value = senhaProvisoria(); });
    $('form-redefinir').addEventListener('submit', async ev => {
      ev.preventDefault();
      const senha = $('rd-senha').value;
      Interface.erroCampo($('rd-senha'), senha.length >= 6 ? '' : 'A senha precisa ter pelo menos 6 caracteres.');
      if (senha.length < 6) return;
      try {
        await Api.post(`/admin/equipe/${p.idUsuario}/redefinir-senha`, { novaSenha: senha });
        mostrarSenhaCriada('Senha redefinida', p.nomeExibicao, senha);
        secoes.equipe().catch(erro);
      } catch (e) { erro(e); }
    });
  }

  function ajustarEstoque(p, aoSalvar) {
    abrirDialogo(`Estoque: ${p.nome}`, `
      <form id="form-estoque" novalidate>
        <p>Informe a quantidade que existe agora na prateleira (contagem).</p>
        <div class="grade-campos">
          <div class="campo"><label for="qtd-estoque">Quantidade atual</label><input class="entrada" id="qtd-estoque" type="number" min="0" value="${p.estoque}"></div>
          <div class="campo"><label for="min-estoque">Estoque mínimo</label><input class="entrada" id="min-estoque" type="number" min="0" value="${p.estoqueMinimo}"></div>
        </div>
        <button class="botao" type="submit">Salvar estoque</button>
      </form>`);
    $('form-estoque').addEventListener('submit', async ev => {
      ev.preventDefault();
      try {
        await Api.patch(`/admin/produtos/${p.idMedicamento}/estoque`, {
          quantidade: Number($('qtd-estoque').value), quantidadeMinima: Number($('min-estoque').value),
        });
        dialogo.close();
        Interface.aviso('Estoque atualizado.', 'sucesso');
        aoSalvar?.();
        atualizarContadores();
      } catch (e) { erro(e); }
    });
  }

  function formularioProduto(p) {
    const v = (campo, padrao = '') => esc(p?.[campo] ?? padrao);
    const marcado = campo => (p?.[campo] ? 'checked' : '');
    abrirDialogo(p ? `Editar: ${p.nome}` : 'Novo produto', `
      <form id="form-produto" novalidate>
        <div class="campo"><label for="f-nome">Nome</label><input class="entrada" id="f-nome" value="${v('nome')}" required></div>
        <div class="grade-campos">
          <div class="campo"><label for="f-categoria">Categoria</label><select class="entrada" id="f-categoria">
            ${categorias.map(c => `<option value="${c.idCategoria}" ${p?.idCategoria === c.idCategoria ? 'selected' : ''}>${esc(c.nome)}</option>`).join('')}</select></div>
          <div class="campo"><label for="f-tipo">Tipo</label><input class="entrada" id="f-tipo" value="${v('tipo', 'Genérico')}" list="tipos">
            <datalist id="tipos"><option>Genérico</option><option>Referência</option><option>Similar</option><option>Suplemento</option><option>Higiene</option><option>Dermocosmético</option><option>Correlato</option><option>Infantil</option></datalist></div>
        </div>
        <div class="grade-campos">
          <div class="campo"><label for="f-principio">Princípio ativo</label><input class="entrada" id="f-principio" value="${v('principioAtivo')}"></div>
          <div class="campo"><label for="f-dosagem">Dosagem</label><input class="entrada" id="f-dosagem" value="${v('dosagem')}"></div>
        </div>
        <div class="grade-campos">
          <div class="campo"><label for="f-apresentacao">Apresentação</label><input class="entrada" id="f-apresentacao" value="${v('apresentacao')}" placeholder="Ex.: 30 comprimidos"></div>
          <div class="campo"><label for="f-fabricante">Fabricante</label><input class="entrada" id="f-fabricante" value="${v('fabricante')}" required></div>
        </div>
        <div class="campo"><label for="f-descricao">Descrição</label><textarea class="entrada" id="f-descricao" required>${v('descricao')}</textarea></div>
        <div class="grade-campos">
          <div class="campo"><label for="f-preco">Preço (R$)</label><input class="entrada" id="f-preco" type="number" step="0.01" min="0.01" value="${p?.preco ?? ''}" required></div>
          <div class="campo"><label for="f-validade">Validade do lote</label><input class="entrada" id="f-validade" type="date" value="${p?.validadeLote ? p.validadeLote.slice(0, 10) : ''}"></div>
        </div>
        <div class="grade-campos">
          <div class="campo"><label for="f-via">Via de uso</label><input class="entrada" id="f-via" value="${v('viaAdministracao')}"></div>
          <div class="campo"><label for="f-anvisa">Registro Anvisa</label><input class="entrada" id="f-anvisa" value="${v('registroAnvisa')}"></div>
        </div>
        ${p ? '' : `<div class="grade-campos">
          <div class="campo"><label for="f-estoque">Estoque inicial</label><input class="entrada" id="f-estoque" type="number" min="0" value="0"></div>
          <div class="campo"><label for="f-minimo">Estoque mínimo</label><input class="entrada" id="f-minimo" type="number" min="0" value="5"></div></div>`}
        <fieldset style="border:1px solid var(--linha);border-radius:var(--r-m);padding:1rem;margin:0 0 1rem">
          <legend style="font-weight:700;padding:0 .3rem">Foto do produto</legend>
          ${p ? `<div style="display:flex;gap:1rem;align-items:center;margin-bottom:.75rem">${Interface.foto(p, 'mini')}
            <small>Sem foto, o site mostra um desenho da embalagem.</small></div>
          <div class="campo"><label for="f-arquivo">Enviar foto (JPG, PNG ou WEBP, até 3 MB)</label>
            <input class="entrada" id="f-arquivo" type="file" accept=".jpg,.jpeg,.png,.webp"></div>` : '<p style="margin-top:0">Depois de cadastrar, abra "Editar" para enviar a foto.</p>'}
          <div class="campo" style="margin-bottom:0"><label for="f-imagem-url">Ou endereço (URL) de uma foto</label>
            <input class="entrada" id="f-imagem-url" type="url" placeholder="https://..." value="${esc(p?.imagemUrl && /^https?:/.test(p.imagemUrl) ? p.imagemUrl : '')}"></div>
        </fieldset>
        <label class="marcar"><input type="checkbox" id="f-receita" ${marcado('exigeReceita')}> Exige receita (tarja vermelha)</label>
        <label class="marcar"><input type="checkbox" id="f-continuo" ${marcado('usoContinuo')}> Uso contínuo (aceita assinatura)</label>
        <label class="marcar"><input type="checkbox" id="f-popular" ${marcado('farmaciaPopular')}> Farmácia Popular</label>
        <label class="marcar" style="margin-bottom:1rem"><input type="checkbox" id="f-ativo" ${p ? marcado('ativo') : 'checked'}> À venda no site e no app</label>
        <button class="botao" type="submit">${p ? 'Salvar alterações' : 'Cadastrar produto'}</button>
      </form>`);

    $('form-produto').addEventListener('submit', async ev => {
      ev.preventDefault();
      const obrig = ['f-nome', 'f-fabricante', 'f-descricao', 'f-preco'].map($);
      obrig.forEach(c => Interface.erroCampo(c, c.value.trim() ? '' : 'Preencha este campo.'));
      if (obrig.some(c => !c.value.trim())) return;
      const nulo = id => $(id).value.trim() || null;
      const dto = {
        nome: $('f-nome').value.trim(), idCategoria: Number($('f-categoria').value), tipo: $('f-tipo').value.trim() || 'Genérico',
        principioAtivo: nulo('f-principio'), descricao: $('f-descricao').value.trim(), dosagem: nulo('f-dosagem'),
        apresentacao: nulo('f-apresentacao'), fabricante: $('f-fabricante').value.trim(), viaAdministracao: nulo('f-via'),
        registroAnvisa: nulo('f-anvisa'), validadeLote: nulo('f-validade'), preco: Number($('f-preco').value),
        exigeReceita: $('f-receita').checked, usoContinuo: $('f-continuo').checked, farmaciaPopular: $('f-popular').checked,
        ativo: $('f-ativo').checked,
        estoqueInicial: p ? null : Number($('f-estoque').value), estoqueMinimo: p ? null : Number($('f-minimo').value),
        imagemUrl: $('f-imagem-url').value.trim() || (p?.imagemUrl && !/^https?:/.test(p.imagemUrl) ? p.imagemUrl : ''),
      };
      try {
        if (p) await Api.put(`/admin/produtos/${p.idMedicamento}`, dto);
        else await Api.post('/admin/produtos', dto);
        const arquivo = $('f-arquivo')?.files?.[0];
        if (p && arquivo) await enviarFoto(p.idMedicamento, arquivo);
        dialogo.close();
        Interface.aviso(p ? 'Produto atualizado.' : 'Produto cadastrado.', 'sucesso');
        secoes.produtos().catch(erro);
      } catch (e) { erro(e); }
    });
  }

  /** Envia a foto como arquivo (multipart), com o token de login. */
  async function enviarFoto(idProduto, arquivo) {
    const corpo = new FormData();
    corpo.append('arquivo', arquivo);
    const r = await fetch(`${Api.base}/admin/produtos/${idProduto}/imagem`, {
      method: 'POST', headers: { Authorization: `Bearer ${Sessao.token()}` }, body: corpo,
    });
    if (!r.ok) {
      let msg = 'Não foi possível enviar a foto.';
      try { msg = (await r.json()).mensagem || msg; } catch { /* sem corpo */ }
      throw new ErroApi(msg, r.status);
    }
  }

  // ------------------------------------------------------------ navegação do menu
  async function ir(secao) {
    menu.querySelectorAll('button').forEach(b => {
      if (b.dataset.secao === secao) b.setAttribute('aria-current', 'page'); else b.removeAttribute('aria-current');
    });
    Interface.carregando(conteudo);
    try { await secoes[secao](); }
    catch (e) { conteudo.innerHTML = `<div class="vazio"><strong>Não foi possível carregar.</strong>${esc(e.message)}</div>`; }
    history.replaceState(null, '', '#' + secao);
  }
  menu.querySelectorAll('button').forEach(b => b.addEventListener('click', () => ir(b.dataset.secao)));
  // Link ou endereço digitado com #secao (ex.: admin.html#ouvidoria) com o painel já aberto: troca a seção.
  window.addEventListener('hashchange', () => { const s = location.hash.slice(1); if (secoes[s]) ir(s); });

  const inicial = location.hash.slice(1);
  await ir(secoes[inicial] ? inicial : 'painel');
  atualizarContadores();
};
