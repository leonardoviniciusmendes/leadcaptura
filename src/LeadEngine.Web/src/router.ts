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

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', component: DashboardView, meta: { title: 'Dashboard', subtitle: 'Visao geral da operacao' } },
    { path: '/campanhas', component: CampanhasView, meta: { title: 'Campanhas', subtitle: 'Geracao, revisao e publicacao' } },
    { path: '/campanhas/nova', component: NovaCampanhaView, meta: { title: 'Nova campanha', subtitle: 'Briefing de geracao' } },
    { path: '/campanhas/:id', component: CampanhaRevisaoView, props: true, meta: { title: 'Revisao', subtitle: 'Conteudo comercial e landing' } },
    { path: '/campanhas/:id/googleads-preview', component: GoogleAdsPreviewView, props: true, meta: { title: 'Preview Google Ads', subtitle: 'Pre-publicacao tecnica' } },
    { path: '/googleads/publicacoes/:id', component: GoogleAdsPublicacaoView, props: true, meta: { title: 'Publicacao Google Ads', subtitle: 'Recursos criados' } },
    { path: '/leads', component: LeadsView, meta: { title: 'Leads', subtitle: 'Capturas da landing page' } },
    { path: '/configuracoes', component: ConfiguracoesView, meta: { title: 'Configuracoes', subtitle: 'Operacao e integracoes' } },
    { path: '/lp/:slug', component: PublicLandingView, meta: { public: true } }
  ]
});
