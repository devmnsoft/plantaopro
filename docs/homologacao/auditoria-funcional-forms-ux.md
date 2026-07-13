# Auditoria funcional, forms e UX premium

## Build inicial
- `dotnet clean/build`: não executável no ambiente porque o SDK `dotnet` não está instalado (`dotnet: command not found`).

## Diagnóstico real
- Controllers API Saúde 360 e lookups existem em `backend/PlantaoPro.Api/Controllers`.
- Controllers Web principais existem em `Saude360WebControllers.cs` e renderizam views específicas quando presentes.
- Uso crítico remanescente identificado: formulário genérico de Saúde 360 ainda expunha IDs manuais para paciente, médico, agendamento, consulta e plano.
- Menu global já estava próximo da jornada desejada, com ajustes pendentes documentados em QA de menu.

## Plano executado nesta branch
1. Substituir os campos de ID manual do formulário genérico por componentes de lookup por nome.
2. Criar partials `_LookupSelect` e `_AutocompleteField` e script `lookup.js`.
3. Completar endpoints `/api/lookups/agendamentos`, `/api/lookups/consultas` e compatibilidade com query `term`.
4. Registrar matriz funcional, QA de banco, QA final, segurança/LGPD, UX e roteiro demo.

## Pendências críticas reais
- Validar build em ambiente com .NET SDK.
- Executar QA manual logado contra banco PostgreSQL de homologação.
- Evoluir cada view específica para view models fortemente tipados por domínio conforme a matriz funcional.
