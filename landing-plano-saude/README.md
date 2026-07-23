# Landing Page — Captação de Leads para Plano de Saúde

Landing page estática, responsiva e pronta para publicar gratuitamente no Cloudflare Pages ou GitHub Pages.

## Arquivos

- `index.html`: estrutura da página e formulário.
- `styles.css`: layout responsivo.
- `script.js`: captura de UTM/GCLID e envio para API ou WhatsApp.

## Configuração inicial

Abra `script.js` e altere:

```js
const CONFIG = {
  apiUrl: "",
  whatsappNumber: "5521999999999"
};
```

Use somente números no WhatsApp, incluindo `55` e o DDD.

Enquanto `apiUrl` e `whatsappNumber` estiverem vazios, o formulário funcionará em modo de teste e salvará o último lead no navegador.

## Publicação gratuita no Cloudflare Pages

1. Crie um repositório no GitHub.
2. Envie estes arquivos para a raiz do repositório.
3. No Cloudflare Pages, selecione **Connect to Git**.
4. Selecione o repositório.
5. Em framework, escolha **None**.
6. Deixe o comando de build vazio.
7. Diretório de saída: `/`.
8. Publique.

## Próxima etapa recomendada

Criar uma API `.NET` com:

- `POST /api/leads`;
- validação do formulário;
- banco de dados;
- prevenção de duplicidade;
- armazenamento de UTM e GCLID;
- encaminhamento para a API comercial existente;
- registro de falhas e retentativas.

## Atenção

Antes de anunciar, substitua o nome da marca, os textos de atendimento e acrescente os dados jurídicos/comerciais reais do corretor ou empresa.
