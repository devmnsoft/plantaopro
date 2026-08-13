# Homologação visual runtime — PlantãoPro v1.68.0

## Estado da execução

A imagem de trabalho não possui o SDK `.NET` (`dotnet: command not found`). Por isso, em 13/08/2026, o servidor não pôde ser iniciado e **nenhuma screenshot foi gerada**. Este documento não classifica uma tela como premium sem observação real no navegador. O smoke está pronto para produzir evidências em `artifacts/ui-audit/screenshots/v168/` quando executado contra uma instância real.

| Rota | Tipo | Status visual | Screenshot | Problema encontrado | Correção aplicada | Pendência real |
|---|---|---|---|---|---|---|
| `/` | pública | precisa ajuste | não | Runtime indisponível | Contrato de hero mantido no smoke | Homologar proporção no navegador |
| `/Account/Login` | pública | precisa ajuste | não | Runtime indisponível | Contratos de shell, conteúdo e ação verificáveis | Homologar benefício e colisões |
| `/cadastro/empresa` | pública | precisa ajuste | não | Runtime indisponível | Grid self-service coberto pelo smoke | Homologar formulário no navegador |
| `/Planos` | pública | precisa ajuste | não | Runtime indisponível | Rota preservada na matriz | Homologar cards no navegador |
| `/AdminSaas/Index` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Layout administrativo coberto pelo smoke | Executar com storage state |
| `/Home/Dashboard` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Shell, topbar e cards cobertos | Executar com storage state |
| `/MinhaCentral` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Drawer e responsividade cobertos | Executar com storage state |
| `/MeuDia` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Rota incluída nos oito viewports | Executar com storage state |
| `/Agenda` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Tabelas responsivas cobertas | Executar com storage state |
| `/Plantoes` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Drawer e cards cobertos | Executar com storage state |
| `/Escalas` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Drawer e cards cobertos | Executar com storage state |
| `/Saude360` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Jornada real e empty state preservados | Validar dados da API |
| `/Pacientes` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Formulário e jornada longitudinal cobertos | Validar dados e mascaramento reais |
| `/Agendamentos` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Central de recepção e modal cobertos | Validar endpoints por status |
| `/Triagem` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Formulário clínico aderiu a `pp-form` | Validar mensagens server-side |
| `/Consultas` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Jornada e privacidade cobertas | Validar prontuário real |
| `/Pagamentos` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Tabela/card e valores reais preservados | Validar permissões e toasts |
| `/Financeiro` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Workspace e KPIs derivados preservados | Validar ações transacionais |
| `/Relatorios` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Biblioteca sem CTA futuro falso | Validar exportações autorizadas |
| `/Configuracoes` | autenticada | precisa ajuste | não | Runtime e sessão indisponíveis | Central por área e rotas reais preservadas | Validar perfis de acesso |

> “Precisa ajuste” significa “aguarda inspeção runtime”; não afirma defeito visual sem evidência.
