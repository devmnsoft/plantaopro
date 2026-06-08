# Operação assistida

## Implementado nesta rodada

Fluxo guiado com 16 etapas implementado via /api/operacao-assistida/planos, iniciar, concluir/pular etapas, pendências, evidências e atrasadas.

## Validações manuais sugeridas

1. Acessar a rota Web correspondente com usuário autorizado.
2. Executar o endpoint API correspondente no Swagger com JWT válido.
3. Confirmar retorno no padrão `ApiResponse<T>`.
4. Confirmar que ações POST inválidas retornam validação sem stack trace.
5. Confirmar auditoria das ações sensíveis quando a base de dados estiver disponível.

## Pendências reais

- Validar contra banco PostgreSQL de homologação após aplicar `database/migrations/2026_plantao_pro_piloto_comercial_operacao_b2b.sql`.
- Substituir dados-semente em memória por queries específicas quando o volume real do piloto exigir relatórios históricos completos.
