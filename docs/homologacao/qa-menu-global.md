# QA do menu global

## Resultado
Menu reorganizado em: Início, Atendimento, Plantões, Financeiro, Convênios, Relatórios, Gestão do Cliente, Admin SaaS, Parceiro e Ajuda e Governança.

## Critérios validados por inspeção
- Itens apontam para controllers/actions existentes ou para telas consolidadas sem 404 conhecido.
- Grupos duplicados de Clínica/Saúde 360 foram consolidados.
- Módulos premium permanecem bloqueáveis por `RequiresModule` e exibem motivo de bloqueio.
- Perfis usam papéis mínimos existentes em `RolesConstants`.

## QA pendente de ambiente
- Login admin e navegação real do menu inteiro.
- Validação com usuários recepção, triagem, médico, financeiro, parceiro, auditor e suporte.
