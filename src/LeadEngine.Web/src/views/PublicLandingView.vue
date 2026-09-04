<template>
  <main class="public-page">
    <section v-if="loading" class="public-band">Carregando...</section>
    <section v-else-if="error" class="public-band">
      <h1>Landing page indisponível</h1>
      <p>{{ error }}</p>
    </section>
    <template v-else-if="campanha">
      <section class="public-hero" :style="{ backgroundImage: `url(${heroImage})` }">
        <div class="public-hero-copy">
          <h1>{{ campanha.titulo }}</h1>
          <p class="subtitle">{{ campanha.subtitulo }}</p>

          <div class="hero-benefits" aria-label="Diferenciais">
            <span v-for="beneficio in heroBeneficios" :key="beneficio">{{ beneficio }}</span>
          </div>

          <p class="hero-note">Cotação sem compromisso, com atendimento personalizado para seu perfil.</p>
        </div>

        <form class="panel public-form lead-card-form" @submit.prevent="submit">
          <div class="form-heading">
            <span>Receba sua cotação</span>
            <h2>{{ ctaText }}</h2>
            <p>{{ campanha.cidade }}/{{ campanha.estado }} - {{ labelPublico(campanha.tipoPublico) }}</p>
          </div>

          <label>Nome<input v-model.trim="form.nome" required maxlength="120" autocomplete="name" /></label>
          <label>WhatsApp<input v-model="form.telefone" required maxlength="15" inputmode="tel" autocomplete="tel" placeholder="(00) 00000-0000" @input="maskPhone" /></label>
          <label>Quantidade de vidas<input v-model.number="form.quantidadeVidas" required type="number" min="1" max="999" /></label>

          <div class="known-context">
            <span>{{ form.cidade }}/{{ form.estado }}</span>
            <span>{{ labelContratacao(form.tipoContratacao) }}</span>
          </div>

          <label class="consent">
            <input v-model="form.consentimento" type="checkbox" required />
            Autorizo contato sobre esta cotação e entendo que meus dados serão usados apenas para essa solicitação.
          </label>
          <input v-model="form.website" class="hp-field" tabindex="-1" autocomplete="off" />
          <p v-if="submitError" class="error">{{ submitError }}</p>
          <p v-if="success" class="success">{{ success }}</p>
          <button class="button public-cta" :disabled="submitting">{{ submitting ? 'Enviando...' : ctaText }}</button>
          <a v-if="whatsAppUrl" class="button secondary" :href="whatsAppUrl" target="_blank" rel="noopener">Abrir WhatsApp</a>
          <small class="form-trust">Sem compromisso. O atendimento depende do seu consentimento.</small>
        </form>
      </section>

      <section class="public-band">
        <div class="section-heading commercial-heading">
          <span>Como funciona</span>
          <h2>Um caminho simples para comparar opções</h2>
        </div>
        <div class="steps-grid">
          <article>
            <strong>01</strong>
            <h3>Informe seus dados</h3>
            <p>Você envia o essencial para iniciarmos a cotação.</p>
          </article>
          <article>
            <strong>02</strong>
            <h3>Analisamos seu perfil</h3>
            <p>Consideramos quantidade de vidas, localidade e tipo de contratação.</p>
          </article>
          <article>
            <strong>03</strong>
            <h3>Receba opções para comparar</h3>
            <p>Um atendimento consultivo ajuda você a avaliar alternativas.</p>
          </article>
        </div>
      </section>

      <section class="public-band">
        <div class="section-heading commercial-heading">
          <span>Benefícios</span>
          <h2>Diferenciais desta cotação</h2>
        </div>
        <div class="benefit-cards">
          <article v-for="beneficio in campanha.beneficios" :key="beneficio">
            <span aria-hidden="true">OK</span>
            <p>{{ beneficio }}</p>
          </article>
        </div>
      </section>

      <section class="public-band public-trust">
        <div>
          <p class="eyebrow">Atendimento e segurança</p>
          <h2>Dados usados somente para contato sobre esta solicitação</h2>
        </div>
        <div class="trust-grid">
          <article>
            <strong>Atendimento personalizado</strong>
            <p>A cotação considera as informações enviadas no formulário.</p>
          </article>
          <article>
            <strong>Cotação sem compromisso</strong>
            <p>Você recebe orientação para comparar opções antes de decidir.</p>
          </article>
          <article>
            <strong>Tratamento seguro dos dados</strong>
            <p>O contato ocorre apenas mediante consentimento explícito.</p>
          </article>
        </div>
        <p class="notice">Valores, redes, carências e coberturas dependem do plano, perfil, região e regras da operadora.</p>
      </section>

      <section class="public-band">
        <div class="section-heading commercial-heading">
          <span>Dúvidas frequentes</span>
          <h2>Informações importantes antes de contratar</h2>
        </div>
        <div class="faq-list">
          <details v-for="item in campanha.perguntasFrequentes" :key="item.pergunta">
            <summary>{{ item.pergunta }}</summary>
            <p>{{ item.resposta }}</p>
          </details>
        </div>
      </section>

      <footer class="public-footer">
        <strong>Atendimento e cotação de planos de saúde</strong>
        <span>Responsável pelo atendimento e pelas solicitações de cotação: Amanda Pereira Pinto.</span>
        <small>Este site não é uma operadora de planos de saúde. As informações apresentadas têm finalidade de atendimento e solicitação de cotação.</small>
        <small>Preços, coberturas, carências, rede credenciada, disponibilidade e demais condições dependem do perfil informado, da proposta apresentada e das regras da respectiva operadora.</small>
        <small>
          <RouterLink to="/politica-de-privacidade">Política de Privacidade</RouterLink>
          <span> | </span>
          <RouterLink to="/termos-de-uso">Termos de Uso</RouterLink>
        </small>
        <small>Plataforma tecnológica LeadEngine, desenvolvida pela Consultoria Dev / L.V. Mendes Informática.</small>
      </footer>
    </template>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import { capturarLeadPublico, obterCampanhaPublica, type CampanhaPublica, type CapturarLeadPublicoRequest } from '../services/api';
import { trackGoogleAdsConversion } from '../services/tracking';
import heroImage from '../imagens/Pf1.png';

const route = useRoute();
const campanha = ref<CampanhaPublica | null>(null);
const loading = ref(false);
const submitting = ref(false);
const error = ref('');
const submitError = ref('');
const success = ref('');
const whatsAppUrl = ref('');
const openedAt = Date.now();
const trackedConversionLeadIds = new Set<string>();

const form = reactive<CapturarLeadPublicoRequest>({
  nome: '',
  telefone: '',
  cidade: '',
  estado: '',
  quantidadeVidas: 1,
  tipoContratacao: 'Familiar',
  consentimento: false,
  website: '',
  formOpenedAt: openedAt
});

const contextoCampanha = computed(() => {
  if (!campanha.value) return '';
  const partes = [labelPublico(campanha.value.tipoPublico), campanha.value.operadora, `${campanha.value.cidade}/${campanha.value.estado}`];
  return partes.filter(Boolean).join(' - ');
});

const heroBeneficios = computed(() => {
  const lista = campanha.value?.beneficios.slice(0, 2) ?? [];
  return lista.length > 0 ? lista : ['Cotacao personalizada', 'Compare opcoes', 'Atendimento consultivo'];
});

const ctaText = computed(() => {
  const texto = campanha.value?.textoBotao?.trim();
  return texto || 'Receber minha cotacao';
});

onMounted(async () => {
  loading.value = true;
  try {
    campanha.value = await obterCampanhaPublica(String(route.params.slug));
    form.cidade = campanha.value.cidade;
    form.estado = campanha.value.estado;
    form.tipoContratacao = tipoContratacaoPadrao(campanha.value.tipoPublico);
    applyTracking();
  } catch {
    error.value = 'A campanha nao esta ativa ou nao existe.';
  } finally {
    loading.value = false;
  }
});

async function submit() {
  if (submitting.value) return;
  submitting.value = true;
  submitError.value = '';
  success.value = '';
  try {
    const telefone = form.telefone.replace(/\D/g, '');
    const response = await capturarLeadPublico(String(route.params.slug), { ...form, telefone, estado: form.estado.toUpperCase(), formOpenedAt: openedAt });
    if (response.conversaoConfirmada && !trackedConversionLeadIds.has(response.leadId)) {
      trackedConversionLeadIds.add(response.leadId);
      await trackGoogleAdsConversion();
    }
    success.value = response.mensagem;
    whatsAppUrl.value = response.whatsAppUrl;
    window.open(response.whatsAppUrl, '_blank', 'noopener');
  } catch (err: unknown) {
    const response = err as { response?: { data?: { mensagem?: string } } };
    submitError.value = response.response?.data?.mensagem || 'Nao foi possivel enviar seus dados. Tente novamente.';
  } finally {
    submitting.value = false;
  }
}

function applyTracking() {
  const params = new URLSearchParams(window.location.search);
  form.utmSource = params.get('utm_source') || undefined;
  form.utmMedium = params.get('utm_medium') || undefined;
  form.utmCampaign = params.get('utm_campaign') || undefined;
  form.utmTerm = params.get('utm_term') || undefined;
  form.utmContent = params.get('utm_content') || undefined;
  form.gclid = params.get('gclid') || undefined;
  form.fbclid = params.get('fbclid') || undefined;
}

function maskPhone() {
  const digits = form.telefone.replace(/\D/g, '').slice(0, 11);
  if (digits.length <= 2) {
    form.telefone = digits;
    return;
  }

  const ddd = digits.slice(0, 2);
  const prefixLength = digits.length > 10 ? 5 : 4;
  const prefix = digits.slice(2, 2 + prefixLength);
  const suffix = digits.slice(2 + prefixLength);
  form.telefone = `(${ddd}) ${prefix}${suffix ? `-${suffix}` : ''}`;
}

function tipoContratacaoPadrao(tipo: CampanhaPublica['tipoPublico']): CapturarLeadPublicoRequest['tipoContratacao'] {
  if (tipo === 'Individual') return 'Individual';
  if (tipo === 'Mei') return 'Mei';
  if (tipo === 'Empresa') return 'Empresarial';
  return 'Familiar';
}

function labelPublico(tipo: CampanhaPublica['tipoPublico']) {
  const labels: Record<CampanhaPublica['tipoPublico'], string> = {
    Individual: 'Plano individual',
    Casal: 'Plano para casal',
    Familia: 'Plano familiar',
    Mei: 'Plano para MEI',
    Empresa: 'Plano empresarial'
  };
  return labels[tipo];
}

function labelContratacao(tipo: CapturarLeadPublicoRequest['tipoContratacao']) {
  const labels: Record<CapturarLeadPublicoRequest['tipoContratacao'], string> = {
    Individual: 'Contratacao individual',
    Familiar: 'Contratacao familiar',
    Empresarial: 'Contratacao empresarial',
    Mei: 'Contratacao MEI',
    AindaNaoSei: 'Tipo a definir'
  };
  return labels[tipo];
}
</script>
