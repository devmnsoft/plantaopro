# Decisões de contratos — v1.24.3

Contratos textuais históricos não devem induzir a recriação de rotas, jobs ou slogans sem uso operacional. A compatibilidade abaixo preserva comportamentos consumidos e aponta cada substituto mantido pelo produto.

| Contrato anterior | Origem | Ainda vigente | Substituto canônico | Compatibilidade | Decisão |
| --- | --- | ---: | --- | --- | --- |
| `api/customers` | v1.12 | Não | `api/clientes` | Sem alias genérico | Manter o recurso em português e atualizar o contrato de comportamento. |
| `runtime-e2e-v113` / `runtime-e2e-v116` | v1.13–v1.16 | Não | `runtime-from-complete-script` | Evidências preservadas por release | Consolidar o gate atual, sem duplicar jobs antigos. |
| `smoke-test-v114.sh` | v1.14 | Não | `scripts/smoke/smoke-api.sh` | Entrada única multiplataforma | Não restaurar smoke obsoleto. |
| `2026_v115_regras_faturamento_repasses.sql` por nome | v1.15 | Não | catálogo e gerador canônicos de migrations | Conteúdo histórico permanece versionado | Validar objetos de banco, não nome fixo. |
| Menu textual “Jornada” | beta | Não | catálogo canônico por perfil | Rotas atuais preservadas | Validar código, rota, permissão e ordem. |
| Frases e slogans exatos | beta comercial | Não | metadados do catálogo | Sem promessa textual estática | Testar informação e ação disponível. |
| “dotnet não disponível” | ambiente antigo | Não | execução real do SDK no CI | Nenhuma | Falha de ambiente deve ser reportada, nunca usada como aceite. |
| “aplicativo móvel” | beta | Parcial | `mobile/PlantaoPro.App` e disponibilidade mobile do catálogo | Aplicativo preservado | Validar build e navegação, não a frase. |
| “Fluxo operacional médico” | v1.17 | Não | jornadas segmentadas por perfil | Funcionalidades médicas preservadas | Não impor nomenclatura antiga à recepção. |
| `api/jornada-clientes/{clienteId}/eventos` | v1.13 | Sim | mesma rota tipada | Compatível | Preservar por representar histórico real. |
| `api/medicos/me/disponibilidade` | v1.13 | Sim | mesma rota protegida | Compatível | Preservar por suportar agendamento. |

## Painel público

A TV passa a usar identidade persistida do painel e somente o hash SHA-256 do token. Expiração, revogação, tenant e unidade são parte da consulta atômica; respostas não incluem documentos, contatos, queixa, risco, diagnóstico ou observações. Token inválido e painel inexistente produzem a mesma resposta para evitar enumeração.
