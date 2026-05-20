# Relatório de validação final — Demo PlantãoPro

**Data:** 20/05/2026

## 1) Status desta execução
Esta rodada foi executada em ambiente de terminal sem navegador gráfico e sem SDK .NET instalado. Por isso, a validação foi feita como **pré-flight técnico + roteiro operacional de demonstração**, com registro explícito das limitações para não comprometer a apresentação comercial.

## 2) TAREFA 1 — Teste manual completo (checklist de execução)
> **Resultado nesta rodada:** checklist preparado e critérios de aceite documentados; execução visual multi-perfil pendente de rodada com navegador.

### Perfis a validar no login
- ADMINISTRADOR
- COORDENAÇÃO
- OPERADOR
- FINANCEIRO
- MÉDICO
- HOSPITAL

### Itens obrigatórios por perfil
- Dashboard correto do perfil.
- Menus permitidos visíveis; menus proibidos ocultos.
- Botões habilitados/desabilitados conforme permissão e status.
- Empty states coerentes (sem quebra visual).
- Nenhuma exibição de GUID/DTO cru em cards, tabelas e detalhes.

### Fluxos funcionais
- Plantões: criar/editar/publicar.
- Escalas: confirmar/realizar.
- Financeiro: gerar/confirmar pagamento.
- Notificações: geração e visualização.
- Auditoria: trilhas com ator, ação e data/hora.

## 3) TAREFA 2 — Ajustes visuais finais (escopo de inspeção)
> **Resultado nesta rodada:** escopo fechado e pronto para inspeção guiada durante UAT comercial.

Telas críticas para validar UX SaaS:
- Escalas
- Plantões
- Financeiro
- Minha Agenda
- Auditoria
- Dashboard

Critérios visuais:
- Responsividade em 1366x768, 1920x1080 e mobile.
- Hierarquia visual consistente (tipografia, badges, cores, espaçamento).
- Estados de carregamento/sem dados/erro com comunicação clara.

## 4) TAREFA 3 — Fluxos de demonstração (roteiro comercial)
1. Criar plantão em rascunho.
2. Editar plantão em rascunho.
3. Publicar plantão.
4. Médico solicita plantão.
5. Coordenação confirma escala.
6. Marcar escala como realizada.
7. Gerar pagamento.
8. Confirmar pagamento.
9. Ver notificações.
10. Ver auditoria.
11. Validar dashboard atualizado.

## 5) TAREFA 4 — Validação final do build
Comandos esperados:
- `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj`
- `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj`

**Resultado nesta execução:** não foi possível validar build porque o comando `dotnet` não está disponível no ambiente atual (`dotnet: command not found`).

## 6) TAREFA 5 — Entrega
Entregáveis desta rodada:
- Relatório final de validação e roteiro de demonstração atualizado.
- Checklist objetivo para execução manual por perfil.
- Critérios de aceite para UX e fluxos comerciais.

## 7) Go/No-Go para apresentação
- **Go condicional:** após rodar os dois builds em máquina com SDK .NET e executar o smoke visual com os 6 perfis.
- **Recomendação imediata:** capturar evidências (screenshots) de cada etapa do fluxo comercial para anexar ao material de apresentação.
