// Login: guarda o token e volta para a página de onde a pessoa veio.
window.iniciarPagina = async function (online) {
  const form = document.getElementById('form-login');
  const email = document.getElementById('email');
  const senha = document.getElementById('senha');
  const voltar = new URLSearchParams(location.search).get('voltar');
  const destinoSeguro = voltar && /^[\w-]+\.html(\?[\w=&%.-]*)?$/.test(voltar) ? voltar : null;
  if (destinoSeguro) document.getElementById('link-cadastro').href = `cadastro.html?voltar=${encodeURIComponent(destinoSeguro)}`;

  if (Sessao.logado()) { location.href = destinoSeguro || 'conta.html'; return; }

  form.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    Interface.erroCampo(email, /\S+@\S+\.\S+/.test(email.value) ? '' : 'Digite um e-mail válido.');
    Interface.erroCampo(senha, senha.value ? '' : 'Digite sua senha.');
    if (form.querySelector('[aria-invalid="true"]')) { form.querySelector('[aria-invalid="true"]').focus(); return; }
    if (!online) { Interface.aviso('A API está desligada. Ligue a API para entrar (veja o guia).', 'erro'); return; }

    const botao = form.querySelector('button[type="submit"]');
    botao.disabled = true;
    try {
      const resposta = await Api.post('/auth/login', { email: email.value.trim(), senha: senha.value });
      Sessao.entrar(resposta);
      // Senha provisória (RF35): primeiro cria a própria senha, depois segue para onde ia.
      if (resposta.usuario.trocarSenha) {
        const depois = destinoSeguro || (Sessao.ehEquipe() ? 'admin.html' : 'index.html');
        location.href = `alterar-senha.html?obrigatoria=1&voltar=${encodeURIComponent(depois)}`;
        return;
      }
      await Carrinho.aoEntrar();
      // Só atendente e administrador usam o painel; o entregador usa o app.
      location.href = destinoSeguro || (Sessao.ehEquipe() ? 'admin.html' : 'index.html');
    } catch (e) {
      Interface.aviso(e.message, 'erro');
      senha.value = '';
      senha.focus();
      botao.disabled = false;
    }
  });
};
