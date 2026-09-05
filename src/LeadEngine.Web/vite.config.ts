import { randomUUID } from 'node:crypto';
import { defineConfig, loadEnv, type Plugin } from 'vite';
import vue from '@vitejs/plugin-vue';
import basicSsl from '@vitejs/plugin-basic-ssl';

function publicLandingMockPlugin(): Plugin {
  return {
    name: 'public-landing-mock',
    configureServer(server) {
      server.middlewares.use('/api/publico/campanhas/simulacao-planos-saude/leads', (req, res) => {
        if (req.method !== 'POST') return;

        res.setHeader('Content-Type', 'application/json');
        res.end(JSON.stringify({
          leadId: randomUUID(),
          mensagem: 'Solicitacao recebida. Em breve entraremos em contato pelo WhatsApp.',
          whatsAppUrl: 'https://wa.me/5511999999999?text=Gostaria%20de%20receber%20uma%20cotacao.',
          conversaoConfirmada: true
        }));
      });

      server.middlewares.use('/api/publico/campanhas/simulacao-planos-saude', (req, res) => {
        if (req.method !== 'GET') return;

        res.setHeader('Content-Type', 'application/json');
        res.end(JSON.stringify({
          nome: 'Plano familiar Sao Paulo',
          titulo: 'Plano de saude familiar em Sao Paulo',
          subtitulo: 'Compare alternativas para sua familia com atendimento consultivo e cotacao sem compromisso.',
          textoBotao: 'Receber cotacao no WhatsApp',
          beneficios: [
            'Atendimento personalizado para o seu perfil',
            'Comparacao entre operadoras e faixas de cobertura',
            'Orientacao sobre carencias, rede e documentacao',
            'Cotacao para diferentes quantidades de vidas'
          ],
          perguntasFrequentes: [
            {
              pergunta: 'A cotacao tem custo?',
              resposta: 'Nao. O atendimento inicial e a cotacao sao sem compromisso.'
            },
            {
              pergunta: 'Quais informacoes sao necessarias?',
              resposta: 'Nome, WhatsApp, cidade, estado e quantidade de vidas para montar uma comparacao adequada.'
            },
            {
              pergunta: 'Os valores sao garantidos?',
              resposta: 'Valores e condicoes dependem do perfil informado, regiao, rede, cobertura e regras da operadora.'
            }
          ],
          operadora: 'Diversas operadoras',
          cidade: 'Sao Paulo',
          estado: 'SP',
          tipoPublico: 'Familia',
          mensagemBaseWhatsApp: 'Gostaria de receber uma cotacao de plano de saude familiar.'
        }));
      });
    }
  };
}

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const usePublicLandingMock = env.VITE_MOCK_PUBLIC_LANDING === 'true';

  return {
    base: env.VITE_BASE_PATH || '/leadcaptura/',
    plugins: [vue(), mode === 'development' ? basicSsl() : null, usePublicLandingMock ? publicLandingMockPlugin() : null],
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: env.VITE_API_PROXY_TARGET || 'https://localhost:7238',
          changeOrigin: true,
          secure: false
        }
      }
    }
  };
});
