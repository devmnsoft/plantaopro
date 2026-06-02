# Usuários de demonstração

> Ajuste e-mails/senhas conforme o seed do ambiente. Nunca use senhas reais de produção neste documento.

| Perfil | E-mail sugerido | Uso na demo | Resultado esperado |
|---|---|---|---|
| ADMINISTRADOR_GLOBAL | `admin@plantaopro.com` | Abrir dashboard executivo, clientes, auditoria e observabilidade. | Visão ampla sem expor segredos. |
| ADMINISTRADOR | `cliente-admin@plantaopro.com` | Gerir cadastros, operação e relatórios do cliente. | Dados limitados ao cliente. |
| COORDENACAO | `coordenacao@plantaopro.com` | Publicar plantão, confirmar escala e acompanhar Central de Escala. | Fluxo operacional completo. |
| OPERADOR | `operador@plantaopro.com` | Apoiar listagens e ações operacionais permitidas. | Sem acesso financeiro sensível. |
| FINANCEIRO | `financeiro@plantaopro.com` | Gerar/confirmar pagamentos e exportar financeiro. | Sem acesso indevido a cadastros administrativos. |
| MEDICO | `medico@plantaopro.com` | Solicitar plantão, ver agenda, pagamentos e notificações. | Apenas dados próprios. |
| HOSPITAL | `hospital@plantaopro.com` | Acompanhar plantões/escalas da unidade. | Apenas unidade vinculada. |

## Regras para apresentação
- Validar o login de cada usuário antes da reunião.
- Deixar pelo menos um plantão aberto, uma escala pendente e um pagamento pendente.
- Não exibir tokens, connection strings, hashes ou logs brutos.
- Se algum usuário não existir no ambiente, criar com antecedência via fluxo administrativo/seed controlado.
