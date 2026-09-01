# v2.12.5 — Central de Relatórios, BI e exportações

## Modo e ambiente

- **Modo usado:** MODO DESIGN ESTÁTICO.
- **SDK:** `dotnet` não está instalado (`dotnet: command not found`). Conforme a regra da rodada, não foram alterados C#, banco, migrations, projetos, solução ou contratos.
- **Git:** o checkout não possui remoto configurado. A branch `codex/v2125-relatorios-bi-exportacoes-design` foi criada localmente.

## Inventário encontrado

### Relatórios e dashboards existentes

- A Web já contém `RelatoriosController`, a biblioteca em `Views/Relatorios` e visões de cobertura, convites, produtividade médica, SLA, faturamento SaaS e carteira SaaS.
- A API contém `RelatoriosCentralController`, `RelatoriosSaasController`, relatórios executivos/valor e serviços de relatório. Há rotas legadas sobrepostas para a central e endpoints de CSV/PDF/filtros na implementação Fase 6.
- A carteira SaaS possui consulta paginada e exportação CSV via API. A exportação PDF da implementação Fase 6 declara indisponibilidade; não foi apresentada como funcional nesta evolução.
- Existem dashboards de BI, SaaS, clínica e operação fora da Central. Eles não foram duplicados nem apresentados como novas fontes.

### Lacunas e riscos observados

- Favoritos, recentes e filtros salvos não tinham integração segura e verificável na Web; a central anterior já os marcava como indisponíveis.
- Os filtros financeiros estáticos da central não executavam uma consulta e podiam sugerir uma jornada inexistente; foram removidos da central em favor de comunicação honesta por relatório.
- Algumas rotas Web retornam a própria página `Index` em vez de relatório especializado. O catálogo novo inclui somente as seis jornadas com view/ação dedicada.
- Há mais de uma superfície de relatórios na API. Sem SDK não foi seguro consolidar contratos ou validar materialização Dapper.
- O isolamento por tenant e as permissões dependem da autorização server-side. O filtro visual da central atua somente sobre metadados públicos do catálogo e nunca sobre dados operacionais.
- Os relatórios especializados existentes precisam de uma revisão posterior de orientação, estados e filtros; não foram tocados para evitar alterações de backend sem compilação.

## Evolução de design entregue

- Nova identidade executiva para a Central, com hero, sinais de confiança, orientação de uso e hierarquia responsiva.
- Catálogo em cards com nome, descrição, categoria, escopo de acesso, formatos, última execução honesta e ação principal.
- Busca textual acessível e filtros rápidos por categoria, com resumo, limpeza e contador anunciado por `aria-live`.
- Estados vazio e erro explícitos. O estado carregando não é artificialmente exibido porque o catálogo é renderizado no servidor e não dispara consulta assíncrona.
- Seção transparente para favoritos, recentes e filtros salvos, indicando as dependências reais em vez de simular persistência.
- Layout responsivo em três, duas e uma coluna; chips roláveis no mobile; foco visível; contraste reforçado; suporte a movimento reduzido.

## Relatórios representados no catálogo

| Relatório | Categoria | Escopo comunicado | Formato existente |
| --- | --- | --- | --- |
| Cobertura de plantões | Escalas e plantões | Tenant | Tela |
| Convites e confirmações | Operacional | Tenant | Tela |
| Produtividade profissional | Profissionais | Tenant | Tela |
| Nível de serviço | Suporte | Conforme permissão | Tela |
| Receita e cobrança SaaS | Financeiro | Super Admin | Tela |
| Carteira de clientes SaaS | SaaS e clientes | Super Admin | Tela e CSV |

Nenhum KPI, total, execução recente ou dado operacional foi inventado. A central informa “Não informada” quando não possui fonte para a última execução.

## Filtros e exportações

- Foram implementados somente filtros locais de descoberta do catálogo (texto e categoria), sem pedir IDs e sem enviar dados à API.
- Não foram criados filtros avançados operacionais, pois isso exigiria contratos e validação server-side.
- Nenhuma exportação nova foi criada. A Central sinaliza CSV apenas para a carteira SaaS, que já possuía rota Web/API. Não há Excel apresentado e PDF não é anunciado como disponível.
- Senhas, tokens, secrets, CPF/CNPJ e parâmetros operacionais não são armazenados nem processados pelo novo JavaScript.

## Permissão, tenant e dados sensíveis

- Cards comunicam o escopo, mas não substituem autorização server-side.
- O JavaScript pesquisa apenas título, descrição e categoria presentes no HTML; ele não acessa endpoints, tenant, usuário, storage, cookies ou dados sensíveis.
- A exportação existente continua dependente da autorização e auditoria da API; não houve modificação de seu contrato sem SDK.
- Favoritos e filtros não são persistidos em `localStorage`, evitando escopo cruzado e retenção indevida.

## Arquivos e orientação

- `Views/Relatorios/Index.cshtml`: Central reestruturada e orientação “Como usar”.
- `wwwroot/css/pages/report-center.css`: visual premium, acessível e responsivo isolado por página.
- `wwwroot/js/report-center.js`: descoberta local progressiva, contador e estados sem dependências.

## Validações executadas

- Diagnóstico solicitado de Git, remoto e SDK.
- Inventário com `find` e `rg` sobre Web, API e testes.
- Revisões estáticas de padrões C#/Dapper e padrões proibidos.
- `git diff --check`.
- Scripts de segurança/compatibilidade disponíveis no repositório.

## Limitações reais restantes

- Build, testes .NET e screenshot da aplicação não puderam ser executados sem SDK/runtime.
- Relatórios globais adicionais, relatórios completos por tenant, filtros avançados server-side, PDF/print, favoritos, recentes, filtros salvos e auditoria de novas exportações exigem evolução de backend em MODO COMPLETO.
- Acesso efetivo aos relatórios indicados como Super Admin deve ser validado em integração quando houver SDK; a apresentação do card não concede acesso.
- O remoto Git não está configurado neste checkout; fetch, pull, push e abertura remota de PR não são possíveis até que `origin` seja configurado.
