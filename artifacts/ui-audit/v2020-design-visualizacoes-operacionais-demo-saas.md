# Auditoria visual — PlantãoPro v2.02.0

## Escopo

A v2.02.0 avança a Central de Escala para uma **central de cobertura operacional**, sem repetir a rodada de polimento da v2.01.0. A implementação usa exclusivamente o `OperacaoResumoDto` retornado pela API: totais, riscos, conflitos, confirmações, alertas, próximos plantões e solicitações. Não foram adicionados seed, mock, número comercial estático, regra de permissão ou alteração de banco.

## Telas alteradas

| Tela | Evolução | Dados/estado |
|---|---|---|
| `/CentralEscala` | Hero de sala de controle, KPIs acionáveis, seletor de perspectivas, mapa de sinais, painel de risco, timeline e atalhos | Dados reais do resumo operacional; fallback vazio profissional |
| `/Agenda`, `/Plantoes`, `/Hospitais`, `/Medicos` | Acesso contextual pela central nas perspectivas calendário, lista, unidade e profissional | Rotas e permissões existentes preservadas |
| `/CentralEscala/Risco`, `/PlantaoDescoberto`, `/ConvitesPendentes`, `/Substituicoes` | Fila de decisão e atalhos mais explícitos | Fluxos existentes; nenhuma ação simulada |

## Componentes reutilizáveis

- `CoverageCard`: indicador com tom semântico, contexto e destino real opcional.
- `CoverageHeatmap`: matriz responsiva para sinais de cobertura, espera, risco e conflito.
- `ShiftStatusBadge`: adaptador do badge semântico já existente; mantém status textual e acessível.
- `OperationalTimeline`: próximos eventos combinados, ordenados e vinculados ao registro real.
- `RiskPanel`: composição visual da central com filas de plantões abertos, confirmações e conflitos.
- `DemoEmptyState`: vazio demonstrável e honesto, sem fabricar dados.

Os estilos foram consolidados em `v2000-premium.css`, a base carregada do design system vigente. Não foi criada uma folha paralela nem duplicado AppShell, logo, navegação ou tokens.

## Decisões visuais

- Azul profundo e grafite estruturam o hero; teal indica operação ativa; verde fica restrito à cobertura confirmada.
- Vermelho é reservado a risco/conflito e âmbar à espera, sempre acompanhado de texto — cor não é o único sinal.
- O “mapa” é uma matriz operacional e não geográfica, pois o contrato atual não oferece coordenadas nem série completa por unidade/período.
- Todos os cards de prioridade e atalhos possuem destinos MVC reais; não há `href="#"`, botão decorativo ou JavaScript inline.
- A central declara que os sinais refletem apenas dados disponíveis, tornando a experiência comercial clara sem números fictícios.

## Estados implementados

- **Com dados:** KPIs, mapa, timeline cronológica, badges de status, fila prioritária e alertas.
- **Vazio:** mensagem “Agenda operacional tranquila”, explicando quando eventos aparecerão.
- **Erro:** alerta do controller existente e estrutura operacional preenchida com o DTO vazio seguro.
- **Risco/atenção/positivo/neutro:** tratamentos semânticos coerentes e rotulados.
- **Interação:** elevação discreta no hover, foco global existente, rolagem com snap no mobile e respeito a `prefers-reduced-motion`.

## Validação responsiva

- **360 px e 390 px:** KPIs viram cards horizontais com scroll/snap; ações principais mantêm 44 px; mapa usa duas colunas; painéis e timeline ficam em uma coluna; nenhum conteúdo depende de tabela larga.
- **768 px:** grid principal passa a uma coluna e preserva a hierarquia risco → próximos eventos → ações.
- **1024 px:** KPIs usam três colunas e a matriz usa duas, evitando compressão.
- **1366 px, 1440 px e 1920 px:** matriz 4×1 e composição 1,65:0,85 equilibram leitura operacional e fila prioritária.

## Validação desktop e evidência

A revisão estrutural e responsiva foi feita sobre Razor/CSS e as verificações estáticas do repositório. Não foi possível gerar screenshot navegada neste container porque o runtime `dotnet` não está instalado; sem o servidor não há rota autenticada renderizável. Nenhuma credencial ou dado artificial foi criado para contornar a limitação.

## Limitações e próximos passos

1. O endpoint atual não entrega disponibilidade de profissionais, cobertura agregada por unidade/especialidade nem buckets de período. A central oferece destinos reais para esses fluxos, sem inferir totais.
2. Quando a API disponibilizar série de cobertura, `CoverageHeatmap` pode receber linhas unidade × turno sem alteração de linguagem visual.
3. Capturar evidências autenticadas em 360, 390, 1440 e 1920 px em ambiente com API/dados de demonstração seguros.
4. Reutilizar `ShiftStatusBadge` e o padrão mobile em `/Plantoes` e `/MinhaAgenda` numa próxima entrega orientada pelos contratos específicos dessas páginas.

## Comandos executados

```bash
python3 scripts/repository-security-check.py
python3 scripts/check-csharp10-compatibility.py
python3 scripts/validate-scrpt-completo.py
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release --no-restore
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build
git diff --check
rg -n 'href="#"|alert\\(|confirm\\(|Password=123456|Username=postgres;Password=|CHANGE_ME_WITH_32|Host=.*Password=|Server=.*Password=' backend/PlantaoPro.Api backend/PlantaoPro.Web backend/PlantaoPro.Tests scripts docs README.md .env.example
```

Os três comandos `dotnet` ficaram bloqueados por limitação do ambiente (`dotnet: command not found`). Os validadores Python e `git diff --check` passaram. A varredura literal retornou apenas fixtures locais preexistentes e menções documentais/testes de segurança; nenhum resultado novo foi introduzido pela v2.02.0.
