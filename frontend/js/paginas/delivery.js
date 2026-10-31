// Delivery por receita: funciona com ou sem login. A foto/PDF da receita é enviada de verdade (RF39).
const TAMANHO_MAXIMO = 5 * 1024 * 1024;

window.iniciarPagina = async function (online) {
  const $ = id => document.getElementById(id);
  const form = $('form-delivery');
  Mascara.aplicar($('telefone'), 'telefone');

  const usuario = Sessao.usuario();
  if (usuario) $('nome').value = usuario.nomeExibicao;

  form.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const regras = [
      [$('nome'), $('nome').value.trim() ? '' : 'Digite seu nome.'],
      [$('telefone'), $('telefone').value.replace(/\D/g, '').length >= 10 ? '' : 'Digite um celular com DDD.'],
      [$('endereco'), $('endereco').value.trim().length >= 8 ? '' : 'Digite o endereço completo.'],
      [$('pedido'), $('pedido').value.trim() || $('receita').files.length ? '' : 'Conte o que precisa ou anexe a receita.'],
    ];
    regras.forEach(([c, m]) => Interface.erroCampo(c, m));
    const erro = regras.find(([, m]) => m);
    if (erro) { erro[0].focus(); return; }
    if (!online) { Interface.aviso('A API está desligada. Ligue a API para enviar.', 'erro'); return; }

    const arquivo = $('receita').files[0];
    if (arquivo && !/\.(jpe?g|png|pdf)$/i.test(arquivo.name)) {
      Interface.erroCampo($('receita'), 'Envie a receita como foto (JPG ou PNG) ou PDF.'); $('receita').focus(); return;
    }
    if (arquivo && arquivo.size > TAMANHO_MAXIMO) {
      Interface.erroCampo($('receita'), 'A receita pode ter no máximo 5 MB. Tire a foto de novo ou diminua a imagem.'); $('receita').focus(); return;
    }
    Interface.erroCampo($('receita'), '');

    // O arquivo vai junto, de verdade (RF39): formulário multipart com o campo "receita".
    const dados = new FormData();
    dados.append('nomeCliente', $('nome').value.trim());
    dados.append('telefone', $('telefone').value);
    dados.append('enderecoTexto', $('endereco').value.trim());
    dados.append('observacao', $('pedido').value.trim() || 'Ver receita anexada.');
    if (arquivo) dados.append('receita', arquivo, arquivo.name);

    const botao = form.querySelector('button[type="submit"]');
    botao.disabled = true;
    try {
      const r = await Api.post('/delivery', dados);
      form.reset();
      if (usuario) $('nome').value = usuario.nomeExibicao;
      $('resultado-delivery').innerHTML = `<div class="aviso-receita" style="margin-top:1.25rem;border-left-color:var(--folha)">
        ${icone('check', 24)}<p style="margin:0"><strong>Recebemos sua solicitação nº ${r.idDelivery}.</strong>
        A farmacêutica vai chamar no WhatsApp em poucos minutos com o valor.</p></div>`;
      Interface.aviso('Solicitação enviada!', 'sucesso');
    } catch (e) {
      Interface.aviso(e.message, 'erro');
    } finally {
      botao.disabled = false;
    }
  });
};
