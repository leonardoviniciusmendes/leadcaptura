<template>
  <main class="page">
    <section v-if="!success" class="hero">
      <div class="copy">
        <p class="eyebrow">Cotacao rapida</p>
        <h1>{{ config.titulo }}</h1>
        <p class="subtitle">{{ config.subtitulo }}</p>
        <BenefitsSection :beneficios="config.beneficios" />
      </div>
      <aside class="form-panel" aria-label="Formulario de cotacao">
        <div class="type-toggle" role="group" aria-label="Tipo de cotacao">
          <button :class="{ active: leadType === 'Familia' || leadType === 'PessoaFisica' }" type="button" @click="leadType = 'Familia'">
            Pessoa/Familia
          </button>
          <button :class="{ active: leadType === 'Empresa' || leadType === 'Mei' }" type="button" @click="leadType = 'Empresa'">
            Empresa/MEI
          </button>
        </div>
        <LeadFormEmpresa
          v-if="leadType === 'Empresa' || leadType === 'Mei'"
          :tipo="leadType"
          :operadora="config.operadora"
          @success="onSuccess"
        />
        <LeadFormPessoa
          v-else
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
import LeadFormEmpresa from '../components/LeadFormEmpresa.vue';
import LeadFormPessoa from '../components/LeadFormPessoa.vue';
import SuccessView from '../components/SuccessView.vue';
import type { TipoLead } from '../components/forms';
import { captureCampaignParams, pushDataLayer } from '../services/tracking';
import type { CapturaLeadResponse } from '../services/api';
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

function onSuccess(response: CapturaLeadResponse, type: TipoLead) {
  success.value = true;
  pushDataLayer({
    event: 'generate_lead',
    leadId: response.leadId,
    leadType: type,
    landingPage: window.location.pathname
  });
}
</script>
