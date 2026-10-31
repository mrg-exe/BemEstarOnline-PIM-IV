// Criar nova senha pelo link do e-mail (RF33): redefinir-senha.html?token=...
window.iniciarPagina = async function (online) {
  const $ = id => document.getElementById(id);
  const token = new URLSearchParams(location.search).get('token');
  if (!token) { $('form-redefinir').hidden = true; $('sem-link').hidden = false; return; }
  // Tira o código da barra de endereço (não fica no histórico nem aparece num print da tela).
  history.replaceState(null, '', 'redefinir-senha.html');

  $('form-redefinir').addEventListener('submit', async ev => {
    ev.preventDefault();
    const nova = $('nova-senha'), repetir = $('repetir-senha');
    const regras = [
      [nova, nova.value.length >= 6 ? '' : 'A nova senha precisa ter pelo menos 6 caracteres.'],
      [repetir, repetir.value === nova.value ? '' : 'As duas senhas não são iguais.'],
    ];
    regras.forEach(([c, m]) => Interface.erroCampo(c, m));
    const erro = regras.find(([, m]) => m);
    if (erro) { erro[0].focus(); return; }
    if (!online) { Interface.aviso('A API está desligada. Ligue a API para salvar a senha.', 'erro'); return; }

    try {
      const r = await Api.post('/auth/redefinir-senha', { token, novaSenha: nova.value });
      // Se havia alguém logado neste navegador, sai: a pessoa entra de novo com a senha nova.
      Sessao.sair(null);
      $('form-redefinir').hidden = true;
      $('resultado-redefinir').innerHTML = `<div class="aviso-receita" style="border-left-color:var(--folha)">
        ${icone('check', 24)}<div><p style="margin:0 0 .75rem">${esc(r.mensagem)}</p>
        <a class="botao" href="login.html">Entrar</a></div></div>`;
    } catch (e) {
      $('resultado-redefinir').innerHTML = `<div class="aviso-receita" style="margin-top:1rem">${icone('receita', 24)}
        <div><p style="margin:0 0 .75rem">${esc(e.message)}</p><a class="botao botao--contorno" href="esqueci-senha.html">Pedir um link novo</a></div></div>`;
    }
  });
};
