# Roteiro de Homologação — ADMINISTRADOR_GLOBAL

## Cenários

> Preencher **Resultado obtido**, **Status** e **Observações** durante a execução.

### 1) Login
- **Objetivo do teste:** Validar autenticação do administrador global.
- **Usuário de teste:** admin@plantaopro.com
- **Pré-condições:** Usuário ativo, senha válida, API e Web em execução.
- **Passo a passo:**
  1. Acessar `/Account/Login`.
  2. Informar credenciais válidas.
  3. Clicar em entrar.
- **Resultado esperado:** Redirecionamento para Dashboard com sessão autenticada.
- **Resultado obtido:**
- **Status:** Pendente
- **Observações:**

### 2) Dashboard
- **Objetivo do teste:** Validar KPIs, cards e acessos rápidos.
- **Usuário de teste:** admin@plantaopro.com
- **Pré-condições:** Base com seed de demonstração.
- **Passo a passo:**
  1. Acessar Dashboard.
  2. Verificar cards KPI e módulos principais.
- **Resultado esperado:** Painel carregado sem erro técnico/stack trace.
- **Resultado obtido:**
- **Status:** Pendente
- **Observações:**

### 3) Clientes / Planos / Assinaturas / Usuários / Auditoria / Observabilidade / Integrações / Relatórios
- **Objetivo do teste:** Validar governança SaaS e rastreabilidade.
- **Usuário de teste:** admin@plantaopro.com
- **Pré-condições:** Dados de seed disponíveis.
- **Passo a passo:**
  1. Navegar em cada módulo.
  2. Aplicar filtros e paginação.
  3. Executar uma ação crítica (quando existir) e validar toast + auditoria.
- **Resultado esperado:** Navegação íntegra, sem links mortos e com controle de acesso adequado.
- **Resultado obtido:**
- **Status:** Pendente
- **Observações:**
