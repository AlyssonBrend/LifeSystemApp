# Life System — PRD (Product Requirements Document)

> **Versão:** 0.2 · **Data:** 06/07/2026 · **Autor:** Alysson
> **Mudanças na 0.2:** adicionada seção 4 — Sistema de Aconselhamento (finanças, dieta, treinos, estudos)
> **Status:** Rascunho para validação de escopo

---

## 1. Visão

Um "RPG da vida real": o usuário acorda e recebe uma interface de jogo onde, em vez de derrotar monstros, derrota os próprios limites. Cada hábito concluído gera XP e aumenta atributos reais (saúde, disciplina, conhecimento, finanças). A satisfação de progressão de um RPG, com recompensas que existem no mundo real.

**Diferencial central:** o sistema de **Chefes Semanais** — vícios e fraquezas ("Preguiça", "Procrastinação") aparecem como chefes com HP, e cada hábito saudável causa dano neles. Nenhum app de hábitos popular (Habitica, Streaks) tem essa mecânica.

**Princípio de design:** Disciplina é o atributo central do jogo — quase toda missão concede Disciplina além do atributo específico.

---

## 2. Escopo por fases

O conceito completo abrange ~5 produtos em um (hábitos + finanças + dieta + estudos + IA). Para o projeto não morrer antes do primeiro level up, o desenvolvimento é dividido em fases com entregas jogáveis:

| Fase | Nome | Conteúdo | Entrega |
|------|------|----------|---------|
| **1** | **MVP — O Jogo** | Personagem, atributos, XP/level, missões diárias, sequência (streak), chefe semanal, conquistas básicas | Web app (PWA) jogável |
| **2** | Corpo | Módulo de dieta (cálculo de macros, plano de refeições, conselhos alimentares) e academia (exercícios → atributos, sugestões de treino) | Atualização do web app |
| **3** | Mente e Bolso | Árvore de conhecimento (skill tree, planos de estudo) e sistema financeiro (registro de economias, **diagnóstico de saúde financeira**, metas, gráficos) | Atualização do web app |
| **4** | IA Mentora | Conselheiro pessoal completo: analisa progresso, adapta dieta/treino/estudos, orienta finanças, sugere desafios, mensagens personalizadas | Integração com API de LLM (Claude) |
| **5** | Mobile | App Flutter (Android/iOS) consumindo a mesma API + notificações push | App nas lojas |

**Regra de ouro:** nada da Fase 2+ entra no MVP. A Fase 1 precisa ser divertida sozinha.

---

## 3. Fase 1 — MVP em detalhe

### 3.1 Atributos

| Atributo | Ícone | Fonte de progresso |
|----------|-------|--------------------|
| Vitalidade | ❤️ | Alimentação saudável |
| Força | 💪 | Treinos de musculação |
| Resistência | 🏃 | Cardio |
| Inteligência | 🧠 | Estudos |
| Conhecimento | 📚 | Leitura de livros |
| Espírito | 🙏 | Oração, meditação, gratidão |
| Finanças | 💰 | Economizar e investir |
| Recuperação | 😴 | Sono |
| Carisma | 🤝 | Relacionamentos |
| **Disciplina** | ⚡ | **Todas as missões — atributo central** |

No MVP, atributos são apenas contadores que sobem via missões e pontos de level up. Módulos dedicados (dieta, finanças) chegam nas fases seguintes.

### 3.2 Sistema de XP e níveis

**Curva de nível** (progressiva, para levels iniciais serem rápidos e viciantes):

```
XP para subir do level N para N+1 = 500 + (N × 200)
```

| Level | XP necessário | Com dia perfeito (~950 XP), leva |
|-------|--------------|----------------------------------|
| 1 → 2 | 700 | ~1 dia |
| 2 → 3 | 900 | ~1 dia |
| 5 → 6 | 1.500 | ~2 dias |
| 10 → 11 | 2.500 | ~3 dias |
| 20 → 21 | 4.500 | ~5 dias |

**Cada level up concede:**
- +1 ponto de atributo livre (o jogador distribui)
- Título novo (ver classes, 3.5)
- Animação de LEVEL UP em tela cheia

**Anti-inflação:** XP diário máximo limitado a ~1.200 (missões + bônus). Sem cap, o jogador "farma" registros falsos e o jogo perde sentido.

### 3.3 Missões diárias

Conjunto padrão do MVP (configurável pelo usuário depois):

| Missão | Requisitos | Recompensa |
|--------|-----------|------------|
| 🏋️ Treinar | 1 treino registrado | +250 XP, +1 Força, +5 Disciplina |
| 🍗 Alimentação | 200g proteína, 30g fibras, 2 frutas, 3L água (checklist) | +180 XP, +1 Vitalidade, +3 Disciplina |
| 📖 Estudar | 2 horas | +200 XP, +2 Inteligência, +3 Disciplina |
| 🙏 Espiritualidade | Bíblia 20min + oração 15min + gratidão 5min (checklist) | +120 XP, +2 Espírito, +3 Disciplina |
| 💼 Trabalhar | 8 horas | +150 XP, +2 Disciplina |
| 😴 Dormir cedo | Registrar sono antes das 23h | +50 XP, +1 Recuperação, +2 Disciplina |

**Bônus de dia perfeito** (todas as missões concluídas): +100 XP extra e +1 dia de sequência protegida.

**Sequência (streak):** dias consecutivos com ≥1 missão principal concluída. Quebrar a sequência não tira XP (punição gera abandono), mas zera o contador e o chefe da semana **recupera 100 HP**.

### 3.4 Chefes semanais

Toda segunda-feira surge um chefe. O HP dele é reduzido pelos hábitos da semana:

```
CHEFE: Preguiça 👹
HP: 1.200
Ataques (o que ele "quer" que você faça): ficar na cama, redes sociais, comida ruim, desistir
```

| Hábito | Dano |
|--------|------|
| Treino | −120 HP |
| Estudo (2h) | −80 HP |
| Oração | −50 HP |
| Boa alimentação | −100 HP |
| Dormir cedo | −150 HP |

**Balanceamento:** dia perfeito causa ~500 de dano → chefe de 1.200 HP cai em ~3 dias bons; sobra margem para dias ruins. HP do chefe escala com o level do jogador: `HP = 800 + (level × 100)`.

**Vitória:** +1.000 XP, título temporário da semana, caixa de recompensa (recompensa real definida pelo próprio jogador antes da semana começar — ex.: "episódio da série", "jantar fora").

**Derrota** (domingo com HP > 0): sem punição de XP; o chefe volta na semana seguinte com nome de "Preguiça Enfurecida" e +10% HP.

Rotação de chefes do MVP: Preguiça, Procrastinação, Gula, Distração, Desânimo.

### 3.5 Classes e títulos

Classe = **tier pelo level** + **variante pelo atributo dominante**:

| Levels | Tier |
|--------|------|
| 1–4 | Civil |
| 5–9 | Discípulo |
| 10–19 | Guerreiro / Monge / Estudioso / Empreendedor* |
| 20–34 | Líder |
| 35–49 | Mestre |
| 50+ | Lenda |

\* A variante do tier 10–19 é definida pelo maior atributo: Força → **Guerreiro**, Espírito → **Monge**, Inteligência/Conhecimento → **Estudioso**, Finanças → **Empreendedor**. Ex.: "Aprendiz da Disciplina" é o título de Discípulo com Disciplina dominante.

### 3.6 Conquistas (MVP)

- 🔥 7 / 30 / 100 dias de sequência
- 🏋️ 30 dias treinando · 100 dias sem faltar treino
- 🍎 100 refeições saudáveis
- 📚 100 horas estudadas
- 👹 Primeiro chefe derrotado · 10 chefes derrotados
- 💰 Primeiros £10.000 *(registro manual no MVP)*

### 3.7 Tela inicial (MVP)

Painel do personagem no estilo RPG futurista:

```
LEVEL 3 · Aprendiz da Disciplina
Alysson
EXP ▓▓▓▓▓▓░░░░ 425/900     HP 100% · Energia 84%
⚡21 💪17 🧠13 🙏9 · 💰£2.380 · 🔥12 dias
[ Missões de hoje ]  [ Chefe da semana ]  [ Conquistas ]
```

- **HP** = média dos últimos 7 dias de Vitalidade/Recuperação (dormir mal e comer mal "machucam").
- **Energia** = qualidade do sono da última noite (registro manual no MVP).

---

## 4. Sistema de Aconselhamento (Fases 2–4)

O Life System não só registra hábitos — ele **orienta**. O usuário informa seus dados (economias, corpo, rotina) e recebe conselhos práticos de finanças, alimentação, treinos e estudos. O sistema funciona em dois níveis:

- **Nível 1 — Regras determinísticas** (Fases 2–3): conselhos calculados por fórmulas e regras fixas no backend. Sem custo de IA, sempre disponíveis, previsíveis.
- **Nível 2 — IA Mentora** (Fase 4): a API Claude recebe o histórico do jogador e personaliza os conselhos do Nível 1, explica o porquê de cada missão e detecta estagnação.

### 4.1 Saúde financeira 💰 (Fase 3)

O usuário informa: **renda mensal, despesas fixas, despesas variáveis, total economizado, dívidas**.

O sistema devolve um **diagnóstico gamificado**:

| Indicador | Regra | Exemplo de conselho gerado |
|-----------|-------|---------------------------|
| Reserva de emergência | Meta = 6× despesas mensais | "Sua reserva cobre 2,1 meses. Faltam £3.200 para a meta de 6 meses." |
| Regra 50/30/20 | 50% necessidades, 30% desejos, 20% poupança | "Você está poupando 11% da renda. Suba para 20% cortando £180 de desejos." |
| Taxa de poupança | economizado no mês ÷ renda | Vira o **Nível Financeiro** do personagem (E → D → C → B → A → S) |
| Dívidas | juros altos primeiro (método avalanche) | "Quite primeiro o cartão (juros maiores), depois o crediário." |

**Integração com o jogo:** bater a meta de poupança do mês = missão mensal (+300 XP, +3 Finanças); subir de Nível Financeiro desbloqueia conquista; a dívida quitada pode virar um "chefe" derrotado (ex.: CHEFE: Cartão de Crédito 💳).

### 4.2 Alimentação e regime 🍗 (Fase 2)

O usuário informa: **peso, altura, idade, sexo, nível de atividade, objetivo** (emagrecer / manter / ganhar massa).

O sistema calcula (fórmula Mifflin-St Jeor):

```
TMB → gasto diário (× fator de atividade) → calorias-alvo (déficit ou superávit)
→ macros: proteína 1,8–2,2 g/kg · gordura 0,8–1 g/kg · resto em carboidrato
→ fibras ≥ 30 g · água ≥ 35 ml/kg
```

E gera um **plano de refeições** (café, almoço, lanche, jantar, ceia) com alimentos simples e substituições, mais conselhos contextuais: "Seu objetivo é ganhar massa e você bateu só 120g de proteína hoje — inclua 200g de frango no jantar."

A missão diária de Alimentação (seção 3.3) passa a usar **as metas calculadas** em vez de valores fixos.

### 4.3 Treinos 🏋️ (Fase 2)

Catálogo de exercícios com atributos (Supino → +Força, Corrida → +Resistência, Alongamento → +Mobilidade) e **fichas prontas por objetivo e frequência** (ABC, ABCD, full body 3×). Conselhos de progressão: "Você repetiu a mesma carga no supino por 3 semanas — tente +2,5 kg ou +1 repetição."

### 4.4 Estudos 📖 (Fase 3)

Na árvore de conhecimento, cada habilidade (C#, JavaScript, React, .NET, Inglês, Finanças…) tem trilha com marcos. O sistema sugere: sessões com técnica Pomodoro, revisão espaçada dos tópicos marcados como difíceis, e equilíbrio ("Você estudou só C# este mês — 30 min de Inglês destravam a missão bônus").

### 4.5 O que a IA Mentora adiciona (Fase 4)

Tudo acima funciona sem IA. A Fase 4 pluga o Claude para:

- Analisar o progresso semanal e **reescrever os conselhos com o contexto do jogador** (histórico, quedas de streak, chefe atual);
- Adaptar dieta e treino conforme os resultados registrados (peso estagnado → ajustar calorias);
- Criar planos de estudo personalizados por objetivo de carreira;
- Sugerir desafios quando detectar estagnação;
- Mensagens motivacionais baseadas no histórico real, nunca frases genéricas;
- Explicar por que cada missão importa para os objetivos declarados.

> ⚠️ **Aviso obrigatório no app:** os conselhos financeiros e de dieta são educacionais e não substituem profissionais (nutricionista, médico, consultor financeiro). Exigir aceite no primeiro uso de cada módulo.

---

## 5. Modelo de dados (entidades principais)

```
User          (id, email, senha_hash, criado_em)
Character     (id, user_id, nome, level, xp_atual, xp_proximo_level,
               classe, streak_dias, hp_pct, energia_pct)
Attribute     (id, character_id, tipo, valor)           # 10 tipos, enum
MissionTemplate (id, nome, descricao, requisitos_json,
               xp, atributos_recompensa_json, dano_chefe, ativo)
DailyMissionLog (id, character_id, template_id, data,
               status, progresso_json, concluida_em)
Boss          (id, nome, hp_max, semana_inicio, ataques_json)
BossInstance  (id, character_id, boss_id, hp_atual, status)  # ativa/vencida/perdida
DamageEvent   (id, boss_instance_id, mission_log_id, dano, criado_em)
Achievement   (id, nome, criterio_json, icone)
UserAchievement (id, character_id, achievement_id, desbloqueada_em)
RewardBox     (id, character_id, descricao_recompensa_real, aberta_em)
```

Fases 2–3 adicionam: `BodyProfile` (peso, altura, idade, objetivo), `MealPlan`, `Exercise`, `WorkoutLog`, `FinanceProfile` (renda, despesas, economias, dívidas), `FinanceGoal`, `AdviceLog` (conselhos gerados e se foram seguidos), `SkillNode`, `SkillProgress`.

---

## 6. Arquitetura técnica

**Decisão: web-first.** Um web app (PWA, instalável no celular) valida o jogo muito mais rápido que Flutter; o app nativo vem na Fase 5 reaproveitando a mesma API.

| Camada | Tecnologia | Justificativa |
|--------|-----------|---------------|
| Frontend | **React + TypeScript** (Vite), PWA | React já está na sua árvore de estudos; PWA dá "app no celular" sem loja |
| Backend | **.NET 9 Web API** (C#) | Sua stack principal (padrão dos seus projetos GEC-Manager etc.) |
| Banco | **PostgreSQL** + EF Core | Gratuito, robusto, roda em qualquer host |
| Auth | **JWT próprio** no MVP | Simples; Firebase Auth só se precisar de login social |
| IA (Fase 4) | **API Claude** | Análise de progresso e mensagens personalizadas |
| Notificações (Fase 5) | Firebase Cloud Messaging | Junto com o app Flutter |
| Hospedagem MVP | Railway/Render (API+DB) + Vercel (front) | Custo ~zero para 1 usuário |

**Regra de arquitetura:** toda regra de jogo (cálculo de XP, dano, level up, streak) vive **no backend**, nunca no frontend — garante consistência quando o Flutter chegar e evita trapaça client-side.

---

## 7. Fora de escopo do MVP (anotado para não esquecer)

- Cálculo automático de dieta e conselhos alimentares → Fase 2 (seção 4.2)
- Catálogo de exercícios e sugestões de treino → Fase 2 (seção 4.3)
- Registro de economias e diagnóstico de saúde financeira → Fase 3 (seção 4.1)
- Árvore de habilidades e planos de estudo → Fase 3 (seção 4.4)
- IA Mentora → Fase 4 (seção 4.5)
- Multiplayer/social, ranking entre amigos → backlog futuro

---

## 8. Métricas de sucesso do MVP

1. **Você usa o app 30 dias seguidos** (dogfooding — é a única métrica que importa no início).
2. Registrar uma missão leva **< 15 segundos** (senão vira fricção e abandono).
3. Pelo menos 1 chefe derrotado por mês.

## 9. Riscos

| Risco | Mitigação |
|-------|-----------|
| Escopo crescer e o projeto não lançar | Fases fechadas; nada da Fase 2+ entra no MVP |
| Registro manual virar fricção | UX de 1 toque por missão; checklist simples |
| Auto-relato desonesto quebrar o jogo | Jogo pessoal — o custo da trapaça é do próprio jogador; cap de XP diário limita farm |
| Punições gerarem abandono | Quebra de streak não tira XP; derrota para chefe não pune |
| Responsabilidade por conselhos de saúde/finanças | Disclaimer obrigatório (seção 4.5); conselhos baseados em fórmulas consagradas; nunca prescrever medicamentos ou investimentos específicos |
| Custo de API de IA com múltiplos usuários | Nível 1 (regras) cobre 80% do valor sem custo; IA só na Fase 4, com limite de chamadas/dia por usuário |

---

## Próximos passos

1. ✅ PRD (este documento)
2. ⬜ Validar/ajustar regras de XP e a lista de missões padrão
3. ⬜ Protótipo visual das 3 telas (painel, missões, chefe)
4. ⬜ Scaffold do projeto: `LifeSystem.Api` (.NET 9) + `lifesystem-web` (React)
5. ⬜ Implementar núcleo: personagem + missões + XP/level
6. ⬜ Chefe semanal + streak + conquistas
7. ⬜ Deploy e começar os 30 dias de uso real
