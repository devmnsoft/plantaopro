# PlantãoPro v2.14.0 — jornadas guiadas e design premium

## Escopo e pré-validação

Esta rodada foi limitada ao frontend Razor, estilos e documentação. O contêiner não possui o comando `dotnet`, portanto nenhum backend, banco, migration ou arquivo de projeto foi alterado. O repositório também não possui remoto configurado no ambiente de execução.

Foram executados antes das alterações: `git status --short --branch`, `git remote -v || true`, `dotnet --info || true` e `dotnet --list-sdks || true`. O estado inicial estava limpo na branch `work`.

## Jornadas revisadas

| Jornada | Entrada e ação principal | Validação e feedback | Estado vazio / próximo passo | Escopo |
|---|---|---|---|---|
| Login e recuperação | Login institucional, senha visível sob demanda e recuperação dedicada | resumo visual, erro inline, aviso de demora, Caps Lock e estado de envio | orientação de acesso seguro | anônimo até autenticar |
| Primeiro acesso | onboarding e primeiros passos | componentes de feedback existentes | continuar ativação e implantação | administrador autorizado |
| Central Global MNSOFT | Central Global, clientes e mapa de módulos | contexto global destacado e ações auditáveis | consultar fonte real do módulo | somente Super Admin MNSOFT |
| Clientes e registros globais | busca, tenant atual e atalhos por domínio | guard de rota e confirmação do contexto | selecionar cliente antes da ação | global, sem misturar tenants |
| Usuários e perfis | novo usuário / novo perfil | labels, permissões e feedback próprio | revisar perfis ou criar o primeiro | tenant atual; global quando autorizado |
| Médicos e hospitais | cadastro e consulta | formulários existentes com componentes de validação | completar credenciais/unidade | tenant atual |
| Escalas e plantões | criar, filtrar e acompanhar cobertura | erros visuais, filtros tipados e detalhe seguro | criar plantão ou ajustar filtros | tenant atual |
| Financeiro e relatórios | filtrar competência e abrir composição | estados honestos, sem inferir valores ausentes | ajustar filtros e validar origem | permissão financeira no contexto |
| Auditoria | filtrar evento e abrir detalhe | status visual e exportação rastreável | ampliar período | auditor/global autorizado |

## Refinamentos implementados

- Foi criado um navegador de jornada compartilhado e responsivo, adaptado a três experiências: Super Admin MNSOFT, administrador do cliente e usuário operacional.
- Cada jornada apresenta objetivo, escopo, etapas numeradas, etapa atual e links reais. Não há dados simulados nem desbloqueio artificial de funcionalidades.
- A Central Global ganhou um mapa explícito para médicos, hospitais/unidades, escalas, plantões, financeiro, relatórios, saúde da plataforma e implantação, além dos acessos existentes a clientes, usuários, perfis, cobranças, bloqueios e auditoria.
- O texto de contexto reforça que a seleção do tenant, as permissões no servidor e a auditoria permanecem obrigatórias antes de qualquer ação global.
- O componente compartilhado **Como usar esta tela** permanece em todas as páginas autenticadas e descreve finalidade, público, ação, cuidado, contexto e próximo passo. A cópia duplicada da Central Global foi removida.
- O rodapé de autenticação foi atualizado para v2.14.0, preservando o fluxo de sessão, anti-forgery token, recuperação de senha, mostrar/ocultar senha, Caps Lock e mensagens acessíveis já existentes.

## Isolamento e segurança

O novo navegador não modifica autorização, claims, sessão ou consultas. O perfil global recebe atalhos globais; o administrador do cliente recebe a jornada da própria instituição; o usuário operacional recebe uma jornada simplificada. Todos os destinos continuam submetidos ao `SaasRouteGuardFilter`, às permissões retornadas pelo servidor e ao contexto real da sessão. Nenhum tenant, usuário ou registro fictício foi introduzido.

## Formulários e mensagens

Os formulários revisados já usam labels visíveis, campos de data, seletores, resumos de erro e componentes próprios. Esta mudança não introduz campos de ID manual nem substitui relacionamentos por texto livre. Login preserva validação inline, painel de erro, estado processando, alerta de resposta lenta e recuperação de senha. A identificação continua como e-mail porque o contrato atual do view model não autoriza prometer CPF/CNPJ sem alteração e validação de backend.

## Responsividade e acessibilidade

- Em desktop, a jornada combina contexto e quatro etapas; em tablet e celular, as etapas recebem rolagem horizontal sem comprimir ou cortar os rótulos.
- O mapa global passa de quatro para duas e depois uma coluna.
- Estados atuais usam `aria-current="step"`; regiões possuem rótulos; foco por teclado e redução de movimento são respeitados.

## QA, screenshots e limitações

Playwright está instalado como dependência, mas a aplicação não pôde ser iniciada sem o SDK/runtime .NET. Por isso não foram geradas novas screenshots nesta rodada; as imagens antigas em `artifacts/screenshots` não foram apresentadas como evidência da v2.14.0.

Comandos de validação executados:

```bash
rg -n 'href="#"|alert\(|confirm\(|prompt\(|Digite.*Id|Digite.*ID|placeholder=.*Id|placeholder=.*ID|style="|onclick="|innerHTML' backend/PlantaoPro.Web backend/PlantaoPro.Api
git diff --check
```

A busca obrigatória terminou sem ocorrências. Compilação, testes .NET, execução autenticada e screenshots permanecem pendentes exclusivamente pela ausência do SDK/runtime .NET. A validação deve ser repetida em CI ou em uma estação com .NET 10 antes do merge.
