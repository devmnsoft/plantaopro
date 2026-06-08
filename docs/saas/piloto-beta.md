# Piloto/Beta comercial

## Implementado nesta rodada

Módulo implementado com API /api/piloto/programas, /api/piloto/clientes, /api/piloto/feedbacks e /api/piloto/indicadores. A Web expõe Piloto/Programas, Piloto/Clientes, Piloto/Feedbacks, Piloto/Bugs e Piloto/Indicadores. O fluxo cobre cadastro do programa, vínculo do tenant, início, pausa, conclusão, conversão, classificação e resolução de feedback.

## Validações manuais sugeridas

1. Acessar a rota Web correspondente com usuário autorizado.
2. Executar o endpoint API correspondente no Swagger com JWT válido.
3. Confirmar retorno no padrão `ApiResponse<T>`.
4. Confirmar que ações POST inválidas retornam validação sem stack trace.
5. Confirmar auditoria das ações sensíveis quando a base de dados estiver disponível.

## Pendências reais

- Validar contra banco PostgreSQL de homologação após aplicar `database/migrations/2026_plantao_pro_piloto_comercial_operacao_b2b.sql`.
- Substituir dados-semente em memória por queries específicas quando o volume real do piloto exigir relatórios históricos completos.
