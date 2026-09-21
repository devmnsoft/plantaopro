# Prompt de Engenharia: Cliente Genérico, Ciclo Operacional Completo e Motor de Alertas

> **Repositório:** `https://github.com/devmnsoft/plantaopro`  
> **Base:** `main`  
> **Branch de Entrega:** `feat/cliente-generico-ciclo-operacional-alertas`  
> **Stack:** ASP.NET Core (.NET 10), C# 10, Dapper, PostgreSQL, MVC Razor.

---

## 1. Contexto e Premissas Inegociáveis

Implementar suporte nativo a clientes/tenants genéricos no PlantãoPro, garantindo isolamento estrito multi-tenant, ciclo operacional completo e motor proativo de alertas operacionais, sem depender de sementes ou hardcodes específicos.

### Premissas:
1. **Neutralidade de Marca:** "Santa Casa Demonstração" é estritamente um cliente/tenant modelo de laboratório para testes de desenvolvimento (`DemoSeed:Enabled = true`). NUNCA deve ser tratada como perfil, regra de negócio ou marca em telas autenticadas de produção. As telas devem utilizar terminologia genérica e contextualizada: "Sua unidade", "Seu dia", "Clientes da plataforma", "Organização selecionada".
2. **Perfis Canônicos:** Alinhamento estrito a `RolesConstants` em `PlantaoPro.Web.Security` e `PlantaoPro.Api.Constants`:
   - Plataforma: `ADMINISTRADOR_GLOBAL`, `SUPORTE`, `AUDITOR`.
   - Gestão de Tenant: `ADMINISTRADOR_CLIENTE`, `ADMINISTRADOR`, `DIRETOR`, `COORDENACAO`, `OPERADOR`, `HOSPITAL`, `FINANCEIRO`.
   - Assistencial: `MEDICO`.
3. **Autenticação e Sessão:** 100% PostgreSQL + BCrypt. Sem senhas hardcoded em código versionado ou arquivos base `appsettings.json`. O endpoint `POST api/auth/refresh-context` e a ação `Account/RefreshContext` devem permitir recarregar claims, módulos contratados e dados do tenant sem deslogar o usuário.
4. **Isolamento Multi-Tenant:** Toda consulta de plantões, escalas, presenças e financeiro deve filtrar estritamente por `(p.tenant_id = @tenantId or p.cliente_id = @tenantId)`. O `ADMINISTRADOR_GLOBAL` enxerga a plataforma (saúde SaaS, clientes, contratos), nunca a grade interna de um cliente como se fosse global.
5. **Componentes Padronizados:** Reutilizar o design system existente:
   - Toasts: `window.PlantaoProToast` (`plantaopro-toast.js`).
   - Modais de Confirmação: Ações irreversíveis utilizam `data-confirm="true"` com modal acessível WAI-ARIA (`#pp-confirm-modal`). Proibido o uso de `window.alert()` ou `window.confirm()`.
   - Banners de Alerta: `.pp-alert-banner` para riscos contextuais na tela.
   - Mensageria em Tempo Real: SignalR no hub `/hubs/notificacoes`.

---

## 2. Ciclo Operacional

Implementar e validar o ciclo operacional de ponta a ponta:
1. **Onboarding / Criação do Cliente:** Provisionamento atômico de `tenant`, `cliente`, `unidade`, `hospital`, `assinatura` e módulos operacionais (`ESCALAS`, `EXECUCAO`, `CONFERENCIA`), gerando usuário gestor inicial com senha protegida por BCrypt.
2. **Planejamento de Escala:** Publicação de plantões e vagas vinculadas à unidade e especialidade.
3. **Convites e Aceite:** Aceite concorrente protegido por transação ACID no PostgreSQL com `pg_advisory_xact_lock` e validação `expira_em is null or expira_em > now()`, assegurando preenchimento seguro de vagas.
4. **Execução (Check-in / Check-out):** Registro de entrada e saída com timestamp do servidor (`checkin_recebido_em`) e fuso horário do navegador. Opção de proposta de correção com preservação do registro original.
5. **Conferência de Execução:** Comparação de horários (previsto, registrado, proposto e aprovado) pela coordenação. Bloqueio de auto-aprovação pelo médico que solicitou ajuste.
6. **Fechamento Financeiro:** Liquidação com registro de auditoria imutável e suporte a contestação com preservação de histórico.

---

## 3. Motor de Alertas (`IAlertRuleService`)

Motor determinístico de alertas acoplado à Central Operacional e Centro de Comando:
- `ESCALA_SEM_COBERTURA`: Identifica plantões sem profissional alocado nas próximas 48 horas.
- `CHECKIN_PENDENTE`: Identifica plantões iniciados há mais de 15 minutos sem registro de entrada.
- `CONVITE_PENDENTE`: Monitora convites pendentes de resposta médica.
- `OCORRENCIA_ABERTA`: Acompanha ocorrências operacionais não resolvidas.
- `FECHAMENTO_FINANCEIRO_PENDENTE`: Avisa a coordenação sobre plantões encerrados aguardando conferência.
- `PLATAFORMA_CLIENTES_SUSPENSOS`: Regra de governança para o Superadmin.

---

## 4. Requisitos de Testes de Contrato

Criar testes de contrato automatizados em `GenericClientOperationalContractTests.cs` cobrindo:
1. Provisionamento de cliente genérico sem dependência do nome "Santa Casa".
2. Isolamento de dados entre clientes distintos em plantões e escalas.
3. Avaliação determinística de regras do motor de alertas (`IAlertRuleService`).
4. Neutralidade textual de cópia na interface autenticada.
5. Presença de atributos acessíveis de confirmação (`data-confirm="true"`) em botões críticos.
6. Alinhamento e unicidade de perfis em `RolesConstants`.
7. Execução de 100% da suíte de testes com zero falhas.
