# Importação CID-10

A CID oficial é armazenada em `plantaopro.cid_tabela` com `codigo`, `descricao`, `descricao_normalizada`, capítulo, grupo, categoria, fonte e versão. O sistema não depende da coluna `nome`.

## CSV esperado

Colunas obrigatórias:

- `codigo`
- `descricao`

Colunas opcionais:

- `capitulo_codigo`
- `capitulo_nome`
- `grupo_codigo`
- `grupo_nome`
- `categoria`

Exemplo:

```csv
codigo;descricao;capitulo_codigo;capitulo_nome;grupo_codigo;grupo_nome;categoria
I10;Hipertensão essencial;IX;Doenças do aparelho circulatório;I10-I15;Doenças hipertensivas;I10
```

## Endpoints

- `POST /api/cid/importar-csv` com o conteúdo em `csv`.
- `POST /api/cid/importar-url` usando URL enviada ou `CidSettings:FonteOficialUrl`.
- `GET /api/cid/importacoes` para auditar importações.

Se a fonte oficial não estiver automatizável, exporte o CSV da fonte oficial e use upload manual.
