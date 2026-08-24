import axios from 'axios';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || (import.meta.env.DEV ? '/' : 'http://localhost:5018'),
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
  | 'GoogleAds'
  | 'MetaAds';

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
  metaAds: { configurado: boolean; conectado: boolean; contaSelecionada: boolean; status: string };
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
  customerIdMascarado: string;
  nome: string;
  email?: string;
  ativa: boolean;
  padrao: boolean;
  tipoConta: string;
  gerente: boolean;
  dataConexao: string;
  accessTokenExpiraEm?: string;
}

export interface GoogleAdsOAuthCallback {
  sucesso: boolean;
  conectado: boolean;
  contasEncontradas: number;
  mensagem: string;
  contas: GoogleAdsConta[];
}

export interface GoogleAdsTeste {
  sucesso: boolean;
  status: string;
  customerId?: string;
  duracaoMs?: number;
  ambiente?: string;
  customerIdMascarado?: string;
  tokenRenovado: boolean;
  contaAcessivel: boolean;
  consultaExecutada: boolean;
  pendencias?: string[];
}

export interface GoogleAdsAmbiente {
  modo: string;
  customerIdMascarado?: string;
  contaCompativel: boolean;
  publicacaoPermitida: boolean;
  pendencias: string[];
}

export interface MetaAdsStatus {
  configurado: boolean;
  conectado: boolean;
  contaSelecionada: boolean;
  status: string;
  contaId?: string;
  metaUserId?: string;
  nome?: string;
  dataConexao?: string;
  accessTokenExpiraEm?: string;
}

export interface MetaAdsAuthUrl {
  url: string;
  state: string;
}

export interface MetaAdsOAuthCallback {
  sucesso: boolean;
  conectado: boolean;
  mensagem: string;
  status: MetaAdsStatus;
}

export interface MetaAdsAssetListResponse<T> {
  sucesso: boolean;
  itens: T[];
  mensagem?: string;
  permissaoNecessaria: boolean;
}

export interface MetaAdsBusiness { id: string; nome: string }
export interface MetaAdsAdAccount { id: string; accountId?: string; nome: string; status?: string; moeda?: string }
export interface MetaAdsInstagramAccount { id: string; nome?: string; username?: string }
export interface MetaAdsPage { id: string; nome: string; instagram?: MetaAdsInstagramAccount }
export interface MetaAdsPixel { id: string; nome: string }
export interface MetaAdsAssetSelection {
  id?: string;
  metaAdsContaId?: string;
  businessId?: string;
  businessNome?: string;
  adAccountId?: string;
  adAccountNome?: string;
  pageId?: string;
  pageNome?: string;
  instagramAccountId?: string;
  instagramNome?: string;
  pixelId?: string;
  pixelNome?: string;
  dataAtualizacao?: string;
}

export interface MetaAdsPreview {
  campanhaId: string;
  assets: {
    businessId?: string;
    businessNome?: string;
    adAccountId?: string;
    adAccountNome?: string;
    pageId?: string;
    pageNome?: string;
    instagramAccountId?: string;
    instagramNome?: string;
    pixelId?: string;
    pixelNome?: string;
  };
  campaign: {
    name: string;
    objective: string;
    status: string;
    specialAdCategory: string;
    specialAdCategories: string[];
  };
  adSet: {
    name: string;
    campaignObjective: string;
    dailyBudget: number;
    dailyBudgetMinorUnits?: number;
    currency?: string;
    billingEvent: string;
    optimizationGoal: string;
    bidStrategy: string;
    targeting: { countries: string[]; location?: MetaAdsLocation; regionText?: string; cityText?: string; ageMin: number; ageMax: number };
    startTime?: string;
    endTime?: string;
    pixelId?: string;
  };
  creative: {
    pageId?: string;
    instagramAccountId?: string;
    primaryText: string;
    headline: string;
    description: string;
    destinationUrl: string;
    callToAction: string;
    imageUrl?: string;
    mediaReference?: string;
    metaImageHash?: string;
    mediaUploaded: boolean;
  };
  ad: { name: string; status: string };
  preflight: { readyToPublish: boolean; items: Array<{ code: string; status: 'OK' | 'WARNING' | 'ERROR' | string; message: string }> };
}

export interface MetaAdsLocation {
  key: string;
  name: string;
  type: string;
  countryCode?: string;
  countryName?: string;
  region?: string;
  regionId?: string;
  supportsRegion: boolean;
  supportsCity: boolean;
}

export interface MetaAdsLocationSearchResponse {
  sucesso: boolean;
  itens: MetaAdsLocation[];
  mensagem?: string;
  permissaoNecessaria: boolean;
}

export interface MetaAdsUploadImageResponse {
  sucesso: boolean;
  imagemId?: string;
  nomeArquivo: string;
  contentType: string;
  tamanhoBytes?: number;
  contentHash: string;
  metaImageHash?: string;
  reutilizado: boolean;
  dataUpload?: string;
  mensagem: string;
}

export interface MetaAdsPublicacao {
  id: string;
  campanhaId: string;
  status: string;
  ultimaEtapaConcluida: string;
  campaignExternalId?: string;
  adSetExternalId?: string;
  creativeExternalId?: string;
  adExternalId?: string;
  dataInicio: string;
  dataConclusao?: string;
  dataAtualizacao?: string;
  ultimoErroCodigo?: string;
  ultimoErroSubcodigo?: string;
  ultimoErroMensagem?: string;
  fbTraceId?: string;
  podeTentarNovamente: boolean;
  mensagem: string;
}

export interface MetaAdsPublicationStatus {
  existe: boolean;
  publicacao?: MetaAdsPublicacao;
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
  campaign: Record<string, unknown> & { countryCode?: string; locationName?: string; geoTargetResourceName?: string };
  budget: Record<string, unknown>;
  adGroups: Array<{
    name: string;
    status: string;
    cpcBid?: number;
    cpcBidMicros?: number;
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

export type StatusGoogleAdsPublicacao = 'Preparada' | 'ValidandoRemotamente' | 'Validada' | 'Publicando' | 'ParcialmentePublicada' | 'Publicada' | 'Falhou' | 'RequerIntervencao' | 'Reconciliada';
export interface GoogleAdsPublicationError {
  codigo: string;
  mensagem: string;
  operacao?: string;
  indiceOperacao?: number;
  campo?: string;
  valorRejeitado?: string;
  requestId?: string;
  recuperavel: boolean;
  acaoSugerida?: string;
  location?: string;
  fieldPathElements?: string[];
  trigger?: string;
  statusCode?: string;
  detail?: string;
}
export interface GoogleAdsDiagnosticResponse {
  sucesso: boolean;
  codigo: string;
  mensagem: string;
  requestId?: string;
  erros: GoogleAdsPublicationError[];
  statusCode?: string;
  detail?: string;
  stackTrace?: string;
}
export interface GoogleAdsPublishedResource { tipoRecurso: string; resourceName: string; externalId?: string; nome?: string; status: string }
export interface GoogleAdsRemoteValidation { valido: boolean; sucesso?: boolean; codigo?: string; mensagem?: string; requestId?: string; erros: GoogleAdsPublicationError[]; avisos: string[]; dataValidacao: string; stackTrace?: string }
export interface GoogleAdsDryRun { operacoes: Array<{ indice: number; tipo: string; status: string; resourceNameTemporario?: string }>; quantidadeOperacoes: number; valido: boolean; erros: GoogleAdsPublicationError[]; avisos: string[] }
export interface GoogleAdsPreparePublication {
  publicacaoId: string; confirmationToken: string; nome: string; conta: string; customerIdMascarado: string; orcamentoDiario: number; quantidadeGrupos: number; quantidadeKeywords: number; quantidadeNegativas: number; quantidadeAnuncios: number; url: string; statusPlanejado: string; hash: string; versao: number; validacaoLocal: boolean; validacaoRemota: boolean; teste: boolean;
}
export interface GoogleAdsPublication {
  id: string; previewId: string; campanhaId: string; contaId: string; customerIdMascarado: string; previewVersao: number; previewHash: string; status: StatusGoogleAdsPublicacao; requestIdValidacao?: string; requestIdPublicacao?: string; erroCodigo?: string; erroMensagemControlada?: string; erros: GoogleAdsPublicationError[]; recursos: GoogleAdsPublishedResource[]; dataCriacao: string; dataAtualizacao?: string; teste: boolean;
}
export interface GoogleAdsPublicationHistory { id: string; statusAnterior?: StatusGoogleAdsPublicacao; statusNovo: StatusGoogleAdsPublicacao; operacao: string; mensagemControlada?: string; requestId?: string; data: string }
export interface GoogleAdsDashboardResumo { campanhasPublicadas: number; campanhasAtivas: number; campanhasPausadas: number; impressoes: number; cliques: number; ctr: number; custo: number; cpcMedio: number; conversoes: number; valorConversoes: number; leads: number; custoPorLead: number; taxaConversao: number; roas: number; ultimaSincronizacao?: string; qualidadeAtribuicao: string }
export interface GoogleAdsDashboardCampanha { publicacaoId: string; campanha: string; status: string; impressoes: number; cliques: number; ctr: number; custo: number; conversoes: number; leads: number; custoPorLead: number; ultimaSincronizacao?: string }
export interface GoogleAdsEvolucao { data: string; cliques: number; custo: number; conversoes: number; leads: number }
export interface GoogleAdsAnalise { id: string; publicacaoId: string; resumo: string; resultado: { diagnostico: string[]; pontosFortes: string[]; problemas: string[]; headlinesSugeridas: string[]; descriptionsSugeridas: string[]; keywordsSugeridas: string[]; negativasSugeridas: string[]; acoesPrioritarias: string[]; nivelConfianca: number }; aplicada: boolean; dataCriacao: string }

export async function obterGoogleAdsStatus(): Promise<GoogleAdsStatus> {
  const { data } = await api.get<GoogleAdsStatus>('/api/googleads/status');
  return data;
}

export async function obterGoogleAdsAmbiente(): Promise<GoogleAdsAmbiente> {
  const { data } = await api.get<GoogleAdsAmbiente>('/api/googleads/ambiente');
  return data;
}

export async function obterGoogleAdsAuthUrl(): Promise<GoogleAdsAuthUrl> {
  const { data } = await api.get<GoogleAdsAuthUrl>('/api/googleads/auth-url');
  return data;
}

export async function concluirGoogleAdsOAuth(payload: { code: string; state?: string }): Promise<GoogleAdsOAuthCallback> {
  const { data } = await api.post<GoogleAdsOAuthCallback>('/api/googleads/oauth/callback', payload);
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

export async function obterMetaAdsStatus(): Promise<MetaAdsStatus> {
  const { data } = await api.get<MetaAdsStatus>('/api/metaads/status');
  return data;
}

export async function obterMetaAdsAuthUrl(publicacao = false): Promise<MetaAdsAuthUrl> {
  const { data } = await api.get<MetaAdsAuthUrl>('/api/metaads/auth-url', { params: { publicacao } });
  return data;
}

export async function concluirMetaAdsOAuth(payload: { code: string; state?: string }): Promise<MetaAdsOAuthCallback> {
  const { data } = await api.post<MetaAdsOAuthCallback>('/api/metaads/oauth/callback', payload);
  return data;
}

export async function desconectarMetaAds(): Promise<MetaAdsStatus> {
  const { data } = await api.post<MetaAdsStatus>('/api/metaads/disconnect');
  return data;
}

export async function listarMetaAdsBusinesses(): Promise<MetaAdsAssetListResponse<MetaAdsBusiness>> {
  const { data } = await api.get<MetaAdsAssetListResponse<MetaAdsBusiness>>('/api/metaads/businesses');
  return data;
}

export async function listarMetaAdsAdAccounts(businessId: string): Promise<MetaAdsAssetListResponse<MetaAdsAdAccount>> {
  const { data } = await api.get<MetaAdsAssetListResponse<MetaAdsAdAccount>>(`/api/metaads/businesses/${encodeURIComponent(businessId)}/ad-accounts`);
  return data;
}

export async function listarMetaAdsPages(): Promise<MetaAdsAssetListResponse<MetaAdsPage>> {
  const { data } = await api.get<MetaAdsAssetListResponse<MetaAdsPage>>('/api/metaads/pages');
  return data;
}

export async function obterMetaAdsInstagram(pageId: string): Promise<MetaAdsAssetListResponse<MetaAdsInstagramAccount>> {
  const { data } = await api.get<MetaAdsAssetListResponse<MetaAdsInstagramAccount>>(`/api/metaads/pages/${encodeURIComponent(pageId)}/instagram`);
  return data;
}

export async function listarMetaAdsPixels(adAccountId: string): Promise<MetaAdsAssetListResponse<MetaAdsPixel>> {
  const { data } = await api.get<MetaAdsAssetListResponse<MetaAdsPixel>>(`/api/metaads/ad-accounts/${encodeURIComponent(adAccountId)}/pixels`);
  return data;
}

export async function obterMetaAdsAssetSelection(): Promise<MetaAdsAssetSelection> {
  const { data } = await api.get<MetaAdsAssetSelection>('/api/metaads/assets-selection');
  return data;
}

export async function salvarMetaAdsAssetSelection(payload: { businessId?: string; adAccountId?: string; pageId?: string; pixelId?: string }): Promise<MetaAdsAssetSelection> {
  const { data } = await api.put<MetaAdsAssetSelection>('/api/metaads/assets-selection', payload);
  return data;
}

export async function gerarMetaAdsPreview(payload: { campanhaId: string; specialAdCategory?: string; idadeMinima?: number; idadeMaxima?: number; locationKey?: string }): Promise<MetaAdsPreview> {
  const { data } = await api.post<MetaAdsPreview>('/api/metaads/preview', payload);
  return data;
}

export async function buscarMetaAdsLocalizacoes(query: string): Promise<MetaAdsLocationSearchResponse> {
  const { data } = await api.get<MetaAdsLocationSearchResponse>('/api/metaads/targeting/locations', { params: { query } });
  return data;
}

export async function salvarMetaAdsTargeting(payload: { campanhaId: string; locationKey: string; idadeMinima?: number; idadeMaxima?: number }): Promise<MetaAdsLocation> {
  const { data } = await api.put<MetaAdsLocation>('/api/metaads/publication-targeting', payload);
  return data;
}

export async function enviarMetaAdsImagem(campanhaId: string, file: File): Promise<MetaAdsUploadImageResponse> {
  const form = new FormData();
  form.append('file', file);
  const { data } = await api.post<MetaAdsUploadImageResponse>(`/api/metaads/campaigns/${campanhaId}/image`, form, { headers: { 'Content-Type': 'multipart/form-data' } });
  return data;
}

export async function obterMetaAdsPublicacao(campanhaId: string): Promise<MetaAdsPublicationStatus> {
  const { data } = await api.get<MetaAdsPublicationStatus>(`/api/metaads/campaigns/${campanhaId}/publication`);
  return data;
}

export async function publicarMetaAds(campanhaId: string): Promise<MetaAdsPublicacao> {
  const { data } = await api.post<MetaAdsPublicacao>(`/api/metaads/campaigns/${campanhaId}/publish`);
  return data;
}

export async function retentarMetaAdsPublicacao(id: string): Promise<MetaAdsPublicacao> {
  const { data } = await api.post<MetaAdsPublicacao>(`/api/metaads/publicacoes/${id}/retry`);
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

export async function dryRunGoogleAds(previewId: string): Promise<GoogleAdsDryRun> {
  const { data } = await api.post<GoogleAdsDryRun>(`/api/googleads/publicacoes/preview/${previewId}/dry-run`);
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

export async function ativarPublicacaoGoogleAds(id: string): Promise<GoogleAdsPublication> {
  const { data } = await api.post<GoogleAdsPublication>(`/api/googleads/publicacoes/${id}/ativar`, { confirmarAtivacaoEmContaTeste: true });
  return data;
}

export async function historicoPublicacaoGoogleAds(id: string): Promise<GoogleAdsPublicationHistory[]> {
  const { data } = await api.get<GoogleAdsPublicationHistory[]>(`/api/googleads/publicacoes/${id}/historico`);
  return data;
}

export async function obterGoogleAdsDashboard(params: Record<string, string | undefined> = {}): Promise<GoogleAdsDashboardResumo> {
  const { data } = await api.get<GoogleAdsDashboardResumo>('/api/googleads/dashboard', { params });
  return data;
}

export async function obterGoogleAdsDashboardCampanhas(params: Record<string, string | undefined> = {}): Promise<GoogleAdsDashboardCampanha[]> {
  const { data } = await api.get<GoogleAdsDashboardCampanha[]>('/api/googleads/dashboard/campanhas', { params });
  return data;
}

export async function obterGoogleAdsDashboardEvolucao(params: Record<string, string | undefined> = {}): Promise<GoogleAdsEvolucao[]> {
  const { data } = await api.get<GoogleAdsEvolucao[]>('/api/googleads/dashboard/evolucao', { params });
  return data;
}

export async function sincronizarGoogleAdsMetricas(dataInicial?: string, dataFinal?: string): Promise<unknown> {
  const { data } = await api.post('/api/googleads/metricas/sincronizar', { dataInicial, dataFinal });
  return data;
}

export async function analisarGoogleAdsPublicacao(id: string, dataInicial?: string, dataFinal?: string): Promise<GoogleAdsAnalise> {
  const { data } = await api.post<GoogleAdsAnalise>(`/api/googleads/publicacoes/${id}/analisar`, { dataInicial, dataFinal });
  return data;
}
