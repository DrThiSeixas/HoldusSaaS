const fs = require('fs');
const {
  Document, Packer, Paragraph, TextRun, Table, TableRow, TableCell,
  Header, Footer, AlignmentType, BorderStyle, WidthType, ShadingType,
  PageBreak, PageNumber
} = require('docx');

// ═══════════════════════════════════════════════════════════════
// HOLDUS — 8 Templates de Contrato
//
// Start (2): Clássico, Moderno
// Pro (4):   + Institucional, Minimalista
// Premium (8): + Executivo, Editorial, Luxo, Artesanal
// ═══════════════════════════════════════════════════════════════

// ── Shared legal content ─────────────────────────────────────

const SOCIOS = [
  { nome: 'JOSÉ CARLOS SEIXAS', cpf: '___.___.___-__', nat: 'brasileiro', prof: 'empresário', ec: 'casado', regime: 'comunhão parcial de bens', conjuge: 'MARIA HELENA SEIXAS', rg: '__.___.___ SSP/SP', quotas: 500, pct: '50%' },
  { nome: 'MARIA HELENA SEIXAS', cpf: '___.___.___-__', nat: 'brasileira', prof: 'do lar', ec: 'casada', regime: 'comunhão parcial de bens', conjuge: 'JOSÉ CARLOS SEIXAS', rg: '__.___.___ SSP/SP', quotas: 500, pct: '50%' },
];

const EMPRESA = {
  nome: 'SEIXAS PARTICIPAÇÕES E INVESTIMENTOS LTDA.',
  cnpj: '__.___.___/____-__',
  endereco: 'Rua ________________, nº ___, Bairro _________',
  cidade: 'São João da Boa Vista',
  uf: 'SP',
  cep: '_____-___',
  capital: 'R$ 1.000,00 (um mil reais)',
  totalQuotas: '1.000 (um mil)',
  valorQuota: 'R$ 1,00 (um real)',
};

const CLAUSULAS = [
  { cap: 'DO NOME EMPRESARIAL, DA SEDE E DAS FILIAIS', num: 'PRIMEIRA', texto: `A sociedade girará sob o nome empresarial de ${EMPRESA.nome}, com sede e foro na cidade de ${EMPRESA.cidade}, Estado de ${EMPRESA.uf}, na ${EMPRESA.endereco}, CEP ${EMPRESA.cep}.`, paragrafo: 'A sociedade poderá abrir filiais, agências, escritórios, depósitos ou representações em qualquer localidade do País ou do exterior, por deliberação da maioria do capital social.' },
  { cap: 'DO OBJETO SOCIAL E DA DURAÇÃO', num: 'SEGUNDA', texto: 'A sociedade tem por objeto social a participação em outras sociedades, na qualidade de sócia ou acionista (CNAE 6462-0/00), bem como a administração de bens próprios.', paragrafo: 'A sociedade não exercerá atividade operacional própria, limitando-se à gestão de participações societárias e administração de bens de titularidade dos sócios a ela integralizados.' },
  { cap: null, num: 'TERCEIRA', texto: 'O prazo de duração da sociedade é por tempo indeterminado, iniciando suas atividades na data do registro deste instrumento na Junta Comercial competente.' },
  { cap: 'DO CAPITAL SOCIAL E DAS QUOTAS', num: 'QUARTA', texto: `O capital social é de ${EMPRESA.capital}, dividido em ${EMPRESA.totalQuotas} quotas no valor nominal de ${EMPRESA.valorQuota} cada uma, totalmente integralizado em moeda corrente nacional neste ato.`, hasTable: true, paragrafo: 'A responsabilidade de cada sócio é restrita ao valor de suas quotas, respondendo todos solidariamente pela integralização do capital social, nos termos do art. 1.052 do Código Civil.' },
  { cap: 'DA ADMINISTRAÇÃO', num: 'QUINTA', texto: `A administração da sociedade será exercida pelo sócio ${SOCIOS[0].nome}, que usará do nome empresarial, respondendo ativa e passivamente, judicial e extrajudicialmente, praticando todos os atos necessários à administração da sociedade.`, paragrafo: 'É vedado ao administrador obrigar a sociedade em operações estranhas ao objeto social, onerar ou alienar bens imóveis da sociedade, prestar aval, fiança ou qualquer outra garantia, sem prévia autorização da totalidade dos sócios.' },
  { cap: 'DO PRÓ-LABORE', num: 'SEXTA', texto: 'O administrador perceberá retirada mensal a título de pró-labore em valor a ser fixado pelos sócios, limitado ao montante equivalente ao piso estabelecido para a atividade, observados os limites legais aplicáveis.' },
  { cap: 'DO BALANÇO E DOS LUCROS', num: 'SÉTIMA', texto: 'O exercício social encerra-se no dia 31 de dezembro de cada ano, quando será levantado o balanço patrimonial e o demonstrativo de resultados, cabendo aos sócios, na proporção de suas quotas, os lucros ou perdas apurados.' },
  { cap: 'DAS DELIBERAÇÕES E DA RETIRADA', num: 'OITAVA', texto: 'As deliberações sociais serão tomadas por sócios que representem a maioria do capital social, ressalvadas as hipóteses de quórum qualificado previstas no art. 1.076 do Código Civil.' },
  { cap: null, num: 'NONA', texto: 'O sócio que desejar retirar-se da sociedade deverá notificar os demais com antecedência mínima de 60 (sessenta) dias, assegurado aos sócios remanescentes o direito de preferência na aquisição das quotas do retirante pelo valor patrimonial apurado em balanço.' },
  { cap: 'DAS DISPOSIÇÕES GERAIS', num: 'DÉCIMA', texto: 'No caso de falecimento de qualquer dos sócios, a sociedade não se dissolverá, sendo assegurado aos herdeiros e sucessores o ingresso na sociedade, nos termos do art. 1.028 do Código Civil.' },
  { cap: null, num: 'DÉCIMA PRIMEIRA', texto: 'Os casos omissos serão regidos pelas disposições do Código Civil, Lei nº 10.406/2002, e demais legislações aplicáveis.' },
  { cap: 'DO FORO', num: 'DÉCIMA SEGUNDA', texto: `Os sócios elegem o foro da Comarca de ${EMPRESA.cidade}, Estado de ${EMPRESA.uf}, para dirimir quaisquer dúvidas ou controvérsias oriundas deste contrato, com renúncia expressa a qualquer outro, por mais privilegiado que seja.` },
];

// ═══════════════════════════════════════════════════════════════
// TEMPLATE DEFINITIONS
// ═══════════════════════════════════════════════════════════════

const TEMPLATES = {
  classico: {
    name: '01-Classico',
    font: 'Lato',
    colors: { primary: '2C3E50', secondary: '555555', accent: 'C9A84C', line: 'CCCCCC', headerBg: 'EBEDF0', totalBg: 'F7F8FA', text: '1A1A2E' },
    sizes: { title: 36, subtitle: 18, heading: 18, clause: 24, body: 24, small: 16 },
    margins: { top: 1600, right: 1300, bottom: 1200, left: 1300 },
    headerStyle: 'text', // text-based letterhead
    brandName: 'THIAGO SEIXAS',
    brandOab: 'OAB/SP – 249.179',
    brandTagline: 'Planejamento Patrimonial e Holding Familiar',
  },
  moderno: {
    name: '02-Moderno',
    font: 'Calibri',
    colors: { primary: '1565C0', secondary: '616161', accent: '1565C0', line: 'E0E0E0', headerBg: 'E3F2FD', totalBg: 'F5F5F5', text: '212121' },
    sizes: { title: 40, subtitle: 20, heading: 20, clause: 24, body: 22, small: 16 },
    margins: { top: 1800, right: 1400, bottom: 1200, left: 1400 },
    headerStyle: 'bar',
    brandName: 'THIAGO SEIXAS',
    brandOab: 'OAB/SP – 249.179',
    brandTagline: 'Planejamento Patrimonial e Holding Familiar',
  },
  institucional: {
    name: '03-Institucional',
    font: 'Georgia',
    colors: { primary: '1B2A4A', secondary: '5D6D7E', accent: '1B2A4A', line: 'B0BEC5', headerBg: 'ECEFF1', totalBg: 'F5F7FA', text: '1B2A4A' },
    sizes: { title: 36, subtitle: 18, heading: 20, clause: 24, body: 24, small: 16 },
    margins: { top: 1700, right: 1200, bottom: 1200, left: 1200 },
    headerStyle: 'formal',
    brandName: 'THIAGO SEIXAS',
    brandOab: 'OAB/SP – 249.179',
    brandTagline: 'Advocacia Empresarial — Planejamento Patrimonial',
  },
  minimalista: {
    name: '04-Minimalista',
    font: 'Helvetica',
    colors: { primary: '333333', secondary: '888888', accent: '333333', line: 'E8E8E8', headerBg: 'FAFAFA', totalBg: 'F9F9F9', text: '222222' },
    sizes: { title: 32, subtitle: 18, heading: 18, clause: 22, body: 22, small: 16 },
    margins: { top: 2000, right: 1600, bottom: 1400, left: 1600 },
    headerStyle: 'minimal',
    brandName: 'THIAGO SEIXAS',
    brandOab: 'OAB/SP – 249.179',
    brandTagline: 'Planejamento Patrimonial',
  },
  executivo: {
    name: '05-Executivo',
    font: 'Cambria',
    colors: { primary: '0D1B2A', secondary: '415A77', accent: '778DA9', line: '415A77', headerBg: '0D1B2A', totalBg: 'E0E1DD', text: '1B263B' },
    sizes: { title: 38, subtitle: 20, heading: 20, clause: 24, body: 24, small: 16 },
    margins: { top: 1600, right: 1300, bottom: 1200, left: 1300 },
    headerStyle: 'dark',
    brandName: 'THIAGO SEIXAS',
    brandOab: 'OAB/SP – 249.179',
    brandTagline: 'Advocacia Empresarial',
  },
  editorial: {
    name: '06-Editorial',
    font: 'Garamond',
    colors: { primary: '2D2D2D', secondary: '6B6B6B', accent: '8B0000', line: 'D4D4D4', headerBg: 'FFF8F0', totalBg: 'FFF5EE', text: '2D2D2D' },
    sizes: { title: 40, subtitle: 20, heading: 20, clause: 26, body: 24, small: 16 },
    margins: { top: 1800, right: 1500, bottom: 1300, left: 1500 },
    headerStyle: 'editorial',
    brandName: 'THIAGO SEIXAS',
    brandOab: 'OAB/SP – 249.179',
    brandTagline: 'Planejamento Patrimonial e Holding Familiar',
  },
  luxo: {
    name: '07-Luxo',
    font: 'Palatino Linotype',
    colors: { primary: '1A1A2E', secondary: '4A4A5E', accent: 'B8860B', line: 'B8860B', headerBg: 'FDF8ED', totalBg: 'FFFDF5', text: '1A1A2E' },
    sizes: { title: 38, subtitle: 20, heading: 20, clause: 24, body: 24, small: 16 },
    margins: { top: 1800, right: 1400, bottom: 1300, left: 1400 },
    headerStyle: 'gold',
    brandName: 'THIAGO SEIXAS',
    brandOab: 'OAB/SP – 249.179',
    brandTagline: 'Planejamento Patrimonial e Holding Familiar',
  },
  artesanal: {
    name: '08-Artesanal',
    font: 'Book Antiqua',
    colors: { primary: '3E2723', secondary: '6D4C41', accent: '5D4037', line: 'BCAAA4', headerBg: 'EFEBE9', totalBg: 'FBE9E7', text: '3E2723' },
    sizes: { title: 36, subtitle: 18, heading: 20, clause: 24, body: 24, small: 16 },
    margins: { top: 1700, right: 1400, bottom: 1300, left: 1400 },
    headerStyle: 'warm',
    brandName: 'THIAGO SEIXAS',
    brandOab: 'OAB/SP – 249.179',
    brandTagline: 'Planejamento Patrimonial e Holding Familiar',
  },
};

// ═══════════════════════════════════════════════════════════════
// DOCUMENT BUILDER
// ═══════════════════════════════════════════════════════════════

function buildDocument(tmpl) {
  const t = tmpl;
  const LINE_SP = 340;
  const SP = { tight: 120, normal: 200, relaxed: 300, section: 500 };

  const txt = (text, opts = {}) => new TextRun({
    text, font: t.font, size: opts.size ?? t.sizes.body,
    bold: opts.bold ?? false, italics: opts.italics ?? false,
    color: opts.color ?? t.colors.text,
    characterSpacing: opts.spacing ?? undefined,
    allCaps: opts.caps ?? false,
  });

  const para = (runs, opts = {}) => new Paragraph({
    spacing: { after: opts.after ?? SP.normal, before: opts.before ?? 0, line: LINE_SP },
    alignment: opts.align ?? AlignmentType.JUSTIFIED,
    indent: opts.indent ? { firstLine: 720 } : undefined,
    border: opts.border ?? undefined,
    children: Array.isArray(runs) ? runs : [runs],
  });

  const divider = (color = t.colors.accent, size = 4) => new Paragraph({
    spacing: { before: SP.relaxed, after: SP.relaxed },
    border: { bottom: { style: BorderStyle.SINGLE, size, color, space: 1 } },
    children: [],
  });

  const emptyLine = () => new Paragraph({ spacing: { after: 0 }, children: [txt('')] });

  // ── HEADER ────────────────────────────────────────────

  function buildHeader() {
    const lines = [];

    if (t.headerStyle === 'text' || t.headerStyle === 'formal' || t.headerStyle === 'warm') {
      lines.push(new Paragraph({ spacing: { after: 0 }, children: [txt(t.brandName, { size: t.sizes.title - 8, bold: true, color: t.colors.primary })] }));
      lines.push(new Paragraph({ spacing: { after: 0 }, children: [txt(t.brandOab, { size: t.sizes.small, color: t.colors.primary })] }));
      lines.push(new Paragraph({ spacing: { after: 60 }, children: [txt(t.brandTagline, { size: t.sizes.small, color: t.colors.primary, italics: true })] }));
      lines.push(new Paragraph({ spacing: { after: SP.relaxed }, border: { bottom: { style: BorderStyle.SINGLE, size: 4, color: t.colors.accent, space: 1 } }, children: [] }));
    } else if (t.headerStyle === 'bar') {
      lines.push(new Paragraph({ spacing: { after: 0 }, shading: { fill: t.colors.primary, type: ShadingType.CLEAR }, children: [txt('  ', { size: 8 })] }));
      lines.push(new Paragraph({ spacing: { after: 0, before: 100 }, children: [txt(t.brandName, { size: t.sizes.title - 8, bold: true, color: t.colors.primary })] }));
      lines.push(new Paragraph({ spacing: { after: 0 }, children: [txt(`${t.brandOab}  |  ${t.brandTagline}`, { size: t.sizes.small, color: t.colors.secondary })] }));
      lines.push(new Paragraph({ spacing: { after: SP.normal }, children: [] }));
    } else if (t.headerStyle === 'minimal') {
      lines.push(new Paragraph({ spacing: { after: 0 }, children: [txt(t.brandName, { size: t.sizes.small + 2, bold: true, color: t.colors.primary, spacing: 100 })] }));
      lines.push(new Paragraph({ spacing: { after: SP.relaxed }, children: [txt(t.brandOab, { size: t.sizes.small, color: t.colors.secondary })] }));
    } else if (t.headerStyle === 'dark') {
      lines.push(new Paragraph({ spacing: { after: 40 }, children: [txt(t.brandName, { size: t.sizes.title - 6, bold: true, color: t.colors.primary })] }));
      lines.push(new Paragraph({ spacing: { after: 0 }, children: [txt(t.brandOab, { size: t.sizes.small, color: t.colors.accent })] }));
      lines.push(new Paragraph({ spacing: { after: 60 }, children: [txt(t.brandTagline, { size: t.sizes.small, color: t.colors.secondary, italics: true })] }));
      lines.push(new Paragraph({ spacing: { after: SP.relaxed }, border: { bottom: { style: BorderStyle.DOUBLE, size: 3, color: t.colors.primary, space: 1 } }, children: [] }));
    } else if (t.headerStyle === 'editorial') {
      lines.push(new Paragraph({ spacing: { after: SP.relaxed }, border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: t.colors.accent, space: 1 } }, children: [] }));
      lines.push(new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [txt(t.brandName, { size: t.sizes.title - 4, bold: true, color: t.colors.primary, spacing: 120 })] }));
      lines.push(new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [txt(t.brandOab, { size: t.sizes.small, color: t.colors.secondary })] }));
      lines.push(new Paragraph({ spacing: { after: 60 }, alignment: AlignmentType.CENTER, children: [txt(t.brandTagline, { size: t.sizes.small, color: t.colors.accent, italics: true })] }));
      lines.push(new Paragraph({ spacing: { after: SP.relaxed }, border: { bottom: { style: BorderStyle.SINGLE, size: 2, color: t.colors.accent, space: 1 } }, children: [] }));
    } else if (t.headerStyle === 'gold') {
      lines.push(new Paragraph({ spacing: { after: SP.normal }, border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: t.colors.accent, space: 1 } }, children: [] }));
      lines.push(new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [txt(t.brandName, { size: t.sizes.title - 2, bold: true, color: t.colors.primary, spacing: 80 })] }));
      lines.push(new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [txt(t.brandOab, { size: t.sizes.small + 2, color: t.colors.accent })] }));
      lines.push(new Paragraph({ spacing: { after: 80 }, alignment: AlignmentType.CENTER, children: [txt(t.brandTagline, { size: t.sizes.small, color: t.colors.secondary, italics: true })] }));
      lines.push(new Paragraph({ spacing: { after: SP.relaxed }, border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: t.colors.accent, space: 1 } }, children: [] }));
    }

    return new Header({ children: lines });
  }

  function buildFooter() {
    const fLines = [];
    fLines.push(new Paragraph({
      spacing: { before: SP.normal, after: 40 },
      border: { top: { style: BorderStyle.SINGLE, size: 2, color: t.colors.accent, space: 1 } },
      children: [],
    }));
    fLines.push(new Paragraph({
      spacing: { after: 0 }, alignment: AlignmentType.CENTER,
      children: [
        txt('Rua Coronel Lúcio, 1053 – Centro – Vargem Grande do Sul – SP', { size: t.sizes.small, color: t.colors.secondary }),
      ],
    }));
    fLines.push(new Paragraph({
      spacing: { after: 0 }, alignment: AlignmentType.CENTER,
      children: [
        txt('thi_seixas@hotmail.com', { size: t.sizes.small, color: t.colors.secondary }),
        txt('    |    Página ', { size: t.sizes.small, color: t.colors.secondary }),
        new TextRun({ children: [PageNumber.CURRENT], font: t.font, size: t.sizes.small, color: t.colors.secondary }),
      ],
    }));
    return new Footer({ children: fLines });
  }

  // ── TITLE ─────────────────────────────────────────────

  function buildTitle() {
    return [
      para(txt('CONTRATO SOCIAL', { size: t.sizes.title, bold: true, color: t.colors.primary, spacing: 40 }), { align: AlignmentType.CENTER, after: 40 }),
      para(txt('DA SOCIEDADE EMPRESÁRIA LIMITADA', { size: t.sizes.subtitle, color: t.colors.secondary, spacing: 80 }), { align: AlignmentType.CENTER, after: SP.normal }),
      para(txt(EMPRESA.nome, { size: t.sizes.title - 8, bold: true, color: t.colors.text }), { align: AlignmentType.CENTER, after: 40 }),
      para(txt(`CNPJ/MF sob nº ${EMPRESA.cnpj}`, { size: t.sizes.subtitle, color: t.colors.secondary }), { align: AlignmentType.CENTER }),
      divider(),
    ];
  }

  // ── PREAMBLE ──────────────────────────────────────────

  function buildPreamble() {
    const lines = [
      para(txt('Pelo presente instrumento particular e na melhor forma de direito, os abaixo qualificados:'), { after: SP.relaxed }),
    ];
    SOCIOS.forEach((s, i) => {
      lines.push(para([
        txt(s.nome, { bold: true }),
        txt(`, ${s.nat}, ${s.prof}, ${s.ec} sob o regime da ${s.regime} com `),
        txt(s.conjuge, { bold: true }),
        txt(`, portador(a) da Cédula de Identidade RG nº ${s.rg}, inscrito(a) no CPF/MF sob nº ${s.cpf}, residente e domiciliado(a) na cidade de ${EMPRESA.cidade}, Estado de ${EMPRESA.uf}, na ${EMPRESA.endereco}, CEP ${EMPRESA.cep}${i < SOCIOS.length - 1 ? '; e' : ';'}`),
      ], { indent: true }));
    });
    lines.push(para(txt('têm entre si, justo e contratado, a constituição de uma sociedade empresária limitada, que se regerá pelas cláusulas e condições seguintes e pelas disposições legais aplicáveis:'), { after: SP.relaxed }));
    return lines;
  }

  // ── CLAUSES ───────────────────────────────────────────

  function buildClauses() {
    const lines = [];
    let capNum = 0;

    CLAUSULAS.forEach(cl => {
      if (cl.cap) {
        capNum++;
        const romanos = ['I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X'];
        lines.push(para(
          txt(`CAPÍTULO ${romanos[capNum - 1] || capNum} — ${cl.cap}`, { size: t.sizes.heading, bold: true, color: t.colors.primary, spacing: 60 }),
          {
            align: AlignmentType.CENTER, before: SP.section, after: SP.relaxed,
            border: { bottom: { style: BorderStyle.SINGLE, size: 1, color: t.colors.line, space: 1 } }
          }
        ));
      }

      lines.push(para(txt(`CLÁUSULA ${cl.num}`, { size: t.sizes.clause, bold: true, color: t.colors.text }), { after: SP.tight }));
      lines.push(para(txt(cl.texto), { indent: true }));

      if (cl.hasTable) lines.push(buildTable());

      if (cl.paragrafo) {
        lines.push(para([
          txt('§1º — ', { bold: true, color: t.colors.primary }),
          txt(cl.paragrafo),
        ], { indent: true }));
      }
    });
    return lines;
  }

  // ── TABLE ─────────────────────────────────────────────

  function buildTable() {
    const border = { style: BorderStyle.SINGLE, size: 1, color: t.colors.line };
    const borders = { top: border, bottom: border, left: border, right: border };
    const cellMar = { top: 50, bottom: 50, left: 100, right: 100 };
    const tw = 9306; // A4 content width with margins
    const cols = [3200, 1800, 1500, 1200, 1606];

    const hCell = (text, w) => new TableCell({
      borders, width: { size: w, type: WidthType.DXA }, margins: cellMar,
      shading: { fill: t.colors.headerBg, type: ShadingType.CLEAR },
      children: [new Paragraph({ spacing: { after: 0 }, alignment: AlignmentType.CENTER, children: [txt(text, { size: t.sizes.small, bold: true, color: t.colors.primary })] })],
    });

    const dCell = (text, w, opts = {}) => new TableCell({
      borders, width: { size: w, type: WidthType.DXA }, margins: cellMar,
      shading: opts.hl ? { fill: t.colors.totalBg, type: ShadingType.CLEAR } : undefined,
      children: [new Paragraph({ spacing: { after: 0 }, alignment: opts.align ?? AlignmentType.CENTER, children: [txt(text, { size: t.sizes.small, bold: opts.bold })] })],
    });

    return new Table({
      width: { size: tw, type: WidthType.DXA }, columnWidths: cols,
      rows: [
        new TableRow({ children: [hCell('SÓCIO', cols[0]), hCell('CPF', cols[1]), hCell('QUOTAS', cols[2]), hCell('VALOR (R$)', cols[3]), hCell('PART.', cols[4])] }),
        ...SOCIOS.map(s => new TableRow({
          children: [dCell(s.nome.split(' ').slice(0, 2).join(' '), cols[0], { align: AlignmentType.LEFT }), dCell(s.cpf, cols[1]), dCell(String(s.quotas), cols[2]), dCell(`${s.quotas},00`, cols[3]), dCell(s.pct, cols[4])]
        })),
        new TableRow({
          children: [dCell('TOTAL', cols[0], { bold: true, hl: true }), dCell('', cols[1], { hl: true }), dCell('1.000', cols[2], { bold: true, hl: true }), dCell('1.000,00', cols[3], { bold: true, hl: true }), dCell('100%', cols[4], { bold: true, hl: true })]
        }),
      ]
    });
  }

  // ── CLOSING ───────────────────────────────────────────

  function buildClosing() {
    const assinatura = (nome, papel) => [
      emptyLine(), emptyLine(),
      para(txt('_'.repeat(50), { color: t.colors.line, size: t.sizes.small }), { align: AlignmentType.CENTER, after: 40 }),
      para(txt(nome, { bold: true, size: t.sizes.small, spacing: 20 }), { align: AlignmentType.CENTER, after: 20 }),
      para(txt(papel, { size: t.sizes.small, color: t.colors.secondary }), { align: AlignmentType.CENTER, after: 0 }),
    ];

    return [
      divider(),
      para(txt('E, por estarem assim justos e contratados, os sócios assinam o presente instrumento em 03 (três) vias de igual teor e forma, na presença de 02 (duas) testemunhas, para que surta seus jurídicos e legais efeitos, obrigando-se a levá-lo a registro na Junta Comercial do Estado de São Paulo — JUCESP.'), { indent: true }),
      emptyLine(),
      para(txt(`${EMPRESA.cidade}/${EMPRESA.uf}, ___ de ______________ de 2026.`), { align: AlignmentType.CENTER }),
      ...assinatura(SOCIOS[0].nome, 'Sócio-Administrador'),
      ...assinatura(SOCIOS[1].nome, 'Sócia'),
      emptyLine(), emptyLine(),
      para(txt('Testemunhas:', { bold: true, size: t.sizes.small, color: t.colors.primary })),
      ...assinatura('1. ____________________', 'Nome: / CPF:'),
      ...assinatura('2. ____________________', 'Nome: / CPF:'),
      emptyLine(), emptyLine(),
      divider(),
      para(txt('Elaborado por:', { size: t.sizes.small, color: t.colors.secondary }), { align: AlignmentType.CENTER, after: 40 }),
      para(txt(`${t.brandName} — ${t.brandOab}`, { bold: true, size: t.sizes.small + 2, color: t.colors.primary }), { align: AlignmentType.CENTER, after: 20 }),
      para(txt('Thiago Seixas Advocacia Empresarial', { size: t.sizes.small, color: t.colors.secondary, italics: true }), { align: AlignmentType.CENTER }),
    ];
  }

  // ── ASSEMBLE ──────────────────────────────────────────

  return new Document({
    styles: { default: { document: { run: { font: t.font, size: t.sizes.body, color: t.colors.text } } } },
    sections: [{
      properties: {
        page: { size: { width: 11906, height: 16838 }, margin: t.margins },
      },
      headers: { default: buildHeader() },
      footers: { default: buildFooter() },
      children: [
        ...buildTitle(),
        ...buildPreamble(),
        ...buildClauses(),
        ...buildClosing(),
      ],
    }],
  });
}

// ═══════════════════════════════════════════════════════════════
// GENERATE ALL 8 TEMPLATES
// ═══════════════════════════════════════════════════════════════

async function generateAll() {
  const outDir = '/home/claude/templates';
  if (!fs.existsSync(outDir)) fs.mkdirSync(outDir, { recursive: true });

  for (const [key, tmpl] of Object.entries(TEMPLATES)) {
    const doc = buildDocument(tmpl);
    const buffer = await Packer.toBuffer(doc);
    const filePath = `${outDir}/${tmpl.name}.docx`;
    fs.writeFileSync(filePath, buffer);
    console.log(`✓ ${tmpl.name}.docx (${(buffer.length / 1024).toFixed(1)} KB)`);
  }

  console.log('\n═══ Todos os 8 templates gerados ═══');
}

generateAll().catch(console.error);
