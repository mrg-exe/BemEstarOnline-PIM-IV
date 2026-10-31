import html
def ator(x, y, nome):
    return f'''<g stroke="#0D3550" stroke-width="2.2" fill="none">
      <circle cx="{x}" cy="{y-38}" r="11"/><line x1="{x}" y1="{y-27}" x2="{x}" y2="{y+5}"/>
      <line x1="{x-18}" y1="{y-16}" x2="{x+18}" y2="{y-16}"/><line x1="{x}" y1="{y+5}" x2="{x-14}" y2="{y+28}"/>
      <line x1="{x}" y1="{y+5}" x2="{x+14}" y2="{y+28}"/></g>
      <text x="{x}" y="{y+48}" text-anchor="middle" font-size="15" font-weight="700" fill="#0D3550">{html.escape(nome)}</text>'''
def elipse(cx, cy, texto, w, h, destaque):
    linhas = texto.split('|')
    t = ''.join(f'<text x="{cx}" y="{cy + (i - (len(linhas)-1)/2)*16 + 5}" text-anchor="middle" font-size="14" fill="#0D3550">{html.escape(l)}</text>' for i,l in enumerate(linhas))
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{w/2}" ry="{h/2}" fill="{"#E6F2DC" if destaque else "#FFFFFF"}" stroke="#00807C" stroke-width="1.8"/>{t}'

def gerar(nome, titulo, grupos, heranca=False, extras=None):
    """grupos: [(nome_ator, [(id, texto, destaque, ligado_ao_ator)])] — um bloco por ator, numa coluna só."""
    largura, passo, ew, eh = 1000, 62, 300, 48
    cx = 600
    y = 120
    pos, atores = {}, []
    for nome_ator, casos in grupos:
        ys = []
        for cid, txt, dest, ligado in casos:
            pos[cid] = (cx, y, txt, dest); ys.append((y, ligado)); y += passo
        y += 26
        ligados = [yy for yy, l in ys if l]
        atores.append((nome_ator, sum(ligados)/len(ligados), [yy for yy, l in ys if l]))
    altura = y + 20
    svg = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{largura}" height="{altura}" viewBox="0 0 {largura} {altura}" font-family="Arial, Helvetica, sans-serif">',
           '<defs><marker id="seta" viewBox="0 0 10 10" refX="9" refY="5" markerWidth="9" markerHeight="9" orient="auto"><path d="M0,0 L10,5 L0,10" fill="none" stroke="#0D3550" stroke-width="1.5"/></marker></defs>',
           '<rect width="100%" height="100%" fill="#FFFFFF"/>',
           f'<text x="{largura/2}" y="34" text-anchor="middle" font-size="19" font-weight="700" fill="#0D3550">{html.escape(titulo)}</text>',
           f'<rect x="{cx-ew/2-60}" y="62" width="{ew+60+230}" height="{altura-82}" rx="10" fill="#F7FAF9" stroke="#0D3550" stroke-width="1.5"/>',
           f'<text x="{cx-ew/2-46}" y="86" font-size="14" font-weight="700" fill="#0D3550">Sistema Bem Estar Online</text>']
    for nome_ator, ay, ligados in atores:
        for yy in ligados:
            svg.append(f'<line x1="150" y1="{ay-12}" x2="{cx-ew/2}" y2="{yy}" stroke="#6B7F8C" stroke-width="1.3"/>')
    if extras: svg.append(extras(pos, cx, ew))
    for cid, (x, yy, txt, dest) in pos.items(): svg.append(elipse(x, yy, txt, ew, eh, dest))
    for nome_ator, ay, _ in atores: svg.append(ator(110, ay, nome_ator))
    if heranca and len(atores) == 2:
        y1 = atores[0][1]; y2 = atores[1][1]
        svg.append(f'<line x1="110" y1="{y2-62}" x2="110" y2="{y1+66}" stroke="#0D3550" stroke-width="1.6"/>'
                   f'<polygon points="110,{y1+58} 102,{y1+74} 118,{y1+74}" fill="#FFFFFF" stroke="#0D3550" stroke-width="1.6"/>'
                   f'<text x="120" y="{(y1+y2)/2}" font-size="12" fill="#4A6378">herda</text>')
    svg.append('</svg>')
    open(f'{nome}.svg', 'w').write('\n'.join(svg))

gerar('uc-01-cliente', 'Casos de uso — Visitante e Cliente (site e app)', [
  ('Visitante', [('c1','Consultar catálogo e buscar produto',False,True),('c2','Ouvir descrição em voz alta',False,True),
                 ('c14','Ajustar acessibilidade|(letra, contraste, Libras)',True,True),('c3','Criar conta (nome social, LGPD)',False,True),
                 ('c4','Enviar receita (foto ou texto)',False,True),('c5','Registrar relato na ouvidoria',True,True)]),
  ('Cliente', [('c6','Entrar / recuperar senha',False,True),('c7','Montar carrinho e aplicar cupom',False,True),
               ('c8','Finalizar compra',False,True),('c9','Acompanhar pedido no mapa',False,True),('c10','Cancelar pedido',False,True),
               ('c11','Gerenciar assinatura de uso contínuo',False,True),('c12','Gerenciar endereços',False,True),
               ('c13','Excluir a própria conta',False,True)])], heranca=True)

gerar('uc-02-equipe', 'Casos de uso — Equipe da farmácia (painel)', [
  ('Atendente', [('e1','Ver painel (vendas, estoque baixo,|validade)',False,True),('e2','Avançar status do pedido',False,True),
                 ('e3','Ajustar estoque',False,True),('e4','Tratar alertas de reposição',False,True),('e5','Analisar receitas recebidas',False,True),
                 ('e6','Acompanhar assinaturas',False,True),('e7','Responder ouvidoria',True,True),('e8','Consultar clientes',False,True)]),
  ('Administrador', [('e9','Cadastrar e editar produtos (foto)',False,True),('e10','Gerenciar equipe',True,True)])], heranca=True)

def ext_entregador(pos, cx, ew):
    _, y6, _, _ = pos['m6']; _, y7, _, _ = pos['m7']; _, y8, _, _ = pos['m8']
    d = cx + ew/2
    return (f'<path d="M{d} {y6} C {d+42} {y6}, {d+42} {y7}, {d+4} {y7}" fill="none" stroke="#0D3550" stroke-width="1.4" stroke-dasharray="6 5" marker-end="url(#seta)"/>'
            f'<text x="{d+50}" y="{(y6+y7)/2-2}" font-size="11.5" fill="#0D3550">«extend»</text><text x="{d+50}" y="{(y6+y7)/2+12}" font-size="10.5" fill="#4A6378">se exige receita</text>'
            f'<path d="M{d} {y8} C {d+42} {y8}, {d+42} {y7}, {d+4} {y7+6}" fill="none" stroke="#0D3550" stroke-width="1.4" stroke-dasharray="6 5" marker-end="url(#seta)"/>'
            f'<text x="{d+50}" y="{(y7+y8)/2+8}" font-size="11.5" fill="#0D3550">«extend»</text><text x="{d+50}" y="{(y7+y8)/2+22}" font-size="10.5" fill="#4A6378">pagamento em dinheiro</text>')
gerar('uc-03-entregador', 'Casos de uso — Entregador (app)', [
  ('Entregador', [('m1','Receber aviso de pedido novo|(informa se exige receita)',False,True),('m2','Pegar pedido',False,True),
                  ('m3','Ver localização do cliente|e abrir rota',False,True),('m4','Sair para entrega',False,True),
                  ('m5','Enviar posição (GPS ou simulação)',False,True),('m6','Conferir receita',True,False),
                  ('m7','Confirmar entrega',False,True),('m8','Receber pagamento em dinheiro',False,False)])], extras=ext_entregador)
print('ok')
