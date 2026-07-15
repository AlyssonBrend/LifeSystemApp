# Life System — Fases 1 (Jogo) ✅ e 2 (Corpo) ✅ completas

RPG da vida real: hábitos geram XP, vícios viram chefes semanais. Regras completas no [PRD.md](PRD.md).

**Estado da Fase 1:** tudo implementado — personagem (level 0 → ∞), 10 atributos como proxy de consistência,
XP com multiplicador de streak e sequência protegida, 6 missões diárias + missão de classe, chefe semanal
(com Enfurecido), conquistas (+1 oculta), moedas/loja, Modo Foco 50/10 com timestamps no servidor, classes
com carência de 30 dias e a classe secreta ✨ (endgame — hoje inalcançável por design: Carisma só ganha fonte
de dados na Fase 3). Fora do MVP por decisão do PRD: missões configuráveis ("depois") e sugestão de reclasse
por divergência de dados.

**Estado da Fase 2 (Corpo, PRD 4.2–4.3):** aba 🏋️ Corpo com quatro seções —
- **Força:** registro de cargas (carga × reps → 1RM Epley no servidor), PRs com prêmio (+50 XP +5 🪙, máx. 1
  por exercício/semana), gráfico de evolução, fichas prontas (Full Body, ABC, ABCD) e conselho de progressão
  (carga estagnada em 3 sessões → "+2,5 kg ou +1 rep"). Registrar treino completa a missão 🏋️ do dia;
- **Cardio:** corridas com pace calculado no servidor, PRs por faixa (1/5/10/15/21/42 km — a corrida entra na
  maior faixa que cobre), volume mensal e conquistas (5 km, 10k<60min, 100 km/mês, meia);
- **Dieta:** perfil corporal → Mifflin-St Jeor (calorias, macros, fibras, água) + plano de refeições; a missão
  🍗 Alimentação passa a usar as metas calculadas; disclaimer obrigatório com aceite (PRD 4.5);
- **Rankings:** amigos por código de convite (aceitar cria a amizade), ranking entre amigos (padrão) e geral
  (opt-in, selo "auto-relatado"), força relativa (1RM ÷ peso corporal) como métrica padrão.

**Atributos reais da Fase 2:** com dados, 💪 Força = média do 1RM relativo nos básicos (1×peso≈50 · 1,5×≈70 ·
2×≈90) e 🏃 Resistência = melhor pace 5k + volume mensal — janela móvel de 90 dias (inatividade derruba);
sem dados, valem os proxies do MVP.

Suíte de testes: `dotnet test` (95 testes).

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
