<template>
  <form class="lead-form" @submit.prevent="submit">
    <label>Nome<input v-model.trim="form.nome" required maxlength="150" autocomplete="name" /></label>
    <label>WhatsApp<input :value="form.whatsApp" required inputmode="tel" autocomplete="tel" @input="onPhone" /></label>
    <label>Bairro<input v-model.trim="form.bairro" required maxlength="120" autocomplete="address-level3" /></label>

    <fieldset class="radio-group">
      <legend>Você procura para?</legend>
      <label v-for="option in procuraOptions" :key="option.value" class="radio-option">
        <input v-model="form.procuraPara" name="procuraPara" type="radio" :value="option.value" @change="onProcuraChange" />
        <span>{{ option.label }}</span>
      </label>
    </fieldset>

    <label>
      Quantidade de pessoas
      <input
        v-model.number="form.quantidadeVidas"
        required
        :min="minQuantidade"
        max="20"
        type="number"
        :disabled="quantidadeBloqueada"
        @input="onQuantidadeInput"
      />
    </label>

    <div v-if="mostrarIdades" class="ages">
      <span>Idades</span>
      <label v-for="(_, index) in form.idades" :key="index" class="age-row">
        <span>Idade {{ index + 1 }}</span>
        <input v-model.number="form.idades[index]" min="0" max="120" type="number" />
      </label>
    </div>

    <input v-model="form.honeypot" class="hidden-field" tabindex="-1" autocomplete="off" aria-hidden="true" />
    <label class="checkbox consent"><input v-model="form.consentimentoContato" required type="checkbox" /> Autorizo o contato por telefone e WhatsApp sobre esta solicitação.</label>
    <PrivacyNotice />
    <p v-if="error" class="error">{{ error }}</p>
    <button class="submit" type="submit" :disabled="loading">{{ loading ? 'Enviando...' : 'Receber cotação' }}</button>
  </form>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import PrivacyNotice from './PrivacyNotice.vue';
import { digits, maskPhone, type TipoLead } from './forms';
import { buildOrigem, pushDataLayer } from '../services/tracking';
import { enviarLeadParaWhatsApp, type WhatsAppLeadResponse } from '../services/whatsapp';

type ProcuraPara = 'apenas' | 'casal' | 'familia' | 'empresa';

const props = defineProps<{ tipo: TipoLead; operadora?: string }>();
const emit = defineEmits<{ success: [WhatsAppLeadResponse, TipoLead] }>();
const loading = ref(false);
const error = ref('');
const started = ref(false);
const mostrarIdades = ref(false);

const procuraOptions: Array<{ value: ProcuraPara; label: string }> = [
  { value: 'apenas', label: 'Apenas para mim' },
  { value: 'casal', label: 'Casal' },
  { value: 'familia', label: 'Família' },
  { value: 'empresa', label: 'Empresa / MEI' }
];

const form = reactive({
  nome: '',
  whatsApp: '',
  bairro: '',
  procuraPara: initialProcuraPara(props.tipo),
  quantidadeVidas: initialQuantidade(initialProcuraPara(props.tipo)),
  idades: [] as number[],
  hospitalDesejado: '',
  operadoraDesejada: props.operadora || '',
  possuiPlanoAtual: false,
  planoAtual: '',
  consentimentoContato: false,
  honeypot: ''
});

const quantidadeBloqueada = computed(() => form.procuraPara === 'apenas' || form.procuraPara === 'casal');
const minQuantidade = computed(() => form.procuraPara === 'familia' ? 2 : 1);
const tipoLead = computed<TipoLead>(() => {
  if (form.procuraPara === 'apenas') return 'PessoaFisica';
  if (form.procuraPara === 'empresa') return 'Empresa';
  return 'Familia';
});

watch(form, () => {
  if (!started.value) {
    started.value = true;
    pushDataLayer({ event: 'lead_form_start', leadType: tipoLead.value });
  }
});

watch(
  () => form.quantidadeVidas,
  () => syncAges()
);

function initialProcuraPara(tipo: TipoLead): ProcuraPara {
  if (tipo === 'PessoaFisica') return 'apenas';
  if (tipo === 'Empresa' || tipo === 'Mei') return 'empresa';
  return 'familia';
}

function initialQuantidade(procuraPara: ProcuraPara): number {
  if (procuraPara === 'apenas') return 1;
  if (procuraPara === 'casal') return 2;
  if (procuraPara === 'familia') return 2;
  return 1;
}

function onPhone(event: Event) {
  form.whatsApp = maskPhone((event.target as HTMLInputElement).value);
}

function onProcuraChange() {
  form.quantidadeVidas = initialQuantidade(form.procuraPara);
  mostrarIdades.value = true;
  syncAges();
}

function onQuantidadeInput() {
  mostrarIdades.value = true;
  syncAges();
}

function syncAges() {
  const quantity = normalizeQuantity(Number(form.quantidadeVidas));
  if (form.quantidadeVidas !== quantity) form.quantidadeVidas = quantity;

  while (form.idades.length < quantity) form.idades.push(0);
  if (form.idades.length > quantity) form.idades.splice(quantity);
}

function normalizeQuantity(value: number): number {
  const min = minQuantidade.value;
  if (!Number.isFinite(value)) return min;
  return Math.min(20, Math.max(min, Math.trunc(value)));
}

async function submit() {
  if (loading.value) return;
  loading.value = true;
  error.value = '';
  mostrarIdades.value = true;
  syncAges();

  try {
    const response = enviarLeadParaWhatsApp({
      tipo: tipoLead.value,
      nome: form.nome,
      whatsApp: digits(form.whatsApp),
      bairro: form.bairro,
      quantidadeVidas: Number(form.quantidadeVidas),
      idades: form.idades.map(Number),
      hospitalDesejado: form.hospitalDesejado || undefined,
      operadoraDesejada: form.operadoraDesejada || undefined,
      possuiPlanoAtual: form.possuiPlanoAtual,
      planoAtual: form.planoAtual || undefined,
      consentimentoContato: form.consentimentoContato,
      honeypot: form.honeypot,
      origem: buildOrigem()
    });
    emit('success', response, tipoLead.value);
  } catch {
    pushDataLayer({ event: 'lead_form_error', errorType: 'validation' });
    error.value = 'Não foi possível enviar agora. Confira os dados e tente novamente.';
  } finally {
    loading.value = false;
  }
}
</script>
