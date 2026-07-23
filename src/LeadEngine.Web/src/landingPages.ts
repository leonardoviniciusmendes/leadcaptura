export interface LandingPageConfig {
  slug: string;
  titulo: string;
  subtitulo: string;
  tipoLeadPadrao: 'PessoaFisica' | 'Familia' | 'Mei' | 'Empresa';
  operadora?: string;
  campanha?: string;
  beneficios: string[];
}

export const landingPages: LandingPageConfig[] = [
  {
    slug: '/',
    titulo: 'Cotação de Plano de Saúde',
    subtitulo: 'Receba opções conforme sua região, perfil e quantidade de vidas.',
    tipoLeadPadrao: 'Familia',
    beneficios: ['Atendimento pelo WhatsApp', 'Comparação de operadoras', 'Sem compromisso']
  },
  {
    slug: '/plano-de-saude',
    titulo: 'Plano de Saúde Sob Medida',
    subtitulo: 'Compare alternativas individuais, familiares, MEI e empresariais.',
    tipoLeadPadrao: 'Familia',
    beneficios: ['Cotação personalizada', 'Orientação objetiva', 'Contato rápido']
  },
  {
    slug: '/plano-familiar',
    titulo: 'Plano de Saúde Familiar',
    subtitulo: 'Informe as idades e receba opções adequadas para sua família.',
    tipoLeadPadrao: 'Familia',
    campanha: 'plano_familiar',
    beneficios: ['Opções para várias idades', 'Rede hospitalar conforme preferência', 'Atendimento humano']
  },
  {
    slug: '/plano-individual',
    titulo: 'Plano de Saúde Individual',
    subtitulo: 'Encontre alternativas para sua necessidade e região.',
    tipoLeadPadrao: 'PessoaFisica',
    campanha: 'plano_individual',
    beneficios: ['Formulário rápido', 'Comparação clara', 'Contato pelo WhatsApp']
  },
  {
    slug: '/plano-empresarial',
    titulo: 'Plano de Saúde Empresarial',
    subtitulo: 'Cote planos para sócios, colaboradores e dependentes.',
    tipoLeadPadrao: 'Empresa',
    campanha: 'plano_empresarial',
    beneficios: ['Opções PME', 'Simulação por quantidade de vidas', 'Atendimento consultivo']
  },
  {
    slug: '/plano-mei',
    titulo: 'Plano de Saúde para MEI',
    subtitulo: 'Compare possibilidades para MEI e pequenas equipes.',
    tipoLeadPadrao: 'Mei',
    campanha: 'plano_mei',
    beneficios: ['Cotação simples', 'Planos por perfil empresarial', 'Sem envio automático de mensagens']
  },
  {
    slug: '/amil',
    titulo: 'Encontre o Plano Amil Ideal',
    subtitulo: 'Receba uma comparação personalizada conforme sua região e suas idades.',
    tipoLeadPadrao: 'Familia',
    operadora: 'Amil',
    campanha: 'amil',
    beneficios: ['Opções individuais, familiares e empresariais', 'Comparação de rede credenciada', 'Atendimento pelo WhatsApp']
  },
  {
    slug: '/bradesco-saude',
    titulo: 'Cotação Bradesco Saúde',
    subtitulo: 'Veja opções conforme perfil, região e quantidade de vidas.',
    tipoLeadPadrao: 'Empresa',
    operadora: 'Bradesco Saúde',
    campanha: 'bradesco_saude',
    beneficios: ['Foco em planos empresariais', 'Cotação por porte', 'Retorno pelo WhatsApp']
  },
  {
    slug: '/sulamerica-saude',
    titulo: 'Cotação SulAmérica Saúde',
    subtitulo: 'Receba alternativas para família, MEI ou empresa.',
    tipoLeadPadrao: 'Familia',
    operadora: 'SulAmerica',
    campanha: 'sulamerica_saude',
    beneficios: ['Comparação personalizada', 'Preferências de rede', 'Formulário direto']
  },
  {
    slug: '/unimed',
    titulo: 'Cotação Unimed',
    subtitulo: 'Informe seus dados e receba opções disponíveis para sua região.',
    tipoLeadPadrao: 'Familia',
    operadora: 'Unimed',
    campanha: 'unimed',
    beneficios: ['Atendimento regional', 'Opção familiar ou empresarial', 'Contato sem compromisso']
  }
];
