# Correção B2BLaunch escopos

## Causa
O build falhava com CS1061 porque a validação de criação de API Key usava `request.Escopos.Length` em uma sequência que pode ser tratada como `IEnumerable<string>`. `IEnumerable<string>` não expõe `Length`.

## Correção
A criação de API Keys agora normaliza os escopos com trim, lowercase invariant, remoção de valores vazios e distinção case-insensitive antes de validar e persistir. A validação usa o array normalizado e `Array.Empty<string>()` como fallback seguro.

## Segurança
A chave de API continua sendo exibida apenas no retorno de criação. O banco recebe somente `api_key_hash`, e a auditoria registra nome e escopos normalizados sem gravar o segredo.
