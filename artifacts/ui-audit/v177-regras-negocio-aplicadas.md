# Regras de negócio aplicadas — v1.77.0

| Jornada | Aplicada/preservada | Pendente de homologação |
|---|---|---|
| Dashboard | Indicador só aparece quando a API retornou dados; perfil e próxima ação são explícitos | Respostas reais por perfil |
| Agenda | Próxima ação depende de status e vínculo reais | Matriz completa de transições |
| Triagem | Risco/mínimos permanecem condicionados ao servidor | Risco alto e observação em runtime |
| Consulta | Faturamento requer identificador real; sem prescrição/CID fictícios | Conduta mínima e endpoint |
| Plantões/Escalas | Ações parciais permanecem condicionadas; motivos são obrigatórios quando aplicáveis | Cancelamento/substituição reais |
| Fechamentos | Financeiro somente após aprovação | Transições e concorrência |
| Faturamento | Ausente não vira zero nem pago | Exportação real |
| Pagamentos | Inexistente não vira pendência fictícia | Pagar/contestar/resolver |
| Relatórios | Geração/exportação sem endpoint continua indisponível | Backend contratado |
