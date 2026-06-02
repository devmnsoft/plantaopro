# Roteiro de demo comercial — PlantãoPro

## Objetivo
Demonstrar que o PlantãoPro reduz retrabalho da coordenação, aumenta controle de escalas médicas, melhora rastreabilidade e prepara a operação para uso web + mobile.

## Preparação
- Ambiente homologação ativo.
- Usuários de demonstração revisados em `docs/demo/usuarios-demonstracao.md`.
- Dados mínimos: hospital, especialidade, médicos, plantão aberto, escala pendente, pagamento pendente e notificações.
- Swagger aberto em aba separada para mostrar API Mobile MVP, sem expor segredo.

## Narrativa sugerida
1. **Dor do cliente:** escalas por planilha/WhatsApp geram conflitos, atrasos e baixa rastreabilidade.
2. **Dashboard:** mostrar indicadores, pendências e visão executiva.
3. **Cadastros:** abrir médicos, hospitais e especialidades com filtros e status.
4. **Plantões:** criar ou abrir plantão existente, publicar e explicar vagas/status.
5. **Área do médico:** entrar como médico, ver plantões disponíveis, solicitar plantão e observar feedback por toast.
6. **Coordenação:** confirmar escala pela Central de Escala, usando modal de confirmação.
7. **Financeiro:** gerar/confirmar pagamento e mostrar visão do médico.
8. **Comunicação/notificações:** mostrar mensagens e eventos relevantes.
9. **Relatórios:** exportar CSV e explicar auditoria da exportação.
10. **Auditoria/observabilidade:** demonstrar rastreabilidade para operação segura.
11. **Mobile MVP:** mostrar endpoints com JWT, paginação e isolamento do médico.

## Critérios de sucesso da demo
- Fluxo ponta a ponta entendido por decisores.
- Cliente percebe ganhos em controle, tempo e segurança.
- Sem erros visuais graves durante navegação.
- Próximo passo comercial definido: piloto, proposta ou homologação técnica.

## Pendências conhecidas a declarar se questionado
- Disponibilidade/preferências mobile estão em contrato MVP e devem evoluir com o aplicativo.
- Integrações externas dependem de credenciais e escopo de implantação.
