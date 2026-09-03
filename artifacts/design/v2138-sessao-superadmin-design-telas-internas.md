# v2.13.8 — sessão, Super Admin e telas internas

## Escopo e pré-validação

A rodada foi aberta na branch `codex/v2138-sessao-superadmin-design-telas-internas`. O contêiner não possui o comando `dotnet` nem SDK .NET instalado. Em respeito à restrição da rodada, nenhuma implementação de backend, banco, migration ou arquivo de solução/projeto foi alterado; o trabalho ficou limitado a Razor de apresentação, CSS, JavaScript progressivo e esta documentação.

## Diagnóstico do login e da sessão

A inspeção estática confirma que o formulário de login usa `POST` real para `Account/Login`, antiforgery, validação visível e botão `submit`. O script não substitui o envio por `fetch`: após a validação nativa, permite a navegação tradicional. O controller já registra o início do POST e a resposta da API, cria uma identidade com perfis e escopo, emite cookie persistente por oito horas, salva o JWT na sessão e aceita somente `returnUrl` local.

O sintoma visual de “login travado” pode acontecer quando o POST tradicional demora ou sua navegação é interrompida: o botão permanece em estado de processamento até o limite de recuperação. A v2.13.8 torna essa recuperação explícita na tela. Após 15 segundos, o botão é novamente habilitado, os dados digitados continuam preservados e um callout orienta o usuário a conferir a conexão e tentar novamente. O evento `pageshow` também restaura o botão quando o navegador retorna a uma página mantida em cache.

Não foi possível reproduzir o fluxo HTTP, confirmar `Set-Cookie`, consultar logs novos ou provar a continuidade real da sessão sem runtime .NET, API, banco e credenciais válidas. Os artefatos anteriores igualmente registram o ensaio como não executado. Portanto, esta rodada não afirma uma correção server-side nem cria bypass, mock, credencial ou dado artificial.

## Super Admin MNSOFT e isolamento do tenant

A inspeção estática encontrou o papel global explícito `ADMINISTRADOR_GLOBAL`, redirecionamento prioritário para `AdminSaas`, claims de `access_scope`/`context_mode` e sinalização de “Modo Global MNSOFT”. O cabeçalho e o guia contextual deixam claro quando a pessoa está em visão global e recomendam confirmar o tenant antes de agir.

Para usuários comuns, a interface continua descrevendo o contexto como “somente a instituição vinculada à sua sessão”. Nenhuma regra de autorização, filtro multi-tenant, consulta ou rota foi relaxada. A validação efetiva de listagens globais, troca/saída de contexto, bloqueios, funcionalidades, cobranças e auditoria depende de teste integrado do backend e permanece pendente para um ambiente com SDK e serviços reais.

## Refinamentos visuais

- O login exibe um estado tardio visível, acessível por `role=status` e `aria-live`, sem `alert()` ou modal bloqueante.
- O cabeçalho autenticado ganhou indicador compacto de conectividade. Ele representa apenas conectividade do navegador — não inventa validade da sessão — e alerta para evitar alterações enquanto offline.
- Listas, tabelas, filtros e formulários internos receberam acabamento transversal: raio e sombra consistentes, cabeçalhos legíveis, foco reforçado e alinhamento vertical.
- A composição mobile melhora o espaçamento e permite que ações principais ocupem a largura disponível.
- O parcial global “Como usar esta tela” continua presente em todas as telas autenticadas e já diferencia objetivo, público, contexto global/tenant, próximo passo e cuidado operacional.
- A identificação visual foi atualizada para v2.13.8.

## Formulários e mensagens

O formulário tocado mantém labels visíveis, resumo de validação, mensagens por campo, antiforgery, autocomplete apropriado, indicador de Caps Lock, controle acessível de senha e estado de processamento recuperável. A mensagem de demora usa callout não bloqueante. Não foi adicionado campo de ID manual, `href="#"`, `alert()`, `confirm()`, senha ou segredo.

## QA, comandos e resultados

Executados:

```bash
git status --short --branch
git remote -v || true
dotnet --info || true
dotnet --list-sdks || true
node --check backend/PlantaoPro.Web/wwwroot/js/auth-login.js
node --check backend/PlantaoPro.Web/wwwroot/js/session-health.js
python3 scripts/check-premium-ui.py
python3 scripts/check-form-experience.py
git diff --check
rg -n 'href="#"|alert\(|confirm\(|prompt\(|Digite.*Id|Digite.*ID|placeholder=.*Id|placeholder=.*ID|Password=123456|Username=postgres;Password=|CHANGE_ME_WITH_32|Host=.*Password=' backend scripts docs README.md .env.example
```

Os testes de sintaxe e verificadores estáticos de experiência foram executados. Restore, build, testes .NET, servidor e screenshots autenticados não puderam ser executados porque `dotnet` não existe no contêiner. O Playwright está instalado como dependência, mas uma captura fiel exige aplicação/API em execução e credenciais reais; não foram usados mocks ou bypasses para fabricar screenshots.

## Limitações e validação recomendada

Em um ambiente de homologação com .NET 10, PostgreSQL e credenciais segregadas, executar toda a matriz solicitada de clean/restore/build/test e `npm run diagnose:login`. Conferir no navegador e nos logs: POST de login, `Set-Cookie`, claims, destino por perfil, oito horas de persistência, logout, acesso global completo, troca e saída de contexto, negações cross-tenant e auditoria das ações sensíveis. Gerar então screenshots de login, dashboards global/tenant, usuários, clientes, plantões, financeiro e viewport mobile com dados reais autorizados.
