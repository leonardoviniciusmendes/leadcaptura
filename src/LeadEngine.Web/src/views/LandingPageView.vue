<template>
  <main class="page">
    <section v-if="!success" class="hero">
      <div class="copy">
        <p class="eyebrow">Cotação rápida</p>
        <h1>{{ config.titulo }}</h1>
        <p class="subtitle">{{ config.subtitulo }}</p>
        <BenefitsSection :beneficios="config.beneficios" />
      </div>
      <aside class="form-panel" aria-label="Formulário de cotação">
        <LeadFormPessoa
          :tipo="leadType"
          :operadora="config.operadora"
          @success="onSuccess"
        />
      </aside>
    </section>
    <SuccessView v-else />
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import BenefitsSection from '../components/BenefitsSection.vue';
import LeadFormPessoa from '../components/LeadFormPessoa.vue';
import SuccessView from '../components/SuccessView.vue';
import type { TipoLead } from '../components/forms';
import { captureCampaignParams, pushDataLayer } from '../services/tracking';
import type { WhatsAppLeadResponse } from '../services/whatsapp';
import type { LandingPageConfig } from '../landingPages';

const props = defineProps<{ config: LandingPageConfig }>();
const leadType = ref<TipoLead>(props.config.tipoLeadPadrao);
const success = ref(false);

watch(
  () => props.config,
  (config) => {
    leadType.value = config.tipoLeadPadrao;
    success.value = false;
    captureCampaignParams();
    pushDataLayer({ event: 'landing_view', landingPage: window.location.pathname });
  }
);

onMounted(() => {
  captureCampaignParams();
  pushDataLayer({ event: 'landing_view', landingPage: window.location.pathname });
});

function onSuccess(response: WhatsAppLeadResponse, type: TipoLead) {
  success.value = true;
  pushDataLayer({
    event: 'generate_lead',
    leadId: response.leadId,
    leadType: type,
    landingPage: window.location.pathname
  });
}
</script>
