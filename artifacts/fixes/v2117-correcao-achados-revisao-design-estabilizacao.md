# v2.11.7 — correção de achados e estabilização de design

## Resumo da execução

- **Modo usado:** `MODO ESTÁTICO SEGURO`.
- **SDK .NET:** indisponível. `which dotnet` não encontrou executável; `dotnet --info` e `dotnet --list-sdks` retornaram `command not found`.
- **Projeto preservado:** os projetos continuam em `net10.0`, com `LangVersion` `10.0` em `backend/Directory.Build.props`, e não existe `global.json`.
- **Git remoto:** `git remote -v` não listou remoto. Não foi possível fazer `fetch`, `pull`, `push` ou abrir PR remoto a partir do repositório.
- **Fonte principal:** `artifacts/reviews/v2116-design-premium-revisao-codigo-bugs.md`, complementada pelas buscas direcionadas desta rodada.

Sem SDK, nenhum arquivo C#, SQL, migration, `.csproj`, `.props` ou `.sln` foi alterado. A entrega limita-se a uma camada CSS visual e ao seu carregamento no layout compartilhado.

## Classificação dos achados

### P0 — documentado, não corrigido sem runtime

- `Fase6BiIntegracoesController` mantém seis endpoints genéricos com `[AllowAnonymous]`. O contrato de autenticação por API key e o isolamento de tenant devem ser confirmados antes de conectar respostas a dados reais. Alterar a autorização sem testes poderia quebrar integrações existentes.

### P1 — documentados, não corrigidos sem runtime

- Claims `uid` ainda são convertidas com `Guid.Parse` em diversos controllers. Token malformado pode produzir exceção em vez de rejeição controlada; a correção deve centralizar `TryParse` e ser coberta por testes de autenticação e auditoria.
- `DashboardPremiumService.AddCount` captura tabela/coluna inexistente sem log, omitindo KPI silenciosamente. A correção requer validar o contrato de resultado parcial e o logging estruturado.
- `Saude360ClinicalService` e `SaasComercialOperacaoController` ainda possuem `select *`; há nomes de tabela compostos em runtime. As origens parecem controladas na inspeção anterior, mas colunas, aliases, materialização Dapper e whitelists precisam de build e integração com schema real.
- A compatibilidade de DTOs Dapper, nullability e filtros multi-tenant não pode ser certificada por busca textual. Foram localizadas 436 linhas na busca de chamadas Dapper e 1.667 na busca ampla de tenant/permissões, reforçando a necessidade de testes focados com dois tenants.

### P2 — UX e consistência

- `Assinaturas/_Form` ainda pede `ClienteId` e `PlanoId` em campos de texto.
- `Convites/Index` ainda permite colar manualmente o código do plantão.
- `Onboarding/NovoCliente` ainda pede o identificador do plano em campo livre.
- Esses três fluxos foram documentados, mas não foram substituídos apenas visualmente: um dropdown/autocomplete correto exige catálogo real, escopo de tenant, permissões e mudança coordenada de ViewModel/controller.
- O shell, tabelas, ações mobile, modais, mensagens de validação e textos longos tinham variações de acabamento entre módulos. A nova camada visual reduz essas diferenças sem mudar marcação ou contratos.

### P3 — polimento aplicado

- Navegação lateral recebeu separação, scrollbar discreta, estados hover/ativo e foco visual mais calmos.
- Topbar recebeu superfície opaca, borda e sombra contidas para preservar contexto durante a rolagem.
- Cards, painéis, KPIs, estados vazios e modais agora compartilham raio, elevação e hierarquia mais consistentes.
- Títulos, descrições e células passam a quebrar textos longos para evitar estouro de container; valores de KPI usam algarismos tabulares.
- Tabelas responsivas ganharam acabamento de overflow, alinhamento vertical e scrollbar discreta.
- Erros por campo e controles com `aria-invalid="true"` ganharam contraste explícito.
- Em telas pequenas, toolbars e grupos de ação empilham CTAs, o título da topbar respeita a viewport e modais mantêm margem segura.
- Foram preservadas as preferências de movimento reduzido e o modo de cores forçadas.

## Bugs corrigidos

Nenhum bug de backend foi corrigido no modo estático. A correção desta rodada é estritamente visual: estabiliza overflow, responsividade, contraste de validação e consistência de superfícies no AppShell.

## Arquivos alterados

- `backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml`: carrega a folha versionada v2.11.7 depois da camada v2.11.6.
- `backend/PlantaoPro.Web/wwwroot/css/design-system/v2117-design-stabilization.css`: refinamento visual global, responsivo e acessível.
- `artifacts/fixes/v2117-correcao-achados-revisao-design-estabilizacao.md`: registro desta execução.

## Comandos executados e resultados

| Comando / grupo | Resultado |
|---|---|
| `pwd`, `git status --short --branch`, `git branch --show-current` | repositório `/workspace/plantaopro`; árvore inicial limpa na branch `work`; criada `codex/v2117-correcao-achados-revisao-design-estabilizacao` |
| `git remote -v` | nenhuma saída; remoto não configurado |
| `which dotnet`, `dotnet --info`, `dotnet --list-sdks` | SDK ausente; dois últimos comandos falharam com `command not found` |
| `find` de `global.json`, `.props`, `.sln`, `.csproj` e `rg` de framework/linguagem | soluções e projetos inventariados; `net10.0`, C# 10; sem `global.json` |
| buscas de parses, async, exceções e SQL | 54 linhas candidatas; achados herdados da v2.11.6 reconfirmados |
| busca de Dapper | 436 linhas para triagem; nenhuma correção sem build/schema |
| busca de tenant, perfis, roles e permissões | 1.667 linhas para triagem; isolamento não alterado |
| busca ampla Razor/UX | 477 linhas, majoritariamente IDs HTML válidos; três fluxos de ID/código manual confirmados |
| busca obrigatória de padrões proibidos | nenhum `href="#"`, `alert()` ou `confirm()` ativo adicionado; apontou os IDs manuais preexistentes, strings negativas de testes/documentação e credenciais locais de fixtures já documentadas |
| `python3 scripts/repository-security-check.py` | sucesso: `repository-security ok` |
| `python3 scripts/check-csharp10-compatibility.py` | sucesso: compatibilidade C# 10 e CSS Razor validada |
| `python3 scripts/validate-scrpt-completo.py` | sucesso: `ok: true`, cobertura `100.0%` |
| `git diff --check` | sucesso, sem erros de whitespace |
| `dotnet clean/restore/build/test` | não executados, pois não existe executável `dotnet` no ambiente |

## Limitações reais restantes

- Build, Razor compilation e testes automatizados aguardam ambiente com SDK .NET 10.
- Os P0/P1 listados aguardam correção validável; nenhuma exceção foi mascarada e nenhum fluxo foi removido.
- Os seletores para cliente, plano e plantão exigem fonte real autorizada; esconder os campos atuais quebraria funcionalidade e não seria uma correção.
- Não foi possível executar a aplicação nem produzir screenshot navegável sem o runtime .NET.
- Não há remoto Git configurado; publicação da branch e PR dependem de adicionar/restaurar `origin` em ambiente com acesso ao GitHub.
