# Auditoria de autenticação — homologação (2026-09-22)

## Causa raiz e correção

A autenticação real já consultava `plantaopro.usuarios` e verificava `senha_hash`
com BCrypt, mas o seed de homologação não era seguro para bases previamente
usadas: fazia conflito somente pelo UUID fixo, embora a unicidade real seja o
e-mail normalizado. Uma conta com o mesmo e-mail e outro UUID fazia o seed
falhar. Além disso, perfis não administrativos eram escolhidos sem restringir o
tenant e o médico era relacionado apenas dentro de JSON, enquanto o login e o
isolamento usam `medicos.usuario_id`.

O seed agora atualiza cada identidade pelo e-mail antes de inserir, cria perfis
por tenant, restringe a associação de perfil ao escopo correto e grava o vínculo
tipado do médico. Geração e verificação foram centralizadas em
`PasswordHashService`: BCrypt cost 11, com salt aleatório embutido no hash. Não
há senha em claro, comparação em claro, salt separado ou bypass.

Uma segunda auditoria encontrou outro risco em bases reutilizadas: a reaplicação
do seed adicionava o perfil esperado, mas preservava vínculos ativos indevidos e
não reativava o vínculo determinístico quando ele já existia inativo. O seed
agora inativa previamente os vínculos das cinco contas, reatribui exatamente o
papel canônico, provisiona o perfil global se necessário e aborta a transação se
não terminar com cinco contas ativas, tenant coerente e um único perfil ativo
por identidade.

## Mapa real

- Usuários: `plantaopro.usuarios`; identificador em `email`/`email_normalizado`,
  segredo exclusivamente em `senha_hash`, estado em `status` e `reg_status`.
- Perfis: `plantaopro.perfis`; vínculo N:N em `plantaopro.usuarios_perfis`.
- Tenants/clientes: `plantaopro.tenants` e `plantaopro.clientes`; o usuário e o
  vínculo de perfil carregam `tenant_id`/`cliente_id`.
- API: `AuthController` chama `AuthService.LoginAsync`; aceita e-mail, CPF de
  médico e CNPJ somente quando identifica uma única conta. O serviço valida
  usuário, tenant, perfil, senha e bloqueio antes de emitir JWT.
- JWT: inclui identificadores, roles canônicas, perfil primário, tenant/cliente,
  escopo, sessão, permissões e módulos. A sessão persistida é validada após o
  JWT, permitindo revogação no logout.
- Web: `AccountController` chama a API, cria o cookie ASP.NET, guarda o JWT na
  sessão e recusa `returnUrl` externo. O redirecionamento canônico cobre global,
  administrador, coordenação, financeiro, médico, recepção, triagem e hospital.

## Respostas do diagnóstico obrigatório

1. O seed garante que os cinco usuários existam e repara contas já existentes.
2. E-mail é normalizado com trim/lower no login e lower no seed.
3. `status='ATIVO'` e `reg_status='A'` são exigidos e restaurados pelo seed.
4. Tenant não global precisa estar `ATIVO`; suspenso/cancelado é bloqueado.
5. Cada conta recebe perfil canônico ativo.
6. As quatro contas cliente recebem tenant e cliente; global permanece sem tenant.
7. Os cinco hashes foram conferidos contra as senhas com verificador BCrypt compatível.
8. Seed e aplicação usam a mesma coluna, `senha_hash` (`SenhaHash` é apenas alias Dapper).
9. Não são usados `PasswordHash`, `SenhaHash` ou `HashSenha` como colunas alternativas.
10. Não existe salt separado: ele está no hash BCrypt codificado.
11. Login aceita e-mail, CPF e CNPJ; CNPJ ambíguo é recusado.
12. A API emite JWT somente após todas as validações e cria sessão persistida.
13. O Web cria cookie com `SignInAsync` após resposta válida da API.
14. O JWT é guardado na sessão Web para chamadas BFF/API.
15. O redirecionamento usa perfil primário normalizado e destinos explícitos.
16. Login é anônimo; validação de sessão ocorre apenas nas rotas autenticadas.
17. Seed e autorização usam códigos canônicos em maiúsculas.
18. Aliases são normalizados pelo catálogo; o seed usa `ADMINISTRADOR_GLOBAL`.

## Gate e pendências reais

Os testes de contrato incluem os cinco pares e senha incorreta, mas não puderam
ser executados nesta rodada porque o SDK .NET não está instalado. Os nove
validadores Python passaram. A tentativa de verificação alternativa via módulo
`crypt` também ficou bloqueada porque esse módulo não existe no Python 3.13 da
imagem.
A execução integrada com PostgreSQL, API e navegador não foi possível neste
container porque `dotnet`, `psql` e `docker` não estão instalados. Portanto esta
rodada **não declara os cinco logins navegados como aprovados** e, respeitando o
gate, não avançou em funcionalidades nem template. Em ambiente equipado, aplique
o schema, execute o seed documentado em `docs/usuarios-teste.md`, rode os testes
e percorra os 28 itens do roteiro manual do pedido, incluindo isolamento de
tenant, logout, auditoria e HelpBox.

## Próximo prompt recomendado

> Em ambiente com .NET 10, PostgreSQL 16 e navegador, instale o banco do zero,
> aplique o seed 122, execute os cinco logins e todo o roteiro QA. Anexe respostas
> HTTP/claims sem tokens, evidências de cookie/logout e consultas sanitizadas de
> tenant/roles; só então prossiga para evolução funcional e cobertura de HelpBox.
