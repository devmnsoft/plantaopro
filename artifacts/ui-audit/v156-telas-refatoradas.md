# Telas refatoradas e homologadas — v1.56.0

## Refatoração desta entrega

### Painel operacional (`Views/Home/Dashboard.cshtml`)

- Substituição da saudação fixa “Administrador” pela identidade autenticada, com fallback neutro.
- Remoção do plano “Profissional”, lista prescritiva estática e conteúdo comercial que não vinha do backend.
- Nova hierarquia: resumo operacional, agenda de hoje, riscos de cobertura, operação médica, financeiro e linha do tempo.
- Indicadores derivados exclusivamente de `DashboardOverviewDto` e de suas coleções.
- Filtros com labels, ação de limpeza e sem placeholders como substitutos de rótulos.
- Tabela desktop e cartões mobile para a agenda.
- Empty states específicos quando agenda, riscos, pagamentos ou notificações não possuem dados.
- CTAs somente para controllers/actions existentes no produto.

## Superfícies homologadas

Foram revisadas e mantidas as evoluções funcionais existentes em Minha Central, Meu Dia, Agenda, Plantões, Escalas, fechamentos operacionais, Saúde 360, Pacientes, Agendamentos, Triagem, Consultas, Convites, Pagamentos, Financeiro, Relatórios, Configurações, Admin SaaS, Planos e Onboarding. O diagnóstico detalha o contrato encontrado em cada área.

## Gates atualizados

- `check-layout-regression.py`: amplia o inventário de views críticas e exige adaptação de tabelas operacionais.
- `check-form-experience.py`: detecta botões sem tipo nos formulários POST das views.
- `check-feedback-ui.py`: bloqueia novos `!important` e botões exclusivamente icônicos sem nome acessível nos arquivos alterados.
- `check-saas-ui.py`: valida a composição funcional do dashboard, configurações e planos.

## Pendências reais

- A captura de screenshots não é reproduzível neste container sem o SDK .NET e sem a API autenticada.
- Totais globais adicionais (convites pendentes, tempo médio assistencial e contestações) exigem extensão dos contratos de API; não foram simulados na view.
