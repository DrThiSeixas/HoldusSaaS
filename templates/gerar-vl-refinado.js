const fs = require('fs');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, BorderStyle, WidthType, ShadingType,
  PageNumber
} = require('docx');

// ═══════════════════════════════════════════════════════════════
// HOLDUS — Visual Law Refinado
// "Facilitar o entendimento sem carregar"
//
// Filosofia: contrato formal + espaçamento generoso +
// hierarquia tipográfica + destaque sutil onde importa
// ═══════════════════════════════════════════════════════════════

const SOCIOS = [
  { nome: 'JOSÉ CARLOS SEIXAS', cpf: '___.___.___-__', nat: 'brasileiro', prof: 'empresário', ec: 'casado', regime: 'comunhão parcial de bens', conjuge: 'MARIA HELENA SEIXAS', rg: '__.___.___ SSP/SP', quotas: 500, pct: 50 },
  { nome: 'MARIA HELENA SEIXAS', cpf: '___.___.___-__', nat: 'brasileira', prof: 'do lar', ec: 'casada', regime: 'comunhão parcial de bens', conjuge: 'JOSÉ CARLOS SEIXAS', rg: '__.___.___ SSP/SP', quotas: 500, pct: 50 },
];

const EMP = {
  nome: 'SEIXAS PARTICIPAÇÕES E INVESTIMENTOS LTDA.',
  cnpj: '__.___.___/____-__',
  end: 'Rua ________________, nº ___, Bairro _________',
  cidade: 'São João da Boa Vista', uf: 'SP', cep: '_____-___',
  capital: 'R$ 1.000,00 (um mil reais)',
  quotas: '1.000 (um mil)',
};

// ═══════════════════════════════════════════════════════════════
// TEMPLATE VL-ELEGANTE (Pro) — sutil, espaçado, poucos destaques
// ═══════════════════════════════════════════════════════════════

function buildVLElegante() {
  const F = 'Lato';
  const C = { text: '1A1A2E', pri: '1B3A5C', acc: 'C9A84C', sec: '5D6D7E', line: 'D0D0D0', subtle: '999999', hdrBg: 'EBEDF0', totBg: 'F5F7FA', alertBg: 'FFF8F0', alertBorder: 'E8C76A' };
  const SZ = { title: 36, sub: 18, head: 18, cl: 24, body: 22, sm: 16 };
  const SP = { tight: 100, norm: 200, relax: 320, sec: 500 };
  const LN = 360; // entrelinhas mais generosa

  const t = (text, o = {}) => new TextRun({ text, font: F, size: o.s ?? SZ.body, bold: o.b, italics: o.i, color: o.c ?? C.text, characterSpacing: o.sp });
  const p = (runs, o = {}) => new Paragraph({
    spacing: { after: o.after ?? SP.norm, before: o.before ?? 0, line: LN },
    alignment: o.align ?? AlignmentType.JUSTIFIED,
    indent: o.indent ? { firstLine: 700 } : undefined,
    border: o.border, shading: o.shading,
    children: Array.isArray(runs) ? runs : [runs],
  });
  const div = () => new Paragraph({ spacing: { before: SP.relax, after: SP.relax }, border: { bottom: { style: BorderStyle.SINGLE, size: 3, color: C.acc, space: 1 } }, children: [] });
  const empty = () => new Paragraph({ spacing: { after: 80 }, children: [t('')] });

  // Destaque sutil — só borda lateral + fundo leve, sem ícone
  const destaque = (texto) => new Paragraph({
    spacing: { after: SP.norm, before: SP.norm },
    border: { left: { style: BorderStyle.SINGLE, size: 10, color: C.alertBorder, space: 10 } },
    shading: { fill: C.alertBg, type: ShadingType.CLEAR },
    indent: { left: 200 },
    children: [t(texto, { s: SZ.body - 1, c: C.pri })],
  });

  // Capítulo — limpo, centralizado, borda inferior sutil
  const cap = (num, titulo) => p(
    t(`CAPÍTULO ${num} — ${titulo}`, { s: SZ.head, b: true, c: C.pri, sp: 50 }),
    { align: AlignmentType.CENTER, before: SP.sec, after: SP.relax, border: { bottom: { style: BorderStyle.SINGLE, size: 1, color: C.line, space: 4 } } }
  );

  // Cláusula
  const cl = (num, texto) => [
    p(t(`CLÁUSULA ${num}`, { s: SZ.cl, b: true }), { after: SP.tight }),
    p(t(texto), { indent: true }),
  ];

  const par = (num, texto) => p([t(`§${num}º — `, { b: true, c: C.pri }), t(texto)], { indent: true });

  // Tabela
  function tabela() {
    const brd = { style: BorderStyle.SINGLE, size: 1, color: C.line };
    const brds = { top: brd, bottom: brd, left: brd, right: brd };
    const cm = { top: 60, bottom: 60, left: 100, right: 100 };
    const tw = 9306; const cols = [3200, 1800, 1500, 1200, 1606];
    const hc = (txt, w) => new TableCell({ borders: brds, width: { size: w, type: WidthType.DXA }, margins: cm, shading: { fill: C.hdrBg, type: ShadingType.CLEAR }, children: [new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [t(txt, { s: SZ.sm, b: true, c: C.pri })] })] });
    const dc = (txt, w, o = {}) => new TableCell({ borders: brds, width: { size: w, type: WidthType.DXA }, margins: cm, shading: o.hl ? { fill: C.totBg, type: ShadingType.CLEAR } : undefined, children: [new Paragraph({ spacing: { after: 0 }, alignment: o.al ?? AlignmentType.CENTER, children: [t(txt, { s: SZ.sm + 2, b: o.b })] })] });
    return new Table({
      width: { size: tw, type: WidthType.DXA }, columnWidths: cols,
      rows: [
        new TableRow({ children: [hc('SÓCIO', cols[0]), hc('CPF', cols[1]), hc('QUOTAS', cols[2]), hc('VALOR (R$)', cols[3]), hc('PARTICIPAÇÃO', cols[4])] }),
        ...SOCIOS.map(s => new TableRow({ children: [dc(s.nome.split(' ').slice(0,2).join(' '), cols[0], { al: AlignmentType.LEFT }), dc(s.cpf, cols[1]), dc(String(s.quotas), cols[2]), dc(`${s.quotas},00`, cols[3]), dc(`${s.pct}%`, cols[4])] })),
        new TableRow({ children: [dc('TOTAL', cols[0], { b: true, hl: true }), dc('', cols[1], { hl: true }), dc('1.000', cols[2], { b: true, hl: true }), dc('1.000,00', cols[3], { b: true, hl: true }), dc('100%', cols[4], { b: true, hl: true })] }),
      ]
    });
  }

  const assin = (nome, papel) => [empty(), empty(),
    p(t('_'.repeat(50), { c: C.line, s: SZ.sm }), { align: AlignmentType.CENTER, after: 40 }),
    p(t(nome, { b: true, s: SZ.sm, sp: 20 }), { align: AlignmentType.CENTER, after: 20 }),
    p(t(papel, { s: SZ.sm, c: C.sec }), { align: AlignmentType.CENTER, after: 0 }),
  ];

  return new Document({
    styles: { default: { document: { run: { font: F, size: SZ.body, color: C.text } } } },
    sections: [{
      properties: { page: { size: { width: 11906, height: 16838 }, margin: { top: 1600, right: 1400, bottom: 1200, left: 1400 } } },
      headers: { default: new Header({ children: [
        new Paragraph({ spacing: { after: 0 }, children: [t('THIAGO SEIXAS', { s: SZ.title - 8, b: true, c: C.pri })] }),
        new Paragraph({ spacing: { after: 0 }, children: [t('OAB/SP – 249.179', { s: SZ.sm, c: C.pri })] }),
        new Paragraph({ spacing: { after: 60 }, children: [t('Planejamento Patrimonial e Holding Familiar', { s: SZ.sm, c: C.pri, i: true })] }),
        new Paragraph({ spacing: { after: SP.relax }, border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: C.acc, space: 1 } }, children: [] }),
      ]}) },
      footers: { default: new Footer({ children: [
        new Paragraph({ spacing: { before: SP.norm, after: 40 }, border: { top: { style: BorderStyle.SINGLE, size: 2, color: C.acc, space: 1 } }, children: [] }),
        new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [
          t('Rua Coronel Lúcio, 1053 – Centro – Vargem Grande do Sul – SP  |  Página ', { s: SZ.sm, c: C.sec }),
          new TextRun({ children: [PageNumber.CURRENT], font: F, size: SZ.sm, color: C.sec }),
        ]}),
      ]}) },
      children: [
        // Título
        p(t('CONTRATO SOCIAL', { s: SZ.title, b: true, c: C.pri, sp: 40 }), { align: AlignmentType.CENTER, after: 40 }),
        p(t('DA SOCIEDADE EMPRESÁRIA LIMITADA', { s: SZ.sub, c: C.sec, sp: 80 }), { align: AlignmentType.CENTER }),
        p(t(EMP.nome, { s: SZ.title - 8, b: true }), { align: AlignmentType.CENTER, after: 40 }),
        p(t(`CNPJ/MF sob nº ${EMP.cnpj}`, { s: SZ.sub, c: C.sec }), { align: AlignmentType.CENTER }),
        div(),

        // Preâmbulo
        p(t('Pelo presente instrumento particular e na melhor forma de direito, os abaixo qualificados:')),
        empty(),
        ...SOCIOS.map((s, i) => p([
          t(s.nome, { b: true }), t(`, ${s.nat}, ${s.prof}, ${s.ec} sob o regime da ${s.regime} com `),
          t(s.conjuge, { b: true }), t(`, portador(a) da Cédula de Identidade RG nº ${s.rg}, inscrito(a) no CPF/MF sob nº ${s.cpf}, residente e domiciliado(a) na cidade de ${EMP.cidade}, Estado de ${EMP.uf}, na ${EMP.end}, CEP ${EMP.cep}${i < SOCIOS.length - 1 ? '; e' : ';'}`),
        ], { indent: true })),
        empty(),
        p(t('têm entre si, justo e contratado, a constituição de uma sociedade empresária limitada, que se regerá pelas cláusulas e condições seguintes e pelas disposições legais aplicáveis:')),

        // Capítulos
        cap('I', 'DO NOME EMPRESARIAL, DA SEDE E DAS FILIAIS'),
        ...cl('PRIMEIRA', `A sociedade girará sob o nome empresarial de ${EMP.nome}, com sede e foro na cidade de ${EMP.cidade}, Estado de ${EMP.uf}, na ${EMP.end}, CEP ${EMP.cep}.`),
        par('1', 'A sociedade poderá abrir filiais, agências, escritórios, depósitos ou representações em qualquer localidade do País ou do exterior, por deliberação da maioria do capital social.'),

        cap('II', 'DO OBJETO SOCIAL E DA DURAÇÃO'),
        ...cl('SEGUNDA', 'A sociedade tem por objeto social a participação em outras sociedades, na qualidade de sócia ou acionista (CNAE 6462-0/00), bem como a administração de bens próprios.'),
        par('1', 'A sociedade não exercerá atividade operacional própria, limitando-se à gestão de participações societárias e administração de bens de titularidade dos sócios a ela integralizados.'),
        ...cl('TERCEIRA', 'O prazo de duração da sociedade é por tempo indeterminado, iniciando suas atividades na data do registro deste instrumento na Junta Comercial competente.'),

        cap('III', 'DO CAPITAL SOCIAL E DAS QUOTAS'),
        ...cl('QUARTA', `O capital social é de ${EMP.capital}, dividido em ${EMP.quotas} quotas no valor nominal de R$ 1,00 (um real) cada uma, totalmente integralizado em moeda corrente nacional neste ato, e assim distribuído entre os sócios:`),
        tabela(),
        empty(),
        par('1', 'A responsabilidade de cada sócio é restrita ao valor de suas quotas, respondendo todos solidariamente pela integralização do capital social, nos termos do art. 1.052 do Código Civil.'),
        par('2', 'As quotas são indivisíveis e não poderão ser cedidas ou transferidas a terceiros sem o consentimento dos demais sócios, nos termos do art. 1.057 do Código Civil.'),

        cap('IV', 'DA ADMINISTRAÇÃO'),
        ...cl('QUINTA', `A administração da sociedade será exercida pelo sócio ${SOCIOS[0].nome}, que usará do nome empresarial, respondendo ativa e passivamente, judicial e extrajudicialmente, praticando todos os atos necessários à administração da sociedade.`),
        // ÚNICO DESTAQUE — vedação ao administrador (realmente importante)
        destaque('É vedado ao administrador obrigar a sociedade em operações estranhas ao objeto social, onerar ou alienar bens imóveis da sociedade, prestar aval, fiança ou qualquer outra garantia, sem prévia autorização da totalidade dos sócios.'),
        par('1', 'O administrador declara, sob as penas da lei, não estar impedido de exercer a administração da sociedade, por lei especial, ou em virtude de condenação criminal, ou por se encontrar sob os efeitos dela, a pena que vede, ainda que temporariamente, o acesso a cargos públicos, ou por crime falimentar, de prevaricação, peita ou suborno, concussão, peculato, ou contra a economia popular, contra o sistema financeiro nacional, contra normas de defesa da concorrência, contra as relações de consumo, fé pública, ou a propriedade.'),

        cap('V', 'DO PRÓ-LABORE'),
        ...cl('SEXTA', 'O administrador perceberá retirada mensal a título de pró-labore em valor a ser fixado pelos sócios, limitado ao montante equivalente ao piso estabelecido para a atividade, observados os limites legais aplicáveis.'),

        cap('VI', 'DO BALANÇO E DOS LUCROS'),
        ...cl('SÉTIMA', 'O exercício social encerra-se no dia 31 de dezembro de cada ano, quando será levantado o balanço patrimonial e o demonstrativo de resultados, cabendo aos sócios, na proporção de suas quotas, os lucros ou perdas apurados.'),
        par('1', 'Os lucros líquidos apurados poderão ser distribuídos aos sócios ou reinvestidos na sociedade, por deliberação dos sócios representando a maioria do capital social.'),

        cap('VII', 'DAS DELIBERAÇÕES E DA RETIRADA'),
        ...cl('OITAVA', 'As deliberações sociais serão tomadas por sócios que representem a maioria do capital social, ressalvadas as hipóteses de quórum qualificado previstas no art. 1.076 do Código Civil.'),
        ...cl('NONA', 'O sócio que desejar retirar-se da sociedade deverá notificar os demais com antecedência mínima de 60 (sessenta) dias, assegurado aos sócios remanescentes o direito de preferência na aquisição das quotas do retirante pelo valor patrimonial apurado em balanço.'),

        cap('VIII', 'DAS DISPOSIÇÕES GERAIS'),
        ...cl('DÉCIMA', 'No caso de falecimento de qualquer dos sócios, a sociedade não se dissolverá, sendo assegurado aos herdeiros e sucessores o ingresso na sociedade, nos termos do art. 1.028 do Código Civil.'),
        // SEGUNDO DESTAQUE — continuidade (pilar da estratégia)
        destaque('O ingresso dos herdeiros na sociedade ocorrerá de pleno direito, independentemente de anuência dos demais sócios, preservando-se a estrutura patrimonial e a continuidade da administração dos bens integralizados.'),
        ...cl('DÉCIMA PRIMEIRA', 'Os casos omissos serão regidos pelas disposições do Código Civil, Lei nº 10.406/2002, e demais legislações aplicáveis.'),

        cap('IX', 'DO FORO'),
        ...cl('DÉCIMA SEGUNDA', `Os sócios elegem o foro da Comarca de ${EMP.cidade}, Estado de ${EMP.uf}, para dirimir quaisquer dúvidas ou controvérsias oriundas deste contrato, com renúncia expressa a qualquer outro, por mais privilegiado que seja.`),

        // Fecho
        div(),
        p(t('E, por estarem assim justos e contratados, os sócios assinam o presente instrumento em 03 (três) vias de igual teor e forma, na presença de 02 (duas) testemunhas, para que surta seus jurídicos e legais efeitos, obrigando-se a levá-lo a registro na Junta Comercial do Estado de São Paulo — JUCESP.'), { indent: true }),
        empty(),
        p(t(`${EMP.cidade}/${EMP.uf}, ___ de ______________ de 2026.`), { align: AlignmentType.CENTER }),
        ...assin(SOCIOS[0].nome, 'Sócio-Administrador'),
        ...assin(SOCIOS[1].nome, 'Sócia'),
        empty(), empty(),
        p(t('Testemunhas:', { b: true, s: SZ.sm, c: C.pri })),
        ...assin('1. ____________________', 'Nome: / CPF:'),
        ...assin('2. ____________________', 'Nome: / CPF:'),
        empty(), empty(),
        div(),
        p(t('Elaborado por:', { s: SZ.sm, c: C.sec }), { align: AlignmentType.CENTER, after: 40 }),
        p(t('THIAGO SEIXAS — OAB/SP 249.179', { b: true, s: SZ.sm + 2, c: C.pri }), { align: AlignmentType.CENTER, after: 20 }),
        p(t('Thiago Seixas Advocacia Empresarial', { s: SZ.sm, c: C.sec, i: true }), { align: AlignmentType.CENTER }),
      ],
    }],
  });
}

// ═══════════════════════════════════════════════════════════════
// TEMPLATE VL-PREMIUM — Palatino, dourado, espaçamento premium,
// 3 destaques sutis, referências legais discretas
// ═══════════════════════════════════════════════════════════════

function buildVLPremium() {
  const F = 'Palatino Linotype';
  const C = { text: '1A1A2E', pri: '1B2A4A', acc: 'B8860B', sec: '5D6D7E', line: 'D5D5D5', subtle: '9E9E9E', hdrBg: 'ECEFF1', totBg: 'F5F5F5', alertBg: 'FFFBF0', alertBorder: 'D4A843', legalBg: 'F5F9F5', legalBorder: '8FBC8F' };
  const SZ = { title: 38, sub: 20, head: 20, cl: 24, body: 22, sm: 16, mini: 14 };
  const SP = { tight: 100, norm: 220, relax: 350, sec: 550 };
  const LN = 380; // entrelinhas ainda mais generosa

  const t = (text, o = {}) => new TextRun({ text, font: F, size: o.s ?? SZ.body, bold: o.b, italics: o.i, color: o.c ?? C.text, characterSpacing: o.sp });
  const p = (runs, o = {}) => new Paragraph({
    spacing: { after: o.after ?? SP.norm, before: o.before ?? 0, line: LN },
    alignment: o.align ?? AlignmentType.JUSTIFIED,
    indent: o.indent ? { firstLine: 700 } : undefined,
    border: o.border, shading: o.shading,
    children: Array.isArray(runs) ? runs : [runs],
  });
  const ddiv = () => new Paragraph({ spacing: { before: SP.relax, after: SP.relax }, border: { bottom: { style: BorderStyle.DOUBLE, size: 3, color: C.acc, space: 2 } }, children: [] });
  const sdiv = () => new Paragraph({ spacing: { before: SP.relax, after: SP.relax }, border: { bottom: { style: BorderStyle.SINGLE, size: 3, color: C.acc, space: 1 } }, children: [] });
  const empty = () => new Paragraph({ spacing: { after: 100 }, children: [t('')] });

  // Destaque sutil — borda lateral dourada
  const destaque = (texto) => new Paragraph({
    spacing: { after: SP.norm, before: SP.norm },
    border: { left: { style: BorderStyle.SINGLE, size: 10, color: C.alertBorder, space: 12 } },
    shading: { fill: C.alertBg, type: ShadingType.CLEAR },
    indent: { left: 200 },
    children: [t(texto, { s: SZ.body - 1, c: C.pri })],
  });

  // Referência legal discreta — menor, itálico, borda verde sutil
  const ref = (texto) => new Paragraph({
    spacing: { after: SP.tight, before: 40 },
    border: { left: { style: BorderStyle.SINGLE, size: 6, color: C.legalBorder, space: 8 } },
    shading: { fill: C.legalBg, type: ShadingType.CLEAR },
    indent: { left: 200 },
    children: [t(texto, { s: SZ.sm, c: '4A6A4A', i: true })],
  });

  const cap = (num, titulo) => p(
    t(`CAPÍTULO ${num} — ${titulo}`, { s: SZ.head, b: true, c: C.pri, sp: 50 }),
    { align: AlignmentType.CENTER, before: SP.sec, after: SP.relax, border: { bottom: { style: BorderStyle.SINGLE, size: 2, color: C.pri, space: 4 } } }
  );

  const cl = (num, texto) => [
    p(t(`CLÁUSULA ${num}`, { s: SZ.cl, b: true }), { after: SP.tight }),
    p(t(texto), { indent: true }),
  ];

  const par = (num, texto) => p([t(`§${num}º — `, { b: true, c: C.pri }), t(texto)], { indent: true });

  function tabela() {
    const brd = { style: BorderStyle.SINGLE, size: 1, color: C.line };
    const brds = { top: brd, bottom: brd, left: brd, right: brd };
    const cm = { top: 70, bottom: 70, left: 120, right: 120 };
    const tw = 9106; const cols = [3100, 1700, 1500, 1200, 1606];
    const hc = (txt, w) => new TableCell({ borders: brds, width: { size: w, type: WidthType.DXA }, margins: cm, shading: { fill: C.pri, type: ShadingType.CLEAR }, children: [new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [t(txt, { s: SZ.sm, b: true, c: 'FFFFFF' })] })] });
    const dc = (txt, w, o = {}) => new TableCell({ borders: brds, width: { size: w, type: WidthType.DXA }, margins: cm, shading: o.hl ? { fill: C.totBg, type: ShadingType.CLEAR } : undefined, children: [new Paragraph({ spacing: { after: 0 }, alignment: o.al ?? AlignmentType.CENTER, children: [t(txt, { s: SZ.sm + 2, b: o.b, c: o.c })] })] });
    return new Table({
      width: { size: tw, type: WidthType.DXA }, columnWidths: cols,
      rows: [
        new TableRow({ children: [hc('SÓCIO', cols[0]), hc('CPF', cols[1]), hc('QUOTAS', cols[2]), hc('VALOR (R$)', cols[3]), hc('PARTICIPAÇÃO', cols[4])] }),
        ...SOCIOS.map(s => new TableRow({ children: [dc(s.nome.split(' ').slice(0,2).join(' '), cols[0], { al: AlignmentType.LEFT }), dc(s.cpf, cols[1]), dc(String(s.quotas), cols[2]), dc(`${s.quotas},00`, cols[3]), dc(`${s.pct}%`, cols[4], { b: true, c: C.pri })] })),
        new TableRow({ children: [dc('TOTAL', cols[0], { b: true, hl: true }), dc('', cols[1], { hl: true }), dc('1.000', cols[2], { b: true, hl: true }), dc('1.000,00', cols[3], { b: true, hl: true }), dc('100%', cols[4], { b: true, hl: true, c: C.acc })] }),
      ]
    });
  }

  const assin = (nome, papel) => [empty(), empty(),
    p(t('_'.repeat(50), { c: C.line, s: SZ.sm }), { align: AlignmentType.CENTER, after: 40 }),
    p(t(nome, { b: true, s: SZ.sm, sp: 20 }), { align: AlignmentType.CENTER, after: 20 }),
    p(t(papel, { s: SZ.sm, c: C.sec }), { align: AlignmentType.CENTER, after: 0 }),
  ];

  return new Document({
    styles: { default: { document: { run: { font: F, size: SZ.body, color: C.text } } } },
    sections: [{
      properties: { page: { size: { width: 11906, height: 16838 }, margin: { top: 1800, right: 1500, bottom: 1300, left: 1500 } } },
      headers: { default: new Header({ children: [
        new Paragraph({ spacing: { after: SP.norm }, border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: C.acc, space: 1 } }, children: [] }),
        new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [t('THIAGO SEIXAS', { s: SZ.title - 4, b: true, c: C.pri, sp: 80 })] }),
        new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [t('OAB/SP – 249.179', { s: SZ.sm + 2, c: C.acc })] }),
        new Paragraph({ spacing: { after: 80 }, alignment: AlignmentType.CENTER, children: [t('Planejamento Patrimonial e Holding Familiar', { s: SZ.sm, c: C.sec, i: true })] }),
        new Paragraph({ spacing: { after: SP.relax }, border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: C.acc, space: 1 } }, children: [] }),
      ]}) },
      footers: { default: new Footer({ children: [
        new Paragraph({ spacing: { before: SP.norm, after: 40 }, border: { top: { style: BorderStyle.SINGLE, size: 3, color: C.acc, space: 1 } }, children: [] }),
        new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [
          t('Rua Coronel Lúcio, 1053 – Centro – Vargem Grande do Sul – SP  |  Página ', { s: SZ.sm, c: C.sec }),
          new TextRun({ children: [PageNumber.CURRENT], font: F, size: SZ.sm, color: C.sec }),
        ]}),
      ]}) },
      children: [
        // Título
        p(t('CONTRATO SOCIAL', { s: SZ.title, b: true, c: C.pri, sp: 60 }), { align: AlignmentType.CENTER, after: 40 }),
        p(t('DA SOCIEDADE EMPRESÁRIA LIMITADA', { s: SZ.sub, c: C.sec, sp: 100 }), { align: AlignmentType.CENTER }),
        empty(),
        p(t(EMP.nome, { s: SZ.title - 10, b: true }), { align: AlignmentType.CENTER, after: 40 }),
        p(t(`CNPJ/MF sob nº ${EMP.cnpj}`, { s: SZ.sub, c: C.sec }), { align: AlignmentType.CENTER }),
        ddiv(),

        // Preâmbulo
        p(t('Pelo presente instrumento particular e na melhor forma de direito, os abaixo qualificados:')),
        empty(),
        ...SOCIOS.map((s, i) => p([
          t(s.nome, { b: true }), t(`, ${s.nat}, ${s.prof}, ${s.ec} sob o regime da ${s.regime} com `),
          t(s.conjuge, { b: true }), t(`, portador(a) da Cédula de Identidade RG nº ${s.rg}, inscrito(a) no CPF/MF sob nº ${s.cpf}, residente e domiciliado(a) na cidade de ${EMP.cidade}, Estado de ${EMP.uf}, na ${EMP.end}, CEP ${EMP.cep}${i < SOCIOS.length - 1 ? '; e' : ';'}`),
        ], { indent: true })),
        empty(),
        p(t('têm entre si, justo e contratado, a constituição de uma sociedade empresária limitada, que se regerá pelas cláusulas e condições seguintes e pelas disposições legais aplicáveis:')),

        // Capítulos
        cap('I', 'DO NOME EMPRESARIAL, DA SEDE E DAS FILIAIS'),
        ...cl('PRIMEIRA', `A sociedade girará sob o nome empresarial de ${EMP.nome}, com sede e foro na cidade de ${EMP.cidade}, Estado de ${EMP.uf}, na ${EMP.end}, CEP ${EMP.cep}.`),
        par('1', 'A sociedade poderá abrir filiais, agências, escritórios, depósitos ou representações em qualquer localidade do País ou do exterior, por deliberação da maioria do capital social.'),

        cap('II', 'DO OBJETO SOCIAL E DA DURAÇÃO'),
        ...cl('SEGUNDA', 'A sociedade tem por objeto social a participação em outras sociedades, na qualidade de sócia ou acionista (CNAE 6462-0/00), bem como a administração de bens próprios.'),
        par('1', 'A sociedade não exercerá atividade operacional própria, limitando-se à gestão de participações societárias e administração de bens de titularidade dos sócios a ela integralizados.'),
        ...cl('TERCEIRA', 'O prazo de duração da sociedade é por tempo indeterminado, iniciando suas atividades na data do registro deste instrumento na Junta Comercial competente.'),

        cap('III', 'DO CAPITAL SOCIAL E DAS QUOTAS'),
        ...cl('QUARTA', `O capital social é de ${EMP.capital}, dividido em ${EMP.quotas} quotas no valor nominal de R$ 1,00 (um real) cada uma, totalmente integralizado em moeda corrente nacional neste ato, e assim distribuído entre os sócios:`),
        tabela(),
        empty(),
        ref('Art. 1.052, Código Civil — Responsabilidade restrita ao valor das quotas, solidariedade na integralização.'),
        par('1', 'As quotas são indivisíveis e não poderão ser cedidas ou transferidas a terceiros sem o consentimento dos demais sócios, nos termos do art. 1.057 do Código Civil.'),

        cap('IV', 'DA ADMINISTRAÇÃO'),
        ...cl('QUINTA', `A administração da sociedade será exercida pelo sócio ${SOCIOS[0].nome}, que usará do nome empresarial, respondendo ativa e passivamente, judicial e extrajudicialmente, praticando todos os atos necessários à administração da sociedade.`),
        // DESTAQUE 1 — vedação (crítico)
        destaque('É vedado ao administrador obrigar a sociedade em operações estranhas ao objeto social, onerar ou alienar bens imóveis da sociedade, prestar aval, fiança ou qualquer outra garantia, sem prévia autorização da totalidade dos sócios.'),
        par('1', 'O administrador declara, sob as penas da lei, não estar impedido de exercer a administração da sociedade, por lei especial, ou em virtude de condenação criminal, ou por se encontrar sob os efeitos dela, a pena que vede, ainda que temporariamente, o acesso a cargos públicos, ou por crime falimentar, de prevaricação, peita ou suborno, concussão, peculato, ou contra a economia popular, contra o sistema financeiro nacional, contra normas de defesa da concorrência, contra as relações de consumo, fé pública, ou a propriedade.'),

        cap('V', 'DO PRÓ-LABORE'),
        ...cl('SEXTA', 'O administrador perceberá retirada mensal a título de pró-labore em valor a ser fixado pelos sócios, limitado ao montante equivalente ao piso estabelecido para a atividade, observados os limites legais aplicáveis.'),

        cap('VI', 'DO BALANÇO E DOS LUCROS'),
        ...cl('SÉTIMA', 'O exercício social encerra-se no dia 31 de dezembro de cada ano, quando será levantado o balanço patrimonial e o demonstrativo de resultados, cabendo aos sócios, na proporção de suas quotas, os lucros ou perdas apurados.'),
        par('1', 'Os lucros líquidos apurados poderão ser distribuídos aos sócios ou reinvestidos na sociedade, por deliberação dos sócios representando a maioria do capital social.'),

        cap('VII', 'DAS DELIBERAÇÕES E DA RETIRADA'),
        ...cl('OITAVA', 'As deliberações sociais serão tomadas por sócios que representem a maioria do capital social, ressalvadas as hipóteses de quórum qualificado previstas no art. 1.076 do Código Civil.'),
        ...cl('NONA', 'O sócio que desejar retirar-se da sociedade deverá notificar os demais com antecedência mínima de 60 (sessenta) dias, assegurado aos sócios remanescentes o direito de preferência na aquisição das quotas do retirante pelo valor patrimonial apurado em balanço.'),

        cap('VIII', 'DAS DISPOSIÇÕES GERAIS'),
        ...cl('DÉCIMA', 'No caso de falecimento de qualquer dos sócios, a sociedade não se dissolverá, sendo assegurado aos herdeiros e sucessores o ingresso na sociedade, nos termos do art. 1.028 do Código Civil.'),
        // DESTAQUE 2 — continuidade (pilar)
        destaque('O ingresso dos herdeiros na sociedade ocorrerá de pleno direito, independentemente de anuência dos demais sócios, preservando-se a estrutura patrimonial e a continuidade da administração dos bens integralizados.'),
        ref('Art. 1.028, Código Civil — A sociedade não se dissolverá pelo falecimento de sócio, salvo disposição contratual em contrário.'),
        ...cl('DÉCIMA PRIMEIRA', 'Os casos omissos serão regidos pelas disposições do Código Civil, Lei nº 10.406/2002, e demais legislações aplicáveis.'),

        cap('IX', 'DO FORO'),
        ...cl('DÉCIMA SEGUNDA', `Os sócios elegem o foro da Comarca de ${EMP.cidade}, Estado de ${EMP.uf}, para dirimir quaisquer dúvidas ou controvérsias oriundas deste contrato, com renúncia expressa a qualquer outro, por mais privilegiado que seja.`),

        // Fecho
        ddiv(),
        p(t('E, por estarem assim justos e contratados, os sócios assinam o presente instrumento em 03 (três) vias de igual teor e forma, na presença de 02 (duas) testemunhas, para que surta seus jurídicos e legais efeitos, obrigando-se a levá-lo a registro na Junta Comercial do Estado de São Paulo — JUCESP.'), { indent: true }),
        empty(),
        p(t(`${EMP.cidade}/${EMP.uf}, ___ de ______________ de 2026.`), { align: AlignmentType.CENTER }),
        ...assin(SOCIOS[0].nome, 'Sócio-Administrador'),
        ...assin(SOCIOS[1].nome, 'Sócia'),
        empty(), empty(),
        p(t('Testemunhas:', { b: true, s: SZ.sm, c: C.pri })),
        ...assin('1. ____________________', 'Nome: / CPF:'),
        ...assin('2. ____________________', 'Nome: / CPF:'),
        empty(), empty(),
        ddiv(),
        p(t('Elaborado por:', { s: SZ.sm, c: C.sec }), { align: AlignmentType.CENTER, after: 40 }),
        p(t('THIAGO SEIXAS — OAB/SP 249.179', { b: true, s: SZ.sm + 2, c: C.pri }), { align: AlignmentType.CENTER, after: 20 }),
        p(t('Thiago Seixas Advocacia Empresarial', { s: SZ.sm, c: C.sec, i: true }), { align: AlignmentType.CENTER }),
      ],
    }],
  });
}

// ═══════════════════════════════════════════════════════════════
// GENERATE
// ═══════════════════════════════════════════════════════════════

async function generate() {
  const dir = '/home/claude/templates';

  const buf1 = await Packer.toBuffer(buildVLElegante());
  fs.writeFileSync(`${dir}/03-VL-Elegante.docx`, buf1);
  console.log(`✓ 03-VL-Elegante.docx (${(buf1.length / 1024).toFixed(1)} KB)`);

  const buf2 = await Packer.toBuffer(buildVLPremium());
  fs.writeFileSync(`${dir}/09-VL-Premium.docx`, buf2);
  console.log(`✓ 09-VL-Premium.docx (${(buf2.length / 1024).toFixed(1)} KB)`);
}

generate().catch(console.error);
