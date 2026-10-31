// Página de um produto: ficha completa, leitura em voz alta, compra e relacionados.
window.iniciarPagina = async function () {
  const chave = new URLSearchParams(location.search).get('p');
  const alvo = document.getElementById('produto');
  const p = chave ? await Catalogo.produto(chave) : null;

  if (!p) {
    alvo.innerHTML = `<div class="vazio"><strong>Produto não encontrado.</strong><a href="medicamentos.html">Voltar ao catálogo</a></div>`;
    return;
  }

  document.title = `${p.nome} | Bem Estar Online`;
  Interface.lembrarProdutos([p]);
  document.getElementById('trilha').innerHTML += `
    <li><a href="medicamentos.html?categoria=${encodeURIComponent(p.categoriaSlug)}">${esc(p.categoria)}</a></li>
    <li aria-current="page">${esc(p.nome)}</li>`;

  const linhas = [
    ['Princípio ativo', p.principioAtivo], ['Dosagem', p.dosagem], ['Apresentação', p.apresentacao],
    ['Fabricante', p.fabricante], ['Tipo', p.tipo], ['Uso', p.viaAdministracao],
    ['Registro na Anvisa', p.registroAnvisa], ['Validade do lote', p.validadeLote ? Formatar.data(p.validadeLote) : null],
  ].filter(([, v]) => v);

  let quantidade = 1;
  alvo.innerHTML = `
    <div class="detalhe-produto">
      <div>${Interface.foto(p, 'grande')}</div>
      <div>
        <h1>${esc(p.nome)}</h1>
        <p style="color:var(--tinta-suave)">${esc(p.apresentacao || '')}${p.fabricante ? `, ${esc(p.fabricante)}` : ''}</p>
        ${Interface.selos(p)}
        <p style="margin-top:1rem">${esc(p.descricao)}</p>
        <button type="button" class="link-botao" id="ouvir">Ouvir a descrição em voz alta</button>

        <div class="compra">
          <div class="preco">${Formatar.moeda(p.preco)}<small>${Formatar.parcelas(p.preco)}</small></div>
          ${p.disponivel === false
            ? '<p class="estoque-baixo" style="margin:0">Esgotado no momento. <a href="delivery.html">Peça pelo delivery</a> que avisamos quando chegar.</p>'
            : `<div class="linha-compra">
                <div class="quantidade" role="group" aria-label="Quantidade">
                  <button type="button" id="menos" aria-label="Diminuir quantidade">−</button>
                  <output id="qtd" aria-live="polite">1</output>
                  <button type="button" id="mais" aria-label="Aumentar quantidade">+</button>
                </div>
                <button class="botao" type="button" id="adicionar">Adicionar ao carrinho</button>
                <button class="botao botao--contorno" type="button" id="comprar">Comprar agora</button>
              </div>`}
          ${p.exigeReceita ? `<div class="aviso-receita">${icone('receita', 24)}
            <p style="margin:0"><strong>Venda sob prescrição médica.</strong> Tenha a receita em mãos: o entregador confere na hora da entrega.</p></div>` : ''}
          ${p.usoContinuo ? `<p style="margin:0">Toma todo mês? <a href="assinatura.html?produto=${p.idMedicamento}">Assine e ganhe 10% em todas as entregas</a>.</p>` : ''}
        </div>

        <h2 style="font-size:1.2rem">Ficha do produto</h2>
        <dl class="ficha">${linhas.map(([k, v]) => `<dt>${esc(k)}</dt><dd>${esc(v)}</dd>`).join('')}</dl>
        <p style="margin-top:1rem;color:var(--tinta-suave);font-size:.95rem">Leia a bula antes de usar. Se os sintomas continuarem, procure um médico.</p>
      </div>
    </div>`;

  // Junta as partes com ". " sem repetir o ponto que a descrição já traz no fim.
  document.getElementById('ouvir').addEventListener('click', () =>
    Acessibilidade.ouvir([p.nome, p.apresentacao, p.descricao, `Preço: ${Formatar.moeda(p.preco)}`, p.exigeReceita ? 'Precisa de receita médica' : '']
      .filter(Boolean).map(t => t.trim().replace(/\.+$/, '')).join('. ') + '.'));

  if (p.disponivel !== false) {
    const saida = document.getElementById('qtd');
    document.getElementById('menos').addEventListener('click', () => { quantidade = Math.max(1, quantidade - 1); saida.textContent = quantidade; });
    document.getElementById('mais').addEventListener('click', () => { quantidade = Math.min(Math.max(1, p.estoque || 99), quantidade + 1); saida.textContent = quantidade; });
    document.getElementById('adicionar').addEventListener('click', () => {
      Carrinho.adicionar(p, quantidade);
      Interface.aviso(`${quantidade}x ${p.nome} no carrinho.`, 'sucesso');
    });
    document.getElementById('comprar').addEventListener('click', () => {
      Carrinho.adicionar(p, quantidade);
      location.href = 'carrinho.html';
    });
  }

  const mesmaCategoria = (await Catalogo.produtos({ categoria: p.categoriaSlug }))
    .filter(x => x.idMedicamento !== p.idMedicamento).slice(0, 4);
  if (mesmaCategoria.length) {
    const grade = document.getElementById('relacionados');
    grade.innerHTML = mesmaCategoria.map(x => Interface.cartaoProduto(x)).join('');
    Interface.ligarAdicionar(grade, mesmaCategoria);
    document.getElementById('relacionados-secao').hidden = false;
  }
};
