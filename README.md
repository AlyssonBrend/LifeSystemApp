# Life System — MVP (Fase 1) ✅ completa

RPG da vida real: hábitos geram XP, vícios viram chefes semanais. Regras completas no [PRD.md](PRD.md).

**Estado da Fase 1:** tudo implementado — personagem (level 0 → ∞), 10 atributos como proxy de consistência,
XP com multiplicador de streak e sequência protegida, 6 missões diárias + missão de classe, chefe semanal
(com Enfurecido), conquistas (+1 oculta), moedas/loja, Modo Foco 50/10 com timestamps no servidor, classes
com carência de 30 dias e a classe secreta ✨ (endgame — hoje inalcançável por design: Carisma só ganha fonte
de dados na Fase 3). Fora do MVP por decisão do PRD: missões configuráveis ("depois"), sugestão de reclasse
por divergência de dados (precisa do histórico de atributos da Fase 2) e tudo das Fases 2+.

Suíte de testes: `dotnet test` (57 testes).

## Estrutura

| Pasta | O quê |
|---|---|
| `LifeSystem.Api/` | Backend .NET 8 — **todas** as regras de jogo, auth JWT, EF Core + SQLite (`lifesystem.db`) |
| `lifesystem-web/` | Frontend React + TS (Vite) — PWA instalável no Android/iOS e jogável no navegador |

## Rodar localmente

```bash
# 1. API (porta 5090)
cd LifeSystem.Api
dotnet run

# 2. Web (porta 5173, com proxy /api -> 5090)
cd lifesystem-web
pnpm install
pnpm dev
```

Abra http://localhost:5173, crie seu perfil e jogue. Todo o progresso fica no servidor (SQLite).

## Jogar no celular (mesma rede Wi-Fi)

1. Deixe API + web rodando no PC;
2. No celular, abra `http://<IP-do-PC>:5173` (ex.: http://192.168.0.50:5173);
3. **Android (Chrome):** menu ⋮ → "Adicionar à tela inicial". **iOS (Safari):** compartilhar → "Adicionar à Tela de Início";
4. Se o celular não conectar, libere o Node/dotnet no firewall do Windows quando ele perguntar.

> O modo offline do PWA (service worker) exige HTTPS — ele ativa sozinho quando o app for publicado (Vercel + Railway/Render, ver PRD seção 6). Na rede local o app instala como atalho standalone e funciona 100% online.

## Produção

Roteiro completo em [DEPLOY.md](DEPLOY.md) — Railway (API + PostgreSQL, via `DATABASE_URL`) + Vercel (web, via `VITE_API_URL`). O código já detecta o ambiente sozinho.

## Decisões técnicas

- .NET 8 (LTS instalado na máquina; PRD citava 9 — sem impacto);
- SQLite no dev para zero instalação; mesmo código EF roda PostgreSQL em prod;
- Regra de arquitetura do PRD: XP, streak, dano, chefe, moedas e Modo Foco são calculados **no backend** — o frontend só exibe (`src/lib/api.ts` não tem regra nenhuma);
- Modo Foco 50/10 usa timestamps do servidor: fechar o app não reinicia nem acelera o ciclo (anti-trapaça do PRD 3.9).
