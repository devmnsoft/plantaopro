# QA de permissões por perfil

| Perfil | Resultado esperado | Status da rodada |
|---|---|---|
| ADMINISTRADOR_GLOBAL | Acessa Admin SaaS e todos os tenants | Coberto por menu, serviços e matriz |
| ADMINISTRADOR_CLIENTE | Acessa apenas Portal Cliente e tenant próprio | Coberto por menu e serviços |
| COORDENADOR | Acessa Central de Escala, plantões, escalas e convites | Coberto por menu e matriz |
| FINANCEIRO | Acessa financeiro e faturas, não white label | Coberto por menu e matriz |
| MEDICO | Acessa área médica, agenda e pagamentos próprios | Coberto por menu e redirecionamento |
| HOSPITAL | Acessa operação da unidade | Coberto por menu |
| PARCEIRO | Acessa portal parceiro e propostas | Coberto por menu |
| SUPORTE | Acessa suporte/chamados | Coberto por menu e matriz |
| AUDITOR | Acessa auditoria/relatórios | Coberto por menu e matriz |
| COMERCIAL | Acessa comercial e propostas | Coberto por menu e matriz |
| CUSTOMER_SUCCESS | Acessa Customer Success e onboarding | Coberto por menu e matriz |

## Pendência

Executar navegação manual completa com usuários reais de homologação após build em ambiente com SDK .NET.
