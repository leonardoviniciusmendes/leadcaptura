import { createRouter, createWebHistory } from 'vue-router';
import CampanhasView from './views/CampanhasView.vue';
import CampanhaRevisaoView from './views/CampanhaRevisaoView.vue';
import LeadsView from './views/LeadsView.vue';
import NovaCampanhaView from './views/NovaCampanhaView.vue';
import PublicLandingView from './views/PublicLandingView.vue';
import DashboardView from './views/DashboardView.vue';
import ConfiguracoesView from './views/ConfiguracoesView.vue';
import GoogleAdsPreviewView from './views/GoogleAdsPreviewView.vue';
import GoogleAdsPublicacaoView from './views/GoogleAdsPublicacaoView.vue';
import GoogleAdsDashboardView from './views/GoogleAdsDashboardView.vue';
import LoginView from './views/LoginView.vue';
import MetaAdsPreviewView from './views/MetaAdsPreviewView.vue';
import PrivacyPolicyView from './views/PrivacyPolicyView.vue';
import TermsOfUseView from './views/TermsOfUseView.vue';
import DataDeletionView from './views/DataDeletionView.vue';
import { ensureAuthenticated } from './services/auth';

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', component: DashboardView, meta: { title: 'Dashboard', subtitle: 'Visao geral da operacao' } },
    { path: '/campanhas', component: CampanhasView, meta: { title: 'Campanhas', subtitle: 'Geracao, revisao e publicacao' } },
    { path: '/campanhas/nova', component: NovaCampanhaView, meta: { title: 'Nova campanha', subtitle: 'Briefing de geracao' } },
    { path: '/campanhas/:id', component: CampanhaRevisaoView, props: true, meta: { title: 'Revisao', subtitle: 'Conteudo comercial e landing' } },
    { path: '/campanhas/:id/googleads-preview', component: GoogleAdsPreviewView, props: true, meta: { title: 'Preview Google Ads', subtitle: 'Pre-publicacao tecnica' } },
    { path: '/campanhas/:id/metaads-preview', component: MetaAdsPreviewView, props: true, meta: { title: 'Preview Meta Ads', subtitle: 'Pre-publicacao tecnica' } },
    { path: '/googleads/publicacoes/:id', component: GoogleAdsPublicacaoView, props: true, meta: { title: 'Publicacao Google Ads', subtitle: 'Recursos criados' } },
    { path: '/googleads/dashboard', component: GoogleAdsDashboardView, meta: { title: 'Google Ads', subtitle: 'Metricas, sincronizacao e otimizacao' } },
    { path: '/leads', component: LeadsView, meta: { title: 'Leads', subtitle: 'Capturas da landing page' } },
    { path: '/configuracoes', component: ConfiguracoesView, meta: { title: 'Configuracoes', subtitle: 'Operacao e integracoes' } },
    { path: '/login', component: LoginView, meta: { public: true } },
    { path: '/politica-de-privacidade', component: PrivacyPolicyView, meta: { public: true } },
    { path: '/termos-de-uso', component: TermsOfUseView, meta: { public: true } },
    { path: '/exclusao-de-dados', component: DataDeletionView, meta: { public: true } },
    { path: '/lp/:slug', component: PublicLandingView, meta: { public: true } }
  ]
});

router.beforeEach(async (to) => {
  if (to.meta.public) return true;

  const user = await ensureAuthenticated();
  if (user) return true;

  return { path: '/login', query: { redirect: to.fullPath } };
});
