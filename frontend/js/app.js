/* =================================================================
   Bem Estar Online — script principal (usado por todas as páginas)
   -----------------------------------------------------------------
   Organização:
     Armazenamento -> guarda dados no navegador (localStorage)
     Sessao        -> login do usuário (token JWT recebido da API)
     Api           -> conversa com a API C# (fetch + token)
     Catalogo      -> produtos e categorias (API ou cópia local)
     Carrinho      -> itens do carrinho, sincronizados com a API
     Formatar      -> dinheiro, datas e nomes de status
     Interface     -> cabeçalho, rodapé, avisos, cartões de produto
     Acessibilidade-> tamanho da letra, alto contraste, tema escuro
   Cada página tem seu próprio arquivo em js/paginas/ com a função
   iniciarPagina(), chamada no fim deste arquivo.
   ================================================================= */

/* ------------------------------------------------------------ Armazenamento */
const Armazenamento = {
  ler(chave, padrao = null) {
    try { const v = localStorage.getItem(chave); return v === null ? padrao : JSON.parse(v); }
    catch { return padrao; }
  },
  gravar(chave, valor) { try { localStorage.setItem(chave, JSON.stringify(valor)); } catch { /* modo privado */ } },
  remover(chave) { try { localStorage.removeItem(chave); } catch { /* ignora */ } },
};

/* ------------------------------------------------------------ Acessibilidade */
// Aplicado o quanto antes para a página não "piscar" com o tema errado.
const Acessibilidade = {
  preferencias() {
    return Armazenamento.ler('beo_acessibilidade', { fonte: 'normal', contraste: 'normal', tema: 'auto' });
  },
  aplicar() {
    const p = this.preferencias();
    const raiz = document.documentElement;
    raiz.dataset.fonte = p.fonte;
    if (p.contraste === 'alto') raiz.dataset.contraste = 'alto'; else delete raiz.dataset.contraste;
    if (p.tema === 'auto') delete raiz.dataset.tema; else raiz.dataset.tema = p.tema;
  },
  alterar(campo, valor) {
    const p = this.preferencias();
    p[campo] = valor;
    Armazenamento.gravar('beo_acessibilidade', p);
    this.aplicar();
    Interface.atualizarBotoesAcessibilidade();
  },
  temaEscuroAtivo() {
    const p = this.preferencias();
    if (p.tema === 'escuro') return true;
    if (p.tema === 'claro') return false;
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
  },
  /** Lê um texto em voz alta (síntese de voz do navegador), útil para pessoas com baixa visão ou pouca leitura. */
  ouvir(texto) {
    if (!('speechSynthesis' in window)) { Interface.aviso('Seu navegador não tem leitura em voz alta.', 'erro'); return; }
    window.speechSynthesis.cancel();
    const fala = new SpeechSynthesisUtterance(texto);
    fala.lang = 'pt-BR';
    fala.rate = 0.95;
    window.speechSynthesis.speak(fala);
  },
};
Acessibilidade.aplicar();

/* ------------------------------------------------------------ Sessão (login) */
const Sessao = {
  dados() {
    const s = Armazenamento.ler('beo_sessao');
    if (!s) return null;
    if (s.expiraEm && new Date(s.expiraEm) < new Date()) { Armazenamento.remover('beo_sessao'); return null; }
    return s;
  },
  usuario() { return this.dados()?.usuario ?? null; },
  token() { return this.dados()?.token ?? null; },
  logado() { return !!this.dados(); },
  ehEquipe() { const p = this.usuario()?.perfil; return p === 'ADMIN' || p === 'ATENDENTE'; },
  ehAdmin() { return this.usuario()?.perfil === 'ADMIN'; },
  entrar(resposta) {
    Armazenamento.gravar('beo_sessao', { token: resposta.token, expiraEm: resposta.expiraEm, usuario: resposta.usuario });
  },
  /** Troca só os dados do usuário guardados (ex.: depois de alterar a senha), mantendo o token. */
  atualizarUsuario(usuario) {
    const s = this.dados();
    if (s) Armazenamento.gravar('beo_sessao', { ...s, usuario });
  },
  /** Sai da conta e apaga deste navegador tudo o que era dela (carrinho, cupom, endereços em cache). */
  sair(destino = 'index.html') {
    Armazenamento.remover('beo_sessao');
    Armazenamento.remover('beo_carrinho');
    try { sessionStorage.clear(); } catch { /* ignora */ }
    if (destino) window.location.href = destino;
  },
  /** Leva para o login e, depois de entrar, volta para a página atual. */
  exigirLogin() {
    if (this.logado()) return true;
    const voltar = encodeURIComponent(location.pathname.split('/').pop() + location.search);
    window.location.href = `login.html?voltar=${voltar}`;
    return false;
  },
};

/* ------------------------------------------------------------ API */
class ErroApi extends Error {
  constructor(mensagem, status) { super(mensagem); this.status = status; }
}

const Api = {
  base: (window.BEO_CONFIG && window.BEO_CONFIG.apiBase) || 'http://localhost:5088/api',
  /** Endereço do servidor sem o "/api" (as fotos enviadas pelo painel ficam em /imagens/...). */
  get origem() { return this.base.replace(/\/api\/?$/, ''); },

  async requisitar(metodo, caminho, corpo) {
    const cabecalhos = { 'Accept': 'application/json' };
    // FormData (formulário com arquivo, ex.: a receita): o navegador monta o multipart sozinho.
    const ehFormulario = corpo instanceof FormData;
    if (corpo !== undefined && !ehFormulario) cabecalhos['Content-Type'] = 'application/json';
    const token = Sessao.token();
    if (token) cabecalhos['Authorization'] = `Bearer ${token}`;

    let resposta;
    try {
      resposta = await fetch(this.base + caminho, {
        method: metodo,
        headers: cabecalhos,
        body: corpo === undefined ? undefined : ehFormulario ? corpo : JSON.stringify(corpo),
      });
    } catch {
      sessionStorage.setItem('beo_api_online', 'nao');
      throw new ErroApi('Não foi possível falar com o servidor. Confira se a API está ligada.', 0);
    }

    if (resposta.status === 204) return null;
    const texto = await resposta.text();
    let dados = null;
    try { dados = texto ? JSON.parse(texto) : null; } catch { dados = null; }

    if (resposta.status === 401 && token) {
      Armazenamento.remover('beo_sessao');
      // A API explica quando a conta foi desativada ou o perfil mudou (RF35); senão, a sessão venceu.
      throw new ErroApi(dados?.mensagem || 'Sua sessão expirou. Entre de novo para continuar.', 401);
    }
    if (!resposta.ok) {
      const mensagem = dados?.mensagem || dados?.title || 'Não foi possível concluir. Tente de novo.';
      throw new ErroApi(mensagem, resposta.status);
    }
    return dados;
  },
  get(caminho) { return this.requisitar('GET', caminho); },
  post(caminho, corpo = {}) { return this.requisitar('POST', caminho, corpo); },
  put(caminho, corpo) { return this.requisitar('PUT', caminho, corpo); },
  patch(caminho, corpo) { return this.requisitar('PATCH', caminho, corpo); },
  delete(caminho) { return this.requisitar('DELETE', caminho); },

  /** Baixa um arquivo protegido (ex.: receita no painel) com o login e devolve um Blob. */
  async arquivo(caminho) {
    const token = Sessao.token();
    let resposta;
    try {
      resposta = await fetch(this.base + caminho, { headers: token ? { 'Authorization': `Bearer ${token}` } : {}, cache: 'no-store' });
    } catch {
      throw new ErroApi('Não foi possível falar com o servidor. Confira se a API está ligada.', 0);
    }
    if (!resposta.ok) {
      let mensagem = 'Não foi possível abrir o arquivo.';
      try { mensagem = (await resposta.json()).mensagem || mensagem; } catch { /* resposta sem JSON */ }
      throw new ErroApi(mensagem, resposta.status);
    }
    return resposta.blob();
  },

  /** Verifica (com tempo limite) se a API está no ar. O resultado fica guardado por 1 minuto. */
  async online() {
    const guardado = sessionStorage.getItem('beo_api_online');
    const quando = Number(sessionStorage.getItem('beo_api_quando') || 0);
    if (guardado && Date.now() - quando < 60000) return guardado === 'sim';

    const controle = new AbortController();
    const limite = setTimeout(() => controle.abort(), 2500);
    let ok = false;
    try {
      const r = await fetch(this.base + '/health', { signal: controle.signal });
      ok = r.ok;
    } catch { ok = false; }
    clearTimeout(limite);
    sessionStorage.setItem('beo_api_online', ok ? 'sim' : 'nao');
    sessionStorage.setItem('beo_api_quando', String(Date.now()));
    return ok;
  },
};

/* ------------------------------------------------------------ Texto */
function normalizar(texto) {
  return (texto || '').toString().normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
}
function esc(valor) {
  return String(valor ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

/* ------------------------------------------------------------ Catálogo */
const Catalogo = {
  local: window.CATALOGO_LOCAL || { categorias: [], produtos: [] },

  async categorias() {
    if (await Api.online()) {
      try { return await Api.get('/categorias'); } catch { /* usa a cópia local */ }
    }
    return this.local.categorias.map(c => ({
      ...c, totalProdutos: this.local.produtos.filter(p => p.categoriaSlug === c.slug).length,
    }));
  },

  async produtos(filtro = {}) {
    if (await Api.online()) {
      const q = new URLSearchParams();
      if (filtro.categoria) q.set('categoria', filtro.categoria);
      if (filtro.busca) q.set('busca', filtro.busca);
      if (filtro.farmaciaPopular) q.set('farmaciaPopular', 'true');
      if (filtro.usoContinuo) q.set('usoContinuo', 'true');
      try { return await Api.get('/produtos' + (q.toString() ? `?${q}` : '')); } catch { /* cópia local */ }
    }
    let lista = [...this.local.produtos];
    if (filtro.categoria) lista = lista.filter(p => p.categoriaSlug === filtro.categoria);
    if (filtro.farmaciaPopular) lista = lista.filter(p => p.farmaciaPopular);
    if (filtro.usoContinuo) lista = lista.filter(p => p.usoContinuo);
    if (filtro.busca) {
      const termo = normalizar(filtro.busca);
      lista = lista.filter(p => normalizar(`${p.nome} ${p.principioAtivo} ${p.fabricante} ${p.descricao} ${p.tipo}`).includes(termo));
    }
    return lista.sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'));
  },

  async produto(idOuSlug) {
    if (await Api.online()) {
      try { return await Api.get(`/produtos/${encodeURIComponent(idOuSlug)}`); } catch (e) { if (e.status === 404) return null; }
    }
    return this.local.produtos.find(p => p.slug === idOuSlug || String(p.idMedicamento) === String(idOuSlug)) || null;
  },
};

/* ------------------------------------------------------------ Carrinho */
const Carrinho = {
  itens() { return Armazenamento.ler('beo_carrinho', []); },
  quantidadeTotal() { return this.itens().reduce((s, i) => s + i.quantidade, 0); },
  subtotal() { return this.itens().reduce((s, i) => s + i.preco * i.quantidade, 0); },

  salvar(itens) {
    Armazenamento.gravar('beo_carrinho', itens);
    Interface.atualizarContador();
    this.agendarSincronizacao();
  },

  adicionar(produto, quantidade = 1) {
    const itens = this.itens();
    const existente = itens.find(i => i.idMedicamento === produto.idMedicamento);
    if (existente) existente.quantidade = Math.min(99, existente.quantidade + quantidade);
    else itens.push({
      idMedicamento: produto.idMedicamento, quantidade, nome: produto.nome, preco: Number(produto.preco),
      slug: produto.slug, categoriaSlug: produto.categoriaSlug, tipo: produto.tipo, exigeReceita: !!produto.exigeReceita,
    });
    this.salvar(itens);
  },

  alterar(idMedicamento, quantidade) {
    let itens = this.itens();
    if (quantidade <= 0) itens = itens.filter(i => i.idMedicamento !== idMedicamento);
    else itens.forEach(i => { if (i.idMedicamento === idMedicamento) i.quantidade = Math.min(99, quantidade); });
    this.salvar(itens);
  },

  limpar() { this.salvar([]); },

  /** Envia o carrinho para a API (com atraso, para não mandar a cada clique). */
  agendarSincronizacao() {
    if (!Sessao.logado()) return;
    clearTimeout(this._timer);
    this._timer = setTimeout(() => {
      Api.put('/carrinho', this.itens().map(i => ({ idMedicamento: i.idMedicamento, quantidade: i.quantidade }))).catch(() => {});
    }, 800);
  },

  /** Ao entrar: se o carrinho local está vazio, traz o salvo na conta; senão, o local vale. */
  async aoEntrar() {
    try {
      if (this.itens().length === 0) {
        const servidor = await Api.get('/carrinho');
        if (servidor?.itens?.length) {
          Armazenamento.gravar('beo_carrinho', servidor.itens.map(i => ({
            idMedicamento: i.idMedicamento, quantidade: i.quantidade, nome: i.nome, preco: i.precoUnitario,
            slug: i.slug, exigeReceita: i.exigeReceita,
          })));
        }
      } else {
        await Api.put('/carrinho', this.itens().map(i => ({ idMedicamento: i.idMedicamento, quantidade: i.quantidade })));
      }
    } catch { /* sem problema: o carrinho local continua valendo */ }
  },
};

/* ------------------------------------------------------------ Formatação */
/**
 * Converte o texto de data da API em Date sem erro de fuso horário.
 * "2027-01-01" (só a data) é lido como dia local; sem isso o navegador entende como
 * meia-noite UTC e mostra 31/12/2026 no Brasil (o "ano errado").
 */
function lerData(v) {
  if (!v) return null;
  if (v instanceof Date) return v;
  const soData = /^(\d{4})-(\d{2})-(\d{2})$/.exec(v) || /^(\d{4})-(\d{2})-(\d{2})T00:00:00$/.exec(v);
  if (soData) return new Date(Number(soData[1]), Number(soData[2]) - 1, Number(soData[3]));
  const d = new Date(v);
  return isNaN(d) ? null : d;
}

const Formatar = {
  moeda(v) { return Number(v || 0).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }); },
  data(v) { const d = lerData(v); return d ? d.toLocaleDateString('pt-BR') : '—'; },
  dataHora(v) {
    const d = lerData(v);
    return d ? d.toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }) : '—';
  },
  hora(v) { const d = lerData(v); return d ? d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) : '—'; },
  diaCurto(v) { const d = lerData(v); return d ? d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' }) : ''; },
  status: {
    AGUARDANDO_PAGAMENTO: 'Aguardando pagamento', PAGO: 'Pagamento aprovado', EM_SEPARACAO: 'Em separação',
    SAIU_PARA_ENTREGA: 'Saiu para entrega', ENTREGUE: 'Entregue', CANCELADO: 'Cancelado',
    ATIVA: 'Ativa', PAUSADA: 'Pausada', CANCELADA: 'Cancelada',
    RECEBIDO: 'Recebido', EM_ANALISE: 'Em análise', EM_ROTA: 'A caminho', CONCLUIDO: 'Concluído',
    PENDENTE: 'Pendente', APROVADO: 'Aprovado', ESTORNADO: 'Estornado',
    ABERTA: 'Aberta', RESPONDIDA: 'Respondida', ENCERRADA: 'Encerrada',
  },
  pagamento: { PIX: 'Pix', CARTAO_CREDITO: 'Cartão de crédito', CARTAO_DEBITO: 'Cartão de débito', DINHEIRO: 'Dinheiro na entrega' },
  /** Ouvidoria (RF30): tipos de manifestação e de discriminação. */
  ouvidoria: {
    DISCRIMINACAO: 'Denúncia de discriminação', ATENDIMENTO: 'Reclamação de atendimento', ENTREGA: 'Problema na entrega',
    SUGESTAO: 'Sugestão', ELOGIO: 'Elogio',
  },
  discriminacao: {
    RACIAL: 'Racial (cor, raça ou etnia)', RELIGIOSA: 'Religiosa', GENERO: 'De gênero ou orientação sexual',
    DEFICIENCIA: 'Contra pessoa com deficiência', IDADE: 'Por idade', OUTRA: 'Outra',
  },
  situacao(codigo) { return `<span class="situacao situacao--${esc(codigo)}">${esc(this.status[codigo] || codigo)}</span>`; },
  parcelas(preco) {
    const n = preco >= 60 ? 3 : preco >= 30 ? 2 : 1;
    return n === 1 ? 'à vista' : `ou ${n}x de ${this.moeda(preco / n)} sem juros`;
  },
};

/* ------------------------------------------------------------ Interface */
const ICONES = {
  busca: '<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>',
  pessoa: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true"><circle cx="12" cy="8" r="4"/><path d="M4 21c1.5-4 4.5-6 8-6s6.5 2 8 6"/></svg>',
  sacola: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M5 8h14l-1 12H6L5 8Z"/><path d="M9 8V6a3 3 0 0 1 6 0v2"/></svg>',
  painel: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><rect x="3" y="3" width="7" height="9" rx="1"/><rect x="14" y="3" width="7" height="5" rx="1"/><rect x="14" y="12" width="7" height="9" rx="1"/><rect x="3" y="16" width="7" height="5" rx="1"/></svg>',
};

const svgLinha = (corpo, tamanho = 28) =>
  `<svg width="${tamanho}" height="${tamanho}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${corpo}</svg>`;

/** Ícones de linha (sem emoji) para categorias, vantagens e avisos. */
const ICONES_LINHA = {
  'genericos': '<path d="M10.5 3.5a5 5 0 0 1 7 7l-7 7a5 5 0 0 1-7-7z"/><path d="M7 7l7 7"/>',
  'referencia': '<path d="M3 12V4h8l10 10-8 8z"/><circle cx="7.5" cy="7.5" r="1.5"/>',
  'uso-continuo': '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/><path d="M9 15l2 2 4-4"/>',
  'vitaminas': '<path d="M9 3h6v3H9z"/><rect x="7" y="6" width="10" height="15" rx="2"/><path d="M7 11h10M7 16h10"/>',
  'higiene': '<path d="M12 3s6 6.5 6 11a6 6 0 0 1-12 0c0-4.5 6-11 6-11z"/>',
  'dermocosmeticos': '<path d="M9 3h6l-1 4h-4z"/><path d="M9 7h6l1 14H8z"/><path d="M9.5 12h5"/>',
  'mamae-bebe': '<path d="M10 2h4v3h-4z"/><path d="M8 5h8l-1 3v11a2 2 0 0 1-2 2h-2a2 2 0 0 1-2-2V8z"/><path d="M9 12h6"/>',
  'primeiros-socorros': '<rect x="3" y="6" width="18" height="14" rx="2"/><path d="M9 6V4h6v2M12 10v6M9 13h6"/>',
  'aparelhos': '<path d="M6 3v6a4 4 0 0 0 8 0V3"/><path d="M10 13v3a4 4 0 0 0 8 0v-2"/><circle cx="18" cy="12" r="2"/>',
  entrega: '<path d="M3 7h11v9H3z"/><path d="M14 10h4l3 3v3h-7"/><circle cx="7" cy="17.5" r="1.8"/><circle cx="17" cy="17.5" r="1.8"/>',
  desconto: '<circle cx="8" cy="8" r="2"/><circle cx="16" cy="16" r="2"/><path d="M18 6 6 18"/>',
  cartao: '<rect x="3" y="5" width="18" height="14" rx="2"/><path d="M3 10h18M7 15h4"/>',
  conversa: '<path d="M4 5h16v11H9l-5 4z"/><path d="M8 9h8M8 12h5"/>',
  receita: '<path d="M6 3h9l4 4v14H6z"/><path d="M14 3v5h5M9 12h7M9 16h5"/>',
  mapa: '<path d="M12 21s-6-5.6-6-10a6 6 0 0 1 12 0c0 4.4-6 10-6 10z"/><circle cx="12" cy="11" r="2.2"/>',
  brasil: '<path d="M12 3l9 9-9 9-9-9z"/><circle cx="12" cy="12" r="3"/>',
  check: '<path d="M5 12l4 4 10-10"/>',
};
const icone = (nome, tamanho) => svgLinha(ICONES_LINHA[nome] || ICONES_LINHA.genericos, tamanho);

const Interface = {
  paginaAtual() { return location.pathname.split('/').pop() || 'index.html'; },

  montarCabecalho() {
    const alvo = document.getElementById('cabecalho');
    if (!alvo) return;
    const usuario = Sessao.usuario();
    const categorias = Catalogo.local.categorias;
    const params = new URLSearchParams(location.search);
    const catAtual = this.paginaAtual() === 'medicamentos.html' ? params.get('categoria') : null;

    const conta = usuario
      ? `<a class="atalho" href="conta.html">${ICONES.pessoa}<span class="rotulo-atalho">Olá, ${esc(usuario.nomeExibicao.split(' ')[0])}</span></a>
         ${Sessao.ehEquipe() ? `<a class="atalho" href="admin.html">${ICONES.painel}<span class="rotulo-atalho">Painel</span></a>` : ''}`
      : `<a class="atalho" href="login.html">${ICONES.pessoa}<span class="rotulo-atalho">Entrar</span></a>`;

    alvo.innerHTML = `
      <a class="pular-conteudo" href="#conteudo">Pular para o conteúdo</a>
      <div class="faixa-topo">
        <div class="conteiner">
          <p>Entrega em até 2 horas em Cajamar e região. Frete grátis acima de R$ 99.</p>
          <div class="acessibilidade" role="group" aria-label="Acessibilidade">
            <span>Letra</span>
            <button type="button" data-fonte="normal" aria-label="Letra normal">A</button>
            <button type="button" data-fonte="grande" aria-label="Letra grande">A+</button>
            <button type="button" data-fonte="maior" aria-label="Letra maior">A++</button>
            <button type="button" data-contraste aria-label="Alto contraste">Contraste</button>
            <button type="button" data-tema aria-label="Tema escuro">Escuro</button>
          </div>
        </div>
      </div>
      <header class="cabecalho">
        <div class="conteiner">
          <a class="marca" href="index.html"><img src="img/logo.png" alt="Bem Estar Online, página inicial" width="163" height="46"></a>
          <form class="busca" role="search" action="medicamentos.html">
            <label for="busca-topo" class="sr-only">Buscar produtos</label>
            <input id="busca-topo" name="busca" type="search" autocomplete="off" placeholder="Busque por nome, sintoma ou princípio ativo"
                   value="${esc(params.get('busca') || '')}" aria-controls="sugestoes-busca" aria-expanded="false">
            <button type="submit" aria-label="Buscar">${ICONES.busca}</button>
            <ul id="sugestoes-busca" class="sugestoes oculto" role="listbox" aria-label="Sugestões"></ul>
          </form>
          <nav class="atalhos" aria-label="Sua conta">
            ${conta}
            <a class="atalho" href="carrinho.html" aria-label="Carrinho">${ICONES.sacola}<span class="rotulo-atalho">Carrinho</span>
              <span class="contador" id="contador-carrinho" aria-live="polite">0</span></a>
          </nav>
        </div>
        <nav class="menu-categorias" aria-label="Categorias">
          <div class="conteiner">
            <ul>
              <li><a href="medicamentos.html" ${this.paginaAtual() === 'medicamentos.html' && !catAtual && !params.get('busca') ? 'aria-current="page"' : ''}>Todos os produtos</a></li>
              ${categorias.map(c => `<li><a href="medicamentos.html?categoria=${c.slug}" ${catAtual === c.slug ? 'aria-current="page"' : ''}>${esc(c.nome)}</a></li>`).join('')}
              <li><a href="assinatura.html" ${this.paginaAtual() === 'assinatura.html' ? 'aria-current="page"' : ''}>Assinatura</a></li>
              <li><a href="delivery.html" ${this.paginaAtual() === 'delivery.html' ? 'aria-current="page"' : ''}>Enviar receita</a></li>
            </ul>
          </div>
        </nav>
      </header>
      <div id="aviso-vitrine" class="aviso-vitrine oculto" role="status"><div class="conteiner">
        <p><strong>Modo vitrine:</strong> a API está desligada. Dá para navegar e montar o carrinho; para entrar e comprar, ligue a API (veja o GUIA-SIMPLES-DE-EXECUCAO.txt).</p>
      </div></div>`;

    alvo.querySelectorAll('[data-fonte]').forEach(b => b.addEventListener('click', () => Acessibilidade.alterar('fonte', b.dataset.fonte)));
    alvo.querySelector('[data-contraste]').addEventListener('click', () =>
      Acessibilidade.alterar('contraste', Acessibilidade.preferencias().contraste === 'alto' ? 'normal' : 'alto'));
    alvo.querySelector('[data-tema]').addEventListener('click', () =>
      Acessibilidade.alterar('tema', Acessibilidade.temaEscuroAtivo() ? 'claro' : 'escuro'));
    this.atualizarBotoesAcessibilidade();
    this.atualizarContador();
    this.ligarBusca();
  },

  atualizarBotoesAcessibilidade() {
    const p = Acessibilidade.preferencias();
    document.querySelectorAll('.acessibilidade [data-fonte]').forEach(b => b.setAttribute('aria-pressed', String(b.dataset.fonte === p.fonte)));
    const contraste = document.querySelector('.acessibilidade [data-contraste]');
    if (contraste) contraste.setAttribute('aria-pressed', String(p.contraste === 'alto'));
    const tema = document.querySelector('.acessibilidade [data-tema]');
    if (tema) tema.setAttribute('aria-pressed', String(Acessibilidade.temaEscuroAtivo()));
  },

  atualizarContador() {
    const el = document.getElementById('contador-carrinho');
    if (!el) return;
    const n = Carrinho.quantidadeTotal();
    el.textContent = n;
    el.dataset.zero = String(n === 0);
    el.setAttribute('aria-label', `${n} ${n === 1 ? 'item' : 'itens'} no carrinho`);
  },

  ligarBusca() {
    const campo = document.getElementById('busca-topo');
    const lista = document.getElementById('sugestoes-busca');
    if (!campo || !lista) return;
    let timer;
    const fechar = () => { lista.classList.add('oculto'); campo.setAttribute('aria-expanded', 'false'); };
    campo.addEventListener('input', () => {
      clearTimeout(timer);
      const termo = campo.value.trim();
      if (termo.length < 2) { fechar(); return; }
      timer = setTimeout(async () => {
        const achados = (await Catalogo.produtos({ busca: termo })).slice(0, 6);
        if (!achados.length) {
          lista.innerHTML = `<li><a href="delivery.html">Não achou? Envie sua receita e nós procuramos <small>Delivery</small></a></li>`;
        } else {
          lista.innerHTML = achados.map(p =>
            `<li role="option"><a href="produto.html?p=${encodeURIComponent(p.slug)}">${esc(p.nome)} <small>${Formatar.moeda(p.preco)}</small></a></li>`).join('');
        }
        lista.classList.remove('oculto');
        campo.setAttribute('aria-expanded', 'true');
      }, 200);
    });
    campo.addEventListener('keydown', e => { if (e.key === 'Escape') fechar(); });
    document.addEventListener('click', e => { if (!e.target.closest('.busca')) fechar(); });
  },

  montarRodape() {
    const alvo = document.getElementById('rodape');
    if (!alvo) return;
    alvo.innerHTML = `
      <footer class="rodape">
        <div class="conteiner">
          <div>
            <h2>Bem Estar Online</h2>
            <p>Farmácia de bairro, agora também no celular. Atendimento humano, entrega rápida e preço justo.</p>
            <p>Rua das Flores, 120, Centro, Cajamar/SP</p>
          </div>
          <div>
            <h2>Atendimento</h2>
            <ul>
              <li>(11) 99711-2773 (WhatsApp)</li>
              <li><a href="mailto:contato@bemestaronline.com.br">contato@bemestaronline.com.br</a></li>
              <li>Segunda a sábado, 8h às 22h</li>
              <li>Domingos e feriados, 9h às 18h</li>
              <li><a href="ouvidoria.html">Ouvidoria e denúncias</a></li>
            </ul>
          </div>
          <div>
            <h2>Serviços</h2>
            <ul>
              <li><a href="assinatura.html">Assinatura de uso contínuo</a></li>
              <li><a href="delivery.html">Enviar receita</a></li>
              <li><a href="medicamentos.html?farmaciaPopular=true">Farmácia Popular</a></li>
              <li><a href="conta.html">Acompanhar pedido</a></li>
            </ul>
          </div>
          <div>
            <h2>Para você</h2>
            <ul>
              <li><a href="medicamentos.html?categoria=uso-continuo">Remédios de uso contínuo</a></li>
              <li><a href="medicamentos.html?categoria=aparelhos">Aparelhos de saúde</a></li>
              <li><a href="cadastro.html">Criar conta</a></li>
            </ul>
          </div>
        </div>
        <div class="legal"><div class="conteiner">
          Projeto acadêmico (PIM IV, UNIP). Empresa e produtos fictícios. Seus dados são usados apenas para entregar seus pedidos, conforme a LGPD (Lei 13.709/2018).
          <a href="privacidade.html">Política de Privacidade</a> · <a href="termos.html">Termos de Uso</a>
          Medicamentos podem causar efeitos indesejados. Em caso de dúvida, fale com o farmacêutico.
        </div></div>
      </footer>
      <div id="toast" class="toast" role="status" aria-live="polite"></div>`;
  },

  aviso(mensagem, tipo = 'info') {
    const t = document.getElementById('toast');
    if (!t) { alert(mensagem); return; }
    t.textContent = mensagem;
    t.className = `toast visivel ${tipo === 'erro' ? 'toast--erro' : tipo === 'sucesso' ? 'toast--sucesso' : ''}`;
    clearTimeout(this._toast);
    this._toast = setTimeout(() => t.classList.remove('visivel'), 3800);
  },

  /**
   * Foto do produto. Ordem de busca:
   *   1) foto enviada pelo painel da farmácia (imagemUrl, guardada na API);
   *   2) arquivo na pasta do site: img/produtos/<slug>.jpg, .png ou .webp;
   *   3) se nada existir, um desenho da embalagem gerado aqui mesmo.
   */
  foto(p, tamanho = 'normal') {
    const fontes = [];
    if (p.imagemUrl) fontes.push(/^https?:\/\//.test(p.imagemUrl) ? p.imagemUrl : Api.origem + p.imagemUrl);
    if (p.slug) ['jpg', 'png', 'webp'].forEach(ext => fontes.push(`img/produtos/${p.slug}.${ext}`));
    const classe = tamanho === 'grande' ? 'foto-produto foto-produto--grande' : tamanho === 'mini' ? 'foto-produto foto-produto--mini' : 'foto-produto';
    const etiqueta = p.farmaciaPopular && tamanho !== 'mini' ? '<span class="etiqueta-foto">Farmácia Popular</span>' : '';
    if (!fontes.length) return `<span class="${classe}">${this.embalagem(p)}${etiqueta}</span>`;
    const [primeira, ...resto] = fontes;
    return `<span class="${classe}">${etiqueta}<img src="${esc(primeira)}" alt="" loading="lazy" data-slug="${esc(p.slug || '')}"
      data-fontes="${esc(resto.join('|'))}" onerror="Interface.proximaFoto(this)"></span>`;
  },

  /** Chamado quando uma foto não existe: tenta a próxima; no fim, desenha a embalagem. */
  proximaFoto(img) {
    const resto = (img.dataset.fontes || '').split('|').filter(Boolean);
    if (resto.length) { img.dataset.fontes = resto.slice(1).join('|'); img.src = resto[0]; return; }
    const p = this._produtosPorSlug?.[img.dataset.slug] || { nome: '', slug: img.dataset.slug };
    img.outerHTML = this.embalagem(p);
  },

  lembrarProdutos(lista) {
    this._produtosPorSlug = this._produtosPorSlug || {};
    (lista || []).forEach(p => { if (p?.slug) this._produtosPorSlug[p.slug] = p; });
  },

  /** Desenho da embalagem em SVG (caixa, frasco, bisnaga, pacote ou aparelho), usado quando não há foto. */
  embalagem(p) {
    const cores = {
      'genericos': '#1F5FAD', 'referencia': '#7A3E9D', 'uso-continuo': '#00807C', 'vitaminas': '#E07B1F',
      'higiene': '#3B8FCB', 'dermocosmeticos': '#C9567A', 'mamae-bebe': '#5AA9E6', 'primeiros-socorros': '#C8281E', 'aparelhos': '#55626F',
    };
    const cor = cores[p.categoriaSlug] || '#00807C';
    const nome = (p.nome || '').replace(/\s+\d.*$/, '') || 'Produto';
    const dose = p.dosagem && p.dosagem.length <= 12 ? p.dosagem : '';
    const linhas = [];
    nome.split(' ').forEach(palavra => {
      const atual = linhas[linhas.length - 1];
      if (atual && (atual + ' ' + palavra).length <= 13) linhas[linhas.length - 1] = atual + ' ' + palavra;
      else linhas.push(palavra);
    });
    const texto = (x, y, tam, peso = 700, fill = '#12324A') => linhas.slice(0, 3).map((l, i) =>
      `<text x="${x}" y="${y + i * (tam + 2)}" font-size="${tam}" font-weight="${peso}" fill="${fill}" text-anchor="middle" font-family="Verdana, sans-serif">${esc(l)}</text>`).join('');
    const sombra = '<ellipse cx="100" cy="172" rx="62" ry="7" fill="#0D3550" opacity=".10"/>';
    const apres = p.apresentacao || '';
    let forma;

    if (/frasco|spray/i.test(apres)) {
      forma = `<rect x="78" y="22" width="44" height="26" rx="4" fill="${cor}"/>
        <rect x="62" y="46" width="76" height="122" rx="16" fill="#FFFFFF" stroke="#D3DEE0" stroke-width="2"/>
        <rect x="62" y="78" width="76" height="62" fill="${cor}" opacity=".12"/>
        <rect x="128" y="54" width="5" height="100" rx="2.5" fill="#FFFFFF" opacity=".9"/>
        ${texto(100, 98, 10)}${dose ? `<text x="100" y="134" font-size="${dose.length > 8 ? 8 : 11}" fill="${cor}" font-weight="700" text-anchor="middle" font-family="Verdana, sans-serif">${esc(dose)}</text>` : ''}`;
    } else if (/bisnaga|tubo/i.test(apres)) {
      forma = `<path d="M30 74 L150 64 Q160 64 160 74 L160 126 Q160 136 150 136 L30 126 Z" fill="#FFFFFF" stroke="#D3DEE0" stroke-width="2"/>
        <rect x="18" y="72" width="14" height="56" rx="2" fill="#D3DEE0"/>
        <rect x="160" y="84" width="24" height="32" rx="4" fill="${cor}"/>
        <path d="M44 74 L150 66 L150 80 L44 86 Z" fill="${cor}"/>
        ${texto(98, 104, 10)}`;
    } else if (/pacote/i.test(apres)) {
      forma = `<path d="M40 44 Q100 30 160 44 L166 160 Q100 172 34 160 Z" fill="${cor}"/>
        <path d="M58 66 Q100 58 142 66 L144 128 Q100 136 56 128 Z" fill="#FFFFFF" opacity=".95"/>
        ${texto(100, 92, 11)}`;
    } else if (p.categoriaSlug === 'aparelhos' && !/tira/i.test(apres + p.nome)) {
      forma = `<rect x="52" y="30" width="96" height="136" rx="22" fill="#F2F5F6" stroke="#B9C6CB" stroke-width="2"/>
        <rect x="66" y="48" width="68" height="44" rx="6" fill="#20343F"/>
        <text x="100" y="78" font-size="18" fill="#7FE0D8" text-anchor="middle" font-family="Verdana, sans-serif" font-weight="700">36,5</text>
        <circle cx="100" cy="124" r="16" fill="${cor}"/>
        ${texto(100, 156, 8, 700, '#55626F')}`;
    } else {
      const generico = p.tipo === 'Genérico';
      forma = `<polygon points="150,46 168,34 168,148 150,160" fill="#E4EBED"/>
        <polygon points="40,46 58,34 168,34 150,46" fill="#F1F5F6"/>
        <rect x="40" y="46" width="110" height="114" fill="#FFFFFF" stroke="#D3DEE0" stroke-width="1.5"/>
        <rect x="40" y="46" width="110" height="24" fill="${cor}"/>
        ${texto(95, 90, 11)}
        ${dose ? `<text x="95" y="${linhas.length > 1 ? 128 : 116}" font-size="15" font-weight="700" fill="${cor}" text-anchor="middle" font-family="Verdana, sans-serif">${esc(dose)}</text>` : ''}
        ${generico ? `<rect x="40" y="134" width="110" height="12" fill="#F5C400"/><rect x="132" y="128" width="16" height="16" fill="#1F3E8C"/><text x="140" y="140" font-size="12" font-weight="700" fill="#F5C400" text-anchor="middle" font-family="Verdana, sans-serif">G</text>` : ''}
        ${p.exigeReceita ? `<rect x="40" y="148" width="110" height="12" fill="#C8281E"/><text x="95" y="157" font-size="6.5" font-weight="700" fill="#FFFFFF" text-anchor="middle" font-family="Verdana, sans-serif">VENDA SOB PRESCRIÇÃO MÉDICA</text>` : ''}`;
    }
    return `<svg viewBox="0 0 200 180" role="img" aria-label="Embalagem de ${esc(p.nome || 'produto')}">${sombra}${forma}</svg>`;
  },

  selos(p, completo = true) {
    const s = [];
    if (p.exigeReceita) s.push('<span class="selo-produto selo-produto--receita">Exige receita</span>');
    if (completo && p.farmaciaPopular) s.push('<span class="selo-produto selo-produto--popular">Farmácia Popular</span>');
    if (completo && p.usoContinuo) s.push('<span class="selo-produto selo-produto--continuo">Uso contínuo</span>');
    if (p.disponivel === false) s.push('<span class="selo-produto selo-produto--esgotado">Esgotado</span>');
    return s.length ? `<div class="selos">${s.join('')}</div>` : '';
  },

  cartaoProduto(p) {
    const link = `produto.html?p=${encodeURIComponent(p.slug)}`;
    return `
      <li class="produto">
        <a href="${link}" tabindex="-1" aria-hidden="true" style="display:contents">${this.foto(p)}</a>
        <div class="produto-corpo">
          <h3><a href="${link}">${esc(p.nome)}</a></h3>
          <p class="apresentacao">${esc(p.apresentacao || '')}${p.fabricante ? `, ${esc(p.fabricante)}` : ''}</p>
          ${this.selos(p, false)}
          <div class="rodape-produto">
            <div class="preco">${Formatar.moeda(p.preco)}<small>${Formatar.parcelas(p.preco)}</small></div>
            <button class="botao botao--pequeno" type="button" data-adicionar="${p.idMedicamento}" ${p.disponivel === false ? 'disabled' : ''}
              aria-label="Adicionar ${esc(p.nome)} ao carrinho">Adicionar</button>
          </div>
        </div>
      </li>`;
  },

  /** Liga os botões "Adicionar" de uma lista de produtos já desenhada. */
  ligarAdicionar(container, produtos) {
    this.lembrarProdutos(produtos);
    container.querySelectorAll('[data-adicionar]').forEach(botao => {
      botao.addEventListener('click', () => {
        const p = produtos.find(x => String(x.idMedicamento) === botao.dataset.adicionar);
        if (!p) return;
        Carrinho.adicionar(p, 1);
        this.aviso(`${p.nome} foi para o carrinho.`, 'sucesso');
        botao.textContent = 'Adicionado';
        setTimeout(() => { botao.textContent = 'Adicionar'; }, 1500);
      });
    });
  },

  /** Mostra a mensagem de erro de um campo de formulário. */
  erroCampo(campo, mensagem) {
    if (!campo) return;
    campo.setAttribute('aria-invalid', mensagem ? 'true' : 'false');
    let el = campo.parentElement.querySelector('.erro-campo');
    if (!mensagem) { el?.remove(); return; }
    if (!el) {
      el = document.createElement('span');
      el.className = 'erro-campo';
      el.id = `${campo.id}-erro`;
      campo.setAttribute('aria-describedby', el.id);
      campo.insertAdjacentElement('afterend', el);
    }
    el.textContent = mensagem;
  },

  /** Linha do tempo do pedido (rastreamento): etapas feitas + próximas etapas. */
  linhaDoTempo(pedido) {
    const nomes = {
      AGUARDANDO_PAGAMENTO: 'Pedido recebido, pagamento na entrega', PAGO: 'Pagamento aprovado',
      EM_SEPARACAO: 'Separando seus produtos', SAIU_PARA_ENTREGA: 'Saiu para entrega',
      ENTREGUE: 'Entregue', CANCELADO: 'Pedido cancelado',
    };
    const historico = pedido.historico || [];
    const linhas = historico.map((h, i) => {
      const ultimo = i === historico.length - 1;
      const classe = h.statusNovo === 'CANCELADO' ? 'cancelado' : (ultimo && h.statusNovo !== 'ENTREGUE' ? 'atual' : 'feito');
      return `<li class="${classe}"><strong>${esc(nomes[h.statusNovo] || h.statusNovo)}</strong><small>${Formatar.dataHora(h.alteradoEm)}</small></li>`;
    });
    if (pedido.status !== 'CANCELADO' && pedido.status !== 'ENTREGUE') {
      const pagoNaEntrega = pedido.pagamento?.metodo === 'DINHEIRO';
      const fluxo = pagoNaEntrega ? ['EM_SEPARACAO', 'SAIU_PARA_ENTREGA', 'ENTREGUE'] : ['PAGO', 'EM_SEPARACAO', 'SAIU_PARA_ENTREGA', 'ENTREGUE'];
      const feitos = historico.map(h => h.statusNovo);
      const posicao = fluxo.indexOf(pedido.status);
      fluxo.filter((s, i) => i > posicao && !feitos.includes(s))
           .forEach(s => linhas.push(`<li><strong>${esc(nomes[s])}</strong><small>Próxima etapa</small></li>`));
    }
    return `<ol class="linha-tempo">${linhas.join('')}</ol>`;
  },

  /** Itens, valores, endereço e pagamento de um pedido. */
  resumoPedido(pedido) {
    return `
      <div class="tabela-rolagem" style="margin:.5rem 0 1rem">
        <table>
          <thead><tr><th>Produto</th><th class="numero">Qtd.</th><th class="numero">Preço</th><th class="numero">Subtotal</th></tr></thead>
          <tbody>${pedido.itens.map(i => `<tr><td>${esc(i.nomeProduto)}</td><td class="numero">${i.quantidade}</td>
            <td class="numero">${Formatar.moeda(i.precoUnitario)}</td><td class="numero">${Formatar.moeda(i.subtotal)}</td></tr>`).join('')}</tbody>
        </table>
      </div>
      <div class="linha-valor"><span>Produtos</span><span>${Formatar.moeda(pedido.subtotal)}</span></div>
      ${pedido.desconto > 0 ? `<div class="linha-valor"><span>Desconto</span><span class="desconto">− ${Formatar.moeda(pedido.desconto)}</span></div>` : ''}
      <div class="linha-valor"><span>Entrega</span><span>${pedido.frete === 0 ? 'Grátis' : Formatar.moeda(pedido.frete)}</span></div>
      <div class="linha-valor total"><span>Total</span><span>${Formatar.moeda(pedido.total)}</span></div>
      <p style="margin-top:1rem"><strong>Entrega:</strong> ${esc(pedido.endereco?.textoCompleto || '—')}</p>
      <p><strong>Pagamento:</strong> ${esc(Formatar.pagamento[pedido.pagamento?.metodo] || '—')}
        ${pedido.pagamento ? `(${esc((Formatar.status[pedido.pagamento.status] || pedido.pagamento.status).toLowerCase())})` : ''}</p>
      ${pedido.entregador ? `<p><strong>Entregador:</strong> ${esc(pedido.entregador)}</p>` : ''}
      ${pedido.requerReceita ? `<p class="estoque-baixo">${pedido.receitaConferida ? 'Receita conferida na entrega.' : 'Este pedido tem remédio que exige receita: apresente ao entregador.'}</p>` : ''}
      ${pedido.observacao ? `<p><strong>Observação:</strong> ${esc(pedido.observacao)}</p>` : ''}`;
  },

  carregando(alvo, texto = 'Carregando...') {
    if (alvo) alvo.innerHTML = `<p class="carregando" role="status">${esc(texto)}</p>`;
  },
};

/* ------------------------------------------------------------ Máscaras de digitação */
const Mascara = {
  cpf: v => v.replace(/\D/g, '').slice(0, 11).replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d{1,2})$/, '$1-$2'),
  telefone: v => v.replace(/\D/g, '').slice(0, 11).replace(/^(\d{2})(\d)/, '($1) $2').replace(/(\d{5})(\d{1,4})$/, '$1-$2'),
  cep: v => v.replace(/\D/g, '').slice(0, 8).replace(/(\d{5})(\d)/, '$1-$2'),
  aplicar(campo, tipo) { if (campo) campo.addEventListener('input', () => { campo.value = this[tipo](campo.value); }); },
};

/** Busca o endereço pelo CEP (serviço público ViaCEP) e preenche o formulário. */
async function preencherPorCep(cep, campos) {
  const numeros = cep.replace(/\D/g, '');
  if (numeros.length !== 8) return false;
  try {
    const r = await fetch(`https://viacep.com.br/ws/${numeros}/json/`);
    const d = await r.json();
    if (d.erro) return false;
    if (campos.logradouro && !campos.logradouro.value) campos.logradouro.value = d.logradouro || '';
    if (campos.bairro) campos.bairro.value = d.bairro || campos.bairro.value;
    if (campos.cidade) campos.cidade.value = d.localidade || campos.cidade.value;
    if (campos.estado) campos.estado.value = d.uf || campos.estado.value;
    return true;
  } catch { return false; }
}

/* ------------------------------------------------------------ Mapa e rastreamento da entrega */
// Mapa com Leaflet + OpenStreetMap (gratuito, sem chave). Se não carregar (sem internet),
// mostramos só a distância e a hora da última posição.
const Mapa = {
  async carregar() {
    if (window.L) return true;
    if (!this._carregando) {
      this._carregando = new Promise(resolve => {
        const css = document.createElement('link');
        css.rel = 'stylesheet';
        css.href = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.css';
        document.head.appendChild(css);
        const js = document.createElement('script');
        js.src = 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/leaflet.min.js';
        js.onload = () => resolve(true);
        js.onerror = () => resolve(false);
        document.head.appendChild(js);
        setTimeout(() => resolve(!!window.L), 7000);
      });
    }
    return this._carregando;
  },

  distanciaKm(a1, o1, a2, o2) {
    const r = 6371, rad = x => x * Math.PI / 180;
    const d = Math.sin(rad(a2 - a1) / 2) ** 2 + Math.cos(rad(a1)) * Math.cos(rad(a2)) * Math.sin(rad(o2 - o1) / 2) ** 2;
    return 2 * r * Math.asin(Math.sqrt(d));
  },

  /** Desenha (ou atualiza) o mapa com loja, casa do cliente e entregador. */
  async desenhar(alvo, r) {
    if (!(await this.carregar())) { alvo.hidden = true; return; }
    alvo.hidden = false;
    const L = window.L;
    if (!alvo._mapa) {
      alvo._mapa = L.map(alvo, { scrollWheelZoom: false });
      L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19, attribution: '&copy; OpenStreetMap',
      }).addTo(alvo._mapa);
      alvo._camadas = L.layerGroup().addTo(alvo._mapa);
    }
    const mapa = alvo._mapa;
    alvo._camadas.clearLayers();
    const pontos = [];
    const marcar = (lat, lng, cor, raio, texto) => {
      L.circleMarker([lat, lng], { radius: raio, color: '#FFFFFF', weight: 3, fillColor: cor, fillOpacity: 1 })
        .bindTooltip(texto, { permanent: false }).addTo(alvo._camadas);
      pontos.push([lat, lng]);
    };
    marcar(r.lojaLatitude, r.lojaLongitude, '#00807C', 8, 'Farmácia Bem Estar');
    if (r.destinoLatitude != null) marcar(r.destinoLatitude, r.destinoLongitude, '#0A5FA8', 8, 'Endereço de entrega');
    if (r.entregadorLatitude != null) {
      marcar(r.entregadorLatitude, r.entregadorLongitude, '#C8281E', 11, `Entregador${r.entregador ? ': ' + r.entregador : ''}`);
      if (r.destinoLatitude != null)
        L.polyline([[r.entregadorLatitude, r.entregadorLongitude], [r.destinoLatitude, r.destinoLongitude]],
          { color: '#0A5FA8', weight: 3, dashArray: '6 8' }).addTo(alvo._camadas);
    }
    if (!alvo._enquadrado) { mapa.fitBounds(pontos, { padding: [30, 30], maxZoom: 15 }); alvo._enquadrado = true; }
    setTimeout(() => mapa.invalidateSize(), 50);
  },
};

const Rastreio = {
  /**
   * Mostra o mapa e atualiza a cada 8 segundos enquanto o pedido estiver "Saiu para entrega".
   * Para sozinho quando o mapa sai da tela ou o pedido é entregue.
   */
  acompanhar(idPedido, container, aoMudarStatus) {
    container.innerHTML = `<p class="mapa-legenda" role="status">Carregando a posição do entregador...</p><div class="mapa-entrega" hidden></div>`;
    const legenda = container.querySelector('.mapa-legenda');
    const alvoMapa = container.querySelector('.mapa-entrega');
    let statusInicial = null;
    const atualizar = async () => {
      if (!document.body.contains(container)) return clearInterval(timer);
      try {
        const r = await Api.get(`/pedidos/${idPedido}/rastreio`);
        statusInicial ??= r.status;
        if (r.status !== statusInicial) { clearInterval(timer); aoMudarStatus?.(r.status); return; }
        if (r.status !== 'SAIU_PARA_ENTREGA') { clearInterval(timer); container.innerHTML = ''; return; }
        let texto = `${r.entregador ? esc(r.entregador) + ' está' : 'O entregador está'} a caminho.`;
        if (r.entregadorLatitude != null && r.destinoLatitude != null) {
          const km = Mapa.distanciaKm(r.entregadorLatitude, r.entregadorLongitude, r.destinoLatitude, r.destinoLongitude);
          texto += ` Faltam cerca de ${km < 1 ? Math.round(km * 1000) + ' m' : km.toFixed(1).replace('.', ',') + ' km'}`;
          texto += `, previsão de ${Math.max(2, Math.round(km / 0.4))} min.`;
        }
        if (r.localizacaoEm) texto += ` Posição atualizada às ${Formatar.hora(r.localizacaoEm)}.`;
        legenda.innerHTML = texto;
        await Mapa.desenhar(alvoMapa, r);
      } catch (e) { legenda.textContent = e.message; }
    };
    const timer = setInterval(atualizar, 8000);
    atualizar();
  },
};

/* ------------------------------------------------------------ VLibras (RF40) */
// Tradutor de Libras oficial do governo federal (vlibras.gov.br): o botão fica no canto direito de todas as páginas.
// O script é pequeno; o avatar (Ícaro, Hosana ou Guga) só é baixado quando a pessoa clica. Precisa de internet:
// se não carregar, o site segue normal, sem o botão.
const TradutorLibras = {
  endereco: 'https://vlibras.gov.br/app',
  carregar() {
    if (window.VLibras || document.getElementById('script-vlibras')) return;
    const script = document.createElement('script');
    script.id = 'script-vlibras';
    script.src = `${this.endereco}/vlibras-plugin.js`;
    script.async = true;
    script.onload = () => { try { new window.VLibras.Widget(this.endereco); } catch (e) { console.warn('VLibras não iniciou.', e); } };
    script.onerror = () => console.info('VLibras indisponível (sem internet?). O site segue sem o tradutor de Libras.');
    document.body.appendChild(script);
  },
};

/* ------------------------------------------------------------ Localização (endereços) */
const Geo = {
  /** Posição atual pelo GPS do navegador (pede permissão). */
  minhaPosicao() {
    return new Promise(resolve => {
      if (!navigator.geolocation) return resolve(null);
      navigator.geolocation.getCurrentPosition(
        p => resolve({ latitude: p.coords.latitude, longitude: p.coords.longitude }),
        () => resolve(null), { enableHighAccuracy: true, timeout: 8000 });
    });
  },
  /** Descobre as coordenadas pelo endereço digitado (OpenStreetMap/Nominatim). Se falhar, segue sem. */
  async doEndereco(e) {
    const controle = new AbortController();
    const limite = setTimeout(() => controle.abort(), 4000);
    try {
      const q = new URLSearchParams({ format: 'json', limit: '1', country: 'Brasil', street: `${e.numero} ${e.logradouro}`, city: e.cidade, state: e.estado });
      const r = await fetch(`https://nominatim.openstreetmap.org/search?${q}`, { signal: controle.signal });
      const d = await r.json();
      return d[0] ? { latitude: Number(d[0].lat), longitude: Number(d[0].lon) } : null;
    } catch { return null; }
    finally { clearTimeout(limite); }
  },
};

/** Campo de data digitado como DD/MM/AAAA (evita o calendário do navegador que deixa o ano com 6 dígitos). */
const CampoData = {
  ligar(campo) {
    if (!campo) return;
    campo.addEventListener('input', () => {
      campo.value = campo.value.replace(/\D/g, '').slice(0, 8)
        .replace(/^(\d{2})(\d)/, '$1/$2').replace(/^(\d{2})\/(\d{2})(\d)/, '$1/$2/$3');
    });
  },
  /** "20/05/1998" -> "1998-05-20"; devolve null se a data não existir ou estiver fora do intervalo. */
  paraIso(texto, { anoMinimo = 1900, permitirFuturo = false } = {}) {
    const m = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(texto || '');
    if (!m) return null;
    const [dia, mes, ano] = [Number(m[1]), Number(m[2]), Number(m[3])];
    const d = new Date(ano, mes - 1, dia);
    if (d.getFullYear() !== ano || d.getMonth() !== mes - 1 || d.getDate() !== dia) return null;
    if (ano < anoMinimo || (!permitirFuturo && d > new Date())) return null;
    return `${m[3]}-${m[2]}-${m[1]}`;
  },
};

/* ------------------------------------------------------------ Início */
document.addEventListener('DOMContentLoaded', async () => {
  // Senha provisória dada pelo administrador (RF35): antes de usar o site, a pessoa cria a própria senha.
  if (Sessao.usuario()?.trocarSenha && Interface.paginaAtual() !== 'alterar-senha.html') {
    location.replace('alterar-senha.html?obrigatoria=1');
    return;
  }
  Interface.montarCabecalho();
  Interface.montarRodape();
  TradutorLibras.carregar();

  const online = await Api.online();
  if (!online) document.getElementById('aviso-vitrine')?.classList.remove('oculto');

  if (typeof window.iniciarPagina === 'function') {
    try { await window.iniciarPagina(online); }
    catch (e) { console.error(e); Interface.aviso(e.message || 'Algo deu errado ao abrir a página.', 'erro'); }
  }
});
