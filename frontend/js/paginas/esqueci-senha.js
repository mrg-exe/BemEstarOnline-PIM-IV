// Esqueci minha senha (RF33): pede o link por e-mail. A resposta é sempre a mesma, exista ou não a conta.
window.iniciarPagina = async function (online) {
  const $ = id => document.getElementById(id);
  if (Sessao.usuario()?.email) $('email').value = Sessao.usuario().email;

  $('form-esqueci').addEventListener('submit', async ev => {
    ev.preventDefault();
    const email = $('email');
    Interface.erroCampo(email, /\S+@\S+\.\S+/.test(email.value) ? '' : 'Digite um e-mail válido.');
    if (email.getAttribute('aria-invalid') === 'true') { email.focus(); return; }
    if (!online) { Interface.aviso('A API está desligada. Ligue a API para pedir o link.', 'erro'); return; }

    const botao = $('form-esqueci').querySelector('button');
    botao.disabled = true;
    try {
      const r = await Api.post('/auth/esqueci-senha', { email: email.value.trim() });
      $('form-esqueci').hidden = true;
      $('resultado-esqueci').innerHTML = `<div class="aviso-receita" style="border-left-color:var(--folha)">
        ${icone('check', 24)}<p style="margin:0">${esc(r.mensagem)}</p></div>`;
    } catch (e) {
      Interface.aviso(e.message, 'erro');
      botao.disabled = false;
    }
  });
};
