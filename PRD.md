# Life System — PRD (Product Requirements Document)

> **Versão:** 0.9 · **Data:** 06/07/2026 · **Autor:** Alysson
> **Mudanças na 0.2:** adicionada seção 4 — Sistema de Aconselhamento (finanças, dieta, treinos, estudos)
> **Mudanças na 0.3:** adicionada seção 3.8 — Sistema de Moedas (loja de recompensas no MVP, conversão em dinheiro real na Fase 3)
> **Mudanças na 0.4:** adicionada seção 3.9 — Modo Foco 50/10 com cronômetro de progresso (MVP); Não Perturbe nativo na Fase 5
> **Mudanças na 0.5:** seção 4.3 expandida — registro de cargas (PRs com 1RM estimado) e rankings geral/entre amigos na Fase 2
> **Mudanças na 0.6:** cardio na seção 4.3 — corridas com pace, PRs por faixa de distância (5k–42k) e rankings; integração Fit/Health/Strava na Fase 5
> **Mudanças na 0.7:** personagem inicia no level 0; multiplicador de disciplina no XP; atributos viram medições reais 0–100 (seção 3.1) — fim dos pontos de atributo
> **Mudanças na 0.8:** classes escolhíveis com bônus de foco e pisos de manutenção (seção 3.5); sugestão de reclasse pelos dados; classe oculta Transcendente (endgame)
> **Mudanças na 0.9:** classes refeitas em estilo fantasia — Aldeão, Guerreiro, Ranger, Mago, Monge, Paladino, Mercador — com tabela de títulos por tier; classe oculta vira Avatar Transcendente
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
| **2** | Corpo | Módulo de dieta (cálculo de macros, plano de refeições, conselhos alimentares) e academia (exercícios → atributos, registro de cargas/PRs, rankings geral e entre amigos, sugestões de treino) | Atualização do web app |
| **3** | Mente e Bolso | Árvore de conhecimento (skill tree, planos de estudo) e sistema financeiro (registro de economias, **diagnóstico de saúde financeira**, metas, gráficos) | Atualização do web app |
| **4** | IA Mentora | Conselheiro pessoal completo: analisa progresso, adapta dieta/treino/estudos, orienta finanças, sugere desafios, mensagens personalizadas | Integração com API de LLM (Claude) |
| **5** | Mobile | App Flutter (Android/iOS) consumindo a mesma API + notificações push + Modo Foco nativo (Não Perturbe do sistema) | App nas lojas |

**Regra de ouro:** nada da Fase 2+ entra no MVP. A Fase 1 precisa ser divertida sozinha.

---

## 3. Fase 1 — MVP em detalhe

### 3.1 Atributos — espelho da vida real

**Princípio:** os atributos não são pontos de jogo — são **medições da pessoa real**, numa escala 0–100. A Força no app deve corresponder à força de verdade: quem levanta mais peso tem atributo maior. Missões não dão "+1 Força" diretamente; elas geram os **dados reais** que a fórmula de cada atributo lê. *"Você evolui de verdade"* — literalmente.

**Escala universal (0–100):** 0–19 Iniciante · 20–39 Casual · 40–59 Intermediário · 60–79 Avançado · 80–94 Elite · 95–100 Lendário.

| Atributo | Fórmula real (fonte de dados) | Fase da fórmula |
|----------|------------------------------|-----------------|
| 💪 Força | 1RM relativo médio nos básicos (supino, agachamento, terra ÷ peso corporal), via `LiftRecord`. Ex.: supino 1× peso corporal ≈ 50; 1,5× ≈ 70; 2× ≈ 90 | Fase 2 |
| 🏃 Resistência | Melhor pace 5k + volume mensal de km, via `CardioLog`. Ex.: 5k em 30min ≈ 40; em 25min ≈ 60; em 20min ≈ 85 | Fase 2 |
| ❤️ Vitalidade | Adesão às metas nutricionais nos últimos 30 dias | Fase 2 |
| 😴 Recuperação | Constância de sono adequado nos últimos 30 dias | MVP |
| 🧠 Inteligência | Horas de estudo/mês (medidas pelo Modo Foco) | MVP |
| 📚 Conhecimento | Livros concluídos + marcos da árvore de habilidades | Fase 3 |
| 🙏 Espírito | Constância das práticas espirituais nos últimos 30 dias | MVP |
| 💰 Finanças | Nível Financeiro (taxa de poupança + reserva, seção 4.1) | Fase 3 |
| 🤝 Carisma | Interações sociais registradas (tempo de qualidade, contatos) | Fase 3 |
| ⚡ **Disciplina** | **Streak atual + taxa de conclusão de missões (janela de 90 dias) — atributo central** | MVP |

**No MVP**, os atributos cuja fórmula real depende de módulos futuros usam um *proxy de consistência* (ex.: Força = frequência de treino nos últimos 30 dias) e são substituídos pela fórmula real quando o módulo chega — o valor é recalculado, nunca inventado.

**Atributos podem cair.** Como na vida real: parar de treinar por semanas derruba a Força (janela móvel de 30–90 dias). Isso é intencional — o jogo reflete a realidade, não protege dela.

### 3.2 Sistema de XP e níveis

**O personagem começa no LEVEL 0** — todo mundo parte do zero, e o primeiro level up chega já no primeiro dia bem vivido (onboarding motivador).

**Curva de nível** (progressiva, para levels iniciais serem rápidos e viciantes):

```
XP para subir do level N para N+1 = 500 + (N × 200)
```

| Level | XP necessário | Com dia perfeito (~950 XP), leva |
|-------|--------------|----------------------------------|
| 0 → 1 | 500 | primeiro dia |
| 1 → 2 | 700 | ~1 dia |
| 2 → 3 | 900 | ~1 dia |
| 5 → 6 | 1.500 | ~2 dias |
| 10 → 11 | 2.500 | ~3 dias |
| 20 → 21 | 4.500 | ~5 dias |

**Multiplicador de Disciplina 🔥:** subir de level depende de completar as quests diárias **sem falhar**. Cada dia de sequência aumenta o XP ganho:

```
XP recebido = XP base × (1 + 0,02 × dias de streak)   · máximo +40% (20 dias)
```

Quebrar a sequência zera o multiplicador — a constância literalmente acelera a evolução, e falhar custa velocidade (mas nunca XP já ganho).

**Cada level up concede:**
- +50 🪙 (seção 3.8)
- Título novo (ver classes, 3.5)
- Animação de LEVEL UP em tela cheia

*(Level up não dá mais "ponto de atributo livre" — atributos são medições reais, seção 3.1, e não podem ser comprados com pontos.)*

**Anti-inflação:** XP base diário máximo de ~1.200 (missões + bônus); com o multiplicador máximo, teto efetivo de ~1.700. Sem cap, o jogador "farma" registros falsos e o jogo perde sentido.

### 3.3 Missões diárias

Conjunto padrão do MVP (configurável pelo usuário depois):

| Missão | Requisitos | Recompensa | Alimenta o atributo |
|--------|-----------|------------|---------------------|
| 🏋️ Treinar | 1 treino registrado | +250 XP, +25 🪙 | 💪 Força / 🏃 Resistência |
| 🍗 Alimentação | 200g proteína, 30g fibras, 2 frutas, 3L água (checklist) | +180 XP, +18 🪙 | ❤️ Vitalidade |
| 📖 Estudar | 2 horas (via Modo Foco ⏱️, seção 3.9, ou registro manual) | +200 XP, +20 🪙 | 🧠 Inteligência |
| 🙏 Espiritualidade | Bíblia 20min + oração 15min + gratidão 5min (checklist) | +120 XP, +12 🪙 | 🙏 Espírito |
| 💼 Trabalhar | 8 horas | +150 XP, +15 🪙 | 💰 Finanças |
| 😴 Dormir cedo | Registrar sono antes das 23h | +50 XP, +5 🪙 | 😴 Recuperação |

Missões pagam **XP e moedas**; os atributos sobem porque a missão concluída **gera os dados reais** que as fórmulas da seção 3.1 leem (toda missão alimenta ⚡ Disciplina via taxa de conclusão).

**Bônus de dia perfeito** (todas as missões concluídas): +100 XP, +20 🪙 extra e +1 dia de sequência protegida.

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

**Vitória:** +1.000 XP, +200 🪙, título temporário da semana, caixa de recompensa (recompensa real definida pelo próprio jogador antes da semana começar — ex.: "episódio da série", "jantar fora").

**Derrota** (domingo com HP > 0): sem punição de XP; o chefe volta na semana seguinte com nome de "Preguiça Enfurecida" e +10% HP.

Rotação de chefes do MVP: Preguiça, Procrastinação, Gula, Distração, Desânimo.

### 3.5 Classes — escolha com consequências

Todo personagem começa como **🌾 Aldeão** (levels 0–4) — um plebeu comum, antes do despertar. Ao atingir o **level 5**, escolhe sua classe — e a escolha muda o jogo:

| Classe | Atributos primários | Piso de manutenção (escala 0–100) |
|--------|--------------------|------------------------------------|
| ⚔️ **Guerreiro** | 💪 Força, 🏃 Resistência | 🧠 Inteligência ≥ 30 · ❤️ Vitalidade ≥ 40 *(músculo sem mente é só uma arma sem punho)* |
| 🏹 **Ranger** | 🏃 Resistência, ❤️ Vitalidade | 💪 Força ≥ 30 · 📚 Conhecimento ≥ 25 |
| 🧙 **Mago** | 🧠 Inteligência, 📚 Conhecimento | ❤️ Vitalidade ≥ 40 · 💪 Força ≥ 30 *(físico suficiente para manter a saúde em dia — a torre não sustenta um corpo em ruínas)* |
| 🧘 **Monge** | 🙏 Espírito, ⚡ Disciplina | ❤️ Vitalidade ≥ 40 · 🤝 Carisma ≥ 25 |
| 🛡️ **Paladino** | 🙏 Espírito, 💪 Força | 🧠 Inteligência ≥ 30 · 😴 Recuperação ≥ 35 *(fé e ferro, temperados pelo descanso)* |
| 💰 **Mercador** | 💰 Finanças, 🤝 Carisma | 😴 Recuperação ≥ 40 *(anti-burnout)* · 💪 Força ≥ 25 |

**O que a classe muda na prática:**

- **Bônus de foco:** missões ligadas aos atributos primários pagam **+20% XP** — o Guerreiro evolui mais treinando do que estudando, e vice-versa;
- **Missões de classe:** 1 missão diária extra temática (Guerreiro: treino de força; Ranger: corrida/trilha; Mago: leitura profunda; Monge: meditação longa; Paladino: oração + treino; Mercador: revisar finanças);
- **Piso de manutenção:** a classe favorece seus atributos primários, mas **não perdoa negligência** — se um atributo do piso ficar abaixo do mínimo por 7 dias, o bônus de classe é **suspenso** e missões corretivas entram em destaque até recuperar. O Mago não pode virar sedentário; o Guerreiro não pode parar de pensar;
- **Títulos por tier dentro da classe**, com progressão de fantasia própria:

| Levels | ⚔️ Guerreiro | 🏹 Ranger | 🧙 Mago | 🧘 Monge | 🛡️ Paladino | 💰 Mercador |
|--------|-------------|-----------|---------|----------|-------------|-------------|
| 5–9 | Escudeiro | Batedor | Aprendiz | Noviço | Escudeiro da Fé | Ambulante |
| 10–19 | Guerreiro | Ranger | Mago | Monge | Paladino | Mercador |
| 20–34 | Cavaleiro | Caçador | Feiticeiro | Asceta | Cruzado | Magnata |
| 35–49 | Campeão | Andarilho | Arquimago | Ancião | Templário | Barão |
| 50+ | Senhor da Guerra | Lorde das Trilhas | Arcano | Iluminado | Guardião Sagrado | Rei Mercador |

**A classe se ajusta a você:** o sistema monitora os atributos reais (seção 3.1). Se os seus dados divergirem da classe escolhida por 14 dias (ex.: um Mago cujo maior crescimento é Força), o app sugere: *"Seus dados contam outra história — deseja empunhar a espada como Guerreiro?"*. Trocar de classe é livre, com carência de **30 dias** entre trocas (evita farm de bônus). O nível e o XP nunca se perdem na troca.

**🔒 Classe oculta *(não aparece no app — é descoberta)*:** quando **todos os 10 atributos atingem 80+ (Elite) e se mantêm por 30 dias**, o personagem desperta a classe secreta **✨ Avatar Transcendente** — aquele que dominou espada, grimório, fé e ouro. Título exclusivo, aura dourada na interface, +10% XP permanente em tudo e a conquista oculta "Sem Pontos Fracos". Na tela de classes ela aparece apenas como `???` — a surpresa é parte da recompensa. *(Como atributos podem cair, manter a Transcendência exige manter a vida inteira em dia — é o endgame.)*

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
LEVEL 6 · Escudeiro ⚔️
Alysson
EXP ▓▓▓░░░░░░░ 425/1.700     HP 100% · Energia 84%
⚡21 💪17 🧠13 🙏9 · 🪙340 · 💰£2.380 · 🔥12 dias
[ Missões de hoje ]  [ Chefe da semana ]  [ Loja ]  [ Conquistas ]
```

- **HP** = média dos últimos 7 dias de Vitalidade/Recuperação (dormir mal e comer mal "machucam").
- **Energia** = qualidade do sono da última noite (registro manual no MVP).

### 3.8 Sistema de Moedas 🪙

Além de XP, toda missão concede **Moedas** — a economia do jogo. XP mede progresso (só sobe); moedas são **gastáveis** e criam decisões.

**Ganho** (regra geral: `moedas = XP da missão ÷ 10`):

| Fonte | Moedas |
|-------|--------|
| Missões diárias | +5 a +25 🪙 (tabela 3.3) |
| Dia perfeito | +20 🪙 |
| Level up | +50 🪙 |
| Chefe derrotado | +200 🪙 |
| Conquista desbloqueada | +100 🪙 |

Ganho máximo teórico: ~115 🪙/dia + bônus semanais ≈ **~1.000 🪙/mês** para um jogador consistente.

**Gasto — Loja de Recompensas (MVP):** o próprio jogador cadastra recompensas reais e define o preço em moedas:

| Recompensa (exemplos) | Preço |
|----------------------|-------|
| 1 episódio de série | 50 🪙 |
| Sobremesa livre | 80 🪙 |
| Jogar 2h de videogame | 100 🪙 |
| Jantar fora | 300 🪙 |
| Comprar algo da wishlist | 800 🪙 |

Isso transforma o lazer em recompensa **comprada com disciplina** em vez de culpa.

**Conversão em dinheiro real (Fase 3):** as moedas **não são pagas pelo app** — elas desbloqueiam o **orçamento de recompensa do próprio jogador**. Funciona assim:

1. No módulo financeiro, o jogador define um orçamento mensal de recompensa (ex.: £100 — sai da fatia "desejos" do 50/30/20);
2. Taxa de conversão: **10 🪙 = £1** (ajustável), com teto = orçamento definido;
3. Moedas convertidas viram "saldo liberado para gastar sem culpa"; moedas não convertidas acumulam.

Assim o dinheiro é do próprio jogador, o app nunca deve nada a ninguém, e o sistema ainda **reforça** a saúde financeira (só converte quem definiu orçamento — ou seja, quem organizou as finanças). Um mês perfeito (~1.000 🪙) libera até £100 do próprio orçamento: gastar passa a ser algo que se **conquista**.

### 3.9 Modo Foco ⏱️ (estudos)

Na área de estudos, o jogador pode ativar o **Modo Foco**: ciclos de **50 minutos de foco + 10 minutos de descanso** (técnica Pomodoro adaptada), com cronômetro visual de progresso.

**Durante o foco (50 min):**
- O app entra em **tela de foco**: cronômetro circular em destaque mostrando tempo restante, % do ciclo e ciclo atual (ex.: `2/3 · 31:24 restantes`);
- Navegação do app bloqueada (só "Desistir do ciclo" disponível);
- Sair da tela/abandonar por mais de 30s **invalida o ciclo** (sem punição — ele só não conta).

**Durante o descanso (10 min):**
- Cronômetro inverte a cor e conta os 10 minutos de relaxamento;
- Sugestões rápidas na tela: levantar, alongar, beber água, respirar — **longe de telas**;
- Ao fim, alerta sonoro/notificação convida para o próximo ciclo.

**Integração com o jogo:**
- Cada ciclo completo alimenta o progresso da missão 📖 Estudar (50/120 min) — o cronômetro vira o **registro honesto** do estudo, sem digitar nada;
- Bônus de foco ininterrupto: +10 XP e +1 🪙 por ciclo completo (além do XP da missão);
- Sessões contam para a árvore de conhecimento na Fase 3 (tempo por habilidade).

**Controle real do celular (modo não perturbe):**
- **MVP (web/PWA):** o foco vale dentro do app + notificações de início/fim. Navegador não tem permissão para ativar o "não perturbe" do sistema — limitação dos sistemas operacionais, não do projeto;
- **Fase 5 (Flutter):** integração nativa — Android: ativar Não Perturbe via permissão de política de notificações; iOS: acionar o modo Foco via Atalhos/App Intents (limitações da Apple documentadas na fase).

**Anti-trapaça leve:** início e fim do ciclo são registrados **no backend** (timestamps); o cronômetro do frontend é só exibição — fechar e reabrir o app não reinicia nem acelera o ciclo.

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

**Integração com o jogo:** bater a meta de poupança do mês = missão mensal (+300 XP, +30 🪙); o Nível Financeiro alimenta o atributo 💰 Finanças (seção 3.1) e subir de nível desbloqueia conquista; a dívida quitada pode virar um "chefe" derrotado (ex.: CHEFE: Cartão de Crédito 💳). É aqui que se define o **orçamento de recompensa** que habilita a conversão de moedas em dinheiro real (seção 3.8).

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

### 4.3 Treinos e Força 🏋️ (Fase 2)

Catálogo de exercícios com atributos (Supino → +Força, Corrida → +Resistência, Alongamento → +Mobilidade) e **fichas prontas por objetivo e frequência** (ABC, ABCD, full body 3×). Conselhos de progressão: "Você repetiu a mesma carga no supino por 3 semanas — tente +2,5 kg ou +1 repetição."

**Registro de cargas (PRs — recordes pessoais):**

Em cada exercício, o jogador registra quanto peso levanta: `carga × repetições`. O sistema:

- Calcula o **1RM estimado** (fórmula de Epley: `carga × (1 + reps/30)`) — padroniza a comparação mesmo com repetições diferentes;
- Mantém o **histórico com gráfico de evolução** por exercício;
- Celebra recorde novo: animação **NOVO RECORDE 💥** + 50 XP + 5 🪙 (máx. 1 PR premiado por exercício por semana, para não virar farm);
- Conquistas de força: "Supino com o peso do corpo", "Agachamento 100 kg", "Primeiro PR".

**Rankings 🏆:**

| Ranking | Como funciona |
|---------|---------------|
| **Entre amigos** | Posição entre os amigos adicionados, por exercício e no geral — a visão padrão |
| **Geral** | Posição entre todos os usuários do app, com faixas por peso corporal |

Regras de design:

- A métrica padrão é a **força relativa** (`1RM ÷ peso corporal`) — senão a pessoa mais pesada sempre vence; um toque alterna para carga absoluta;
- Ranking geral exibe o selo *"auto-relatado"* — sem verificação por vídeo, ele é informativo, não competitivo-oficial; o ranking **entre amigos** é o foco (amigos se conhecem, trapaça morre socialmente);
- Privacidade: participar dos rankings é **opt-in**; dá para compartilhar só exercícios escolhidos;
- Sistema de amigos: convite por código/link; aceitar cria a amizade (sem feed social — só rankings, por enquanto).

**Cardio e corrida 🏃:**

O jogador registra cada corrida com **distância + tempo**, e o sistema calcula o **pace** (min/km) — a métrica universal de corrida. Mesma estrutura da força:

- **Histórico com gráfico**: evolução do pace e volume semanal/mensal (km acumulados);
- **PRs por distância padrão**: melhor pace em cada faixa — 1 km, 5 km, 10 km, 15 km, 21 km (meia) e 42 km (maratona). Uma corrida entra na faixa da maior distância padrão que ela cobre (ex.: corrida de 7 km conta para a faixa de 5 km);
- Recorde de pace numa faixa: **NOVO RECORDE 💥** + 50 XP + 5 🪙 (máx. 1 PR premiado por faixa por semana);
- Corridas concedem +Resistência (e alimentam a missão de treino do dia);
- Conquistas de cardio: "Primeiros 5 km", "10 km abaixo de 60 min", "100 km no mês", "Primeira meia-maratona".

**Rankings de corrida 🏆** — mesmas regras dos rankings de força:

| Ranking | Métrica |
|---------|---------|
| **Entre amigos** | Melhor pace por faixa de distância (5k, 10k, 21k…) + km acumulados no mês — visão padrão |
| **Geral** | Melhor pace por faixa de distância, com selo "auto-relatado" |

- Pace já é uma métrica naturalmente relativa (não depende de peso corporal), então dispensa normalização;
- Opt-in, privacidade por faixa de distância, e ranking de amigos como foco — idêntico à força;
- Fase 5 (Flutter): importar corridas automaticamente do **Google Fit / Apple Health / Strava** — elimina o auto-relato e dá credibilidade ao ranking geral.

### 4.4 Estudos 📖 (Fase 3)

Na árvore de conhecimento, cada habilidade (C#, JavaScript, React, .NET, Inglês, Finanças…) tem trilha com marcos. O sistema sugere: sessões com o **Modo Foco 50/10** (seção 3.9, com o tempo de cada sessão creditado à habilidade estudada), revisão espaçada dos tópicos marcados como difíceis, e equilíbrio ("Você estudou só C# este mês — 30 min de Inglês destravam a missão bônus").

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
               classe_escolhida, classe_suspensa_bool, streak_dias,
               hp_pct, energia_pct)   # level inicia em 0 · classe escolhida no lv. 5
ClassHistory  (id, character_id, classe, escolhida_em)   # carência de 30 dias entre trocas
Attribute     (id, character_id, tipo, valor_0_100, fonte, recalculado_em)
              # 10 tipos, enum · valor recalculado das fórmulas da seção 3.1, nunca editado direto
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
Wallet        (id, character_id, saldo_moedas)
CoinTransaction (id, wallet_id, tipo, valor, origem_ref, criado_em)  # ganho/gasto/conversão — extrato auditável
ShopItem      (id, character_id, nome, preco_moedas, ativo)
FocusSession  (id, character_id, tipo, iniciada_em, encerrada_em,
               status, skill_node_id?)  # tipo: foco/descanso · status: completa/abandonada
```

Fases 2–3 adicionam: `BodyProfile` (peso, altura, idade, objetivo), `MealPlan`, `Exercise`, `WorkoutLog`, `LiftRecord` (exercício, carga, reps, 1RM estimado, data — histórico de PRs), `CardioLog` (tipo, distância, duração, pace calculado, faixa de distância, data), `Friendship` (solicitante, aceitante, status), `RankingPreference` (opt-in, exercícios/faixas visíveis), `FinanceProfile` (renda, despesas, economias, dívidas), `FinanceGoal`, `AdviceLog` (conselhos gerados e se foram seguidos), `SkillNode`, `SkillProgress`.

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
- Catálogo de exercícios, registro de cargas (PRs), corridas com pace e rankings → Fase 2 (seção 4.3)
- Registro de economias e diagnóstico de saúde financeira → Fase 3 (seção 4.1)
- Árvore de habilidades e planos de estudo → Fase 3 (seção 4.4)
- IA Mentora → Fase 4 (seção 4.5)
- Outras features sociais (feed, guildas, desafios em grupo) → backlog futuro *(rankings de força já têm fase: 2)*

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
| App "pagar" usuários quebrar o modelo (legal e financeiro) | Conversão de moedas usa o orçamento do próprio jogador (seção 3.8) — o app nunca movimenta nem deve dinheiro; sem gateway de pagamento |
| Inflação de moedas desvalorizar a loja | Ganho atrelado ao XP (÷10), que já tem cap diário; extrato auditável em `CoinTransaction` |
| Cargas falsas poluírem o ranking global | Ranking padrão é entre amigos (controle social); global com selo "auto-relatado" e força relativa por faixa de peso; PR premiado limitado a 1/exercício/semana |
| Rankings desmotivarem iniciantes | Métrica padrão é força relativa (1RM ÷ peso corporal); rankings são opt-in; a comparação central do jogo continua sendo consigo mesmo (PRs) |

---

## Próximos passos

1. ✅ PRD (este documento)
2. ⬜ Validar/ajustar regras de XP e a lista de missões padrão
3. ⬜ Protótipo visual das 3 telas (painel, missões, chefe)
4. ⬜ Scaffold do projeto: `LifeSystem.Api` (.NET 9) + `lifesystem-web` (React)
5. ⬜ Implementar núcleo: personagem + missões + XP/level
6. ⬜ Chefe semanal + streak + conquistas
7. ⬜ Deploy e começar os 30 dias de uso real
