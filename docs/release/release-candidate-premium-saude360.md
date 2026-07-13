# Release Candidate premium — PlantãoPro Saúde 360

## Validado/corrigido
Matriz de telas e rotas, checklist QA Web, testes de contrato da API/Web, índices de performance, checklist LGPD, manuais por perfil, roteiro demo e checklist pré-demo.

## Como rodar
Aplicar migrations em `database/migrations`, seeds em `database/seeds`, iniciar API e Web, abrir Swagger, autenticar e percorrer a jornada comercial.

## Usuários de teste
Usar contas seedadas para admin global, admin clínica, recepção, triagem, médico, financeiro, convênios e auditor.

## Endpoints principais
`/api/clinica-dashboard/resumo`, `/api/pacientes`, `/api/agendamentos`, `/api/painel-chamada`, `/api/triagens`, `/api/consultas`, `/api/cid`, `/api/prescricoes`, `/api/clinica-financeiro/contas-receber`, `/api/convenios`, `/api/planos-saude`, `/api/pendencias-clinicas`.

## Pendências
Build e testes automatizados dependem do SDK `dotnet`, ausente no container desta execução.
