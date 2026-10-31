// Minha conta: pedidos (com rastreamento), assinaturas, endereços e dados pessoais.
window.iniciarPagina = async function (online) {
  if (!Sessao.exigirLogin()) return;
  const usuario = Sessao.usuario();
  const $ = id => document.getElementById(id);
  $('saudacao').textContent = `Olá, ${usuario.nomeExibicao.split(' ')[0]}`;
  $('sair').addEventListener('click', () => Sessao.sair());

  if (!online) {
    $('painel-pedidos').innerHTML = '<div class="vazio"><strong>A API está desligada.</strong>Ligue a API para ver seus pedidos.</div>';
    return;
  }

  // ---------------- abas (teclado: setas esquerda/direita) ----------------
  const abas = [...document.querySelectorAll('[role="tab"]')];
  const carregadores = { 'aba-pedidos': pedidos, 'aba-assinaturas': assinaturas, 'aba-enderecos': enderecos, 'aba-dados': dados };
  function abrir(aba) {
    abas.forEach(a => {
      const ativa = a === aba;
      a.setAttribute('aria-selected', String(ativa));
      a.tabIndex = ativa ? 0 : -1;
      $(a.getAttribute('aria-controls')).hidden = !ativa;
    });
    carregadores[aba.id]();
    history.replaceState(null, '', '#' + aba.id.replace('aba-', ''));
  }
  abas.forEach((a, i) => {
    a.addEventListener('click', () => abrir(a));
    a.addEventListener('keydown', e => {
      if (e.key === 'ArrowRight' || e.key === 'ArrowLeft') {
        const prox = abas[(i + (e.key === 'ArrowRight' ? 1 : abas.length - 1)) % abas.length];
        prox.focus(); abrir(prox);
      }
    });
  });

  // ---------------- pedidos (com filtros) ----------------
  let todosPedidos = [];
  const filtro = { situacao: 'todos', periodo: '90', busca: '' };
  const grupos = {
    todos: () => true,
    andamento: p => !['ENTREGUE', 'CANCELADO'].includes(p.status),
    entregues: p => p.status === 'ENTREGUE',
    cancelados: p => p.status === 'CANCELADO',
  };

  async function pedidos() {
    const painel = $('painel-pedidos');
    Interface.carregando(painel);
    try {
      todosPedidos = await Api.get('/pedidos');
      if (!todosPedidos.length) {
        painel.innerHTML = '<div class="vazio"><strong>Você ainda não fez pedidos.</strong><a href="medicamentos.html">Ver produtos</a></div>';
        return;
      }
      painel.innerHTML = `
        <div class="filtros-admin" role="group" aria-label="Filtrar por situação">
          ${[['todos', 'Todos'], ['andamento', 'Em andamento'], ['entregues', 'Entregues'], ['cancelados', 'Cancelados']]
            .map(([v, r]) => `<button type="button" data-situacao="${v}" aria-pressed="${filtro.situacao === v}">${r}</button>`).join('')}
        </div>
        <div class="grade-campos" style="max-width:640px">
          <div class="campo"><label for="filtro-periodo">Período</label>
            <select class="entrada" id="filtro-periodo">
              <option value="30">Últimos 30 dias</option><option value="90">Últimos 3 meses</option>
              <option value="365">Último ano</option><option value="todos">Todos</option>
            </select></div>
          <div class="campo"><label for="filtro-busca">Número do pedido</label>
            <input class="entrada" id="filtro-busca" inputmode="numeric" placeholder="Ex.: 12" value="${esc(filtro.busca)}"></div>
        </div>
        <p id="contagem-pedidos" role="status" style="color:var(--tinta-suave)"></p>
        <ul class="lista-cartoes" id="lista-pedidos"></ul>`;
      $('filtro-periodo').value = filtro.periodo;
      painel.querySelectorAll('[data-situacao]').forEach(b => b.addEventListener('click', () => {
        filtro.situacao = b.dataset.situacao;
        painel.querySelectorAll('[data-situacao]').forEach(x => x.setAttribute('aria-pressed', String(x === b)));
        listarPedidos();
      }));
      $('filtro-periodo').addEventListener('change', e => { filtro.periodo = e.target.value; listarPedidos(); });
      $('filtro-busca').addEventListener('input', e => { filtro.busca = e.target.value.replace(/\D/g, ''); listarPedidos(); });
      listarPedidos();
    } catch (e) { painel.innerHTML = `<div class="vazio"><strong>Não foi possível carregar.</strong>${esc(e.message)}</div>`; }
  }

  function listarPedidos() {
    const limite = filtro.periodo === 'todos' ? null : Date.now() - Number(filtro.periodo) * 86400000;
    const lista = todosPedidos.filter(p => grupos[filtro.situacao](p)
      && (!limite || lerData(p.criadoEm) >= limite)
      && (!filtro.busca || String(p.idPedido).includes(filtro.busca)));
    $('contagem-pedidos').textContent = `${lista.length} de ${todosPedidos.length} pedido(s)`;
    const ul = $('lista-pedidos');
    if (!lista.length) { ul.innerHTML = '<li class="vazio"><strong>Nenhum pedido com esses filtros.</strong></li>'; return; }
    ul.innerHTML = lista.map(p => `
      <li class="cartao-linha" id="pedido-${p.idPedido}">
        <div>
          <h3>Pedido nº ${p.idPedido}</h3>
          <p>${Formatar.dataHora(p.criadoEm)}, ${p.quantidadeItens} ${p.quantidadeItens === 1 ? 'item' : 'itens'}, ${Formatar.moeda(p.total)}${p.canal === 'APP' ? ', feito pelo app' : ''}</p>
        </div>
        <div style="display:grid;justify-items:end;gap:.4rem">
          ${Formatar.situacao(p.status)}
          <button type="button" class="link-botao" data-ver="${p.idPedido}" aria-expanded="false">${p.status === 'SAIU_PARA_ENTREGA' ? 'Ver no mapa' : 'Acompanhar'}</button>
        </div>
      </li>`).join('');
    ul.querySelectorAll('[data-ver]').forEach(botao => botao.addEventListener('click', () => abrirPedido(botao)));
  }

  async function abrirPedido(botao) {
    const linha = $(`pedido-${botao.dataset.ver}`);
    const aberto = linha.querySelector('.detalhe-expandido');
    if (aberto) { aberto.remove(); botao.setAttribute('aria-expanded', 'false'); botao.textContent = 'Acompanhar'; return; }
    const p = await Api.get(`/pedidos/${botao.dataset.ver}`);
    const div = document.createElement('div');
    div.className = 'detalhe-expandido';
    div.innerHTML = `
      ${p.status === 'SAIU_PARA_ENTREGA' ? '<h4>Onde está o entregador</h4><div class="area-mapa"></div>' : ''}
      <div class="duas-colunas"><div><h4>Linha do tempo</h4>${Interface.linhaDoTempo(p)}</div>
      <div><h4>Resumo</h4>${Interface.resumoPedido(p)}
      ${p.podeCancelar ? '<button type="button" class="botao botao--perigo botao--pequeno" data-cancelar>Cancelar pedido</button>' : ''}</div></div>`;
    linha.appendChild(div);
    botao.setAttribute('aria-expanded', 'true');
    botao.textContent = 'Fechar';
    const areaMapa = div.querySelector('.area-mapa');
    if (areaMapa) Rastreio.acompanhar(p.idPedido, areaMapa, () => { Interface.aviso('Seu pedido foi entregue!', 'sucesso'); pedidos(); });
    div.querySelector('[data-cancelar]')?.addEventListener('click', async () => {
      if (!confirm(`Cancelar o pedido nº ${p.idPedido}? Se já foi pago, o valor é estornado.`)) return;
      try { await Api.post(`/pedidos/${p.idPedido}/cancelar`); Interface.aviso('Pedido cancelado.', 'sucesso'); pedidos(); }
      catch (e) { Interface.aviso(e.message, 'erro'); }
    });
  }

  // ---------------- assinaturas ----------------
  async function assinaturas() {
    const painel = $('painel-assinaturas');
    Interface.carregando(painel);
    try {
      const lista = await Api.get('/assinaturas');
      if (!lista.length) {
        painel.innerHTML = '<div class="vazio"><strong>Nenhuma assinatura ainda.</strong><a href="assinatura.html">Montar uma assinatura com 10% de desconto</a></div>';
        return;
      }
      painel.innerHTML = `<ul class="lista-cartoes">${lista.map(a => `
        <li class="cartao-linha">
          <div>
            <h3>${esc(a.medicamento)}</h3>
            <p>${a.quantidade} cx. a cada ${a.periodicidadeDias} dias, para ${esc(a.nomePaciente)}. ${Formatar.moeda(a.valorPorEntrega)} por entrega.</p>
            ${a.status === 'ATIVA' ? `<p>Próxima entrega: <strong>${Formatar.data(a.proximaEntrega)}</strong></p>` : ''}
          </div>
          <div style="display:grid;justify-items:end;gap:.4rem">
            ${Formatar.situacao(a.status)}
            ${a.status === 'ATIVA' ? `<button class="link-botao" data-status="PAUSADA" data-id="${a.idAssinatura}">Pausar</button>` : ''}
            ${a.status === 'PAUSADA' ? `<button class="link-botao" data-status="ATIVA" data-id="${a.idAssinatura}">Retomar</button>` : ''}
            ${a.status !== 'CANCELADA' ? `<button class="link-botao" data-status="CANCELADA" data-id="${a.idAssinatura}">Cancelar</button>` : ''}
          </div>
        </li>`).join('')}</ul>`;
      painel.querySelectorAll('[data-status]').forEach(b => b.addEventListener('click', async () => {
        if (b.dataset.status === 'CANCELADA' && !confirm('Cancelar esta assinatura?')) return;
        try { await Api.patch(`/assinaturas/${b.dataset.id}/status`, { status: b.dataset.status }); Interface.aviso('Assinatura atualizada.', 'sucesso'); assinaturas(); }
        catch (e) { Interface.aviso(e.message, 'erro'); }
      }));
    } catch (e) { painel.innerHTML = `<div class="vazio"><strong>Não foi possível carregar.</strong>${esc(e.message)}</div>`; }
  }

  // ---------------- endereços ----------------
  async function enderecos() {
    const painel = $('painel-enderecos');
    Interface.carregando(painel);
    try {
      const lista = await Api.get('/enderecos');
      painel.innerHTML = `
        <div class="duas-colunas" style="align-items:start">
          <div>
            ${lista.length ? `<ul class="lista-cartoes">${lista.map(e => `
              <li class="cartao-linha"><div><h3>${esc(e.apelido || 'Endereço')}${e.principal ? ' (principal)' : ''}</h3><p>${esc(e.textoCompleto)}</p></div>
              <button class="link-botao" data-remover="${e.idEndereco}">Remover</button></li>`).join('')}</ul>`
              : '<div class="vazio"><strong>Nenhum endereço salvo.</strong>Cadastre ao lado ou na hora da compra.</div>'}
          </div>
          <form class="cartao" id="form-endereco" novalidate>
            <h3>Novo endereço</h3>
            <div class="grade-campos">
              <div class="campo"><label for="e-apelido">Nome do endereço</label><input class="entrada" id="e-apelido" placeholder="Casa, trabalho..."></div>
              <div class="campo"><label for="e-cep">CEP</label><input class="entrada" id="e-cep" inputmode="numeric" required></div>
            </div>
            <div class="campo"><label for="e-logradouro">Rua</label><input class="entrada" id="e-logradouro" required></div>
            <div class="grade-campos">
              <div class="campo"><label for="e-numero">Número</label><input class="entrada" id="e-numero" required></div>
              <div class="campo"><label for="e-complemento">Complemento</label><input class="entrada" id="e-complemento"></div>
            </div>
            <div class="campo"><label for="e-bairro">Bairro</label><input class="entrada" id="e-bairro" required></div>
            <div class="grade-campos">
              <div class="campo"><label for="e-cidade">Cidade</label><input class="entrada" id="e-cidade" required></div>
              <div class="campo"><label for="e-estado">Estado</label><input class="entrada" id="e-estado" maxlength="2" required></div>
            </div>
            <label class="marcar"><input type="checkbox" id="e-principal"> Usar como principal</label>
            <p style="margin:.5rem 0 1rem"><button type="button" class="link-botao" id="e-gps">Usar minha localização atual</button>
              <small id="e-gps-status" style="color:var(--tinta-suave)"></small></p>
            <button class="botao" type="submit">Salvar endereço</button>
          </form>
        </div>`;
      painel.querySelectorAll('[data-remover]').forEach(b => b.addEventListener('click', async () => {
        try { await Api.delete(`/enderecos/${b.dataset.remover}`); Interface.aviso('Endereço removido.'); enderecos(); }
        catch (e) { Interface.aviso(e.message, 'erro'); }
      }));
      let coordenadas = null;
      $('e-gps').addEventListener('click', async () => {
        $('e-gps-status').textContent = ' Procurando...';
        coordenadas = await Geo.minhaPosicao();
        $('e-gps-status').textContent = coordenadas ? ' Localização registrada.' : ' Não deu para usar a localização; usaremos o endereço.';
      });
      const cep = $('e-cep');
      Mascara.aplicar(cep, 'cep');
      cep.addEventListener('blur', () => preencherPorCep(cep.value, {
        logradouro: $('e-logradouro'), bairro: $('e-bairro'), cidade: $('e-cidade'), estado: $('e-estado') }));
      $('form-endereco').addEventListener('submit', async ev => {
        ev.preventDefault();
        const obrig = ['e-cep', 'e-logradouro', 'e-numero', 'e-bairro', 'e-cidade', 'e-estado'].map($);
        obrig.forEach(c => Interface.erroCampo(c, c.value.trim() ? '' : 'Preencha este campo.'));
        const vazio = obrig.find(c => !c.value.trim());
        if (vazio) { vazio.focus(); return; }
        try {
          const dto = {
            apelido: $('e-apelido').value || null, cep: cep.value, logradouro: $('e-logradouro').value, numero: $('e-numero').value,
            complemento: $('e-complemento').value || null, bairro: $('e-bairro').value, cidade: $('e-cidade').value,
            estado: $('e-estado').value.toUpperCase(), principal: $('e-principal').checked,
          };
          const pos = coordenadas || await Geo.doEndereco(dto);
          if (pos) { dto.latitude = pos.latitude; dto.longitude = pos.longitude; }
          await Api.post('/enderecos', dto);
          Interface.aviso('Endereço salvo.', 'sucesso');
          enderecos();
        } catch (e) { Interface.aviso(e.message, 'erro'); }
      });
    } catch (e) { painel.innerHTML = `<div class="vazio"><strong>Não foi possível carregar.</strong>${esc(e.message)}</div>`; }
  }

  // ---------------- dados ----------------
  async function dados() {
    const painel = $('painel-dados');
    try {
      const u = await Api.get('/auth/eu');
      painel.innerHTML = `<div class="cartao" style="max-width:640px">
        <dl class="ficha">
          <dt>Nome</dt><dd>${esc(u.nome)}</dd>
          ${u.nomeSocial ? `<dt>Nome social</dt><dd>${esc(u.nomeSocial)}</dd>` : ''}
          <dt>E-mail</dt><dd>${esc(u.email)}</dd>
          <dt>CPF</dt><dd>${esc(u.cpf.replace(/^(\d{3})\.\d{3}\.\d{3}/, '$1.***.***'))}</dd>
          <dt>Celular</dt><dd>${esc(u.telefone || '—')}</dd>
        </dl>
        <h3 style="margin-top:1.5rem">Senha</h3>
        <p><a class="botao botao--contorno botao--pequeno" href="alterar-senha.html">Alterar senha</a></p>
        <h3>Acessibilidade</h3>
        <p>Use os botões no topo da página para aumentar a letra, ativar o alto contraste ou o tema escuro. A escolha fica salva neste aparelho.</p>
        <h3>Privacidade</h3>
        <p>Usamos seus dados só para entregar pedidos e cumprir a lei (LGPD). Saiba mais na
          <a href="privacidade.html">Política de Privacidade</a> e nos <a href="termos.html">Termos de Uso</a>.</p>
        ${u.perfil === 'CLIENTE' ? `
        <details style="margin-top:1rem">
          <summary style="color:var(--tarja);font-weight:700;cursor:pointer">Excluir minha conta</summary>
          <form id="form-excluir" novalidate style="margin-top:1rem">
            <p>Ao excluir: seus dados pessoais, endereços, carrinho e receitas enviadas são apagados, as assinaturas são canceladas
               e você sai da conta. Os pedidos antigos continuam guardados pela farmácia, sem seu nome, CPF ou contato (exigência fiscal).
               Veja seus direitos na <a href="privacidade.html#direitos">Política de Privacidade</a>.</p>
            <div class="campo"><label for="excluir-senha">Digite sua senha para confirmar</label>
              <input class="entrada" id="excluir-senha" type="password" autocomplete="current-password"></div>
            <div class="campo"><label for="excluir-confirma">Digite EXCLUIR para confirmar</label>
              <input class="entrada" id="excluir-confirma" autocomplete="off"></div>
            <button class="botao botao--perigo" type="submit">Excluir minha conta definitivamente</button>
          </form>
        </details>` : '<p>Contas da equipe são desativadas pelo administrador.</p>'}
      </div>`;
      $('form-excluir')?.addEventListener('submit', async ev => {
        ev.preventDefault();
        if ($('excluir-confirma').value.trim().toUpperCase() !== 'EXCLUIR') { Interface.erroCampo($('excluir-confirma'), 'Digite EXCLUIR.'); return; }
        Interface.erroCampo($('excluir-confirma'), '');
        if (!$('excluir-senha').value) { Interface.erroCampo($('excluir-senha'), 'Digite sua senha.'); return; }
        try {
          await Api.post('/auth/excluir-conta', { senha: $('excluir-senha').value });
          Sessao.sair(null);
          alert('Sua conta foi excluída. Obrigado por ter sido nosso cliente.');
          location.href = 'index.html';
        } catch (e) { Interface.aviso(e.message, 'erro'); }
      });
    } catch (e) { painel.innerHTML = `<div class="vazio"><strong>Não foi possível carregar.</strong>${esc(e.message)}</div>`; }
  }

  const inicial = abas.find(a => a.id === 'aba-' + location.hash.slice(1)) || abas[0];
  abrir(inicial);
};
