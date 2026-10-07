# Assistente IA — pronto para chaves (B10 · Rodada 4)

- **Data**: 2026-10-07 · **Bloco**: B10 IA (Groq / Gemini / DeepSeek) · **Suíte**: camada IA revalidada verde (194 testes `*Ai*`); suíte completa reexecutada nesta árvore.
- **Prontidão técnica**: implementada, testada e **pronta para receber chaves reais**. Com chave, o sistema já infere de verdade; sem chave, cada ponto devolve o estado explicável `NAO_CONFIGURADO` (sem gastar chamada de LLM).
- **Homologação com provedor real**: **BLOQUEADO p/ chave**. Mocks/unitários não declaram homologação externa (regra da pauta). O que falta é somente a credencial + uma execução real.

## 1. Onde colocar as chaves

A camada lê as chaves em **dois níveis** e usa **a ordem: chave do tenant > chave global do servidor**. Se não houver nenhuma, o ponto de IA responde `NAO_CONFIGURADO` (mensagem amigável, sem segredo na resposta).

### (A) Chave global do servidor — caminho recomendado (mais simples)

Define-se **uma chave por provedor** no processo da **API**, como **variável de ambiente** (mesmos nomes que o código lê):

| Provedor | Variável de ambiente |
|---|---|
| Groq | `Ai__Providers__Groq__ApiKey` |
| Gemini | `Ai__Providers__Gemini__ApiKey` |
| DeepSeek | `Ai__Providers__DeepSeek__ApiKey` |

Precedência do .NET: **variável de ambiente > `appsettings.{Ambiente}.json` > `appsettings.json`**. Os arquivos `appsettings*.json` versionados mantêm `ApiKey` **vazio**; a chave real vai **somente** na variável — nunca commitar.

- **Dev local (Windows)**: definir antes de subir a API, ex. `dotnet run`:
  ```powershell
  $env:Ai__Providers__Groq__ApiKey = "gsk_..."
  $env:Ai__Providers__Gemini__ApiKey = "AIza..."
  $env:Ai__Providers__DeepSeek__ApiKey = "sk-..."
  dotnet run --project backend\PlantaoPro.Api
  ```
  Ou persistir no usuário/sessão com `setx Ai__Providers__Groq__ApiKey "gsk_..."` (requisita nova janela de terminal).
- **Produção/homologação (IIS)**: `%APPCMD% set config "plantao-api" -section:applicationSettings -+[name="Ai__Providers__Groq__ApiKey"].value:"gsk_..."` (idem p/ cada provedor). Ver `docs/deploy/guia-implantacao-iis.md` §5.

Com a chave global, **qualquer tenant** que habilitar a tarefa já usa ela (nenhuma chave por tenant necessária).

### (B) Chave por tenant — quando cada cliente traz a própria chave

Na página **`/AssistenteIa`** (administradores), no campo **“Chave do provedor”** da tarefa. A chave fica **cifrada no banco** (AES-GCM, `ai_config.api_key_cifrada`) e só volta mascarada (`…7890`).

**Exige a chave mestra do servidor** `Ai__EncryptionKey`. Sem ela, o painel exibe um aviso e **bloqueia salvar** chave do tenant (a mensagem é explícita, nada falha em silêncio).

## 2. `Ai__EncryptionKey` (chave mestra de cifragem)

- **Formato exigido pelo código: EXATAMENTE 64 caracteres hexadecimais (= 32 bytes).** Se vier em outro tamanho ou hex malformado, é tratada como **ausente** (sem falha silenciosa) — ver `Ai/AiSecretProtector.cs`.
- Somente para o modo **(B)** (chaves por tenant). No modo **(A)** (chave global) **não é obrigatória**.
- Gerar uma:
  ```powershell
  [System.Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
  # ou
  python -c "import secrets; print(secrets.token_hex(32))"
  ```
- Definir como `Ai__EncryptionKey` (variável de ambiente) no processo da API. É segredo: nunca versionar.

> Observação de consistência: o guia IIS (§5) descreve `Ai__EncryptionKey` como “32+ chars”. O **código** exige **64 hex**. Prevalente aqui é o valor que o código valida (64 hex); o guia pode ser afinado se quiser.

## 3. Como rodar a prova de homologação externa (a ferramenta pronta)

`scripts\ai-external-probe.ps1` faz **uma chamada real e mínima** a cada provedor configurado — mesmos endpoints e esquemas de auth dos adaptadores da API (Bearer p/ Groq/DeepSeek; `x-goog-api-key` p/ Gemini) — **sem banco nem JWT**. Lê as variáveis acima e, opcionalmente, um `.env` na raiz do repo (git-ignorado) no formato `KEY=VALUE`.

```powershell
# 1) chaves como env (ou cole num .env raiz) e rode:
powershell -File scripts\ai-external-probe.ps1
```

Saída por provedor: `[OK]` (chave válida + resposta real), `[FALHA <http>]` (com trecho do erro) ou `[SEM_CHAVE]`.
Exit codes: `0` = pelo menos um `[OK]`; `2` = nenhum homologado; `1` = erro de execução.

Modelos usados pelo probe (sobrescreva com `AI_GROQ_MODEL` / `AI_GEMINI_MODEL` / `AI_DEEPSEEK_MODEL`): `gpt-oss-20b` / `gemini-2.5-flash` / `deepseek-flash` (vigentes, conforme `AiGateway.cs`).

## 4. Fluxo completo no app, depois que as chaves estiverem no ar

1. Subir **API** e **Web** com as variáveis de ambiente das chaves definidas.
2. Em **`/AssistenteIa`**: ligar o checkbox **“Habilitar tarefa”**, escolher **provedor** (e modelo, se quiser sair do sugerido) e clicar em **Testar conexão** — deve devolver `OK`.
   - Com **chave global**: deixe o campo “Chave do provedor” em branco (usa a do servidor).
   - Com **chave do tenant**: preencha o campo (exige `Ai__EncryptionKey` no servidor).
3. Usar os pontos reais:
   - **Resumo das pendências (Meu Dia)** em **`/Pendencias`** (`MEU_DIA_RESUMO`).
   - **Análise de cotação** nos **Detalhes da cotação** do Administrativo 360 (`COTACAO_ANALISE`).
4. Cada uso é auditado em `ai_usos` (tokens, custo, sucesso); a cota mensal só consome em sucesso.

## 5. Critério que destrava a homologação externa (BLOQUEADO → APROVADO)

Para declarar **homologação externa** de um provedor precisa de **prova com chave real**, e mocks não contam:
- **[OK]** do `ai-external-probe.ps1` para esse provedor (chave válida + resposta real), **e**
- **pelo menos uma geração real pelo app** (Meu Dia ou Cotação) retornando texto gerado — não estado `*_CONFIGURADO`.

Se qualquer um dos provedores passar, o item passa de **BLOQUEADO p/ chave** a **APROVADO (homologação externa)** para aquele provedor; os demais seguem classificados conforme sua prova.

## 6. Quando você colar as chaves aqui (no chat)

Informe **qual(is) provedor(es)** e a(s) chave(s). A partir disso:
1. Gravo as chaves no processo desta máquina (variáveis de ambiente; ou `.env` raiz git-ignorado) — **sem commitar**.
2. Rodo o `ai-external-probe.ps1` (prova de chave/rede/wire).
3. Se a API estiver acessível, valido também o **Testar conexão** do `/AssistenteIa` e **uma geração real** pelo app.
4. Registro o resultado como evidência e atualizo a classificação do B10.

> Segurança: colar a chave no chat serve para eu configurar esta máquina de teste. Se preferir tratá-la como permanente/secreta, rotacione-a depois; o que fica no repositório são apenas os **nomes** das variáveis, nunca o valor.
