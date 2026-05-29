# Segurança multiempresa

O isolamento por cliente é centralizado em `TenantGuardService`.

## Regras

- `ADMINISTRADOR_GLOBAL` pode consultar todos os clientes.
- Usuários de cliente acessam somente `cliente_id` do token/contexto.
- Médico deve acessar somente seu cadastro, agenda, escalas, convites e pagamentos.
- Hospital deve acessar somente a unidade vinculada.
- Toda negativa é registrada por auditoria e log estruturado.

## Claims

O JWT deve carregar `uid`, roles e, quando disponível, `cliente_id`. Esses dados alimentam `UsuarioContextService`.

## Homologação

Teste sempre cenários cruzados: admin de um cliente tentando outro cliente, médico tentando outro médico e hospital tentando outra unidade.
