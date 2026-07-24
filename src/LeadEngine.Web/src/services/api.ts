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

export type CategoriaConfiguracao =
  | 'OpenRouter'
  | 'CampaignGeneration'
  | 'WhatsApp'
  | 'LeadCapture'
  | 'ExternalLeadApi'
  | 'Application'
  | 'Landing'
  | 'GoogleAds';

export interface ConfiguracaoItem {
  chave: string;
  valor?: string | null;
  sensivel: boolean;
  configurado: boolean;
  origem: 'Banco' | 'VariavelAmbiente' | 'AppSettings' | 'Padrao';
  descricao?: string;
}

export interface ConfiguracaoCategoria {
  categoria: CategoriaConfiguracao;
  configuracoes: ConfiguracaoItem[];
}

export interface ConfiguracoesStatus {
  openRouter: { configurado: boolean; status: string };
  geracaoIa: { configurado: boolean; status: string };
  whatsApp: { configurado: boolean; status: string };
  capturaLeads: { configurado: boolean; status: string };
  externalLeadApi: { configurado: boolean; status: string };
  urlPublica: { configurado: boolean; status: string };
  googleAds: { configurado: boolean; status: string };
  pendencias: string[];
}

export interface TesteConfiguracao {
  sucesso: boolean;
  status: string;
  modelo?: string;
  duracaoMs?: number;
  urlExemplo?: string;
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

export async function listarConfiguracoes(): Promise<ConfiguracaoCategoria[]> {
  const { data } = await api.get<ConfiguracaoCategoria[]>('/api/configuracoes');
  return data;
}

export async function obterConfiguracaoCategoria(categoria: CategoriaConfiguracao): Promise<ConfiguracaoCategoria> {
  const { data } = await api.get<ConfiguracaoCategoria>(`/api/configuracoes/${categoria}`);
  return data;
}

export async function salvarConfiguracaoCategoria(categoria: CategoriaConfiguracao, payload: Record<string, unknown>): Promise<ConfiguracaoCategoria> {
  const { data } = await api.put<ConfiguracaoCategoria>(`/api/configuracoes/${categoria}`, payload);
  return data;
}

export async function testarConfiguracao(categoria: CategoriaConfiguracao): Promise<TesteConfiguracao> {
  const { data } = await api.post<TesteConfiguracao>(`/api/configuracoes/${categoria}/testar`);
  return data;
}

export async function obterStatusConfiguracoes(): Promise<ConfiguracoesStatus> {
  const { data } = await api.get<ConfiguracoesStatus>('/api/configuracoes/status');
  return data;
}

export interface GoogleAdsStatus {
  conectado: boolean;
  status: string;
  contaPadraoId?: string;
  customerId?: string;
  nome?: string;
}

export interface GoogleAdsAuthUrl {
  url: string;
  state: string;
}

export interface GoogleAdsConta {
  id: string;
  customerId: string;
  nome: string;
  email?: string;
  ativa: boolean;
  padrao: boolean;
  dataConexao: string;
  accessTokenExpiraEm?: string;
}

export interface GoogleAdsTeste {
  sucesso: boolean;
  status: string;
  customerId?: string;
  duracaoMs?: number;
}

export type StatusGoogleAdsPreview = 'Rascunho' | 'Valido' | 'Invalido' | 'Desatualizado' | 'Publicado' | 'Erro';

export interface GoogleAdsPreview {
  id: string;
  campanhaId: string;
  googleAdsContaId: string;
  contaNome: string;
  customerId: string;
  nomeCampanha: string;
  objetivo?: string;
  status: StatusGoogleAdsPreview;
  tipoRede: string;
  orcamentoDiario: number;
  orcamentoMicros: number;
  codigoMoeda: string;
  idioma: string;
  pais: string;
  urlFinal: string;
  dataCriacao: string;
  dataAtualizacao?: string;
  dataValidacao?: string;
  versao: number;
  erros: string[];
  avisos: string[];
  desatualizado: boolean;
  payload: GoogleAdsPreviewPayload;
  contadores: { headlinesValidas: number; descriptionsValidas: number; keywords: number; negativas: number; erros: number; avisos: number };
}

export interface GoogleAdsPreviewPayload {
  campaign: Record<string, unknown>;
  budget: Record<string, unknown>;
  adGroups: Array<{
    name: string;
    status: string;
    cpcBid?: number;
    keywords: Array<{ text: string; matchType: string; status: string; origem: string }>;
    negativeKeywords: Array<{ text: string; matchType: string; origem: string }>;
    responsiveSearchAd: {
      headlines: string[];
      descriptions: string[];
      finalUrls: string[];
      path1: string;
      path2: string;
      status: string;
    };
  }>;
}

export interface GoogleAdsSuggestion {
  campo: string;
  indice: number;
  original: string;
  sugestao: string;
  limite: number;
}

export type StatusGoogleAdsPublicacao = 'Preparada' | 'ValidandoRemotamente' | 'Validada' | 'Publicando' | 'ParcialmentePublicada' | 'Publicada' | 'Falhou' | 'RequerIntervencao';
export interface GoogleAdsPublicationError { codigo: string; mensagem: string; operacao?: string; indiceOperacao?: number; campo?: string; valorRejeitado?: string; requestId?: string; recuperavel: boolean; acaoSugerida?: string }
export interface GoogleAdsPublishedResource { tipoRecurso: string; resourceName: string; externalId?: string; nome?: string; status: string }
export interface GoogleAdsRemoteValidation { valido: boolean; requestId?: string; erros: GoogleAdsPublicationError[]; avisos: string[]; dataValidacao: string }
export interface GoogleAdsPreparePublication {
  publicacaoId: string; confirmationToken: string; nome: string; conta: string; customerIdMascarado: string; orcamentoDiario: number; quantidadeGrupos: number; quantidadeKeywords: number; quantidadeNegativas: number; quantidadeAnuncios: number; url: string; statusPlanejado: string; hash: string; versao: number; validacaoLocal: boolean; validacaoRemota: boolean; teste: boolean;
}
export interface GoogleAdsPublication {
  id: string; previewId: string; campanhaId: string; contaId: string; customerIdMascarado: string; previewVersao: number; previewHash: string; status: StatusGoogleAdsPublicacao; requestIdValidacao?: string; requestIdPublicacao?: string; erroCodigo?: string; erroMensagemControlada?: string; erros: GoogleAdsPublicationError[]; recursos: GoogleAdsPublishedResource[]; dataCriacao: string; dataAtualizacao?: string; teste: boolean;
}

export async function obterGoogleAdsStatus(): Promise<GoogleAdsStatus> {
  const { data } = await api.get<GoogleAdsStatus>('/api/googleads/status');
  return data;
}

export async function obterGoogleAdsAuthUrl(): Promise<GoogleAdsAuthUrl> {
  const { data } = await api.get<GoogleAdsAuthUrl>('/api/googleads/auth-url');
  return data;
}

export async function concluirGoogleAdsOAuth(payload: { code: string; state?: string; redirectUri?: string }): Promise<GoogleAdsConta[]> {
  const { data } = await api.post<GoogleAdsConta[]>('/api/googleads/oauth/callback', payload);
  return data;
}

export async function listarGoogleAdsContas(): Promise<GoogleAdsConta[]> {
  const { data } = await api.get<GoogleAdsConta[]>('/api/googleads/contas');
  return data;
}

export async function selecionarGoogleAdsConta(id: string): Promise<GoogleAdsConta> {
  const { data } = await api.post<GoogleAdsConta>(`/api/googleads/contas/${id}/selecionar`);
  return data;
}

export async function testarGoogleAds(contaId?: string): Promise<GoogleAdsTeste> {
  const { data } = await api.post<GoogleAdsTeste>('/api/googleads/testar', { contaId });
  return data;
}

export async function gerarGoogleAdsPreview(campanhaId: string): Promise<GoogleAdsPreview> {
  const { data } = await api.post<GoogleAdsPreview>(`/api/googleads/preview/campanhas/${campanhaId}`);
  return data;
}

export async function obterGoogleAdsPreview(id: string): Promise<GoogleAdsPreview> {
  const { data } = await api.get<GoogleAdsPreview>(`/api/googleads/preview/${id}`);
  return data;
}

export async function obterGoogleAdsPreviewPorCampanha(campanhaId: string): Promise<GoogleAdsPreview> {
  const { data } = await api.get<GoogleAdsPreview>(`/api/googleads/preview/campanhas/${campanhaId}`);
  return data;
}

export async function validarGoogleAdsPreview(id: string): Promise<GoogleAdsPreview> {
  const { data } = await api.post<GoogleAdsPreview>(`/api/googleads/preview/${id}/validar`);
  return data;
}

export async function atualizarGoogleAdsPreview(id: string, payload: Record<string, unknown>): Promise<GoogleAdsPreview> {
  const { data } = await api.put<GoogleAdsPreview>(`/api/googleads/preview/${id}`, payload);
  return data;
}

export async function sugerirAjustesGoogleAdsPreview(id: string, campos: string[] = ['headlines', 'descriptions']): Promise<{ previewId: string; sugestoes: GoogleAdsSuggestion[] }> {
  const { data } = await api.post<{ previewId: string; sugestoes: GoogleAdsSuggestion[] }>(`/api/googleads/preview/${id}/sugerir-ajustes`, { campos });
  return data;
}

export async function aplicarSugestaoGoogleAdsPreview(id: string, payload: { campo: string; indice: number; sugestao: string }): Promise<GoogleAdsPreview> {
  const { data } = await api.post<GoogleAdsPreview>(`/api/googleads/preview/${id}/aplicar-sugestao`, payload);
  return data;
}

export async function obterPayloadGoogleAdsPreview(id: string): Promise<GoogleAdsPreviewPayload> {
  const { data } = await api.get<GoogleAdsPreviewPayload>(`/api/googleads/preview/${id}/payload`);
  return data;
}

export async function excluirGoogleAdsPreview(id: string): Promise<void> {
  await api.delete(`/api/googleads/preview/${id}`);
}

export async function validarRemotamenteGoogleAds(previewId: string): Promise<GoogleAdsRemoteValidation> {
  const { data } = await api.post<GoogleAdsRemoteValidation>(`/api/googleads/publicacoes/preview/${previewId}/validar-remotamente`);
  return data;
}

export async function prepararPublicacaoGoogleAds(previewId: string): Promise<GoogleAdsPreparePublication> {
  const { data } = await api.post<GoogleAdsPreparePublication>(`/api/googleads/publicacoes/preview/${previewId}/preparar`);
  return data;
}

export async function publicarGoogleAds(previewId: string, payload: { confirmationToken: string; confirmarCriacaoPausada: boolean }): Promise<GoogleAdsPublication> {
  const { data } = await api.post<GoogleAdsPublication>(`/api/googleads/publicacoes/preview/${previewId}/publicar`, payload);
  return data;
}

export async function obterPublicacaoGoogleAds(id: string): Promise<GoogleAdsPublication> {
  const { data } = await api.get<GoogleAdsPublication>(`/api/googleads/publicacoes/${id}`);
  return data;
}

export async function reconciliarPublicacaoGoogleAds(id: string): Promise<Record<string, unknown>> {
  const { data } = await api.post(`/api/googleads/publicacoes/${id}/reconciliar`);
  return data;
}
