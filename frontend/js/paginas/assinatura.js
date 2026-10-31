// Assinatura de uso contínuo: escolhe remédio, quantidade e intervalo; mostra o valor com 10% de desconto.
window.iniciarPagina = async function (online) {
  const area = document.getElementById('area-assinatura');
  const preSelecionado = new URLSearchParams(location.search).get('produto');
  const produtos = (await Catalogo.produtos({ usoContinuo: true })).filter(p => p.disponivel !== false);

  if (!online) {
    area.innerHTML = '<h2 style="font-size:1.3rem">Monte sua assinatura</h2><p>Ligue a API para montar uma assinatura.</p>';
    return;
  }
  if (!Sessao.logado()) {
    area.innerHTML = `<h2 style="font-size:1.3rem">Monte sua assinatura</h2>
      <p>Entre na sua conta para escolher o remédio e a data das entregas.</p>
      <a class="botao" href="login.html?voltar=assinatura.html">Entrar</a>
      <a class="botao botao--contorno" href="cadastro.html?voltar=assinatura.html" style="margin-left:.5rem">Criar conta</a>`;
    return;
  }

  const usuario = Sessao.usuario();
  area.innerHTML = `
    <h2 style="font-size:1.3rem">Monte sua assinatura</h2>
    <form id="form-assinatura" novalidate>
      <div class="campo">
        <label for="medicamento">Medicamento</label>
        <select class="entrada" id="medicamento" required>
          <option value="">Escolha da lista</option>
          ${produtos.map(p => `<option value="${p.idMedicamento}" ${String(p.idMedicamento) === preSelecionado ? 'selected' : ''}>
            ${esc(p.nome)} (${esc(p.apresentacao || '')}) - ${Formatar.moeda(p.preco)}</option>`).join('')}
        </select>
      </div>
      <div class="campo">
        <label for="paciente">Nome de quem vai tomar</label>
        <input class="entrada" id="paciente" value="${esc(usuario.nomeExibicao)}" required>
        <small>Pode ser você ou alguém que você cuida.</small>
      </div>
      <div class="campo">
        <label for="quantidade">Caixas por entrega</label>
        <select class="entrada" id="quantidade">${[1, 2, 3, 4].map(n => `<option>${n}</option>`).join('')}</select>
      </div>
      <fieldset style="border:0;padding:0;margin:0 0 1rem">
        <legend style="font-weight:700;margin-bottom:.5rem">Entregar a cada</legend>
        <div class="opcoes-radio" style="grid-template-columns:repeat(4,1fr)">
          ${[15, 30, 60, 90].map(d => `<label class="opcao"><input type="radio" name="periodo" value="${d}" ${d === 30 ? 'checked' : ''}>${d} dias</label>`).join('')}
        </div>
      </fieldset>
      <div id="valor-assinatura" class="compra" aria-live="polite" style="margin:0 0 1rem"></div>
      <button class="botao botao--largo" type="submit">Confirmar assinatura</button>
    </form>
    <div id="minhas" style="margin-top:1.5rem"></div>`;

  const $ = id => document.getElementById(id);
  const form = $('form-assinatura');

  function atualizarValor() {
    const p = produtos.find(x => String(x.idMedicamento) === $('medicamento').value);
    if (!p) { $('valor-assinatura').innerHTML = '<p style="margin:0">Escolha um medicamento para ver o valor.</p>'; return; }
    const qtd = Number($('quantidade').value);
    const cheio = p.preco * qtd;
    const comDesconto = Math.round(cheio * 0.9 * 100) / 100;
    $('valor-assinatura').innerHTML = `
      <div class="linha-valor"><span>Preço normal</span><span><s>${Formatar.moeda(cheio)}</s></span></div>
      <div class="linha-valor total" style="margin:0"><span>Por entrega</span><span>${Formatar.moeda(comDesconto)}</span></div>
      <p style="margin:0;font-size:.92rem;color:var(--tinta-suave)">Você economiza ${Formatar.moeda(cheio - comDesconto)} a cada entrega. Primeira entrega: amanhã.</p>`;
  }
  ['medicamento', 'quantidade'].forEach(id => $(id).addEventListener('change', atualizarValor));
  atualizarValor();

  async function listarMinhas() {
    try {
      const lista = await Api.get('/assinaturas');
      $('minhas').innerHTML = lista.length ? `<h3>Suas assinaturas</h3><ul class="lista-cartoes">${lista.map(a => `
        <li class="cartao-linha"><div><h3>${esc(a.medicamento)}</h3><p>${a.quantidade} cx. a cada ${a.periodicidadeDias} dias, para ${esc(a.nomePaciente)}</p></div>
        ${Formatar.situacao(a.status)}</li>`).join('')}</ul>
        <p style="margin-top:.5rem"><a href="conta.html#assinaturas">Pausar ou cancelar em Minha conta</a></p>` : '';
    } catch { /* lista é opcional */ }
  }
  listarMinhas();

  form.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    Interface.erroCampo($('medicamento'), $('medicamento').value ? '' : 'Escolha o medicamento.');
    Interface.erroCampo($('paciente'), $('paciente').value.trim() ? '' : 'Digite o nome do paciente.');
    if (form.querySelector('[aria-invalid="true"]')) return;
    try {
      const a = await Api.post('/assinaturas', {
        idMedicamento: Number($('medicamento').value),
        nomePaciente: $('paciente').value.trim(),
        quantidade: Number($('quantidade').value),
        periodicidadeDias: Number(form.periodo.value),
      });
      Interface.aviso(`Assinatura criada! Primeira entrega em ${Formatar.data(a.proximaEntrega)}.`, 'sucesso');
      listarMinhas();
    } catch (e) { Interface.aviso(e.message, 'erro'); }
  });
};
