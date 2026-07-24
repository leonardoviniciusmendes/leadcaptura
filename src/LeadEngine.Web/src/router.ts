import { createRouter, createWebHistory } from 'vue-router';
import CampanhasView from './views/CampanhasView.vue';
import CampanhaRevisaoView from './views/CampanhaRevisaoView.vue';
import NovaCampanhaView from './views/NovaCampanhaView.vue';

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', redirect: '/campanhas' },
    { path: '/campanhas', component: CampanhasView },
    { path: '/campanhas/nova', component: NovaCampanhaView },
    { path: '/campanhas/:id', component: CampanhaRevisaoView, props: true }
  ]
});
