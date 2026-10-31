-- =============================================================
-- 04_seed.sql  |  Dados iniciais do catálogo e cupons
-- Mesmos dados usados pela API no modo demonstração (CatalogoSeed.cs).
-- Os usuários de teste e os pedidos de exemplo são criados pela API na
-- primeira execução, porque as senhas precisam ser criptografadas (PBKDF2).
-- =============================================================
SET NAMES utf8mb4;
USE db_bemestar;

INSERT INTO categorias (nome, slug, descricao, icone, ordem) VALUES
  ('Genéricos', 'genericos', 'Medicamentos genéricos com a mesma eficácia do de referência, por um preço menor.', '💊', 1),
  ('Referência', 'referencia', 'Medicamentos de marca que deram origem aos genéricos.', '🏷️', 2),
  ('Uso contínuo', 'uso-continuo', 'Pressão, diabetes, colesterol e tireoide: remédios do dia a dia, com opção de assinatura.', '🔁', 3),
  ('Vitaminas e suplementos', 'vitaminas', 'Vitaminas, minerais e suplementos para completar a alimentação.', '🍊', 4),
  ('Higiene pessoal', 'higiene', 'Cuidados diários com o corpo, a boca e os cabelos.', '🧼', 5),
  ('Dermocosméticos', 'dermocosmeticos', 'Proteção solar, hidratação e cuidados com a pele.', '🧴', 6),
  ('Mamãe e bebê', 'mamae-bebe', 'Fraldas, pomadas e cuidados para os pequenos.', '🍼', 7),
  ('Primeiros socorros', 'primeiros-socorros', 'Curativos, antissépticos e itens para pequenos acidentes.', '🩹', 8),
  ('Aparelhos de saúde', 'aparelhos', 'Termômetros, medidores de pressão e oxímetros para acompanhar a saúde em casa.', '🩺', 9);

INSERT INTO medicamentos (id_categoria, nome, slug, tipo, principio_ativo, descricao, dosagem, apresentacao,
  fabricante, via_administracao, registro_anvisa, validade_lote, preco, exige_receita, uso_continuo, farmacia_popular)
SELECT c.id_categoria, p.nome, p.slug, p.tipo, p.principio, p.descricao, p.dosagem, p.apresentacao,
       p.fabricante, p.via, p.anvisa, p.validade, p.preco, p.receita, p.continuo, p.popular
FROM (
  SELECT 'genericos' AS cat, 'Dipirona Sódica 500mg' AS nome, 'dipirona-sodica-500mg' AS slug, 'Genérico' AS tipo, 'Dipirona monoidratada' AS principio, 'Analgésico e antitérmico para dores leves a moderadas e febre.' AS descricao, '500mg' AS dosagem, '10 comprimidos' AS apresentacao, 'Lab. Vida Genéricos' AS fabricante, 'Oral' AS via, '1035500010001' AS anvisa, '2027-10-30' AS validade, 4.50 AS preco, FALSE AS receita, FALSE AS continuo, FALSE AS popular
  UNION ALL
  SELECT 'genericos', 'Paracetamol 750mg', 'paracetamol-750mg', 'Genérico', 'Paracetamol', 'Alívio de dores de cabeça, dores no corpo e febre.', '750mg', '20 comprimidos', 'Lab. Vida Genéricos', 'Oral', '1035500010002', '2027-08-31', 7.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'genericos', 'Ibuprofeno 600mg', 'ibuprofeno-600mg', 'Genérico', 'Ibuprofeno', 'Anti-inflamatório para dores musculares, cólicas e febre.', '600mg', '20 comprimidos', 'Pharmabrás', 'Oral', '1035500010003', '2027-06-30', 11.40, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'genericos', 'Loratadina 10mg', 'loratadina-10mg', 'Genérico', 'Loratadina', 'Antialérgico para rinite, coceira e espirros. Não costuma dar sono.', '10mg', '12 comprimidos', 'Pharmabrás', 'Oral', '1035500010004', '2027-09-30', 9.80, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'genericos', 'Omeprazol 20mg', 'omeprazol-20mg', 'Genérico', 'Omeprazol', 'Reduz a acidez do estômago; indicado para gastrite e refluxo.', '20mg', '28 cápsulas', 'Lab. Vida Genéricos', 'Oral', '1035500010005', '2027-05-31', 14.90, FALSE, TRUE, FALSE
  UNION ALL
  SELECT 'genericos', 'Amoxicilina 500mg', 'amoxicilina-500mg', 'Genérico', 'Amoxicilina tri-hidratada', 'Antibiótico para infecções bacterianas. Venda com retenção de receita.', '500mg', '21 cápsulas', 'Pharmabrás', 'Oral', '1035500010006', '2027-03-31', 24.90, TRUE, FALSE, FALSE
  UNION ALL
  SELECT 'genericos', 'Butilbrometo de Escopolamina 10mg', 'butilbrometo-de-escopolamina-10mg', 'Genérico', 'Butilbrometo de escopolamina', 'Alívio de cólicas abdominais e desconfortos intestinais.', '10mg', '20 drágeas', 'Lab. Vida Genéricos', 'Oral', '1035500010007', '2026-12-20', 12.60, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'referencia', 'Ibuprofeno 400mg Referência', 'ibuprofeno-400mg-referencia', 'Referência', 'Ibuprofeno', 'Anti-inflamatório com ação contra dor, febre e inflamação.', '400mg', '20 cápsulas', 'Indústria Saúde Total', 'Oral', '1035500020001', '2027-09-30', 19.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'referencia', 'Relaxan Plus', 'relaxan-plus', 'Referência', 'Dipirona + orfenadrina + cafeína', 'Relaxante muscular para dores de tensão e torcicolo.', '300mg+35mg+50mg', '24 comprimidos', 'Indústria Saúde Total', 'Oral', '1035500020002', '2027-11-30', 22.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'referencia', 'Cefalix Enxaqueca', 'cefalix-enxaqueca', 'Referência', 'Dipirona + isometepteno + cafeína', 'Indicado para dores de cabeça e enxaqueca.', '300mg', '20 drágeas', 'Indústria Saúde Total', 'Oral', '1035500020003', '2027-07-31', 26.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'referencia', 'Xarope Expectorante', 'xarope-expectorante', 'Referência', 'Ambroxol', 'Ajuda a soltar o catarro em tosses com secreção.', '30mg/5ml', 'Frasco 120ml', 'Indústria Saúde Total', 'Oral', '1035500020004', '2026-12-10', 29.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'uso-continuo', 'Losartana Potássica 50mg', 'losartana-potassica-50mg', 'Genérico', 'Losartana potássica', 'Controle da pressão alta e proteção dos rins em diabéticos.', '50mg', '30 comprimidos', 'Lab. Vida Genéricos', 'Oral', '1035500030001', '2027-12-31', 12.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Hidroclorotiazida 25mg', 'hidroclorotiazida-25mg', 'Genérico', 'Hidroclorotiazida', 'Diurético usado no tratamento da pressão alta e do inchaço.', '25mg', '30 comprimidos', 'Pharmabrás', 'Oral', '1035500030002', '2027-10-31', 6.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Atenolol 50mg', 'atenolol-50mg', 'Genérico', 'Atenolol', 'Controle da pressão arterial e dos batimentos do coração.', '50mg', '30 comprimidos', 'Lab. Vida Genéricos', 'Oral', '1035500030003', '2027-08-31', 9.50, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Metformina 850mg', 'metformina-850mg', 'Genérico', 'Cloridrato de metformina', 'Controle da glicose no diabetes tipo 2.', '850mg', '30 comprimidos', 'Pharmabrás', 'Oral', '1035500030004', '2027-11-30', 8.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Glibenclamida 5mg', 'glibenclamida-5mg', 'Genérico', 'Glibenclamida', 'Auxilia no controle do diabetes tipo 2 estimulando a insulina.', '5mg', '30 comprimidos', 'Lab. Vida Genéricos', 'Oral', '1035500030005', '2027-04-30', 5.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Sinvastatina 20mg', 'sinvastatina-20mg', 'Genérico', 'Sinvastatina', 'Reduz o colesterol e o risco de problemas no coração.', '20mg', '30 comprimidos', 'Pharmabrás', 'Oral', '1035500030006', '2027-09-30', 13.50, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'uso-continuo', 'Levotiroxina 50mcg', 'levotiroxina-50mcg', 'Referência', 'Levotiroxina sódica', 'Reposição do hormônio da tireoide.', '50mcg', '30 comprimidos', 'Indústria Saúde Total', 'Oral', '1035500030007', '2027-06-30', 16.80, TRUE, TRUE, FALSE
  UNION ALL
  SELECT 'uso-continuo', 'Salbutamol Spray 100mcg', 'salbutamol-spray-100mcg', 'Genérico', 'Sulfato de salbutamol', 'Broncodilatador para crises de asma e falta de ar.', '100mcg/dose', 'Frasco 200 doses', 'Pharmabrás', 'Inalatória', '1035500030008', '2027-02-28', 21.90, TRUE, TRUE, TRUE
  UNION ALL
  SELECT 'vitaminas', 'Vitamina C 1g Efervescente', 'vitamina-c-1g-efervescente', 'Suplemento', 'Ácido ascórbico', 'Vitamina C sabor laranja para apoiar a imunidade.', '1g', '10 pastilhas', 'VitaBem', 'Oral', NULL, '2027-11-30', 15.00, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'vitaminas', 'Vitamina D3 2000 UI', 'vitamina-d3-2000-ui', 'Suplemento', 'Colecalciferol', 'Ajuda na saúde dos ossos e na absorção de cálcio.', '2000 UI', '60 cápsulas', 'VitaBem', 'Oral', NULL, '2027-12-31', 34.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'vitaminas', 'Complexo B', 'complexo-b', 'Suplemento', 'Vitaminas do complexo B', 'Apoio ao metabolismo energético e ao sistema nervoso.', NULL, '30 comprimidos', 'VitaBem', 'Oral', NULL, '2027-07-31', 18.40, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'vitaminas', 'Ômega 3 1000mg', 'omega-3-1000mg', 'Suplemento', 'Óleo de peixe (EPA e DHA)', 'Fonte de ácidos graxos essenciais para o coração.', '1000mg', '60 cápsulas', 'NutriVida', 'Oral', NULL, '2027-05-31', 42.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'vitaminas', 'Polivitamínico A-Z', 'polivitaminico-a-z', 'Suplemento', 'Vitaminas e minerais', 'Combinação diária de vitaminas e minerais.', NULL, '60 comprimidos', 'NutriVida', 'Oral', NULL, '2027-10-31', 39.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Sabonete Líquido Neutro', 'sabonete-liquido-neutro', 'Higiene', NULL, 'pH neutro para peles sensíveis, sem fragrância artificial.', NULL, 'Frasco 250ml', 'Bem Care', 'Tópica', NULL, '2028-01-31', 18.20, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Creme Dental Sensível', 'creme-dental-sensivel', 'Higiene', 'Nitrato de potássio + flúor', 'Alívio da sensibilidade e proteção contra cáries.', NULL, 'Tubo 90g', 'Sorriso Leve', 'Tópica', NULL, '2028-03-31', 12.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Escova Dental Macia', 'escova-dental-macia', 'Higiene', NULL, 'Cerdas macias e cabeça pequena para alcançar o fundo da boca.', NULL, '2 unidades', 'Sorriso Leve', NULL, NULL, '2030-12-31', 9.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Desodorante Roll-on sem Álcool', 'desodorante-roll-on-sem-alcool', 'Higiene', NULL, 'Proteção de 48h, suave para a pele.', NULL, '50ml', 'Bem Care', 'Tópica', NULL, '2028-02-28', 14.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'higiene', 'Shampoo Anticaspa', 'shampoo-anticaspa', 'Higiene', 'Piritionato de zinco', 'Controla a caspa e a coceira do couro cabeludo.', NULL, 'Frasco 200ml', 'Bem Care', 'Tópica', NULL, '2027-12-31', 24.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'dermocosmeticos', 'Protetor Solar FPS 50', 'protetor-solar-fps-50', 'Dermocosmético', NULL, 'Amplo espectro UVA e UVB, resistente à água, toque seco.', 'FPS 50', 'Frasco 200ml', 'Derma Sol', 'Tópica', NULL, '2027-08-31', 54.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'dermocosmeticos', 'Protetor Solar Facial FPS 60 com Cor', 'protetor-solar-facial-fps-60-com-cor', 'Dermocosmético', NULL, 'Proteção facial com cor universal e controle de oleosidade.', 'FPS 60', 'Bisnaga 40g', 'Derma Sol', 'Tópica', NULL, '2027-06-30', 69.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'dermocosmeticos', 'Hidratante Corporal Karité', 'hidratante-corporal-karite', 'Dermocosmético', 'Manteiga de karité + vitamina E', 'Hidratação profunda por até 48h.', NULL, 'Frasco 400ml', 'Bem Care', 'Tópica', NULL, '2028-04-30', 32.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'dermocosmeticos', 'Água Micelar', 'agua-micelar', 'Dermocosmético', NULL, 'Limpa e remove maquiagem sem precisar enxaguar.', NULL, 'Frasco 200ml', 'Derma Sol', 'Tópica', NULL, '2027-09-30', 36.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'mamae-bebe', 'Pomada para Assaduras', 'pomada-para-assaduras', 'Infantil', 'Óxido de zinco + vitaminas A e D', 'Previne e trata assaduras na área da fralda.', NULL, 'Bisnaga 45g', 'Pequeno Cuidado', 'Tópica', NULL, '2027-11-30', 19.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'mamae-bebe', 'Fralda Infantil Tamanho M', 'fralda-infantil-tamanho-m', 'Infantil', NULL, 'Até 12h de proteção com camada respirável.', 'Tamanho M', 'Pacote 32 unidades', 'Pequeno Cuidado', NULL, NULL, '2030-12-31', 44.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'mamae-bebe', 'Paracetamol Gotas Infantil', 'paracetamol-gotas-infantil', 'Genérico', 'Paracetamol', 'Antitérmico e analgésico em gotas para crianças. Siga a dose por peso indicada na bula.', '200mg/ml', 'Frasco 15ml', 'Lab. Vida Genéricos', 'Oral', '1035500070001', '2027-03-31', 8.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'mamae-bebe', 'Soro Fisiológico 0,9%', 'soro-fisiologico-0-9', 'Correlato', 'Cloreto de sódio 0,9%', 'Limpeza nasal e higiene dos olhos para toda a família.', '0,9%', 'Frasco 500ml', 'Pequeno Cuidado', 'Tópica', NULL, '2027-12-31', 7.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'primeiros-socorros', 'Kit Curativos Variados', 'kit-curativos-variados', 'Correlato', NULL, 'Curativos em 4 tamanhos com adesivo hipoalergênico.', NULL, '40 unidades', 'Health First', 'Tópica', NULL, '2028-06-30', 9.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'primeiros-socorros', 'Álcool 70% Gel', 'alcool-70-gel', 'Correlato', 'Álcool etílico 70%', 'Higienização das mãos sem água.', '70%', 'Frasco 500g', 'Health First', 'Tópica', NULL, '2027-12-31', 13.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'primeiros-socorros', 'Gaze Estéril', 'gaze-esteril', 'Correlato', NULL, 'Compressas estéreis para limpeza e cobertura de ferimentos.', '7,5 x 7,5 cm', '10 unidades', 'Health First', 'Tópica', NULL, '2029-06-30', 6.50, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'primeiros-socorros', 'Antisséptico Spray', 'antisseptico-spray', 'Correlato', 'Digliconato de clorexidina', 'Limpa pequenos ferimentos e ajuda a prevenir infecções.', '1%', 'Frasco 30ml', 'Health First', 'Tópica', NULL, '2027-07-31', 17.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'aparelhos', 'Termômetro Digital', 'termometro-digital', 'Correlato', NULL, 'Medição em cerca de 60 segundos, com aviso sonoro.', NULL, '1 unidade', 'MedCasa', NULL, NULL, '2031-12-31', 24.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'aparelhos', 'Medidor de Pressão de Pulso', 'medidor-de-pressao-de-pulso', 'Correlato', NULL, 'Aparelho automático com memória das últimas 60 medições.', NULL, '1 unidade', 'MedCasa', NULL, NULL, '2031-12-31', 149.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'aparelhos', 'Oxímetro de Dedo', 'oximetro-de-dedo', 'Correlato', NULL, 'Mostra a saturação de oxigênio e os batimentos por minuto.', NULL, '1 unidade', 'MedCasa', NULL, NULL, '2031-12-31', 89.90, FALSE, FALSE, FALSE
  UNION ALL
  SELECT 'aparelhos', 'Tiras para Glicemia', 'tiras-para-glicemia', 'Correlato', NULL, 'Tiras de teste para medidor de glicose MedCasa.', NULL, '50 tiras', 'MedCasa', NULL, NULL, '2027-04-30', 59.90, FALSE, TRUE, FALSE
) AS p
JOIN categorias c ON c.slug = p.cat;

-- Estoque inicial (alguns itens já começam abaixo do mínimo para demonstrar os alertas)
INSERT INTO estoques (id_medicamento, quantidade, quantidade_minima, localizacao)
SELECT m.id_medicamento, e.qtd, e.minimo, 'Depósito' FROM (
  SELECT 'dipirona-sodica-500mg' AS slug, 180 AS qtd, 20 AS minimo
  UNION ALL
  SELECT 'paracetamol-750mg', 150, 20
  UNION ALL
  SELECT 'ibuprofeno-600mg', 90, 15
  UNION ALL
  SELECT 'loratadina-10mg', 70, 10
  UNION ALL
  SELECT 'omeprazol-20mg', 120, 15
  UNION ALL
  SELECT 'amoxicilina-500mg', 40, 10
  UNION ALL
  SELECT 'butilbrometo-de-escopolamina-10mg', 8, 10
  UNION ALL
  SELECT 'ibuprofeno-400mg-referencia', 60, 10
  UNION ALL
  SELECT 'relaxan-plus', 85, 15
  UNION ALL
  SELECT 'cefalix-enxaqueca', 45, 10
  UNION ALL
  SELECT 'xarope-expectorante', 30, 8
  UNION ALL
  SELECT 'losartana-potassica-50mg', 200, 25
  UNION ALL
  SELECT 'hidroclorotiazida-25mg', 140, 20
  UNION ALL
  SELECT 'atenolol-50mg', 95, 15
  UNION ALL
  SELECT 'metformina-850mg', 160, 25
  UNION ALL
  SELECT 'glibenclamida-5mg', 6, 15
  UNION ALL
  SELECT 'sinvastatina-20mg', 110, 20
  UNION ALL
  SELECT 'levotiroxina-50mcg', 75, 15
  UNION ALL
  SELECT 'salbutamol-spray-100mcg', 35, 10
  UNION ALL
  SELECT 'vitamina-c-1g-efervescente', 130, 20
  UNION ALL
  SELECT 'vitamina-d3-2000-ui', 80, 15
  UNION ALL
  SELECT 'complexo-b', 65, 10
  UNION ALL
  SELECT 'omega-3-1000mg', 50, 10
  UNION ALL
  SELECT 'polivitaminico-a-z', 55, 10
  UNION ALL
  SELECT 'sabonete-liquido-neutro', 90, 15
  UNION ALL
  SELECT 'creme-dental-sensivel', 140, 20
  UNION ALL
  SELECT 'escova-dental-macia', 120, 20
  UNION ALL
  SELECT 'desodorante-roll-on-sem-alcool', 75, 10
  UNION ALL
  SELECT 'shampoo-anticaspa', 40, 10
  UNION ALL
  SELECT 'protetor-solar-fps-50', 60, 10
  UNION ALL
  SELECT 'protetor-solar-facial-fps-60-com-cor', 35, 8
  UNION ALL
  SELECT 'hidratante-corporal-karite', 70, 10
  UNION ALL
  SELECT 'agua-micelar', 4, 8
  UNION ALL
  SELECT 'pomada-para-assaduras', 80, 10
  UNION ALL
  SELECT 'fralda-infantil-tamanho-m', 50, 10
  UNION ALL
  SELECT 'paracetamol-gotas-infantil', 60, 10
  UNION ALL
  SELECT 'soro-fisiologico-0-9', 100, 15
  UNION ALL
  SELECT 'kit-curativos-variados', 110, 15
  UNION ALL
  SELECT 'alcool-70-gel', 130, 20
  UNION ALL
  SELECT 'gaze-esteril', 90, 15
  UNION ALL
  SELECT 'antisseptico-spray', 45, 10
  UNION ALL
  SELECT 'termometro-digital', 40, 8
  UNION ALL
  SELECT 'medidor-de-pressao-de-pulso', 15, 5
  UNION ALL
  SELECT 'oximetro-de-dedo', 20, 5
  UNION ALL
  SELECT 'tiras-para-glicemia', 30, 10
) AS e
JOIN medicamentos m ON m.slug = e.slug;

INSERT INTO cupons (codigo, descricao, tipo, valor, valor_minimo_pedido, data_fim) VALUES
  ('BEMESTAR10', '10% de desconto em todo o site', 'PERCENTUAL', 10.00, 20.00, '2027-12-31 23:59:59'),
  ('SAUDE15', '15% de desconto em compras acima de R$ 80', 'PERCENTUAL', 15.00, 80.00, '2027-12-31 23:59:59'),
  ('FRETEGRATIS', 'Frete grátis em compras acima de R$ 40', 'FRETE_GRATIS', 0.00, 40.00, '2027-12-31 23:59:59'),
  ('PRIMEIRACOMPRA', 'R$ 5,00 de desconto na primeira compra', 'VALOR_FIXO', 5.00, 15.00, '2027-12-31 23:59:59');
