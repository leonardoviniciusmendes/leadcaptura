<template>
  <main class="public-page">
    <section v-if="loading" class="public-band">Carregando...</section>
    <section v-else-if="error" class="public-band">
      <h1>Landing page indisponivel</h1>
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
          <p class="hero-note">{{ heroNote }}</p>
        </div>

        <form class="panel public-form lead-card-form" @submit.prevent="submit">
          <div class="form-heading">
            <span>{{ formTitle }}</span>
          </div>

          <template v-for="field in campanha.form.fields" :key="field.key">
            <label v-if="simpleInputTypes.includes(field.type)">
              {{ field.label }}
              <input
                v-model="answers[field.key]"
                :type="inputType(field.type)"
                :required="field.required"
                :placeholder="field.placeholder"
                :inputmode="field.type === 'phone' ? 'tel' : undefined"
                :autocomplete="autocomplete(field.key, field.type)"
                @input="field.type === 'phone' ? maskPhone(field.key) : undefined"
              />
            </label>

            <label v-else-if="field.type === 'textarea'">
              {{ field.label }}
              <textarea v-model="answers[field.key]" :required="field.required" :placeholder="field.placeholder" rows="3" />
            </label>

            <label v-else-if="field.type === 'select'">
              {{ field.label }}
              <select v-model="answers[field.key]" :required="field.required">
                <option value="">Selecionar</option>
                <option v-for="option in field.options" :key="option" :value="option">{{ option }}</option>
              </select>
            </label>

            <fieldset v-else-if="field.type === 'radio'" class="dynamic-options">
              <legend>{{ field.label }}</legend>
              <label v-for="option in field.options" :key="option" class="option-line">
                <input v-model="answers[field.key]" type="radio" :name="field.key" :value="option" :required="field.required" />
                {{ option }}
              </label>
            </fieldset>

            <fieldset v-else-if="field.type === 'multiselect'" class="dynamic-options">
              <legend>{{ field.label }}</legend>
              <label v-for="option in field.options" :key="option" class="option-line">
                <input type="checkbox" :checked="multiValue(field.key).includes(option)" @change="toggleMulti(field.key, option)" />
                {{ option }}
              </label>
            </fieldset>

            <label v-else-if="field.type === 'checkbox'" class="option-line">
              <input v-model="checkboxAnswers[field.key]" type="checkbox" />
              {{ field.label }}
            </label>
          </template>

          <input v-model="website" class="hp-field" tabindex="-1" autocomplete="off" />
          <p v-if="submitError" class="error">{{ submitError }}</p>
          <p v-if="success" class="success">{{ success }}</p>
          <button class="button public-cta" :disabled="submitting">{{ submitting ? 'Enviando...' : ctaText }}</button>
          <a v-if="whatsAppUrl" class="button secondary" :href="whatsAppUrl" target="_blank" rel="noopener">Abrir WhatsApp</a>
          <small class="form-trust">Sem compromisso. Usaremos seus dados apenas para responder esta solicitacao.</small>
        </form>
      </section>

      <section class="public-band">
        <div class="section-heading commercial-heading">
          <span>Como funciona</span>
          <h2>{{ stepsTitle }}</h2>
        </div>
        <div class="steps-grid">
          <article><strong>01</strong><h3>Informe seus dados</h3><p>{{ stepOne }}</p></article>
          <article><strong>02</strong><h3>{{ stepTwoTitle }}</h3><p>{{ stepTwo }}</p></article>
          <article><strong>03</strong><h3>{{ stepThreeTitle }}</h3><p>{{ stepThree }}</p></article>
        </div>
      </section>

      <section class="public-band">
        <div class="section-heading commercial-heading">
          <span>Beneficios</span>
          <h2>{{ benefitsTitle }}</h2>
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
          <p class="eyebrow">Atendimento e seguranca</p>
          <h2>Dados usados somente para contato sobre esta solicitacao</h2>
        </div>
        <div class="trust-grid">
          <article><strong>Atendimento personalizado</strong><p>{{ trustPersonalizado }}</p></article>
          <article><strong>{{ trustOfferTitle }}</strong><p>{{ trustOfferText }}</p></article>
          <article><strong>Tratamento seguro dos dados</strong><p>O contato ocorre apenas para responder esta solicitacao.</p></article>
        </div>
        <p v-if="isLegacyBriefing" class="notice">Valores, redes, carencias e coberturas dependem do plano, perfil, regiao e regras da operadora.</p>
      </section>

      <section class="public-band">
        <div class="section-heading commercial-heading">
          <span>Duvidas frequentes</span>
          <h2>{{ faqTitle }}</h2>
        </div>
        <div class="faq-list">
          <details v-for="item in campanha.perguntasFrequentes" :key="item.pergunta">
            <summary>{{ item.pergunta }}</summary>
            <p>{{ item.resposta }}</p>
          </details>
        </div>
      </section>

      <footer class="public-footer">
        <strong>{{ footerTitle }}</strong>
        <span>{{ footerSubtitle }}</span>
        <small v-if="isLegacyBriefing">Este site nao e uma operadora de planos de saude. As informacoes apresentadas tem finalidade de atendimento e solicitacao de cotacao.</small>
        <small v-if="isLegacyBriefing">Precos, coberturas, carencias, rede credenciada, disponibilidade e demais condicoes dependem do perfil informado, da proposta apresentada e das regras da respectiva operadora.</small>
        <small>
          <RouterLink to="/politica-de-privacidade">Politica de Privacidade</RouterLink>
          <span> | </span>
          <RouterLink to="/termos-de-uso">Termos de Uso</RouterLink>
        </small>
        <small>Plataforma tecnologica LeadEngine, desenvolvida pela Consultoria Dev / L.V. Mendes Informatica.</small>
      </footer>
    </template>
  </main>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import { capturarLeadPublico, obterCampanhaPublica, type CampanhaPublica, type CapturarLeadPublicoRequest, type LeadFormField } from '../services/api';
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
const website = ref('');
const openedAt = Date.now();
const trackedConversionLeadIds = new Set<string>();
type AnswerValue = string | number | string[];
const answers = reactive<Record<string, AnswerValue>>({});
const checkboxAnswers = reactive<Record<string, boolean>>({});
const tracking = reactive<Record<string, string | undefined>>({});
const simpleInputTypes: LeadFormField['type'][] = ['text', 'phone', 'email', 'number', 'date'];

const isLegacyBriefing = computed(() => Boolean(campanha.value?.usesLegacyBriefing));
const heroBeneficios = computed(() => campanha.value?.beneficios.slice(0, 2) ?? ['Atendimento personalizado', 'Resposta rapida']);
const ctaText = computed(() => campanha.value?.form.submitButtonText || campanha.value?.textoBotao || 'Enviar');
const formTitle = computed(() => isLegacyBriefing.value ? 'Receba sua cotacao' : 'Solicite atendimento');
const heroNote = computed(() => isLegacyBriefing.value ? 'Cotacao sem compromisso, com atendimento personalizado para seu perfil.' : 'Atendimento sem compromisso, com retorno personalizado para sua solicitacao.');
const stepsTitle = computed(() => isLegacyBriefing.value ? 'Um caminho simples para comparar opcoes' : 'Um caminho simples para receber atendimento');
const stepOne = computed(() => isLegacyBriefing.value ? 'Voce envia o essencial para iniciarmos a cotacao.' : 'Voce envia as informacoes solicitadas no formulario.');
const stepTwoTitle = computed(() => isLegacyBriefing.value ? 'Analisamos seu perfil' : 'Entendemos sua necessidade');
const stepTwo = computed(() => isLegacyBriefing.value ? 'Consideramos quantidade de vidas, localidade e tipo de contratacao.' : 'Consideramos as respostas enviadas e a localidade da solicitacao.');
const stepThreeTitle = computed(() => isLegacyBriefing.value ? 'Receba opcoes para comparar' : 'Receba o proximo passo');
const stepThree = computed(() => isLegacyBriefing.value ? 'Um atendimento consultivo ajuda voce a avaliar alternativas.' : 'Um atendimento consultivo ajuda voce a avaliar a melhor alternativa.');
const benefitsTitle = computed(() => isLegacyBriefing.value ? 'Diferenciais desta cotacao' : 'Diferenciais deste atendimento');
const trustPersonalizado = computed(() => isLegacyBriefing.value ? 'A cotacao considera as informacoes enviadas no formulario.' : 'O atendimento considera as respostas enviadas no formulario.');
const trustOfferTitle = computed(() => isLegacyBriefing.value ? 'Cotacao sem compromisso' : 'Atendimento sem compromisso');
const trustOfferText = computed(() => isLegacyBriefing.value ? 'Voce recebe orientacao para comparar opcoes antes de decidir.' : 'Voce recebe orientacao para avaliar o proximo passo antes de decidir.');
const faqTitle = computed(() => isLegacyBriefing.value ? 'Informacoes importantes antes de contratar' : 'Informacoes importantes antes de solicitar atendimento');
const footerTitle = computed(() => isLegacyBriefing.value ? 'Atendimento e cotacao de planos de saude' : (campanha.value?.segment?.name ? `Atendimento - ${campanha.value.segment.name}` : 'Atendimento'));
const footerSubtitle = computed(() => isLegacyBriefing.value ? 'Responsavel pelo atendimento e pelas solicitacoes de cotacao: Amanda Pereira Pinto.' : 'Responsavel pelo atendimento e pelas solicitacoes recebidas por esta pagina.');

onMounted(async () => {
  loading.value = true;
  try {
    campanha.value = await obterCampanhaPublica(String(route.params.slug));
    hydrateAnswers();
    applyTracking();
    await nextTick();
    document.querySelector<HTMLInputElement>('.public-form input:not(.hp-field)')?.focus();
  } catch {
    error.value = 'A campanha nao esta ativa ou nao existe.';
  } finally {
    loading.value = false;
  }
});

async function submit() {
  if (submitting.value || !campanha.value) return;
  submitting.value = true;
  submitError.value = '';
  success.value = '';
  try {
    const payload = buildPayload();
    const response = await capturarLeadPublico(String(route.params.slug), payload);
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

function buildPayload(): CapturarLeadPublicoRequest {
  const name = stringAnswer('name') || stringAnswer('nome');
  const phone = stringAnswer('phone') || stringAnswer('telefone');
  const email = stringAnswer('email');
  const cidade = stringAnswer('city') || stringAnswer('cidade') || campanha.value?.briefing.location?.city || campanha.value?.cidade;
  const estado = stringAnswer('state') || stringAnswer('estado') || campanha.value?.briefing.location?.state || campanha.value?.estado;

  const payload: CapturarLeadPublicoRequest = {
    name,
    phone,
    email,
    cidade,
    estado,
    website: website.value,
    formOpenedAt: openedAt,
    answers: cleanAnswers(),
    utmSource: tracking.utmSource,
    utmMedium: tracking.utmMedium,
    utmCampaign: tracking.utmCampaign,
    utmTerm: tracking.utmTerm,
    utmContent: tracking.utmContent,
    gclid: tracking.gclid,
    fbclid: tracking.fbclid
  };

  if (isLegacyBriefing.value) {
    payload.nome = name || '';
    payload.telefone = phone || '';
    payload.quantidadeVidas = Number(answers.quantidadeVidas || 1);
    payload.tipoContratacao = tipoContratacaoPadrao(campanha.value!.tipoPublico);
  }

  return payload;
}

function hydrateAnswers() {
  if (!campanha.value) return;
  for (const field of campanha.value.form.fields) {
    if (field.type === 'checkbox') {
      checkboxAnswers[field.key] = field.defaultValue === 'true';
      continue;
    }
    answers[field.key] = field.type === 'multiselect'
        ? []
        : field.defaultValue || '';
  }
}

function cleanAnswers() {
  const result: Record<string, unknown> = {};
  for (const field of campanha.value?.form.fields ?? []) {
    if (field.type === 'checkbox') {
      result[field.key] = Boolean(checkboxAnswers[field.key]);
      continue;
    }
    const value = answers[field.key];
    if (Array.isArray(value) ? value.length > 0 : value !== undefined && value !== null && String(value).trim() !== '') {
      result[field.key] = value;
    }
  }
  return result;
}

function stringAnswer(key: string) {
  const value = answers[key];
  return typeof value === 'string' ? value : undefined;
}

function inputType(type: LeadFormField['type']) {
  if (type === 'phone') return 'tel';
  return type;
}

function autocomplete(key: string, type: LeadFormField['type']) {
  if (key === 'name') return 'name';
  if (type === 'phone') return 'tel';
  if (type === 'email') return 'email';
  return undefined;
}

function maskPhone(key: string) {
  const value = typeof answers[key] === 'string' ? String(answers[key]) : '';
  const digits = value.replace(/\D/g, '').slice(0, 11);
  if (digits.length <= 2) {
    answers[key] = digits;
    return;
  }
  const ddd = digits.slice(0, 2);
  const prefixLength = digits.length > 10 ? 5 : 4;
  const prefix = digits.slice(2, 2 + prefixLength);
  const suffix = digits.slice(2 + prefixLength);
  answers[key] = `(${ddd}) ${prefix}${suffix ? `-${suffix}` : ''}`;
}

function multiValue(key: string) {
  return Array.isArray(answers[key]) ? answers[key] as string[] : [];
}

function toggleMulti(key: string, option: string) {
  const current = [...multiValue(key)];
  const index = current.indexOf(option);
  if (index >= 0) current.splice(index, 1);
  else current.push(option);
  answers[key] = current;
}

function applyTracking() {
  const params = new URLSearchParams(window.location.search);
  Object.assign(tracking, {
    utmSource: params.get('utm_source') || undefined,
    utmMedium: params.get('utm_medium') || undefined,
    utmCampaign: params.get('utm_campaign') || undefined,
    utmTerm: params.get('utm_term') || undefined,
    utmContent: params.get('utm_content') || undefined,
    gclid: params.get('gclid') || undefined,
    fbclid: params.get('fbclid') || undefined
  });
}

function tipoContratacaoPadrao(tipo: CampanhaPublica['tipoPublico']): CapturarLeadPublicoRequest['tipoContratacao'] {
  if (tipo === 'Individual') return 'Individual';
  if (tipo === 'Mei') return 'Mei';
  if (tipo === 'Empresa') return 'Empresarial';
  return 'Familiar';
}
</script>
