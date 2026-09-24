# M4 — conferência da jornada clínica

Data da conferência: 24/09/2026. Base: branch `work`, commit inicial
`a316628`. Este registro não declara o PlantãoPro pronto para produção e não
substitui evidência de execução.

## Estado reconfirmado

| Marco/componente | Estado | Evidência e limite |
| --- | --- | --- |
| M1 — banco, build, login e integração | Parcial | As migrations, instalador e CI existem; o SDK .NET e um PostgreSQL de homologação não estão disponíveis neste ambiente, portanto build, login e instalação não foram reexecutados. |
| M2 — identidade, contexto e autorização | Parcial | Os controles por tenant e perfil estão no código, mas não houve ensaio dinâmico A/B nesta rodada. |
| M3 — plantões até pagamento | Não verificado | Regras e testes anteriores existem; a regressão executável ficou bloqueada pela ausência do SDK .NET. |
| Pacientes, agenda, check-in e fila | Parcial | Há controllers, serviço, migrations e regras de estado; persistência e concorrência não foram reexecutadas contra PostgreSQL real. |
| Triagem, consulta e prontuário | Parcial | Há serviços separados para consulta e longitudinal, versionamento e snapshots; falta homologação dinâmica integral. |
| CID, prescrição e documentos | Parcial | Persistência versionada existente e documento inicia como `NAO_ASSINADO`; falta prova de impressão e navegação por perfil. |
| Convênios, autorizações e financeiro | Parcial | A rota de planos agora aplica o convênio da URL e mantém o filtro do tenant; caixa, estorno e repasse ainda exigem teste transacional real. |
| Jornadas particular e convênio | Bloqueado | H24/H25 não podem ser aprovados sem banco real, aplicação em execução e navegação autenticada. |

## Achados comprovados e correções

1. `Saude360ClinicalService` ainda executava `CREATE TABLE`, `ALTER TABLE` e
   índices em cada verificação clínica. O DDL foi retirado do runtime; a
   verificação agora é somente leitura e falha com orientação para aplicar as
   migrations. O inventário inclui as estruturas de CID, consulta e prescrição
   antes criadas sob demanda.
2. `GET /api/convenios/{id}/planos` ignorava `id`. A listagem agora recebe o
   identificador da rota e combina `convenio_id` com o isolamento já existente
   por `cliente_id`.
3. O provedor local de assinatura foi reconfirmado: novos documentos recebem
   explicitamente `NAO_ASSINADO`; nenhuma integração externa foi criada.
4. A classificação de risco permanece fixa no lookup atual. Não foi inventado
   protocolo, limiar clínico ou configuração retrospectiva; a definição de um
   modelo configurável continua pendente de decisão de produto/clínica.

## Componentes e contratos afetados

- `Saude360ClinicalSchema`: readiness somente leitura e inventário ampliado.
- `Saude360ClinicalService.ListarAsync`: filtro opcional `convenioId`, mantendo
  compatibilidade para consumidores existentes por ser o último parâmetro.
- `ConveniosController.Planos`: passa o identificador canônico da rota.
- Nenhuma rota foi removida, migration foi executada ou contrato público
  existente foi renomeado.

## Matriz de homologação desta rodada

| Casos | Resultado real |
| --- | --- |
| H14 | Comprovado por contrato automatizado estático: estado inicial é `NAO_ASSINADO`. |
| H15 | Comprovado por contrato automatizado estático: ID da rota chega à query, junto ao filtro de organização. |
| DDL fora de request clínico | Comprovado por contrato automatizado estático e inspeção do diff. |
| H01–H13, H16–H23 | Não verificados dinamicamente nesta rodada. |
| H24–H27 | Bloqueados neste ambiente: exigem PostgreSQL real, runtime .NET, sessão por perfil e navegador. |

Busca de strings é usada somente como regressão arquitetural dos três defeitos
específicos acima; não é apresentada como prova de persistência, concorrência
ou jornada.

## Status A15–A19 e decisão

- **A15 — Parcial:** a separação já existe em controllers e serviços dedicados,
  mas o serviço clínico legado ainda concentra operações CRUD e permanece como
  adapter de consumidores antigos.
- **A16 — Parcial:** recepção tem superfícies e regras persistentes, sem nova
  prova navegada nesta rodada.
- **A17 — Parcial:** consulta/longitudinal e documentos estão separados; a
  homologação de impressão, retificação e concorrência segue pendente.
- **A18 — Parcial:** corrigido o vínculo convênio/plano; recebimento, caixa,
  estorno e repasse carecem de teste PostgreSQL transacional.
- **A19 — Bloqueado:** jornadas particular e convênio básico não foram navegadas.

M4 **não está homologado**. Para iniciar M5 como preparação de RC1, primeiro é
necessário disponibilizar SDK .NET 10 e PostgreSQL efêmero, executar instalação
limpa/upgrade/reaplicação, rodar toda a suíte, testar concorrência com conexões
independentes, navegar H24–H27 por perfis e registrar evidências sem dados ou
tokens sensíveis.
