<template>
  <main class="page">
    <section class="page-header">
      <p class="eyebrow">Briefing simples</p>
      <h1>Nova campanha</h1>
      <p class="subtitle">Informe o essencial para gerar o primeiro rascunho comercial da campanha.</p>
    </section>

    <form class="panel form-grid elevated-form" @submit.prevent="submit">
      <label class="wide">
        Segmento *
        <select v-model="form.segmentSlug" required :disabled="loadingSegments">
          <option value="" disabled>{{ loadingSegments ? 'Carregando segmentos...' : 'Selecione um segmento' }}</option>
          <option v-for="segment in segments" :key="segment.id" :value="segment.slug">
            {{ segment.name }}
          </option>
        </select>
      </label>

      <template v-if="selectedSegment && !usesLegacyBriefing">
        <label class="wide">Descricao do negocio<textarea v-model.trim="form.businessDescription" maxlength="500" rows="3" /></label>
        <label>Produto ou servico *<input v-model.trim="form.productOrService" required maxlength="180" /></label>
        <label>Publico-alvo *<input v-model.trim="form.targetAudience" required maxlength="300" /></label>
        <label>Objetivo da campanha *<input v-model.trim="form.campaignGoal" required maxlength="300" /></label>
        <label>Oferta<input v-model.trim="form.offer" maxlength="300" /></label>
        <label>Cidade<input v-model.trim="locationForm.city" maxlength="120" /></label>
        <label>Estado<input v-model.trim="locationForm.state" maxlength="2" placeholder="RJ" /></label>
        <label>Regiao<input v-model.trim="locationForm.region" maxlength="120" /></label>
        <label>Tom da marca<input v-model.trim="form.brandTone" maxlength="120" /></label>
        <label class="wide">Restricoes<textarea v-model.trim="restrictionsText" rows="3" placeholder="Uma restricao por linha" /></label>
        <label>Orcamento diario<input v-model.number="form.orcamentoDiario" required min="1" step="0.01" type="number" /></label>
      </template>

      <template v-else>
        <label>
          Tipo de publico
          <select v-model="form.tipoPublico" required>
            <option value="Individual">Individual</option>
            <option value="Casal">Casal</option>
            <option value="Familia">Familia</option>
            <option value="Mei">MEI</option>
            <option value="Empresa">Empresa</option>
          </select>
        </label>

        <label>Cidade<input v-model.trim="form.cidade" required maxlength="120" /></label>
        <label>Estado<input v-model.trim="form.estado" required maxlength="2" placeholder="RJ" /></label>
        <label>Bairro ou regiao<input v-model.trim="form.regiao" maxlength="120" /></label>

        <label>
          Operadora
          <select v-model="form.operadora" required>
            <option>Nenhuma especifica</option>
            <option>Amil</option>
            <option>Bradesco Saude</option>
            <option>SulAmerica</option>
            <option>Unimed</option>
            <option>Outra</option>
          </select>
        </label>

        <label v-if="form.operadora === 'Outra'">Nome da operadora<input v-model.trim="form.operadoraOutra" required maxlength="80" /></label>
        <label>Orcamento diario<input v-model.number="form.orcamentoDiario" required min="1" step="0.01" type="number" /></label>
        <label class="wide">Objetivo ou observacao<textarea v-model.trim="form.objetivo" maxlength="500" rows="4" /></label>
      </template>

      <p v-if="error" class="error">{{ error }}</p>
      <div class="actions">
        <RouterLink to="/campanhas" class="button secondary">Cancelar</RouterLink>
        <button class="button" :disabled="loading || loadingSegments || !selectedSegment">{{ loading ? 'Gerando...' : 'Gerar campanha' }}</button>
      </div>
    </form>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { showToast } from '../components/uiEvents';
import { gerarCampanha, listarSegments, type GerarCampanhaRequest, type Segment } from '../services/api';

const router = useRouter();
const loading = ref(false);
const loadingSegments = ref(false);
const error = ref('');
const segments = ref<Segment[]>([]);
const restrictionsText = ref('');
const locationForm = reactive({ city: '', state: '', region: '' });
const form = reactive<GerarCampanhaRequest>({
  segmentSlug: '',
  tipoPublico: 'Familia',
  cidade: '',
  estado: '',
  regiao: '',
  operadora: 'Nenhuma especifica',
  operadoraOutra: '',
  orcamentoDiario: 20,
  objetivo: '',
  businessDescription: '',
  productOrService: '',
  targetAudience: '',
  campaignGoal: '',
  offer: '',
  brandTone: '',
  restrictions: []
});

const selectedSegment = computed(() => segments.value.find((segment) => segment.slug === form.segmentSlug));
const usesLegacyBriefing = computed(() => Boolean(selectedSegment.value?.usesLegacyBriefing));

onMounted(loadSegments);

watch(selectedSegment, (segment) => {
  if (!segment || usesLegacyBriefing.value) return;
  if (!form.campaignGoal && segment.defaultCampaignGoal) {
    form.campaignGoal = segment.defaultCampaignGoal;
  }
});

async function loadSegments() {
  loadingSegments.value = true;
  error.value = '';
  try {
    segments.value = await listarSegments();
    form.segmentSlug = segments.value[0]?.slug || '';
  } catch {
    error.value = 'Nao foi possivel carregar os segmentos.';
  } finally {
    loadingSegments.value = false;
  }
}

async function submit() {
  if (loading.value) return;
  error.value = validate();
  if (error.value) return;
  loading.value = true;

  try {
    const campanha = await gerarCampanha(buildPayload());
    showToast({ type: 'success', title: 'Campanha gerada', message: 'O conteudo inicial foi criado.' });
    router.push(`/campanhas/${campanha.id}`);
  } catch (err: unknown) {
    const response = err as { response?: { data?: { mensagem?: string } } };
    error.value = response.response?.data?.mensagem || 'Nao foi possivel gerar a campanha. Revise os dados e tente novamente.';
    showToast({ type: 'error', title: 'Falha ao gerar', message: error.value });
  } finally {
    loading.value = false;
  }
}

function validate() {
  if (!form.segmentSlug) return 'Selecione um segmento.';
  if (!usesLegacyBriefing.value) {
    if (!form.productOrService) return 'Informe o produto ou servico.';
    if (!form.targetAudience) return 'Informe o publico-alvo.';
    if (!form.campaignGoal) return 'Informe o objetivo da campanha.';
  }
  return '';
}

function buildPayload(): GerarCampanhaRequest {
  if (usesLegacyBriefing.value) {
    return {
      ...form,
      estado: form.estado.toUpperCase(),
      regiao: form.regiao || undefined,
      operadoraOutra: form.operadora === 'Outra' ? form.operadoraOutra : undefined,
      objetivo: form.objetivo || undefined,
      location: undefined,
      businessDescription: undefined,
      productOrService: undefined,
      targetAudience: undefined,
      campaignGoal: undefined,
      offer: undefined,
      brandTone: undefined,
      restrictions: undefined
    };
  }

  return {
    ...form,
    tipoPublico: form.tipoPublico || 'Individual',
    cidade: locationForm.city || '',
    estado: (locationForm.state || '').toUpperCase(),
    regiao: locationForm.region || undefined,
    operadora: form.operadora || '',
    operadoraOutra: undefined,
    objetivo: undefined,
    location: {
      city: locationForm.city || undefined,
      state: locationForm.state ? locationForm.state.toUpperCase() : undefined,
      region: locationForm.region || undefined
    },
    businessDescription: form.businessDescription || undefined,
    productOrService: form.productOrService || undefined,
    targetAudience: form.targetAudience || undefined,
    campaignGoal: form.campaignGoal || undefined,
    offer: form.offer || undefined,
    brandTone: form.brandTone || undefined,
    restrictions: restrictionsText.value.split('\n').map((item) => item.trim()).filter(Boolean)
  };
}
</script>
