# Deploy — Life System online

Roteiro para publicar o MVP (custo ~zero, PRD seção 6). São ações de conta que só você pode fazer;
tudo no código já está pronto (Dockerfile, PostgreSQL, PORT, vercel.json).

## 1. API no Railway (com PostgreSQL)

1. Acesse https://railway.app e entre com sua conta GitHub;
2. **New Project → Deploy from GitHub repo** → escolha `AlyssonBrend/LifeSystemApp`;
3. Em *Settings → Root Directory*, informe `LifeSystem.Api` (o Dockerfile é detectado sozinho);
4. No projeto, **+ New → Database → PostgreSQL** — o Railway injeta `DATABASE_URL` no serviço da API automaticamente (confira em *Variables*; se não, adicione a referência);
5. Em *Variables* da API, adicione:
   - `Jwt__Chave` = uma chave nova e longa (64+ caracteres aleatórios — **não** use a do appsettings.json);
6. Em *Settings → Networking*, clique **Generate Domain** e copie a URL (ex.: `https://lifesystem-api.up.railway.app`);
7. Teste: `https://SUA-API/api/saude` deve responder `{"ok":true}`.

> Alternativa gratuita: Render (https://render.com) — Web Service apontando para `LifeSystem.Api` (Docker) + banco PostgreSQL; mesmos passos e variáveis.

## 2. Web na Vercel

1. Acesse https://vercel.com e entre com GitHub;
2. **Add New → Project** → importe `AlyssonBrend/LifeSystemApp`;
3. Configure:
   - *Root Directory*: `lifesystem-web`
   - *Framework Preset*: Vite (detectado)
   - *Environment Variables*: `VITE_API_URL` = a URL da API do passo 1 (sem barra no final, ex.: `https://lifesystem-api.up.railway.app`)
4. **Deploy**. A URL final (ex.: `https://lifesystem.vercel.app`) é o seu jogo.

## 3. Instalar no celular (agora com PWA completo)

Com HTTPS, o service worker ativa sozinho:

- **Android (Chrome):** abra a URL → banner "Instalar app" (ou menu ⋮ → *Instalar aplicativo*);
- **iOS (Safari):** abra a URL → botão compartilhar → *Adicionar à Tela de Início*.

O app abre em tela cheia, com ícone próprio, e o progresso continua no servidor — o mesmo perfil funciona no PC, no Android e no iOS.

## Checklist pós-deploy

- [ ] `/api/saude` responde na URL do Railway
- [ ] Registro de conta funciona na URL da Vercel
- [ ] Completar uma missão e recarregar — progresso mantido
- [ ] Instalar no celular e completar uma missão por lá — mesmo personagem do PC

## Custos e limites

- Railway: plano hobby com US$5/mês de crédito — sobra para 1 usuário;
- Render free tier: a API "dorme" após 15min sem uso (primeiro request do dia demora ~30s);
- Vercel: gratuito para esse volume.
