# Funcionalidades fechadas — v1.79.0

- Jornada visual Plantão → Convite → Escala → Confirmação → Presença/Substituição → Fechamento → Financeiro.
- Cobertura calculada exclusivamente por `Vagas` e `VagasDisponiveis`; risco apenas quando todas as vagas reais continuam disponíveis.
- Publicação condicionada a rascunho e mínimos recebidos; cancelamento mantém motivo obrigatório.
- Convites sem KPIs zero na ausência de fonte e sem reenvio fictício.
- Próxima ação de escala derivada do status recebido, declarando ausência de conflito/presença no contrato.
- Substituição protegida por profissional não vazio e justificativa obrigatória no servidor.
- Fechamento vazio não inventa registro, valor ou status; mutações ficam desabilitadas com motivo.
- Contrato visual responsivo e smoke v179.
