# Relatório de Consolidação Beta Homologável — 2026-06-05

## Escopo consolidado

Esta rodada priorizou itens de estabilização que afetam múltiplos módulos do PlantãoPro antes da homologação Beta:

- Resiliência de desserialização JSON no Web para respostas envelopadas em `ApiResponse<T>`, payloads crus e listas paginadas.
- Fallbacks uniformes para mensagens de erro de API (`message`, `mensagem`, `error`, `title`, `detail` e `errors`).
- Null-safety em partials compartilhadas de confirmação e empty state.
- Confirmação visual segura para ações destrutivas, com tipos Bootstrap normalizados.
- Base JavaScript para formulários AJAX opt-in (`data-ajax-form="true"`), com loading, toast e painel de erros.
- SQL incremental seguro para constraints e índices críticos de plantões, escalas, pagamentos e convites.

## Checklist operacional atualizado

| Área | Validação esperada | Status |
| --- | --- | --- |
| Plantões | Criar/editar com período válido, publicar e cancelar com auditoria/log | Pronto para revalidação |
| Escalas | Status restritos a solicitação, confirmação, recusa, cancelamento, substituição e realização | Pronto para revalidação |
| Pagamentos | Status restritos a pendente, gerado, confirmado/pago, cancelado e contestado | Pronto para revalidação |
| Convites | Status restritos a enviado, aceito, recusado, expirado e cancelado | Pronto para revalidação |
| Web JSON | Aceita `ApiResponse<T>`, payload cru, array e paginação com `total`/`totalItems` | Pronto para revalidação |
| UX compartilhada | Toast, modal de confirmação e empty state toleram modelo nulo | Pronto para revalidação |
| Banco | Constraints adicionadas via `DO $$` sem `ALTER TABLE com IF NOT EXISTS em constraints` | Pronto para revalidação |

## Roteiro mínimo de homologação manual

1. Entrar como Admin Global e validar dashboard, listagem de plantões e criação de plantão em rascunho.
2. Publicar o plantão, confirmar que o modal aparece antes da ação e que o toast de sucesso é exibido.
3. Solicitar escala por perfil Médico e confirmar/recusar por Coordenação.
4. Gerar pagamento, confirmar pagamento e testar cancelamento/contestação conforme perfil Financeiro.
5. Enviar convite, aceitar, recusar e validar expiração com status consistente.
6. Validar endpoints Mobile com JWT para listagens paginadas e fluxos do médico.
7. Executar o SQL incremental em homologação e confirmar que a reaplicação não falha.

## Observações de QA

- O JavaScript AJAX é opt-in; telas existentes continuam funcionando com postback tradicional até que cada formulário seja migrado.
- Em ambiente sem SDK .NET instalado, a validação de build deve ser executada no pipeline ou máquina de homologação com .NET 10 SDK.
