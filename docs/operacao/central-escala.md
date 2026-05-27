# Central de Escala Inteligente

## Objetivo
A Central de Escala Inteligente é o cockpit operacional para coordenação, com foco em velocidade de decisão, redução de plantões descobertos e priorização de pendências críticas.

## Perfis com acesso
- `ADMINISTRADOR_GLOBAL`
- `ADMINISTRADOR`
- `COORDENACAO`
- `OPERADOR` (quando autorizado por política do cliente)

Perfis sem acesso direto:
- `MEDICO`

## KPIs prioritários
- Plantões abertos
- Plantões sem médico
- Plantões próximos sem cobertura
- Escalas solicitadas
- Convites pendentes
- Convites expirando
- Conflitos detectados
- Escalas para confirmar
- Pagamentos pendentes
- Alertas críticos

## Filtros obrigatórios
- Período
- Hospital
- Especialidade
- Status
- Severidade

## Ações rápidas
- Publicar plantão
- Ver médicos recomendados
- Enviar convites
- Confirmar escala
- Recusar escala
- Cancelar escala
- Substituir médico
- Resolver alerta
- Abrir agenda

## Regras de segurança e auditoria
- Ações sensíveis devem exigir confirmação em modal.
- Ações relevantes devem exibir feedback por toast.
- Ações críticas devem registrar auditoria com usuário, data/hora UTC, contexto e payload mínimo necessário.
- Nunca registrar senha, token, hash ou segredo em logs/auditoria.

## Integrações com motores inteligentes
- Motor de conflitos: bloqueia ação em conflitos críticos.
- Motor de recomendação: prioriza médicos elegíveis para cobertura.
- Motor de alertas: destaca pendências por severidade/prioridade.

## Checklist de homologação
1. Acessar a Central com usuário de coordenação.
2. Validar cards KPI e filtros principais.
3. Abrir pendência e executar ação rápida com modal.
4. Confirmar toast de sucesso/erro amigável.
5. Verificar registro de auditoria da ação crítica.
6. Confirmar bloqueio em conflito crítico.
7. Confirmar visualização de alertas críticos.

## Pendências recomendadas (próximas iterações)
- SLA visual por tipo de pendência.
- Priorização automática por risco financeiro.
- Atalhos por teclado para operação de alto volume.
- Painel de fila em tempo real para NOCs operacionais.
