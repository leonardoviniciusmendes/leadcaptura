import { createRouter, createWebHistory } from 'vue-router';
import LandingPageView from './views/LandingPageView.vue';
import { landingPages } from './landingPages';

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: landingPages.map((page) => ({
    path: page.slug,
    component: LandingPageView,
    props: { config: page }
  }))
});