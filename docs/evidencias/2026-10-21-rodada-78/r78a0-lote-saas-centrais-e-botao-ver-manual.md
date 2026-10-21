# R78 - Lote de artigos SaaS/Centrais + botao "Ver manual" em todo header (bloco B3)

## Escopo do plano

R78 do plano R66a0: "lotes - SaaS/Centrais (planos, usuarios/perfis, notificacoes,
cobrancas, ajuda da ajuda) + botao 'Ver manual' padronizado presente em todo
header". Esta rodada fecha o lote de artigos do bloco B3 e faz a primeira
mudanca de UI do bloco.

## O que mudou

### Artigos (backend/PlantaoPro.Web/Controllers/AjudaController.cs)

18 novos artigos no formato de linha ja validado pelo parser da R75:

| Id | Topico / Perfil | Pagina | Titulo |
|---|---|---|---|
| adm-inadimplencia | saas / ADMINISTRADOR_GLOBAL | FaturamentoSaas/Inadimplencia | Como acompanhar a inadimplencia dos clientes |
| adm-assinatura | instituicao / ADMINISTRADOR_CLIENTE | MinhaAssinatura/Index | Como consultar a sua assinatura |
| adm-faturas | instituicao / ADMINISTRADOR_CLIENTE | MinhaAssinatura/Faturas | Como pagar as faturas da assinatura |
| adm-uso-plano | instituicao / ADMINISTRADOR_CLIENTE | MinhaAssinatura/Uso | Como acompanhar o uso contra os limites do plano |
| adm-modulos | instituicao / ADMINISTRADOR_CLIENTE | MinhaAssinatura/Modulos | Como contratar modulos a parte do plano |
| adm-usuarios | instituicao / ADMINISTRADOR_CLIENTE | Usuarios/Index | Como gerenciar usuarios da instituicao |
| adm-novo-usuario | instituicao / ADMINISTRADOR_CLIENTE | Usuarios/Create | Como criar um usuario com perfis |
| adm-minha-conta | instituicao / ADMINISTRADOR_CLIENTE | Usuario/Index | Como revisar sua conta pessoal |
| adm-alterar-senha | instituicao / ADMINISTRADOR_CLIENTE | Usuario/AlterarSenha | Como alterar a senha da sua conta |
| adm-perfis | instituicao / ADMINISTRADOR_CLIENTE | Perfis/Index | Como gerir perfis de acesso |
| adm-matriz-permissoes | instituicao / ADMINISTRADOR_CLIENTE | Permissoes/Matriz | Como consultar a matriz de permissoes |
| cad-empresa | central / TODOS | Cadastro/Empresa | Como comecar o cadastro da instituicao |
| cad-plano | central / TODOS | Cadastro/Plano | Como escolher o plano no cadastro |
| cad-administrador | central / TODOS | Cadastro/Usuario | Como informar o administrador responsavel |
| cad-envio | central / TODOS | Cadastro/Confirmacao | Como revisar e enviar o cadastro |
| cad-conclusao | central / TODOS | Cadastro/Sucesso | Como concluir o cadastro self-service |
| cent-notificacoes | central / TODOS | Notificacoes/Index | Como usar a central de notificacoes |
| ajuda-guia | central / TODOS | Ajuda/Index | Como usar a central de ajuda |

Decisoes de conteudo registradas:

- O fluxo de onboarding e um formulario unico com etapas-ancora (recon:
  Empresa/Plano/Usuario/Confirmacao re-renderizam o mesmo cshtml); os 5
  artigos tratam cada etapa como o usuario a percorre. `Cadastro/Confirmar`
  permanece sem artigo (etapa intermedia do mesmo fluxo).
- `ajuda-guia` aponta para `Ajuda/Index`, par FORA_ESCOPO do inventario
  ("superficie do proprio manual"): valida no contrato (que exige par PAGINA)
  mas nao reduz o gap.
- `adm-matriz-permissoes` descreve a matriz como referencia visual + simulador;
  o recon classificou a matriz como estatica (nao vem de API/perfil real), e o
  artigo nao promete mais do que a tela mostra.
- `NotificacoesOperacionais/Index` (cartao estatico v1.16), `SaasPlanos/*`
  (placeholder comercial) e `PlanosPublicos/*` ficaram fora deste lote para o
  backlog.

### Botao "Ver manual" padronizado em todo header

Recon (mapa completo nos commits desta rodada): existem 2 layouts; o header
canonico autenticado e o `_AppTopbar`; os ramos deslogados (`_Layout` sem
sessao e `_AuthLayout`) nao tinham nenhum header. Mudancas:

- `Views/Shared/_AppTopbar.cshtml`: botao rotulado "Ver manual" (classe
  `btn-outline-secondary btn-sm d-none d-md-inline-flex`, icone `Document` do
  registry - nao existe chave "Book" no `AppIconRegistry`) ao lado do icone de
  ajuda existente, apontando para `Ajuda/Index`. Rotulo visivel alem do icone:
  cor nunca e o unico canal.
- `Views/Shared/_PublicHelpLink.cshtml` (parcial novo): atalho discreto
  "Ver manual" (bootstrap icons + rotulo) para paginas sem topbar.
- `Views/Shared/_Layout.cshtml` (ramo deslogado) e
  `Views/Shared/_AuthLayout.cshtml`: rendizam `_PublicHelpLink` no topo do
  main. Cobre landing/planos publicos/cadastro/login/esqueci-senha/convite.
- Localizacao: nova chave `Nav.TopbarManual` nos 4 resx (pt-BR "Ver manual",
  en "View manual", es "Ver manual", fr "Voir le manuel").
- Usuario deslogado clica em "Ver manual" e cai no login (`AjudaController`
  tem [Authorize]); comportamento registrado, sem mudanca de fluxo.
- `Views/Shared/_NavigationRail.cshtml` segue sendo codigo morto (zero
  referencias); nao foi tocado.

### Contrato novo (backend/PlantaoPro.Tests/R78VerManualContratoTests.cs)

Scanner de fonte com 3 fatos: (1) topbar autenticada exibe a chave
`Nav.TopbarManual` 2x no mesmo botao (title + rotulo visivel) e aponta para
`Ajuda`; (2) `_Layout` e `_AuthLayout` incluem o `_PublicHelpLink`, que
aponta para `Ajuda` e usa a chave localizada; (3) a chave existe com valor
nao vazio nas 4 culturas. A suite cresce 1691 -> 1694.

## Numeros apurados

Rerun do `r75_gap.ps1` apos a mudanca (inventario da R75 atualizado no commit):

```
RESUMO|RETURNS_VIEW_BRUTOS=321|ACOES_EFETIVAS_RESOLVIDAS=674|FILE_OK=616|ARTIGOS=82|COBERTAS=80|FORA_ESCOPO=19|GAP_SEM_ARTIGO=517|ACO_AUX=29|POTE_QUEBRA=29
```

- Artigos: 64 -> 82. Cobertas: 63 -> **80** (17 paginas novas; `Ajuda/Index`
  e FORA_ESCOPO e nao entra no gap).
- Gap: 534 -> **517**. `GapMaximoTravado` atualizado de 534 para 517.

## Backlog restante para os proximos lotes

SaaS/Centrais remanescente (32 paginas):

- MinhaAssinatura (5): Cancelamento, Downgrade, Limites, RevisarModulos, Upgrade
- Usuarios (2): Edit, Save · Usuario (2): Admin, Edit
- Perfis (3): Create, Edit, Permissoes · Permissoes (4): Index, PorPerfil,
  PorUsuario, TestarAcesso
- NotificacoesOperacionais (1, placeholder)
- Planos (5): Comparativo, Create, Details, Edit, Recursos
- PlanosPublicos (3): Comparar, Duvidas, Index
- SaasPlanos (5, placeholder) · FaturamentoSaas (2): Details, GerarMensal
- Cadastro (1): Confirmar

Panorama geral do bloco B3 apos R78 (gap 517): nucleo clinico 45 (R76),
administrativo/financeiro ~77 (R77, menos o onboarding entregue aqui),
SaaS/Centrais 32 (acima), e ~363 distribuidos em Relatorios, Bi, portais,
Suporte/Customer Success, SaaS operacional, seguranca, LGPD, integracoes e
outros modulos. As rodadas seguintes consomem esse backlog em ordem de dominio.

## Gates desta rodada

- Build A/B Release sem novos avisos e sem erros.
- Suite completa com apps parados (total agora 1694 com os 3 fatos novos).
- Docscan sobre os arquivos novos/alterados com delta zero.
- Higiene reexecutada apos este documento.
