<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Campanhas</p>
        <h1>Campanhas geradas</h1>
      </div>
      <RouterLink class="button" to="/campanhas/nova">Nova campanha</RouterLink>
    </section>

    <p v-if="error" class="error">{{ error }}</p>

    <section class="grid-layout">
      <div class="panel table-panel">
        <table>
          <thead>
            <tr>
              <th>Nome</th>
              <th>Público</th>
              <th>Localização</th>
              <th>Operadora</th>
              <th>Orçamento</th>
              <th>Status</th>
              <th>Criação</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="loading">
              <td colspan="7">Carregando...</td>
            </tr>
            <tr v-else-if="campanhas.length === 0">
              <td colspan="7">Nenhuma campanha gerada.</td>
            </tr>
            <tr
              v-for="campanha in campanhas"
              :key="campanha.id"
              :class="{ selected: selected?.id === campanha.id }"
              @click="open(campanha.id)"
            >
              <td>{{ campanha.nome }}</td>
              <td>{{ labelPublico(campanha.tipoPublico) }}</td>
              <td>{{ localizacao(campanha) }}</td>
              <td>{{ campanha.operadora }}</td>
              <td>{{ money(campanha.orcamentoDiario) }}</td>
              <td><span class="status">{{ campanha.status }}</span></td>
              <td>{{ date(campanha.dataCriacao) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <aside class="panel details" v-if="selected">
        <p class="eyebrow">Resultado</p>
        <h2>{{ selected.nome }}</h2>
        <RouterLink class="button" :to="`/campanhas/${selected.id}`">Revisar campanha</RouterLink>
        <dl>
          <dt>Título da landing</dt>
          <dd>{{ selected.tituloLandingPage }}</dd>
          <dt>Subtítulo</dt>
          <dd>{{ selected.subtituloLandingPage }}</dd>
          <dt>Botão</dt>
          <dd>{{ selected.textoBotao }}</dd>
          <dt>Mensagem do WhatsApp</dt>
          <dd>{{ selected.mensagemWhatsApp }}</dd>
          <dt>Slug</dt>
          <dd>{{ selected.slug }}</dd>
          <dt>Provider</dt>
          <dd>{{ selected.providerIa || '-' }} <span v-if="selected.modeloIa">/ {{ selected.modeloIa }}</span></dd>
          <dt>Status da geração</dt>
          <dd>{{ selected.erroGeracao || generationStatus(selected) }}</dd>
        </dl>

        <section class="detail-section" v-if="selected.beneficios.length">
          <h3>Benefícios</h3>
          <ul><li v-for="item in selected.beneficios" :key="item">{{ item }}</li></ul>
        </section>

        <section class="detail-section" v-if="selected.perguntasFrequentes.length">
          <h3>FAQ</h3>
          <details v-for="item in selected.perguntasFrequentes" :key="item.pergunta">
            <summary>{{ item.pergunta }}</summary>
            <p>{{ item.resposta }}</p>
          </details>
        </section>

        <section class="detail-section two-cols">
          <div v-if="selected.palavrasChave.length">
            <h3>Palavras-chave</h3>
            <ul><li v-for="item in selected.palavrasChave" :key="item">{{ item }}</li></ul>
          </div>
          <div v-if="selected.palavrasChaveNegativas.length">
            <h3>Negativas</h3>
            <ul><li v-for="item in selected.palavrasChaveNegativas" :key="item">{{ item }}</li></ul>
          </div>
        </section>

        <section class="detail-section" v-if="selected.titulosAnuncios.length">
          <h3>Títulos</h3>
          <div class="chips"><span v-for="item in selected.titulosAnuncios" :key="item">{{ item }}</span></div>
        </section>

        <section class="detail-section" v-if="selected.descricoesAnuncios.length">
          <h3>Descrições</h3>
          <ul><li v-for="item in selected.descricoesAnuncios" :key="item">{{ item }}</li></ul>
        </section>
      </aside>
    </section>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { listarCampanhas, obterCampanha, type Campanha, type TipoPublicoCampanha } from '../services/api';

const route = useRoute();
const router = useRouter();
const campanhas = ref<Campanha[]>([]);
const selected = ref<Campanha | null>(null);
const loading = ref(false);
const error = ref('');

onMounted(load);
watch(() => route.params.id, loadSelected);

async function load() {
  loading.value = true;
  error.value = '';
  try {
    campanhas.value = await listarCampanhas();
    await loadSelected();
  } catch {
    error.value = 'Não foi possível carregar as campanhas.';
  } finally {
    loading.value = false;
  }
}

async function loadSelected() {
  const id = String(route.params.id || '');
  if (!id) {
    selected.value = campanhas.value[0] || null;
    return;
  }

  selected.value = campanhas.value.find((campanha) => campanha.id === id) || await obterCampanha(id);
}

function open(id: string) {
  router.push(`/campanhas/${id}`);
}

function localizacao(campanha: Campanha) {
  return [campanha.regiao, campanha.cidade, campanha.estado].filter(Boolean).join(' / ');
}

function money(value: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
}

function date(value: string) {
  return new Intl.DateTimeFormat('pt-BR').format(new Date(value));
}

function labelPublico(value: TipoPublicoCampanha) {
  const labels: Record<TipoPublicoCampanha, string> = {
    Individual: 'Individual',
    Casal: 'Casal',
    Familia: 'Família',
    Mei: 'MEI',
    Empresa: 'Empresa'
  };
  return labels[value];
}

function generationStatus(campanha: Campanha) {
  if (!campanha.dataGeracao) return 'Geração ainda não concluída.';
  return campanha.duracaoGeracaoMs ? `Gerada em ${campanha.duracaoGeracaoMs} ms.` : 'Gerada com sucesso.';
}
</script>
