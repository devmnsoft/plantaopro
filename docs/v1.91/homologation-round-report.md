# PlantãoPro v1.91 — relatório da rodada de homologação

## Escopo auditado

Esta rodada reconfirmou o estado do Git, os contratos estáticos de rotas, banco,
segurança e UX e revisou o ciclo de autenticação Web/API. O branch de trabalho
estava limpo no início. Os validadores locais passaram sem relaxamento de regra.

## Correções e evolução

- O `refresh-context` Web agora preserva no novo cookie o identificador da sessão
  e o JWT devolvidos pela API. Se a atualização for recusada, cookie e sessão
  local são apagados e o usuário recebe uma mensagem explícita de sessão expirada
  ou revogada, em vez de continuar com uma identidade parcial.
- A redefinição de senha API valida entradas e exige no mínimo 12 caracteres,
  normaliza o e-mail, aceita comparação sem diferença de caixa e usa uma
  transação para trocar o hash, inutilizar todos os tokens de recuperação e
  revogar todas as sessões ainda ativas do usuário.
- Logs de logout e redefinição deixaram de registrar e-mail em claro. Auditoria
  recebe apenas o identificador protegido já adotado pelo fluxo de login.
- Testes de contrato cobrem revogação após redefinição, ausência de PII nos logs,
  preservação de `session_id`/JWT e encerramento local no refresh recusado.
- O foco do *skip link* passou a usar a escala canônica de `z-index`, eliminando a
  regressão apontada pelo gate de experiência sem enfraquecer o validador.

## Banco e dados

Nenhuma migration ou seed foi criada: as correções reutilizam
`recuperacao_senha` e `auth_sessoes` e não mudam o esquema. O manifesto canônico
foi validado estaticamente. Instalação limpa, upgrade e reaplicação não foram
executados por ausência de `dotnet`, PostgreSQL e Docker no ambiente.

## Evidências executadas

- `git status --short --branch`, `git branch --show-current` e
  `git log -5 --oneline --decorate`.
- `dotnet --info`: bloqueado porque o executável não existe no contêiner; por
  consequência, restore, build e testes .NET não puderam ser executados.
- Todos os nove validadores Python solicitados passaram.
- `npm run audit:experience` passou após a correção da escala de overlays.
- `npm run test:login-submit` não iniciou por ausência do Chromium.
- `npx playwright install chromium` tentou instalar o runtime, mas o CDN respondeu
  HTTP 403 em todas as tentativas.
- `git diff --check` passou.

## Falhas, bloqueios e riscos remanescentes

O produto **não está declarado pronto para produção** nesta rodada. Faltam
evidências reais de compilação/testes .NET, Swagger em execução, banco limpo e
upgrade, login API/Web contra PostgreSQL, revogação navegada, isolamento
multi-tenant em runtime e smoke visual. A ausência desses runtimes também impede
afirmar que Super Admin, módulos contratados e a jornada Saúde 360 estejam
homologados ponta a ponta, embora seus contratos estáticos existentes tenham
passado.

Há ainda o risco operacional de indisponibilidade entre a troca de senha e o
registro no serviço de auditoria: a alteração segura já estará confirmada, mas a
auditoria externa pode falhar. O serviço de auditoria deve ser monitorado; não se
deve reverter a proteção da conta para mascarar essa falha.

## Próximo passo recomendado

Executar o pipeline em uma imagem homologada com SDK .NET, PostgreSQL e Chromium:
restore/build/test; instalação limpa e upgrade pelos manifests; subir API e Web;
abrir Swagger; exercitar login/logout/refresh/reset para todos os perfis; provar
403/404 no cruzamento de tenant; validar bloqueio de módulo e limites; navegar a
jornada agenda → check-in → chamada → triagem → consulta → financeiro; e anexar
traces, screenshots e resultados sanitizados aos artefatos de homologação.
