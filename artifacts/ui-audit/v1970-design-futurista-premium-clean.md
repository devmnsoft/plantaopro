# Auditoria de UI — PlantãoPro v1.97.0

## Resumo

A v1.97.0 adiciona uma camada final de acabamento visual ao design system existente, sem alterar autenticação, autorização, APIs, banco, migrations ou regras de negócio. O resultado usa canvas levemente azulado, superfícies brancas, azul profundo estrutural, ciano discreto para interação e verde saúde somente como estado de apoio. Gradientes e sombras foram reduzidos para evitar ruído.

## Pré-condições e segurança

- O checkout local iniciou no merge `40c6be3`, referente à correção de CI pós-merge do PR #390.
- A tentativa de atualizar `main`, consultar PRs abertas e consultar o CI remoto foi bloqueada pelo ambiente: o proxy retornou HTTP 403 no `git fetch` e o GitHub CLI não possui autenticação.
- Como substituição segura, os três gates locais que representam os bloqueios citados foram executados antes do trabalho visual e passaram: segurança do repositório, compatibilidade C# 10/CSS Razor e validação do script completo.
- A evolução visual somente começou após esses gates locais verdes.

## Telas impactadas

- **Shell autenticado:** paleta institucional, topbar mais leve, sidebar sem gradiente e estado ativo mais claro.
- **Login:** composição calma em azul profundo, card de autenticação branco, menor elevação e controles consistentes; o único logo existente foi preservado.
- **Dashboard operacional:** hierarquia de KPIs, filtros, cards de ação e ritmos de seção refinados. Ícones Bootstrap dispersos foram substituídos pelo registro oficial `app-icon`.
- **CRUDs e telas administrativas:** tabelas, campos, paginação, validação e botões recebem os mesmos tokens globais por herança.

## Componentes padronizados

- Canvas, superfícies, bordas, elevação, foco, espaçamento e raios.
- Botões, inputs, selects, validação, paginação e densidade de tabela.
- Cards, KPIs, quick actions, shell, topbar, sidebar e estados ativos.
- Login, dashboard e listas móveis operacionais.
- Ícones funcionais de dashboard, agenda, filtro, escalas, Saúde 360, financeiro e administração.

## Decisões de design

1. Criar uma camada pequena e explícita `premium-clean-v1970.css`, carregada por último, para evoluir com baixo risco e manter compatibilidade com telas históricas.
2. Trocar fundos em gradiente por cores sólidas nas áreas centrais, preservando profundidade somente com bordas e sombras curtas.
3. Reservar ciano a foco, interação e orientação; manter cores semânticas apenas para sucesso, alerta e erro.
4. Usar altura mínima de 44 px nos controles principais e foco visível de alto contraste.
5. Manter dados reais e empty states existentes no dashboard; nenhum KPI demonstrativo foi adicionado.
6. Reutilizar o sprite e o registro oficial de ícones, sem dependência externa ou ícones decorativos.
7. Preservar uma única marca em cada contexto: sidebar no shell e painel institucional no login.

## Riscos e mitigações

- **Cascata histórica extensa:** a nova folha é aditiva e carregada por último; não remove folhas ou classes usadas por módulos antigos.
- **Regressão funcional:** nenhuma action, campo, rota, claim ou contrato foi alterado.
- **CI remoto indisponível:** gates equivalentes locais foram executados; build e testes .NET devem ser repetidos em CI porque o SDK não existe no contêiner.
- **Busca textual de segurança:** retornou somente valores de teste local e exemplos parametrizados/preexistentes em testes, scripts e documentação; o gate dedicado `repository-security-check.py` passou e nenhum segredo foi incluído.

## Checklist responsivo

- [x] Desktop/notebook: conteúdo usa ritmo fluido, largura existente e cards com baixa elevação.
- [x] Tablet: shell mantém breakpoints existentes; ações podem quebrar linha sem overflow.
- [x] Celular: cards de ação e KPIs passam a uma coluna; padding e títulos são reduzidos.
- [x] Sidebar recolhida e menu mobile preservam a implementação existente.
- [x] Tabelas preservam rolagem horizontal e o dashboard mantém cards móveis dedicados.
- [x] Login passa de duas colunas para uma e elimina elementos secundários em telas estreitas.
- [x] Formulários longos mantêm controles de largura fluida e altura mínima de toque.

## Checklist de acessibilidade

- [x] Contraste alto entre grafite/azul profundo e superfícies claras.
- [x] Foco visível em ciano para teclado; erro usa anel vermelho específico.
- [x] Labels, ajuda, autocomplete e regiões de erro do login preservados.
- [x] Alvos principais têm no mínimo 44 px; botões pequenos mantêm 36 px onde apropriado.
- [x] Ícones funcionais acompanham texto e são ocultos de leitores quando decorativos ao rótulo.
- [x] `prefers-reduced-motion` existente foi preservado.
- [x] `forced-colors` recebeu borda explícita para cards e navegação selecionada.
- [x] Empty states descrevem ausência de dados sem inventar valores.

## Validações executadas

```bash
python3 scripts/repository-security-check.py
# PASS: repository-security ok
python3 scripts/check-csharp10-compatibility.py
# PASS: Compatibilidade C# 10 e CSS Razor validada.
python3 scripts/validate-scrpt-completo.py
# PASS: {"ok": true, "coveragePercent": 100.0}
npm run audit:experience
# PASS: Product Experience gate: OK
git diff --check
# PASS
```

```bash
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release --no-restore
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build
# LIMITAÇÃO DO AMBIENTE: dotnet: command not found. Requer execução no CI com .NET 10.
```

```bash
rg -n "Password=123456|Username=postgres;Password=|AllowLegacyPostgresDatabase\": true|AllowDevelopmentAutoCreate\": true|CHANGE_ME_WITH_32|Host=.*Password=|Server=.*Password=" backend/PlantaoPro.Api backend/PlantaoPro.Tests scripts docs README.md .env.example
# Revisado: ocorrências preexistentes são credenciais locais de teste ou exemplos parametrizados/documentais.
```

## Evidência visual

Não foi possível gerar screenshot local porque o contêiner não possui o runtime `dotnet` necessário para iniciar o projeto web. A evidência estática está na folha `backend/PlantaoPro.Web/wwwroot/css/design-system/premium-clean-v1970.css` e nas views alteradas. Recomenda-se captura do login em 360, 768 e 1366 px e do dashboard autenticado no ambiente de homologação/CI visual.

## Limitações

- Não houve validação visual autenticada por perfil sem runtime e credenciais de homologação.
- A consolidação completa de folhas CSS históricas permanece fora do escopo, pois sua remoção sem regressão visual por módulo elevaria o risco.
- PRs e status remoto do GitHub precisam ser reconfirmados pelo executor com acesso/autenticação de rede.
