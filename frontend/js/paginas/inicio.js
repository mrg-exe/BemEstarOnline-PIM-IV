// Página inicial: carrossel de destaques, vantagens, categorias e vitrines de produtos.
window.iniciarPagina = async function () {
  const todos = await Catalogo.produtos();
  Interface.lembrarProdutos(todos);
  const porSlug = s => todos.find(p => p.slug === s);

  // ---------- artes dos slides com as embalagens ----------
  const vitrine = slugs => `<div style="display:flex;justify-content:center;align-items:end">${slugs.map(porSlug).filter(Boolean)
    .map((p, i) => `<div style="width:46%;margin-left:${i ? '-10%' : '0'};filter:drop-shadow(0 8px 14px rgba(0,0,0,.25))">${Interface.embalagem(p)}</div>`).join('')}</div>`;
  document.getElementById('arte-assinatura').innerHTML = vitrine(['losartana-potassica-50mg', 'metformina-850mg', 'sinvastatina-20mg']);
  document.getElementById('arte-popular').innerHTML = vitrine(['hidroclorotiazida-25mg', 'salbutamol-spray-100mcg', 'glibenclamida-5mg']);

  // ---------- carrossel ----------
  const slides = [...document.querySelectorAll('.slide')];
  const pontos = document.getElementById('pontos');
  const botaoPausar = document.getElementById('pausar');
  const reduzirMovimento = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  let atual = 0, timer = null, pausadoPeloUsuario = reduzirMovimento;

  pontos.innerHTML = slides.map((_, i) => `<button type="button" aria-label="Ir para o destaque ${i + 1}"><span></span></button>`).join('');
  const bolinhas = [...pontos.querySelectorAll('button')];

  function mostrar(indice) {
    atual = (indice + slides.length) % slides.length;
    slides.forEach((s, i) => {
      s.classList.toggle('ativo', i === atual);
      s.setAttribute('aria-hidden', String(i !== atual));
      s.querySelectorAll('a,button').forEach(el => { el.tabIndex = i === atual ? 0 : -1; });
    });
    bolinhas.forEach((b, i) => b.setAttribute('aria-current', String(i === atual)));
  }
  function iniciar() { parar(); if (!pausadoPeloUsuario) timer = setInterval(() => mostrar(atual + 1), 6500); }
  function parar() { clearInterval(timer); timer = null; }
  function atualizarBotaoPausa() {
    botaoPausar.setAttribute('aria-label', pausadoPeloUsuario ? 'Retomar a troca automática' : 'Pausar a troca automática');
    botaoPausar.innerHTML = pausadoPeloUsuario
      ? '<svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M7 5l12 7-12 7z"/></svg>'
      : '<svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><rect x="6" y="5" width="4" height="14"/><rect x="14" y="5" width="4" height="14"/></svg>';
  }
  document.getElementById('anterior').addEventListener('click', () => { mostrar(atual - 1); iniciar(); });
  document.getElementById('proximo').addEventListener('click', () => { mostrar(atual + 1); iniciar(); });
  bolinhas.forEach((b, i) => b.addEventListener('click', () => { mostrar(i); iniciar(); }));
  botaoPausar.addEventListener('click', () => { pausadoPeloUsuario = !pausadoPeloUsuario; atualizarBotaoPausa(); iniciar(); });
  const carrossel = document.querySelector('.carrossel');
  carrossel.addEventListener('mouseenter', parar);
  carrossel.addEventListener('mouseleave', iniciar);
  carrossel.addEventListener('focusin', parar);
  carrossel.addEventListener('focusout', iniciar);
  mostrar(0);
  atualizarBotaoPausa();
  iniciar();

  // ---------- vantagens ----------
  document.getElementById('vantagens').innerHTML = [
    ['entrega', 'Entrega em até 2 horas', 'Cajamar e região'],
    ['desconto', 'Frete grátis', 'Em compras acima de R$ 99'],
    ['cartao', 'Pix, cartão ou dinheiro', 'Crédito em até 3x sem juros'],
    ['conversa', 'Farmacêutica online', 'Tire dúvidas pelo WhatsApp'],
  ].map(([ic, titulo, sub]) => `<li>${icone(ic, 30)}<div><strong>${titulo}</strong><span>${sub}</span></div></li>`).join('');

  // ---------- categorias ----------
  const categorias = await Catalogo.categorias();
  document.getElementById('categorias').innerHTML = categorias.map(c => `
    <li><a class="categoria" href="medicamentos.html?categoria=${encodeURIComponent(c.slug)}">
      <span class="icone-cat">${icone(c.slug, 30)}</span>${esc(c.nome)}</a></li>`).join('');

  // ---------- vitrines ----------
  const desenhar = (id, lista) => {
    const grade = document.getElementById(id);
    grade.innerHTML = lista.map(p => Interface.cartaoProduto(p)).join('');
    Interface.ligarAdicionar(grade, lista);
  };
  const maisVendidos = ['dipirona-sodica-500mg', 'protetor-solar-fps-50', 'vitamina-d3-2000-ui', 'loratadina-10mg', 'termometro-digital',
    'pomada-para-assaduras', 'paracetamol-750mg', 'oximetro-de-dedo', 'creme-dental-sensivel', 'complexo-b'].map(porSlug).filter(Boolean);
  desenhar('destaques', maisVendidos.length >= 4 ? maisVendidos.slice(0, 10) : todos.slice(0, 10));
  desenhar('continuos', todos.filter(p => p.usoContinuo && p.categoriaSlug === 'uso-continuo').slice(0, 5));

  document.getElementById('faixa-receita').innerHTML = `${icone('receita', 36)}
    <div><h2>Tem uma receita em mãos?</h2><p>Tire uma foto e mande pelo site. A farmacêutica separa tudo e a entrega sai em até 2 horas.</p></div>
    <a class="botao botao--contorno" href="delivery.html">Enviar receita</a>`;
};
