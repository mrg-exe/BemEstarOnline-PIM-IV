// Alterar senha (RF34). Também é a tela obrigatória de quem recebeu senha provisória do administrador (RF35).
window.iniciarPagina = async function (online) {
  const $ = id => document.getElementById(id);
  if (!Sessao.exigirLogin()) return;

  const parametros = new URLSearchParams(location.search);
  const voltar = parametros.get('voltar');
  const destinoSeguro = voltar && /^[\w-]+\.html(\?[\w=&%.-]*)?(#[\w-]+)?$/.test(voltar) ? voltar : null;
  const obrigatoria = !!Sessao.usuario()?.trocarSenha;

  if (obrigatoria) {
    $('aviso-provisoria').hidden = false;
    $('icone-aviso').innerHTML = icone('receita', 24);
    $('rotulo-atual').textContent = 'Senha provisória (a que o administrador passou)';
    // Sem a senha nova não há para onde voltar: o link vira "sair".
    $('link-voltar').innerHTML = '<button type="button" class="link-botao" id="sair-agora">Sair da conta</button>';
    $('sair-agora').addEventListener('click', () => Sessao.sair('login.html'));
  }

  $('form-senha').addEventListener('submit', async ev => {
    ev.preventDefault();
    const atual = $('senha-atual'), nova = $('nova-senha'), repetir = $('repetir-senha');
    const regras = [
      [atual, atual.value ? '' : 'Digite a senha atual.'],
      [nova, nova.value.length >= 6 ? '' : 'A nova senha precisa ter pelo menos 6 caracteres.'],
      [repetir, repetir.value === nova.value ? '' : 'As duas senhas não são iguais.'],
    ];
    regras.forEach(([c, m]) => Interface.erroCampo(c, m));
    const erro = regras.find(([, m]) => m);
    if (erro) { erro[0].focus(); return; }
    if (!online) { Interface.aviso('A API está desligada. Ligue a API para trocar a senha.', 'erro'); return; }

    try {
      const usuario = await Api.post('/auth/alterar-senha', { senhaAtual: atual.value, novaSenha: nova.value });
      Sessao.atualizarUsuario(usuario);
      Interface.aviso('Senha alterada!', 'sucesso');
      const equipe = usuario.perfil === 'ADMIN' || usuario.perfil === 'ATENDENTE';
      setTimeout(() => { location.href = destinoSeguro || (obrigatoria ? (equipe ? 'admin.html' : 'index.html') : 'conta.html#dados'); }, 900);
    } catch (e) {
      Interface.aviso(e.message, 'erro');
      if (e.status === 401) setTimeout(() => location.href = 'login.html', 1500);
    }
  });
};
