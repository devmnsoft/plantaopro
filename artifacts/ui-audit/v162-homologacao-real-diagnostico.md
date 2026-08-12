# v1.62 — diagnóstico de homologação real

## Escopo e limite desta execução

Em 12/08/2026 o contêiner não possuía o executável `dotnet`. Portanto, esta revisão é uma **auditoria estrutural estática de Razor, CSS e JavaScript**; runtime, autenticação e screenshots permanecem pendentes. Nenhuma conclusão de aprovação visual em navegador é feita neste documento.

O smoke `scripts/ui/visual-smoke.mjs` foi preparado para produzir evidência em sete viewports, separar a página pública da sessão autenticada e falhar explicitamente quando `PLANTAOPRO_STORAGE_STATE` não for informado.

## Diagnóstico por rota

| Rota | Status da análise | Estrutura encontrada | Visual / responsividade | Formulário | Card / tabela | Drawer | Correção aplicada | Pendência real |
|---|---|---|---|---|---|---|---|---|
| `/Account/Login` | Estática | `pp-auth-page`, card e formulário `pp-form` | Estrutura responsiva e benefícios presentes | erros associados, Caps Lock e senha acessível | n/a | n/a | cobertura no smoke v1.62 | screenshot público |
| `/AdminSaas/Index` | Estática | `pp-page`, hero, KPIs e áreas administrativas | grid e ações dentro do container | n/a | cards condicionados ao modelo | n/a | asserção específica de raiz | screenshot autenticado |
| `/Home/Dashboard` | Estática | shell e composição de página | container compartilhado | n/a | composição responsiva | drawer global | cobertura ampliada | runtime |
| `/MinhaCentral` | Estática | central, filtros e kanban | shell compartilhado | filtros reais | cards operacionais | drawer de item acessível | cobertura ampliada | runtime |
| `/MeuDia` | Estática | página operacional | shell compartilhado | n/a | cards provenientes do backend | drawer global | cobertura ampliada | runtime |
| `/Agenda` | Estática | página e agenda | wrapper responsivo | filtros reais | tabela protegida | drawer global | cobertura ampliada | runtime |
| `/Plantoes` | Estática | página operacional | alternativa responsiva | filtros reais | tabela responsiva | detalhe real | cobertura ampliada | runtime |
| `/Escalas` | Estática | página operacional | alternativa responsiva | filtros reais | tabela responsiva | detalhe real | cobertura ampliada | runtime |
| `/Saude360` | Estática | jornada em oito etapas e estados vazios | workspace clínico | n/a | indicadores somente do modelo | drawer global | cobertura ampliada | dados e screenshot |
| `/Pacientes` | Estática | listagem longitudinal | tabela/cards responsivos | busca real | conteúdo condicionado | detalhe global | cobertura ampliada | validar foco no navegador |
| `/Agendamentos` | Estática | recepção e ações transacionais | tabela/contexto mobile | modal de motivo | campos operacionais reais | diálogo acessível | cobertura ampliada | chamada sem endpoint permanece indisponível |
| `/Triagem` | Estática | fila e formulário clínico | composição clínica | limites e validação server-side | fila condicionada | diálogo global | cobertura ampliada | validação visual autenticada |
| `/Consultas` | Estática | atendimento e histórico | composição clínica | ações e campos clínicos | estados do modelo | detalhe global | cobertura ampliada | validação LGPD em runtime |
| `/Pagamentos` | Estática | central financeira `pp-page` | tabela com `data-label` | filtros GET | totais calculados da página | detalhe global | cobertura ampliada | screenshot autenticado |
| `/Financeiro` | Estática | resumo e consolidação reais | tabela em wrapper | filtros existentes | sem valores sintéticos | detalhe global | rota adicionada ao smoke | screenshot autenticado |
| `/Relatorios` | Estática | biblioteca de rotas existentes | cards empilháveis | n/a | recursos futuros sem CTA | n/a | rota adicionada ao smoke | executar exportações com permissão |
| `/Configuracoes` | Estática | central por responsabilidade | cards responsivos | perfil real | atalhos com rotas | n/a | cobertura ampliada | conferir permissões por perfil |

## Execução local obrigatória

1. Instale o SDK definido pelo projeto, restaure e execute `dotnet run --project backend/PlantaoPro.Web`.
2. Autentique-se com uma conta de homologação sem inserir credenciais no repositório.
3. Salve o estado Playwright em arquivo fora do Git e execute:
   `PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 PLANTAOPRO_STORAGE_STATE=/caminho/auth.json node scripts/ui/visual-smoke.mjs`.
4. Revise as imagens em `artifacts/ui-audit/screenshots/v162/`; uma execução sem estado autenticado falha de propósito.

