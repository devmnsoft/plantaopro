# Operação Demonstrável — Santa Casa Demonstração

## Visão Geral

Este documento descreve a infraestrutura de dados, jornadas operacionais e roteiro de demonstração para a **Santa Casa Demonstração** cobrindo o ciclo ponta a ponta: **Escalas → Execução → Conferência**.

---

## 1. Contas de Demonstração e Perfis

Todas as contas estão persistidas no PostgreSQL com os IDs estáveis abaixo:

| Perfil | E-mail | Senha | Perfil Canônico | ID Usuário | Visão Principal |
|---|---|---|---|---|---|
| **Super Administrador** | `superadmin@mnsoft.example` | `MnSoft!Demo2026#Admin` | `ADMINISTRADOR_GLOBAL` | `d3f6584c-2c64-4e5a-9ea9-4e1428647510` | Command Center Global da Plataforma (Saúde SaaS) |
| **Gestor Santa Casa** | `gestor@santacasa-demo.example` | `SantaCasa!Demo2026#Gestor` | `ADMINISTRADOR_CLIENTE` | `d3f6584c-2c64-4e5a-9ea9-4e1428647511` | Meu Dia e Command Center da Santa Casa |
| **Médica Demonstração** | `medico@santacasa-demo.example` | `Medico!Demo2026#Acesso` | `MEDICO` | `d3f6584c-2c64-4e5a-9ea9-4e1428647512` | Meu Dia Pessoal (Minha Agenda, Meu Turno, Minha Produção) |

### Contexto Organizacional
- **Cliente:** `SANTA_CASA_DEMONSTRACAO` (`d3f6584c-2c64-4e5a-9ea9-4e1428647501`)
- **Tenant:** `santa-casa-demonstracao` (`d3f6584c-2c64-4e5a-9ea9-4e1428647502`)
- **Unidade / Hospital:** `UNIDADE_DEMO` (`d3f6584c-2c64-4e5a-9ea9-4e1428647504`)
- **Médico ID (Vínculo):** `d3f6584c-2c64-4e5a-9ea9-4e1428647530`
- **Módulos Contratados:** `ESCALAS`, `EXECUCAO`, `CONFERENCIA`

---

## 2. Cenário de Plantões Provisionados

Os plantões cobrem uma janela deslizante calculada dinamicamente:

| Dia | Horário | Status | Médico Escalado | Cobertura / Pendência | Conferência |
|---|---|---|---|---|---|
| **Ontem** | 07:00 – 19:00 | Realizado | Dra. Ana Souza | Concluído com check-in e check-out | Fechamento em conferência (`PENDENTE`) |
| **Hoje** | 07:00 – 19:00 | Confirmado | Dra. Ana Souza | Check-in ativo realizado às 07:01 | Turno em execução + Ocorrência aberta |
| **Amanhã** | 19:00 – 07:00 | Aberto | Dra. Ana Souza (1 vaga) | 1 vaga atribuída + 1 vaga com convite de cobertura pendente | Aguardando turno |
| **+3 dias** | 07:00 – 19:00 | Aberto | *Sem médico* | **Descoberto** (1 vaga aberta exigindo cobertura pelo gestor) | Futuro |

---

## 3. Roteiro Passo a Passo de Demonstração

### Cenário 1: Gestor da Santa Casa (`gestor@santacasa-demo.example`)
1. **Login:** Acessar `/Account/Login` com `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor`.
2. **Meu Dia:**
   - Visualizar os módulos contratados: **Escalas** (Montar cobertura), **Execução** (Acompanhar o turno) e **Conferência** (Fechar o plantão).
   - A Linha do Tempo apresenta os 4 plantões do ciclo operacional (ontem, hoje, amanhã e +3 dias).
   - A Central de Ações exibe ocorrência operacional em acompanhamento e fechamento aguardando conferência.
3. **Command Center:**
   - Clicar no atalho **Command Center**.
   - Notar os KPIs operacionais reais:
     - Plantões críticos e descobertos.
     - Check-ins ativos em execução no dia.
     - Ocorrências operacionais em acompanhamento.
     - Profissionais ativos no tenant.
   - Analisar o grid de cobertura com os riscos calculados pelo algoritmo de proximidade.

### Cenário 2: Médica da Santa Casa (`medico@santacasa-demo.example`)
1. **Login:** Acessar `/Account/Login` com `medico@santacasa-demo.example` / `Medico!Demo2026#Acesso`.
2. **Meu Dia Pessoal:**
   - Notar que **NÃO** há CTA de "Montar escala" ou "Montar cobertura".
   - Os cards refletem seu trabalho: **Minha Agenda**, **Meu Turno** e **Minha Produção**.
   - Na Linha do Tempo:
     - **Ontem:** Plantão realizado com histórico de cumprimento.
     - **Hoje:** Plantão em andamento com check-in ativo das 07:01.
     - **Amanhã:** Plantão noturno atribuído no Pronto Socorro Adulto.
   - Na Central de Ações: Convite de cobertura pendente aguardando resposta.

### Cenário 3: Super Administrador da Plataforma (`superadmin@mnsoft.example`)
1. **Login:** Acessar `/Account/Login` com `superadmin@mnsoft.example` / `MnSoft!Demo2026#Admin`.
2. **Command Center Global:**
   - Apresenta métricas de saúde da plataforma (tenants ativos, clientes ativos e módulos contratados).
   - **Isolamento multi-tenant garantido:** A lista de cobertura individual de plantões da Santa Casa **NÃO** é vazada no nível global.
   - Atalhos de administração direcionam para Clientes, Planos e Gestão SaaS.

---

## 4. Execução Manual dos Seeds

Para provisionar o ambiente local ou reinstalar:

```bash
# 1. Contas e credenciais locais (idempotente)
psql -d postgres -f database/seeds/development/121_acesso_demo_local.sql

# 2. Operação Santa Casa (Escalas, Execução, Conferência)
psql -d postgres -f database/seeds/development/130_operacao_demo_santacasa.sql
```
