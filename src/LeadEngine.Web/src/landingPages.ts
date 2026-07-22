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
    titulo: 'Cotacao de Plano de Saude',
    subtitulo: 'Receba opcoes conforme sua regiao, perfil e quantidade de vidas.',
    tipoLeadPadrao: 'Familia',
    beneficios: ['Atendimento pelo WhatsApp', 'Comparacao de operadoras', 'Sem compromisso']
  },
  {
    slug: '/plano-de-saude',
    titulo: 'Plano de Saude Sob Medida',
    subtitulo: 'Compare alternativas individuais, familiares, MEI e empresariais.',
    tipoLeadPadrao: 'Familia',
    beneficios: ['Cotacao personalizada', 'Orientacao objetiva', 'Contato rapido']
  },
  {
    slug: '/plano-familiar',
    titulo: 'Plano de Saude Familiar',
    subtitulo: 'Informe as idades e receba opcoes adequadas para sua familia.',
    tipoLeadPadrao: 'Familia',
    campanha: 'plano_familiar',
    beneficios: ['Opcoes para varias idades', 'Rede hospitalar conforme preferencia', 'Atendimento humano']
  },
  {
    slug: '/plano-individual',
    titulo: 'Plano de Saude Individual',
    subtitulo: 'Encontre alternativas para sua necessidade e regiao.',
    tipoLeadPadrao: 'PessoaFisica',
    campanha: 'plano_individual',
    beneficios: ['Formulario rapido', 'Comparacao clara', 'Contato pelo WhatsApp']
  },
  {
    slug: '/plano-empresarial',
    titulo: 'Plano de Saude Empresarial',
    subtitulo: 'Cote planos para socios, colaboradores e dependentes.',
    tipoLeadPadrao: 'Empresa',
    campanha: 'plano_empresarial',
    beneficios: ['Opcoes PME', 'Simulacao por quantidade de vidas', 'Atendimento consultivo']
  },
  {
    slug: '/plano-mei',
    titulo: 'Plano de Saude para MEI',
    subtitulo: 'Compare possibilidades para MEI e pequenas equipes.',
    tipoLeadPadrao: 'Mei',
    campanha: 'plano_mei',
    beneficios: ['Cotacao simples', 'Planos por perfil empresarial', 'Sem envio automatico de mensagens']
  },
  {
    slug: '/amil',
    titulo: 'Encontre o Plano Amil Ideal',
    subtitulo: 'Receba uma comparacao personalizada conforme sua regiao e suas idades.',
    tipoLeadPadrao: 'Familia',
    operadora: 'Amil',
    campanha: 'amil',
    beneficios: ['Opcoes individuais, familiares e empresariais', 'Comparacao de rede credenciada', 'Atendimento pelo WhatsApp']
  },
  {
    slug: '/bradesco-saude',
    titulo: 'Cotacao Bradesco Saude',
    subtitulo: 'Veja opcoes conforme perfil, regiao e quantidade de vidas.',
    tipoLeadPadrao: 'Empresa',
    operadora: 'Bradesco Saude',
    campanha: 'bradesco_saude',
    beneficios: ['Foco em planos empresariais', 'Cotacao por porte', 'Retorno pelo WhatsApp']
  },
  {
    slug: '/sulamerica-saude',
    titulo: 'Cotacao SulAmerica Saude',
    subtitulo: 'Receba alternativas para familia, MEI ou empresa.',
    tipoLeadPadrao: 'Familia',
    operadora: 'SulAmerica',
    campanha: 'sulamerica_saude',
    beneficios: ['Comparacao personalizada', 'Preferencias de rede', 'Formulario direto']
  },
  {
    slug: '/unimed',
    titulo: 'Cotacao Unimed',
    subtitulo: 'Informe seus dados e receba opcoes disponiveis para sua regiao.',
    tipoLeadPadrao: 'Familia',
    operadora: 'Unimed',
    campanha: 'unimed',
    beneficios: ['Atendimento regional', 'Opcao familiar ou empresarial', 'Contato sem compromisso']
  }
];
