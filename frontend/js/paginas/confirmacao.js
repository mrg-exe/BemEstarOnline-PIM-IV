// Confirmação do pedido logo após a compra.
window.iniciarPagina = async function () {
  const alvo = document.getElementById('confirmacao');
  const id = new URLSearchParams(location.search).get('pedido');
  if (!id) { alvo.innerHTML = '<div class="vazio"><strong>Nenhum pedido informado.</strong><a href="conta.html">Ver meus pedidos</a></div>'; return; }
  if (!Sessao.exigirLogin()) return;
  try {
    const p = await Api.get(`/pedidos/${encodeURIComponent(id)}`);
    alvo.innerHTML = `
      <div class="selo-ok" aria-hidden="true">${icone('check', 44)}</div>
      <h1 style="font-size:1.9rem">Pedido nº ${p.idPedido} recebido!</h1>
      <p>Obrigado, ${esc(p.cliente.split(' ')[0])}. ${p.status === 'PAGO' ? 'O pagamento foi aprovado e já vamos separar seus produtos.' : 'Você paga na entrega. Já vamos separar seus produtos.'}</p>
      <div style="text-align:left;margin-top:1.5rem">
        <h2 style="font-size:1.2rem">Acompanhe</h2>
        ${Interface.linhaDoTempo(p)}
        <h2 style="font-size:1.2rem">Resumo</h2>
        ${Interface.resumoPedido(p)}
      </div>
      <div class="linha-compra" style="justify-content:center;margin-top:1rem">
        <a class="botao" href="conta.html">Ver meus pedidos</a>
        <a class="botao botao--contorno" href="medicamentos.html">Continuar comprando</a>
      </div>`;
  } catch (e) {
    alvo.innerHTML = `<div class="vazio"><strong>Não conseguimos abrir este pedido.</strong>${esc(e.message)}</div>`;
  }
};
