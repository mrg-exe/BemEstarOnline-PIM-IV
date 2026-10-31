// Catálogo: filtros por categoria, Farmácia Popular, uso contínuo, receita e ordenação.
window.iniciarPagina = async function () {
  const params = new URLSearchParams(location.search);
  const categoria = params.get('categoria') || '';
  const busca = params.get('busca') || '';
  const el = {
    grade: document.getElementById('grade'),
    titulo: document.getElementById('titulo-resultados'),
    contagem: document.getElementById('contagem'),
    ordenar: document.getElementById('ordenar'),
    popular: document.getElementById('filtro-popular'),
    continuo: document.getElementById('filtro-continuo'),
    disponiveis: document.getElementById('filtro-disponiveis'),
    semReceita: document.getElementById('filtro-sem-receita'),
  };
  el.popular.checked = params.get('farmaciaPopular') === 'true';
  el.continuo.checked = params.get('usoContinuo') === 'true';

  const categorias = await Catalogo.categorias();
  const catAtual = categorias.find(c => c.slug === categoria);
  const total = categorias.reduce((s, c) => s + c.totalProdutos, 0);
  document.getElementById('filtro-categorias').innerHTML =
    `<li><a href="medicamentos.html" ${!categoria ? 'aria-current="true"' : ''}>Todas <small>${total}</small></a></li>` +
    categorias.map(c => `<li><a href="medicamentos.html?categoria=${encodeURIComponent(c.slug)}" ${c.slug === categoria ? 'aria-current="true"' : ''}>
      ${esc(c.nome)} <small>${c.totalProdutos}</small></a></li>`).join('');

  el.titulo.textContent = busca ? `Resultados para "${busca}"` : catAtual ? catAtual.nome : 'Todos os produtos';
  document.title = `${el.titulo.textContent} | Bem Estar Online`;

  Interface.carregando(el.grade);
  const produtos = await Catalogo.produtos({ categoria, busca });
  Interface.lembrarProdutos(produtos);

  function desenhar() {
    let lista = [...produtos];
    if (el.popular.checked) lista = lista.filter(p => p.farmaciaPopular);
    if (el.continuo.checked) lista = lista.filter(p => p.usoContinuo);
    if (el.disponiveis.checked) lista = lista.filter(p => p.disponivel !== false);
    if (el.semReceita.checked) lista = lista.filter(p => !p.exigeReceita);
    if (el.ordenar.value === 'menor') lista.sort((a, b) => a.preco - b.preco);
    if (el.ordenar.value === 'maior') lista.sort((a, b) => b.preco - a.preco);

    el.contagem.textContent = `${lista.length} ${lista.length === 1 ? 'produto encontrado' : 'produtos encontrados'}`;
    if (!lista.length) {
      el.grade.innerHTML = `<li class="vazio" style="grid-column:1/-1"><strong>Não encontramos esse produto por aqui.</strong>
        Tente outro nome ou <a href="delivery.html">envie sua receita</a> que a farmacêutica procura para você.</li>`;
      return;
    }
    el.grade.innerHTML = lista.map(p => Interface.cartaoProduto(p)).join('');
    Interface.ligarAdicionar(el.grade, lista);
  }

  [el.ordenar, el.popular, el.continuo, el.disponiveis, el.semReceita].forEach(c => c.addEventListener('change', desenhar));
  desenhar();
};
