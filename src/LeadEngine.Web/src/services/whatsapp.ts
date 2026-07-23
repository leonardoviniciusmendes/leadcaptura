import type { TipoLead } from '../components/forms';

export interface WhatsAppLeadPayload {
  tipo: TipoLead;
  nome: string;
  whatsApp: string;
  email?: string;
  cep?: string;
  bairro?: string;
  cidade?: string;
  nomeEmpresa?: string;
  cnpj?: string;
  quantidadeVidas?: number;
  quantidadeFuncionarios?: number;
  idades?: number[];
  hospitalDesejado?: string;
  operadoraDesejada?: string;
  possuiPlanoAtual?: boolean;
  planoAtual?: string;
  consentimentoContato?: boolean;
  honeypot?: string;
  origem?: Record<string, string | undefined>;
}

export interface WhatsAppLeadResponse {
  sucesso: boolean;
  leadId: string;
  mensagem: string;
}

const corretorWhatsApp = (import.meta.env.VITE_CORRETOR_WHATSAPP || '').replace(/\D/g, '');

function line(label: string, value: unknown): string | null {
  if (value === undefined || value === null || value === '') return null;
  if (Array.isArray(value) && value.length === 0) return null;
  return `${label}: ${Array.isArray(value) ? value.join(', ') : value}`;
}

function yesNo(value?: boolean): string {
  return value ? 'Sim' : 'Não';
}

function buildMessage(payload: WhatsAppLeadPayload): string {
  const origem = payload.origem || {};
  const lines = [
    '*Nova solicitação de cotação*',
    line('Tipo', payload.tipo),
    line('Nome', payload.nome),
    line('WhatsApp do cliente', payload.whatsApp),
    line('E-mail', payload.email),
    line('CEP', payload.cep),
    line('Bairro', payload.bairro),
    line('Cidade', payload.cidade),
    line('Empresa', payload.nomeEmpresa),
    line('CNPJ', payload.cnpj),
    line('Quantidade de vidas', payload.quantidadeVidas),
    line('Funcionários', payload.quantidadeFuncionarios),
    line('Idades', payload.idades),
    line('Hospital desejado', payload.hospitalDesejado),
    line('Operadora desejada', payload.operadoraDesejada),
    line('Possui plano atual', yesNo(payload.possuiPlanoAtual)),
    line('Plano atual', payload.planoAtual),
    '',
    '*Origem*',
    line('Pagina', origem.landingPage || window.location.pathname),
    line('Campanha', origem.utmCampaign),
    line('Termo', origem.utmTerm),
    line('GCLID', origem.gclid)
  ].filter((item): item is string => item !== null);

  return lines.join('\n');
}

export function enviarLeadParaWhatsApp(payload: WhatsAppLeadPayload): WhatsAppLeadResponse {
  if (payload.honeypot) {
    return {
      sucesso: true,
      leadId: '',
      mensagem: 'Solicitacao recebida.'
    };
  }

  if (!corretorWhatsApp) {
    throw new Error('VITE_CORRETOR_WHATSAPP não configurado.');
  }

  const url = `https://wa.me/${corretorWhatsApp}?text=${encodeURIComponent(buildMessage(payload))}`;
  window.location.href = url;

  return {
    sucesso: true,
    leadId: `whatsapp-${Date.now()}`,
    mensagem: 'Abrindo WhatsApp para enviar a solicitação.'
  };
}
