# Roteiro de homologação do piloto comercial

## Implementado nesta rodada

Validar login admin, criação de programa, cliente piloto, feedback, classificação, resolução, conversão, CS, NPS, health, operação assistida, template, treinamento, escala, agenda, renovação, expansão, auditoria e build.

## Validações manuais sugeridas

1. Acessar a rota Web correspondente com usuário autorizado.
2. Executar o endpoint API correspondente no Swagger com JWT válido.
3. Confirmar retorno no padrão `ApiResponse<T>`.
4. Confirmar que ações POST inválidas retornam validação sem stack trace.
5. Confirmar auditoria das ações sensíveis quando a base de dados estiver disponível.

## Pendências reais

- Validar contra banco PostgreSQL de homologação após aplicar `database/migrations/2026_plantao_pro_piloto_comercial_operacao_b2b.sql`.
- Substituir dados-semente em memória por queries específicas quando o volume real do piloto exigir relatórios históricos completos.
