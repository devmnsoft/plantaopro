# Renovação e expansão

## Implementado nesta rodada

APIs /api/renovacoes e /api/expansoes implementam iniciar, contato, renovar, marcar risco, criar oportunidade, ganhar e perder com motivo.

## Validações manuais sugeridas

1. Acessar a rota Web correspondente com usuário autorizado.
2. Executar o endpoint API correspondente no Swagger com JWT válido.
3. Confirmar retorno no padrão `ApiResponse<T>`.
4. Confirmar que ações POST inválidas retornam validação sem stack trace.
5. Confirmar auditoria das ações sensíveis quando a base de dados estiver disponível.

## Pendências reais

- Validar contra banco PostgreSQL de homologação após aplicar `database/migrations/2026_plantao_pro_piloto_comercial_operacao_b2b.sql`.
- Substituir dados-semente em memória por queries específicas quando o volume real do piloto exigir relatórios históricos completos.
