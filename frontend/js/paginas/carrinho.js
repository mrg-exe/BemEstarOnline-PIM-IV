// Carrinho e finalização da compra.
window.iniciarPagina = async function (online) {
  const el = {
    itens: document.getElementById('itens'),
    valores: document.getElementById('valores'),
    checkout: document.getElementById('checkout'),
    cupom: document.getElementById('cupom'),
    aplicar: document.getElementById('aplicar-cupom'),
    msgCupom: document.getElementById('mensagem-cupom'),
  };
  let cupom = sessionStorage.getItem('beo_cupom') || '';
  let resumo = null;
  el.cupom.value = cupom;

  function desenharItens() {
    const itens = Carrinho.itens();
    Interface.lembrarProdutos(itens);
    if (!itens.length) {
      el.itens.innerHTML = `<div class="vazio"><strong>Seu carrinho está vazio.</strong>
        <a href="medicamentos.html">Ver produtos</a> ou <a href="delivery.html">enviar uma receita</a>.</div>`;
      el.checkout.innerHTML = '';
      return false;
    }
    el.itens.innerHTML = `<ul class="lista-carrinho">${itens.map(i => `
      <li class="item-carrinho">
        <a href="produto.html?p=${encodeURIComponent(i.slug)}" aria-hidden="true" tabindex="-1">${Interface.foto(i, 'mini')}</a>
        <div>
          <h3><a href="produto.html?p=${encodeURIComponent(i.slug)}">${esc(i.nome)}</a></h3>
          <p style="margin:0;color:var(--tinta-suave)">${Formatar.moeda(i.preco)} cada</p>
          ${i.exigeReceita ? '<span class="selo-produto selo-produto--receita">Exige receita</span>' : ''}
        </div>
        <div class="direita">
          <div class="quantidade quantidade--pequena" role="group" aria-label="Quantidade de ${esc(i.nome)}">
            <button type="button" data-menos="${i.idMedicamento}" aria-label="Diminuir">−</button>
            <span>${i.quantidade}</span>
            <button type="button" data-mais="${i.idMedicamento}" aria-label="Aumentar">+</button>
          </div>
          <strong>${Formatar.moeda(i.preco * i.quantidade)}</strong>
          <button type="button" class="link-botao" data-remover="${i.idMedicamento}">Remover</button>
        </div>
      </li>`).join('')}</ul>`;

    const achar = id => Carrinho.itens().find(i => String(i.idMedicamento) === id);
    el.itens.querySelectorAll('[data-menos]').forEach(b => b.addEventListener('click', () => {
      const i = achar(b.dataset.menos); Carrinho.alterar(i.idMedicamento, i.quantidade - 1); atualizar();
    }));
    el.itens.querySelectorAll('[data-mais]').forEach(b => b.addEventListener('click', () => {
      const i = achar(b.dataset.mais); Carrinho.alterar(i.idMedicamento, i.quantidade + 1); atualizar();
    }));
    el.itens.querySelectorAll('[data-remover]').forEach(b => b.addEventListener('click', () => {
      const i = achar(b.dataset.remover); Carrinho.alterar(i.idMedicamento, 0);
      Interface.aviso(`${i.nome} saiu do carrinho.`); atualizar();
    }));
    return true;
  }

  async function calcular() {
    const itens = Carrinho.itens();
    if (!itens.length) return null;
    if (online) {
      try {
        const r = await Api.post('/pedidos/simular', {
          itens: itens.map(i => ({ idMedicamento: i.idMedicamento, quantidade: i.quantidade })),
          codigoCupom: cupom || null,
        });
        // Mantém os preços do carrinho iguais aos do servidor.
        const local = Carrinho.itens();
        let mudou = false;
        r.itens.forEach(s => { const l = local.find(x => x.idMedicamento === s.idMedicamento); if (l && l.preco !== s.precoUnitario) { l.preco = s.precoUnitario; mudou = true; } });
        if (mudou) Armazenamento.gravar('beo_carrinho', local);
        return r;
      } catch (e) {
        Interface.aviso(e.message, 'erro');
      }
    }
    const subtotal = Carrinho.subtotal();
    const frete = subtotal >= 99 ? 0 : 8.9;
    return { subtotal, desconto: 0, frete, total: subtotal + frete, cupomAplicado: null, mensagemCupom: cupom ? 'Cupons só funcionam com a API ligada.' : null,
             requerReceita: itens.some(i => i.exigeReceita), faltaParaFreteGratis: Math.max(0, 99 - subtotal) };
  }

  function desenharValores() {
    if (!resumo) { el.valores.innerHTML = '<p>Adicione produtos para ver o total.</p>'; return; }
    const progresso = Math.min(100, (resumo.subtotal / 99) * 100);
    el.valores.innerHTML = `
      <div class="linha-valor"><span>Produtos</span><span>${Formatar.moeda(resumo.subtotal)}</span></div>
      ${resumo.desconto > 0 ? `<div class="linha-valor"><span>Desconto (${esc(resumo.cupomAplicado)})</span><span class="desconto">− ${Formatar.moeda(resumo.desconto)}</span></div>` : ''}
      <div class="linha-valor"><span>Entrega</span><span>${resumo.frete === 0 ? '<span class="desconto">Grátis</span>' : Formatar.moeda(resumo.frete)}</span></div>
      ${resumo.faltaParaFreteGratis > 0 && resumo.frete > 0 ? `
        <div class="barra-frete" aria-hidden="true"><span style="width:${progresso}%"></span></div>
        <p style="margin:0;font-size:.92rem;color:var(--tinta-suave)">Faltam ${Formatar.moeda(resumo.faltaParaFreteGratis)} para o frete grátis.</p>` : ''}
      <div class="linha-valor total"><span>Total</span><span>${Formatar.moeda(resumo.total)}</span></div>
      ${resumo.requerReceita ? `<div class="aviso-receita" style="margin-top:.75rem">${icone('receita', 24)}
        <p style="margin:0">Seu pedido tem remédio com tarja vermelha. Tenha a receita em mãos na entrega.</p></div>` : ''}`;
    el.msgCupom.textContent = resumo.mensagemCupom || '';
    el.msgCupom.style.color = resumo.cupomAplicado ? 'var(--folha)' : 'var(--tarja)';
  }

  async function desenharCheckout() {
    if (!Carrinho.itens().length) { el.checkout.innerHTML = ''; return; }
    if (!online) {
      el.checkout.innerHTML = `<div class="aviso-receita" style="margin-top:1.5rem">${icone('conversa', 24)}
        <p style="margin:0">Para finalizar a compra, a API precisa estar ligada. O carrinho fica guardado até lá.</p></div>`;
      return;
    }
    if (!Sessao.logado()) {
      el.checkout.innerHTML = `<div class="cartao" style="margin-top:1.5rem">
        <h2 style="font-size:1.3rem">Quase lá!</h2>
        <p>Entre na sua conta para escolher o endereço e pagar. Seu carrinho continua aqui.</p>
        <a class="botao" href="login.html?voltar=carrinho.html">Entrar para finalizar</a>
        <a class="botao botao--contorno" href="cadastro.html?voltar=carrinho.html" style="margin-left:.5rem">Criar conta</a></div>`;
      return;
    }

    let enderecos = [];
    try { enderecos = await Api.get('/enderecos'); } catch (e) { Interface.aviso(e.message, 'erro'); }

    el.checkout.innerHTML = `
      <form class="cartao" id="form-checkout" style="margin-top:1.5rem" novalidate>
        <h2 style="font-size:1.3rem">Entrega e pagamento</h2>
        <fieldset style="border:0;padding:0;margin:0 0 1.25rem">
          <legend class="rotulo" style="font-weight:700;margin-bottom:.5rem">Onde entregar?</legend>
          <div class="opcoes-radio">
            ${enderecos.map((e, i) => `<label class="opcao"><input type="radio" name="endereco" value="${e.idEndereco}" ${i === 0 ? 'checked' : ''}>
              <span><strong>${esc(e.apelido || 'Endereço')}</strong><br>${esc(e.textoCompleto)}</span></label>`).join('')}
            <label class="opcao"><input type="radio" name="endereco" value="novo" ${enderecos.length ? '' : 'checked'}><span><strong>Usar outro endereço</strong></span></label>
          </div>
        </fieldset>
        <div id="novo-endereco" ${enderecos.length ? 'hidden' : ''}>
          <div class="grade-campos">
            <div class="campo"><label for="cep">CEP</label><input class="entrada" id="cep" inputmode="numeric" placeholder="00000-000"></div>
            <div class="campo"><label for="numero">Número</label><input class="entrada" id="numero"></div>
          </div>
          <div class="campo"><label for="logradouro">Rua</label><input class="entrada" id="logradouro" autocomplete="address-line1"></div>
          <div class="grade-campos">
            <div class="campo"><label for="complemento">Complemento</label><input class="entrada" id="complemento" placeholder="Apto, bloco..."></div>
            <div class="campo"><label for="bairro">Bairro</label><input class="entrada" id="bairro"></div>
          </div>
          <div class="grade-campos">
            <div class="campo"><label for="cidade">Cidade</label><input class="entrada" id="cidade" value="Cajamar"></div>
            <div class="campo"><label for="estado">Estado</label><input class="entrada" id="estado" maxlength="2" value="SP"></div>
          </div>
          <p style="margin:0 0 1rem"><button type="button" class="link-botao" id="usar-gps">Usar minha localização atual</button>
            <small id="gps-status" style="color:var(--tinta-suave)"> (ajuda o entregador a achar sua casa no mapa)</small></p>
        </div>
        <fieldset style="border:0;padding:0;margin:0 0 1.25rem">
          <legend class="rotulo" style="font-weight:700;margin-bottom:.5rem">Como prefere pagar?</legend>
          <div class="opcoes-radio">
            <label class="opcao"><input type="radio" name="pagamento" value="PIX" checked><span><strong>Pix</strong>, aprovado na hora</span></label>
            <label class="opcao"><input type="radio" name="pagamento" value="CARTAO_CREDITO"><span><strong>Cartão de crédito</strong>, até 3x sem juros</span></label>
            <label class="opcao"><input type="radio" name="pagamento" value="CARTAO_DEBITO"><span><strong>Cartão de débito</strong></span></label>
            <label class="opcao"><input type="radio" name="pagamento" value="DINHEIRO"><span><strong>Dinheiro na entrega</strong>, avise se precisa de troco</span></label>
          </div>
          <p style="font-size:.9rem;color:var(--tinta-suave);margin:.5rem 0 0">Pagamento simulado (projeto acadêmico): nenhum valor é cobrado de verdade.</p>
        </fieldset>
        <div class="campo"><label for="observacao">Observação para a entrega (opcional)</label>
          <input class="entrada" id="observacao" maxlength="250" placeholder="Ex.: troco para R$ 50, interfone 12"></div>
        <button class="botao botao--largo" type="submit" id="finalizar">Finalizar compra</button>
      </form>`;

    const form = document.getElementById('form-checkout');
    const blocoNovo = document.getElementById('novo-endereco');
    form.querySelectorAll('input[name="endereco"]').forEach(r => r.addEventListener('change', () => {
      blocoNovo.hidden = form.endereco.value !== 'novo';
    }));
    let coordenadas = null;
    document.getElementById('usar-gps').addEventListener('click', async () => {
      const status = document.getElementById('gps-status');
      status.textContent = ' Procurando sua localização...';
      coordenadas = await Geo.minhaPosicao();
      status.textContent = coordenadas ? ' Localização registrada.' : ' Não foi possível usar a localização. Sem problema: usamos o endereço digitado.';
    });
    const cep = document.getElementById('cep');
    Mascara.aplicar(cep, 'cep');
    cep.addEventListener('blur', () => preencherPorCep(cep.value, {
      logradouro: document.getElementById('logradouro'), bairro: document.getElementById('bairro'),
      cidade: document.getElementById('cidade'), estado: document.getElementById('estado'),
    }));

    form.addEventListener('submit', async (ev) => {
      ev.preventDefault();
      const dto = {
        itens: Carrinho.itens().map(i => ({ idMedicamento: i.idMedicamento, quantidade: i.quantidade })),
        codigoCupom: resumo?.cupomAplicado || null,
        metodoPagamento: form.pagamento.value,
        observacao: document.getElementById('observacao').value.trim() || null,
        canal: 'SITE',
      };
      if (form.endereco.value === 'novo') {
        const campos = ['cep', 'logradouro', 'numero', 'bairro', 'cidade', 'estado'].map(id => document.getElementById(id));
        let ok = true;
        campos.forEach(c => { const vazio = !c.value.trim(); Interface.erroCampo(c, vazio ? 'Preencha este campo.' : ''); if (vazio) ok = false; });
        if (!ok) { campos.find(c => !c.value.trim()).focus(); return; }
        dto.novoEndereco = {
          apelido: 'Entrega', cep: cep.value, logradouro: document.getElementById('logradouro').value,
          numero: document.getElementById('numero').value, complemento: document.getElementById('complemento').value || null,
          bairro: document.getElementById('bairro').value, cidade: document.getElementById('cidade').value,
          estado: document.getElementById('estado').value.toUpperCase(), principal: false,
        };
        const pos = coordenadas || await Geo.doEndereco(dto.novoEndereco);
        if (pos) { dto.novoEndereco.latitude = pos.latitude; dto.novoEndereco.longitude = pos.longitude; }
      } else {
        dto.idEndereco = Number(form.endereco.value);
      }

      const botao = document.getElementById('finalizar');
      botao.disabled = true;
      botao.textContent = 'Enviando pedido...';
      try {
        const pedido = await Api.post('/pedidos', dto);
        Armazenamento.gravar('beo_carrinho', []);
        sessionStorage.removeItem('beo_cupom');
        location.href = `confirmacao.html?pedido=${pedido.idPedido}`;
      } catch (e) {
        Interface.aviso(e.message, 'erro');
        if (e.status === 401) setTimeout(() => Sessao.exigirLogin(), 1500);
        botao.disabled = false;
        botao.textContent = 'Finalizar compra';
      }
    });
  }

  async function atualizar() {
    const temItens = desenharItens();
    resumo = temItens ? await calcular() : null;
    desenharItens();
    desenharValores();
  }

  el.aplicar.addEventListener('click', async () => {
    cupom = el.cupom.value.trim().toUpperCase();
    sessionStorage.setItem('beo_cupom', cupom);
    await atualizar();
  });
  el.cupom.addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); el.aplicar.click(); } });

  await atualizar();
  await desenharCheckout();
};
