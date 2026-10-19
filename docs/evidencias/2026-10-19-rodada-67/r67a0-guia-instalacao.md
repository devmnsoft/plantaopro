# Guia de instalação do PlantãoPro — banco de dados completo em 1 comando

Este guia instala tudo o que o sistema precisa no PostgreSQL: os papéis (roles) de acesso,
o banco, o esquema completo com todas as tabelas, os dados de sistema (perfis e permissões)
e a conta inicial de superadministrador. No final, o comando gera o arquivo de conexão
(`.local/plantaopro.env`) que os aplicativos usam para falar com esse banco.

O comando faz tudo sozinho. Se alguma coisa falhar, ele para na hora e diz exatamente o
que faltou — nada é instalado pela metade sem você ver.

## O que você precisa (vale para todos os ambientes)

- **PostgreSQL 16 ou mais novo** rodando (local, virtual ou gerenciado).
- Um **usuário do Postgres com permissão de criar bancos e papéis** (superusuário serve;
  em serviço gerenciado, use o usuário principal/master).
- O **repositório PlantaoPro copiado para a máquina** que vai rodar o comando
  (é só o script que lê os arquivos do sistema; ele não muda nada fora da pasta `.local`).
- A senha desse usuário do Postgres (usada apenas para se conectar; fica em variável de
  ambiente e não é gravada em nenhum arquivo).

## Opções do comando (o que cada uma significa)

| Nome | Significado | Padrão |
| --- | --- | --- |
| `-Mode` | `CLEAN` instala num banco novo (o banco ainda nao pode existir); `UPGRADE` aplica as novidades num banco ja instalado | `UPGRADE` |
| `-HostName` | endereço do servidor Postgres (`localhost` ou o hostname do gerenciado) | `localhost` |
| `-Port` | porta do servidor | `5432` |
| `-Database` | nome do banco que vai receber o sistema | `plantaopro` |
| `-OwnerRole` | papel dono dos objetos (usa um superusuário do Postgres para criar) | `plantaopro_owner` |
| `-ApplicationRole` | papel que os aplicativos usam dia a dia (recebe só o necessário) | `plantaopro_app` |
| `-AdminEmail` | e-mail da conta de superadministrador inicial | `admin.global@plantaopro.local` |
| `-AppPassword` | senha do papel da aplicação (evita o pedido interativo) | vazio (pergunta na tela) |
| `-AdminPassword` | senha inicial do superadministrador (troca obrigatória no 1º acesso) | vazio (pergunta na tela) |
| `-Profile` | `base`: instala apenas o sistema (padrão, correto para produção); `demo`: aplica também as contas e os dados de demonstração (somente em `Development`) | `base` |

Passando `-AppPassword` e `-AdminPassword`, o comando roda **sem nenhuma pergunta** —
é esse o modo 1-comando.

## 1) Computador local (desenvolvimento ou demonstração)

Windows (PowerShell, dentro da pasta do projeto):

```powershell
$env:PGUSER='postgres'
$env:PGPASSWORD='SUA_SENHA_LOCAL'
.\scripts\database\install-plantaopro.ps1 -Mode CLEAN -HostName localhost -Database plantaopro -OwnerRole plantaopro_owner -ApplicationRole plantaopro_app -AdminEmail 'voce@empresa.com.br' -AppPassword 'SENHA_DA_ROLE_APP' -AdminPassword 'SENHA_DO_SUPERADMIN'
```

Linux ou macOS (Bash, dentro da pasta do projeto):

```bash
export PGUSER=postgres PGPASSWORD=SUA_SENHA_LOCAL
PLANTAOPRO_INSTALL_MODE=CLEAN PLANTAOPRO_DATABASE=plantaopro PLANTAOPRO_ADMIN_EMAIL=voce@empresa.com.br PLANTAOPRO_APP_PASSWORD=SENHA_DA_ROLE_APP PLANTAOPRO_BOOTSTRAP_PASSWORD=SENHA_DO_SUPERADMIN scripts/database/install-plantaopro.sh
```

Tudo pronto quando aparecer `status=APROVADO` no fim.

## Perfis de instalação (`-Profile`)

O instalador tem dois perfis, escolhidos com `-Profile` (Windows) ou
`PLANTAOPRO_PROFILE` (Linux/macOS):

- **`base`** (padrão) — instala só o sistema: papéis, banco, esquema, dados de sistema
  (perfis e permissões) e a conta de superadministrador. É o perfil correto para produção:
  nenhuma conta de demonstração é criada.
- **`demo`** — aplica tudo do perfil `base` **e**, logo em seguida, os dados de
  demonstração (contas de gestor e médica fictícias, agenda e documentos de exemplo).
  Só é permitido em ambiente `Development`. As contas ficam em
  `database/seeds/development/README.md`.

Instalar já com a demonstração (Windows, PowerShell):

```powershell
.\scripts\database\install-plantaopro.ps1 -Mode CLEAN -HostName localhost -Database plantaopro -OwnerRole plantaopro_owner -ApplicationRole plantaopro_app -AdminEmail 'voce@empresa.com.br' -AppPassword 'SENHA_DA_ROLE_APP' -AdminPassword 'SENHA_DO_SUPERADMIN' -Profile demo
```

Instalar já com a demonstração (Linux ou macOS, Bash):

```bash
PLANTAOPRO_PROFILE=demo scripts/database/install-plantaopro.sh
```

Alternativa ao perfil `demo`: iniciar a API e o portal normalmente em ambiente
`Development` — nesse caso as contas de demonstração entram sozinhas no primeiro
início, sem nenhum comando extra. O perfil `demo` só deixa isso pronto desde o banco.

## 2) Servidor de produção (local ou nuvem)

Mesmo comando, trocando apenas o endereço e usando nomes separados para não misturar
com ambientes de teste:

```powershell
$env:PGUSER='admin_do_sistema'
$env:PGPASSWORD='SENHA_DO_USUARIO_MASTER'
.\scripts\database\install-plantaopro.ps1 -Mode CLEAN -HostName db.suaempresa.com.br -Port 5432 -Database plantaopro_prod -OwnerRole plantaopro_owner -ApplicationRole plantaopro_app -AdminEmail 'ti@suaempresa.com.br' -AppPassword 'SENHA_DA_ROLE_APP' -AdminPassword 'SENHA_DO_SUPERADMIN'
```

Depois, aponte o aplicativo para o banco gerado (o instalador já escreveu a string de
conexão correta em `.local/plantaopro.env` nesta máquina; copie esse arquivo para onde
o aplicativo for executar, mantendo-o fora do controle de versão).

## 3) Nuvem gerenciada (RDS/Cloud SQL/Azure)

1. No painel do serviço, anote o hostname, a porta e crie (ou confirme) um usuário com
   permissão de criar bancos e papéis (usuário master/principal do serviço).
2. Garanta que o firewall/liberação de rede permite a máquina que roda o comando.
3. Rode o mesmo comando da seção 2 apontando para o hostname gerenciado.

Notas honestas para nuvem:
- Se o usuário não tiver permissão de criar papéis, o instalador emite avisos e continua
  quando o serviço permitir (as funções de segurança seguem funcionando para superusuário);
  o ideal é usar o usuário principal do serviço.
- O arquivo `.local/plantaopro.env` gerado contém a senha da role da aplicação; trate-o
  como segredo (ele é gravado com permissão restrita no próprio computador).

## Verificação automática (já embutida)

Antes de dizer `status=APROVADO`, o comando executa o verificador
`scripts/database/verify-complete-install.sql`, que confere: Postgres 16+, encoding UTF8,
schema presente, tabelas obrigatórias, usuários sem duplicidade, perfis únicos, hash de
senha válido, nenhuma migration em estado de falha e perfis de sistema com permissões.
Se qualquer ponto falhar, a instalação é marcada como falha e o erro aparece na tela.

## Se algo der errado

- `autenticação ... falhou`: confira `$env:PGUSER`/`$env:PGPASSWORD` (a senha é a do
  usuário do Postgres, não a do sistema).
- `banco já existe` no modo `CLEAN`: escolha outro `-Database` ou use `UPGRADE`.
- `psql 16+ ... nao encontrado`: instale o client do PostgreSQL 16+ ou coloque a pasta
  `bin` no PATH.
