# Checklist de homologação funcional — v1.56.0

## Conteúdo e operação

- [x] Dashboard usa identidade autenticada e dados reais do DTO.
- [x] Dashboard organiza resumo, riscos, agenda, operação médica, financeiro e timeline.
- [x] Minha Central e Meu Dia usam `work_items` e mantêm drawer acessível.
- [x] Agenda e Plantões expõem cobertura, período, local, especialidade, status e ação real.
- [x] Escalas e fechamentos preservam contexto e fluxo de estado.
- [x] Saúde 360 mantém a sequência assistencial sem métricas inventadas.
- [x] Pacientes, agendamentos, triagem e consultas não ampliam exposição de dados sensíveis.
- [x] Financeiro, pagamentos e convites usam somente valores/status retornados.
- [x] Relatórios não oferecem CTA falso para recurso futuro.
- [x] Configurações, Admin SaaS, Planos e Onboarding apontam para rotas existentes.

## UX, responsividade e acessibilidade

- [x] Views críticas possuem `pp-page` ou composição equivalente.
- [x] Tabelas operacionais críticas possuem wrapper ou cards mobile.
- [x] Empty states explicam contexto e próxima ação possível.
- [x] Botões alterados possuem `type` explícito.
- [x] Não foi adicionado `href="#"`, `alert()` ou `confirm()`.
- [x] Ícones decorativos usam `aria-hidden`; ações somente por ícone exigem nome acessível.
- [x] Formulários críticos preservam validation summary.
- [x] Nenhum CSS novo ou `!important` foi adicionado.
- [x] Gates de regressão foram ampliados para telas funcionais.

## Breakpoints verificados estaticamente

- [x] 360 / 390 / 430 px: cards mobile e ações empilháveis.
- [x] 768 px: alternância entre cards e tabela do dashboard.
- [x] 1024 / 1366 / 1920 px: grids fluidos, colunas com `min-width: 0` e wrappers responsivos existentes.

## Restrições de ambiente

- [ ] Runtime visual autenticado e screenshots: dependem de .NET e de serviços/API disponíveis.
- [ ] Build/testes .NET: SDK não está instalado no ambiente de execução.
