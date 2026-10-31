// Cadastro com validação de CPF, nome social e consentimento da LGPD.
function cpfValido(cpf) {
  const n = cpf.replace(/\D/g, '');
  if (n.length !== 11 || /^(\d)\1{10}$/.test(n)) return false;
  const digito = (q) => {
    let soma = 0;
    for (let i = 0; i < q; i++) soma += Number(n[i]) * (q + 1 - i);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };
  return digito(9) === Number(n[9]) && digito(10) === Number(n[10]);
}

window.iniciarPagina = async function (online) {
  const $ = id => document.getElementById(id);
  const form = $('form-cadastro');
  const voltar = new URLSearchParams(location.search).get('voltar');
  const destinoSeguro = voltar && /^[\w-]+\.html(\?[\w=&%.-]*)?$/.test(voltar) ? voltar : null;
  Mascara.aplicar($('cpf'), 'cpf');
  Mascara.aplicar($('telefone'), 'telefone');
  CampoData.ligar($('nascimento'));

  form.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const regras = [
      [$('nome'), $('nome').value.trim().split(' ').length >= 2 ? '' : 'Digite nome e sobrenome.'],
      [$('cpf'), cpfValido($('cpf').value) ? '' : 'CPF inválido. Confira os números.'],
      [$('email'), /\S+@\S+\.\S+/.test($('email').value) ? '' : 'Digite um e-mail válido.'],
      [$('senha'), $('senha').value.length >= 6 ? '' : 'A senha precisa ter pelo menos 6 caracteres.'],
      [$('senha2'), $('senha2').value === $('senha').value ? '' : 'As senhas não são iguais.'],
      [$('nascimento'), !$('nascimento').value || CampoData.paraIso($('nascimento').value) ? '' : 'Data inválida. Use dia/mês/ano, por exemplo 20/05/1998.'],
    ];
    regras.forEach(([campo, msg]) => Interface.erroCampo(campo, msg));
    const primeiroErro = regras.find(([, msg]) => msg);
    if (primeiroErro) { primeiroErro[0].focus(); return; }
    if (!$('lgpd').checked) { Interface.aviso('Para criar a conta, marque a concordância com o uso dos dados.', 'erro'); return; }
    if (!online) { Interface.aviso('A API está desligada. Ligue a API para criar a conta.', 'erro'); return; }

    const botao = form.querySelector('button[type="submit"]');
    botao.disabled = true;
    try {
      const resposta = await Api.post('/auth/registrar', {
        nome: $('nome').value.trim(),
        nomeSocial: $('nome-social').value.trim() || null,
        dataNascimento: CampoData.paraIso($('nascimento').value),
        email: $('email').value.trim(),
        cpf: $('cpf').value,
        telefone: $('telefone').value || null,
        senha: $('senha').value,
      });
      Sessao.entrar(resposta);
      await Carrinho.aoEntrar();
      Interface.aviso('Conta criada! Bem-vindo(a).', 'sucesso');
      setTimeout(() => { location.href = destinoSeguro || 'index.html'; }, 800);
    } catch (e) {
      Interface.aviso(e.message, 'erro');
      botao.disabled = false;
    }
  });
};
