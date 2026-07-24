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
