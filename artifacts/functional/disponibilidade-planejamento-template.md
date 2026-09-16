# Disponibilidade e planejamento — levantamento e evolução

## Base confirmada

- **Commit inicial:** `7145382844bbca681dd89375965807df682f9383` (branch local inicialmente `work`, árvore limpa).
- **Stack confirmada:** .NET 10/C# 10, MVC/Razor, Dapper e PostgreSQL; workflows canônicos em `.github/workflows/dotnet-ci.yml`.
- Foram lidos o histórico recente, os relatórios de disponibilidade, central de escalas e cobertura/substituições, a migração Fase 4 e os serviços/controllers atuais.

## Classificação da capacidade

| Capacidade | Estado após esta entrega | Evidência / observação |
|---|---|---|
| Disponibilidade própria | implementada sem execução | API CRUD persistente; GET que faltava foi restaurado; criação idempotente e serializada |
| Indisponibilidade própria | implementada sem execução | persistência, rejeição explícita de sobreposição e aviso de compromisso preservado |
| Profissionais, vínculos e tenant | parcial | identidade é resolvida para `medicos.usuario_id`; candidatos filtram `cliente_id`; hospital é revalidado no tenant |
| Unidade/especialidade | parcial | hospital ativo é validado no cliente; especialidade ativa é validada; vínculo médico-unidade específico não existe no modelo auditado |
| Recorrência e exceções | ausente | `dia_semana` existe, mas não há série, vigência, exceção, fuso ou edição por escopo com rastreabilidade suficiente; não foi simulada |
| Escalas, vagas e atribuições | parcial | existem plantões/escalas/convites/sugestões; esta mudança não substitui o fluxo canônico de publicação |
| Candidatos explicáveis | implementada sem execução | disponibilidade afirmativa, indisponibilidade e conflito compõem motivos/impedimentos; score permanece zero, sem “IA” fictícia |
| Rascunho e revisão versionada | parcial | sugestões persistidas não confirmam pagamento/presença; não há agregado canônico de rascunho com versão para completar com segurança nesta alteração |
| Confirmação/substituição | validada por inspeção | fluxo transacional existente preserva atribuição original até efetivação e revalida elegibilidade |
| Calendário/formulários reais | parcial | calendário de plantões e template compartilhado existem; a rota de disponibilidade ainda usa o dashboard Fase 2, não um formulário CRUD completo |
| Autorização | parcial | autosserviço deriva o médico da identidade; coordenação usa role e tenant; elegibilidade de módulo/unidade ainda depende das políticas existentes |

## Matriz de regras

| Regra | Fonte de dados | Implementação | Lacuna | Teste/evidência |
|---|---|---|---|---|
| Ausência não é disponibilidade | `medico_disponibilidades` | candidato exige janela ativa cobrindo todo o período | execução PostgreSQL pendente | contrato Fase 4 |
| Início anterior ao fim | request | `ValidarPeriodo` no servidor | cliente real ainda não possui formulário dedicado | contrato existente |
| Travessia da meia-noite | timestamps inicial/final | aceita quando o fim cronológico é posterior | fuso local não é persistido | inspeção |
| Sobreposição | disponibilidades + indisponibilidades | regra única: rejeitar com HTTP 409 e explicação | constraint temporal no banco não existe | novo teste de contrato |
| Repetição do salvamento | mesmos médico/período/contexto | devolve o ID existente sob lock, sem duplicar | chave de idempotência explícita não existe para payload alterado | novo teste de contrato |
| Concorrência de cadastro | médico | transação serializable + advisory lock por médico | validar em PostgreSQL real | novo teste de contrato |
| Contexto autorizado | médico/cliente/hospital/especialidade | hospital ativo deve pertencer ao cliente; IDs nunca vêm da autoria/tenant do payload | vínculo fino médico-unidade ausente | novo teste de contrato |
| Limite textual | observação/motivo | 500 caracteres no servidor | espelhar no formulário | novo teste de contrato |
| Indisponibilidade x compromisso | escalas + plantões | registra indisponibilidade, preserva escala e devolve orientação de substituição | UI deve exibir detalhe autorizado do compromisso | novo teste de contrato |
| Candidato elegível | médico, tenant, especialidade, disponibilidade, indisponibilidade, escalas | motivos e impedimentos determinísticos | jornada configurada/vínculo hospitalar não possuem fonte validada neste serviço | contrato existente |
| Publicação concorrente | fluxo canônico de escalas/substituição | não alterado | revisão/versionamento completo continua pendente | relatórios v2166/v2170 |

## Auditoria CRUD dos formulários

| Recurso | Criar | Consultar | Editar | Cancelar/excluir | Confirmação independente |
|---|---:|---:|---:|---:|---:|
| disponibilidade | API real | **corrigido nesta entrega** | API real | inativação auditada | GET próprio |
| indisponibilidade | API real | API real | não disponível | inativação auditada | GET próprio |
| recorrência/exceção | ausente | ausente | ausente | ausente | ausente |
| sugestão de escala | API real | API real | feedback/convite | estado persistido | GET por plantão |
| rascunho publicável | parcial | parcial | parcial | parcial | não há revisão versionada única |

## Alterações realizadas

1. Restaurado `GET /api/medicos/me/disponibilidade`, alinhando contrato documentado e CRUD real.
2. Cadastro de disponibilidade e indisponibilidade agora usa transação serializável e lock por profissional.
3. Salvamento idêntico é idempotente e retorna o registro já existente.
4. Sobreposições do mesmo tipo ou contraditórias são rejeitadas com mensagem explícita; nada é consolidado silenciosamente.
5. Hospital é revalidado no tenant e os campos de texto têm limite no servidor.
6. Indisponibilidade sobre escala solicitada/confirmada não remove nem altera o compromisso; a resposta orienta substituição.
7. Teste de contrato registra as invariantes de segurança e não cancelamento.

## Testes e limitações

- `dotnet build backend/PlantaoPro.sln -c Debug --no-restore`: **não executado**, pois o executável `dotnet` não está instalado neste ambiente.
- PostgreSQL e navegador não foram iniciados; portanto não há alegação de homologação, E2E ou screenshot da aplicação real.
- Recorrência/exceções, tela CRUD dedicada, calendário responsivo unificado e revisão/publicação versionada permanecem lacunas reais. Implementá-los sem confirmar o agregado/versionamento e sem executar migração criaria um motor paralelo ou sucesso simulado, contrariando o escopo.
