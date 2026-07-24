import axios from 'axios';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5018',
  timeout: 15000
});

export type TipoPublicoCampanha = 'Individual' | 'Casal' | 'Familia' | 'Mei' | 'Empresa';
export type StatusCampanha = 'Rascunho' | 'Gerando' | 'Gerada' | 'Revisada' | 'Publicada' | 'Pausada' | 'Erro';
export type CampanhaSecao =
  | 'Nome'
  | 'LandingPage'
  | 'MensagemWhatsApp'
  | 'Beneficios'
  | 'PerguntasFrequentes'
  | 'PalavrasChave'
  | 'PalavrasChaveNegativas'
  | 'TitulosAnuncios'
  | 'DescricoesAnuncios';
export type OrigemRevisaoCampanha = 'Manual' | 'InteligenciaArtificial';

export interface GerarCampanhaRequest {
  tipoPublico: TipoPublicoCampanha;
  cidade: string;
  estado: string;
  regiao?: string;
  operadora: string;
  operadoraOutra?: string;
  orcamentoDiario: number;
  objetivo?: string;
}

export interface RevisarCampanhaRequest {
  nome: string;
  tituloLandingPage: string;
  subtituloLandingPage: string;
  textoBotao: string;
  mensagemWhatsApp: string;
  beneficios: string[];
  perguntasFrequentes: Array<{ pergunta: string; resposta: string }>;
  palavrasChave: string[];
  palavrasChaveNegativas: string[];
  titulosAnuncios: string[];
  descricoesAnuncios: string[];
}

export interface Campanha {
  id: string;
  nome: string;
  tipoPublico: TipoPublicoCampanha;
  cidade: string;
  estado: string;
  regiao?: string;
  operadora: string;
  orcamentoDiario: number;
  objetivo?: string;
  status: StatusCampanha;
  tituloLandingPage: string;
  subtituloLandingPage: string;
  textoBotao: string;
  mensagemWhatsApp: string;
  slug: string;
  beneficios: string[];
  perguntasFrequentes: Array<{ pergunta: string; resposta: string }>;
  palavrasChave: string[];
  palavrasChaveNegativas: string[];
  titulosAnuncios: string[];
  descricoesAnuncios: string[];
  erroGeracao?: string;
  providerIa?: string;
  modeloIa?: string;
  dataGeracao?: string;
  duracaoGeracaoMs?: number;
  dataCriacao: string;
  dataAtualizacao?: string;
  publicada: boolean;
  ativo: boolean;
  dataPublicacao?: string;
  dataDespublicacao?: string;
  urlPublica?: string;
}

export interface CampanhaPublicacao {
  id: string;
  status: StatusCampanha;
  publicada: boolean;
  ativo: boolean;
  dataPublicacao?: string;
  dataDespublicacao?: string;
  slugPublico?: string;
  urlPublica?: string;
}

export interface CampanhaPublica {
  nome: string;
  titulo: string;
  subtitulo: string;
  textoBotao: string;
  beneficios: string[];
  perguntasFrequentes: Array<{ pergunta: string; resposta: string }>;
  operadora: string;
  cidade: string;
  estado: string;
  tipoPublico: TipoPublicoCampanha;
  mensagemBaseWhatsApp: string;
}

export type TipoContratacaoLead = 'Individual' | 'Familiar' | 'Empresarial' | 'Mei' | 'AindaNaoSei';

export interface CapturarLeadPublicoRequest {
  nome: string;
  telefone: string;
  email?: string;
  cidade: string;
  estado: string;
  quantidadeVidas: number;
  tipoContratacao: TipoContratacaoLead;
  observacao?: string;
  consentimento: boolean;
  website?: string;
  formOpenedAt?: number;
  utmSource?: string;
  utmMedium?: string;
  utmCampaign?: string;
  utmTerm?: string;
  utmContent?: string;
  gclid?: string;
  fbclid?: string;
}

export interface CapturarLeadPublicoResponse {
  leadId: string;
  mensagem: string;
  whatsAppUrl: string;
}

export interface Lead {
  id: string;
  campanhaId?: string;
  campanhaNome?: string;
  tipoContratacao?: TipoContratacaoLead;
  status: string;
  nome: string;
  whatsAppMascarado: string;
  emailMascarado?: string;
  cidade?: string;
  uf?: string;
  quantidadeVidas?: number;
  origem?: string;
  utmCampaign?: string;
  criadoEm: string;
  statusEnvioExterno?: string;
}

export interface HistoricoRevisao {
  data: string;
  secao?: CampanhaSecao;
  origem: OrigemRevisaoCampanha;
  resumoAlteracao: string;
  provider?: string;
  modelo?: string;
}

export async function gerarCampanha(payload: GerarCampanhaRequest): Promise<Campanha> {
  const { data } = await api.post<Campanha>('/api/campanhas/gerar', payload);
  return data;
}

export async function listarCampanhas(): Promise<Campanha[]> {
  const { data } = await api.get<Campanha[]>('/api/campanhas');
  return data;
}

export async function obterCampanha(id: string): Promise<Campanha> {
  const { data } = await api.get<Campanha>(`/api/campanhas/${id}`);
  return data;
}

export async function obterRevisaoCampanha(id: string): Promise<Campanha> {
  const { data } = await api.get<Campanha>(`/api/campanhas/${id}/revisao`);
  return data;
}

export async function revisarCampanha(id: string, payload: RevisarCampanhaRequest): Promise<Campanha> {
  const { data } = await api.put<Campanha>(`/api/campanhas/${id}/revisao`, payload);
  return data;
}

export async function regenerarCampanhaSecao(id: string, secao: CampanhaSecao, instrucaoAdicional?: string): Promise<Campanha> {
  const { data } = await api.post<Campanha>(`/api/campanhas/${id}/regenerar`, { secao, instrucaoAdicional });
  return data;
}

export async function aprovarCampanha(id: string): Promise<Campanha> {
  const { data } = await api.post<Campanha>(`/api/campanhas/${id}/aprovar`);
  return data;
}

export async function listarHistoricoRevisoes(id: string): Promise<HistoricoRevisao[]> {
  const { data } = await api.get<HistoricoRevisao[]>(`/api/campanhas/${id}/historico-revisoes`);
  return data;
}

export async function publicarCampanha(id: string): Promise<CampanhaPublicacao> {
  const { data } = await api.post<CampanhaPublicacao>(`/api/campanhas/${id}/publicar`);
  return data;
}

export async function despublicarCampanha(id: string): Promise<CampanhaPublicacao> {
  const { data } = await api.post<CampanhaPublicacao>(`/api/campanhas/${id}/despublicar`);
  return data;
}

export async function obterPublicacaoCampanha(id: string): Promise<CampanhaPublicacao> {
  const { data } = await api.get<CampanhaPublicacao>(`/api/campanhas/${id}/publicacao`);
  return data;
}

export async function obterCampanhaPublica(slug: string): Promise<CampanhaPublica> {
  const { data } = await api.get<CampanhaPublica>(`/api/publico/campanhas/${slug}`);
  return data;
}

export async function capturarLeadPublico(slug: string, payload: CapturarLeadPublicoRequest): Promise<CapturarLeadPublicoResponse> {
  const { data } = await api.post<CapturarLeadPublicoResponse>(`/api/publico/campanhas/${slug}/leads`, payload);
  return data;
}

export async function listarLeads(params: Record<string, string | number | undefined> = {}): Promise<{ itens: Lead[]; total: number; pagina: number; tamanhoPagina: number }> {
  const { data } = await api.get('/api/leads', { params });
  return data;
}

export async function obterLead(id: string): Promise<Record<string, unknown>> {
  const { data } = await api.get(`/api/leads/${id}`);
  return data;
}
