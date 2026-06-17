# Pendências reais pós-auditoria

Escopo: PlantãoPro Saúde 360.

| Severidade | Descrição | Arquivo | Impacto | Solução recomendada | Status | Próxima ação |
|---|---|---|---|---|---|---|
| Crítica | SDK dotnet indisponível no ambiente de execução desta rodada. | Ambiente | Build/test não executáveis localmente. | Rodar em runner com .NET SDK instalado. | Aberta | Validar CI. |
| Alta | Forms Saúde 360 ainda dependem de formulário genérico em alguns cadastros. | backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs | UX e validação limitadas. | Criar ViewModels específicos e partials por formulário. | Aberta | Priorizar paciente, agendamento, triagem, consulta e financeiro. |
| Alta | Lookups globais precisam cobertura completa em selects. | API/Web | Risco de select vazio/confuso. | Consolidar `/api/lookups/*` e consumir no Web. | Aberta | Implementar endpoints e contrato. |
| Média | Menu tem muitos itens e deve ser validado por perfil em browser. | MenuBuilderService.cs | Usuário leigo pode se confundir. | QA navegacional por perfil e telemetria de clique. | Em andamento | Executar checklist. |
| Média | Migrations/seeds espalhados em múltiplas pastas. | database, backend/sql | Ordem operacional pode gerar falha. | Consolidar ordem e idempotência. | Parcial | Revisar no banco de homologação. |
| Baixa | Documentação extensa requer curadoria contínua. | docs | Risco de duplicidade. | Manter matriz mestra como fonte da verdade. | Em andamento | Revisão mensal. |
