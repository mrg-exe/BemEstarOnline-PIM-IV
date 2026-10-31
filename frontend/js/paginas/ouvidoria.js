// Ouvidoria e canal de denúncia (RF30): funciona com ou sem login e pode ser anônimo.
window.iniciarPagina = async function (online) {
  const $ = id => document.getElementById(id);
  const form = $('form-ouvidoria');
  const usuario = Sessao.usuario();
  $('icone-lei').innerHTML = icone('receita', 24);

  $('tipo').insertAdjacentHTML('beforeend', Object.entries(Formatar.ouvidoria)
    .map(([v, r]) => `<option value="${v}">${esc(r)}</option>`).join(''));
  $('subtipo').insertAdjacentHTML('beforeend', Object.entries(Formatar.discriminacao)
    .map(([v, r]) => `<option value="${v}">${esc(r)}</option>`).join(''));

  // A pergunta do tipo de discriminação só aparece quando faz sentido.
  $('tipo').addEventListener('change', () => {
    $('campo-subtipo').hidden = $('tipo').value !== 'DISCRIMINACAO';
    if ($('campo-subtipo').hidden) { $('subtipo').value = ''; Interface.erroCampo($('subtipo'), ''); }
  });

  $('descricao').addEventListener('input', () => {
    $('contador-descricao').textContent = `${$('descricao').value.length} de 4000 letras`;
  });

  // Anônimo: esconde e apaga o contato (nada que identifique a pessoa é enviado).
  function explicarAnonimato() {
    const anonimo = $('anonimo').checked;
    $('campo-contato').hidden = anonimo;
    if (anonimo) $('contato').value = '';
    $('explica-anonimo').textContent = anonimo
      ? 'Não guardamos seu nome, sua conta nem seu contato. Anote o protocolo para ver a resposta.'
      : usuario
        ? `O relato fica ligado à sua conta (${usuario.nomeExibicao}), para a ouvidoria poder falar com você.`
        : 'Se deixar um contato, a ouvidoria pode falar com você. Sem contato, acompanhe pelo protocolo.';
  }
  $('anonimo').addEventListener('change', explicarAnonimato);
  explicarAnonimato();

  form.addEventListener('submit', async (ev) => {
    ev.preventDefault();
    const regras = [
      [$('tipo'), $('tipo').value ? '' : 'Escolha sobre o que você quer falar.'],
      [$('subtipo'), $('tipo').value !== 'DISCRIMINACAO' || $('subtipo').value ? '' : 'Escolha o tipo de discriminação.'],
      [$('descricao'), $('descricao').value.trim().length >= 10 ? '' : 'Conte com um pouco mais de detalhes (pelo menos 10 letras).'],
    ];
    regras.forEach(([c, m]) => Interface.erroCampo(c, m));
    const erro = regras.find(([, m]) => m);
    if (erro) { erro[0].focus(); return; }
    if (!online) { Interface.aviso('A API está desligada. Ligue a API para enviar.', 'erro'); return; }

    try {
      const r = await Api.post('/ouvidoria', {
        tipo: $('tipo').value,
        subtipoDiscriminacao: $('subtipo').value || null,
        descricao: $('descricao').value.trim(),
        anonimo: $('anonimo').checked,
        contato: $('anonimo').checked ? null : ($('contato').value.trim() || null),
      });
      form.reset();
      $('campo-subtipo').hidden = true;
      $('contador-descricao').textContent = '0 de 4000 letras';
      explicarAnonimato();
      $('resultado-ouvidoria').innerHTML = `<div class="aviso-receita" style="margin-top:1.25rem;border-left-color:var(--folha)">
        ${icone('check', 24)}<div><p style="margin:0 0 .4rem"><strong>Recebemos sua manifestação.</strong> Seu protocolo é:</p>
        <p class="protocolo" style="margin:0 0 .5rem">${esc(r.protocolo)}</p>
        <p style="margin:0 0 .6rem">Anote ou copie este número: é com ele que você vê a resposta da ouvidoria.</p>
        <button class="botao botao--pequeno botao--contorno" type="button" id="copiar-protocolo">Copiar protocolo</button></div></div>`;
      $('copiar-protocolo').addEventListener('click', async () => {
        try { await navigator.clipboard.writeText(r.protocolo); Interface.aviso('Protocolo copiado.', 'sucesso'); }
        catch { Interface.aviso(`Anote o protocolo: ${r.protocolo}`); }
      });
      $('protocolo').value = r.protocolo;
      Interface.aviso('Manifestação enviada!', 'sucesso');
      $('resultado-ouvidoria').scrollIntoView({ behavior: 'smooth', block: 'center' });
    } catch (e) {
      Interface.aviso(e.message, 'erro');
    }
  });

  async function consultar(protocolo) {
    const destino = $('resultado-consulta');
    protocolo = protocolo.trim().toUpperCase();
    if (!protocolo) { Interface.erroCampo($('protocolo'), 'Digite o número do protocolo.'); $('protocolo').focus(); return; }
    Interface.erroCampo($('protocolo'), '');
    if (!online) { destino.innerHTML = '<p>A API está desligada. Ligue a API para consultar.</p>'; return; }
    try {
      const r = await Api.get(`/ouvidoria/${encodeURIComponent(protocolo)}`);
      destino.innerHTML = `<div style="margin-top:1rem">
        <p style="margin:0 0 .3rem"><span class="protocolo" style="font-size:1.1rem">${esc(r.protocolo)}</span> ${Formatar.situacao(r.status)}</p>
        <p style="margin:0;color:var(--tinta-suave)">${esc(Formatar.ouvidoria[r.tipo] || r.tipo)}, enviada em ${Formatar.dataHora(r.criadoEm)}</p>
        ${r.resposta
          ? `<div class="resposta-ouvidoria"><strong>Resposta da ouvidoria</strong> <small>(${Formatar.dataHora(r.respondidoEm)})</small>
              <p style="margin:.4rem 0 0;white-space:pre-line">${esc(r.resposta)}</p></div>`
          : '<p style="margin:.75rem 0 0">Ainda sem resposta. A ouvidoria já recebeu e vai responder por aqui: volte a consultar depois.</p>'}
      </div>`;
    } catch (e) {
      destino.innerHTML = `<p style="margin-top:1rem">${esc(e.message)}</p>`;
    }
  }
  $('form-consulta').addEventListener('submit', ev => { ev.preventDefault(); consultar($('protocolo').value); });

  // Link direto com o protocolo: ouvidoria.html?protocolo=OUV-...
  const doLink = new URLSearchParams(location.search).get('protocolo');
  if (doLink) { $('protocolo').value = doLink; consultar(doLink); }
};
