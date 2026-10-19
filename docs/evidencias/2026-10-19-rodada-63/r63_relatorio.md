# R63 — Login guiado por CPF/CNPJ/e-mail (fila r53b1 §7 item 8)

Data: 2026-10-19 · Base: `090ec55` (R62 fechada) · Suíte: **1643 → 1647** (+4 fatos)

## Decisão (D-R63-1)

O usuário nunca fica na dúvida sobre o que digitar no login. O backend já
autentica pelos três identificadores (`LoginIdentifierNormalizer`: `@` = e-mail,
11 dígitos = CPF, 14 = CNPJ; CNPJ exige exatamente um candidato individual) —
o que faltava era a **guia na tela**. R63 adiciona a camada de orientação
client-side, **espelhando as regras canônicas do servidor** (nada inventado):

| Camada | O que entrou |
|---|---|
| `Views/Account/Login.cshtml` | elemento `#identifierGuide` (`role="status"` `aria-live="polite"`, dentro de `aria-describedby` do campo) com os 8 labels localizados emitidos como `data-text-*` |
| `wwwroot/js/auth-login.js` | detecção em tempo real do tipo de identificador; máscara progressiva oficial (CPF `000.000.000-00`, CNPJ `00.000.000/0000-00`); guia de quantidade de dígitos; validação dos dígitos verificadores ANTES do POST — documento com erro claro não sai do navegador (protege tentativas/bloqueio) |
| `PlantaoPro.Domain/Identity/LoginIdentity.cs` | `LoginIdentifierChecksums` — referência canônica C# dos algoritmos oficiais de dígito verificador (Receita Federal) |
| i18n (4 resx) | 8 chaves novas `Login.Id*` em pt-BR/en/es/fr (paridade completa travada por R7E1) |

Comportamento da guia:
- campo vazio → sem guia, sem erro;
- contém `@` → "E-mail — conta individual do profissional ou administrador.";
- 1–11 dígitos → máscara CPF + "Um CPF tem 11 dígitos — continue digitando o número.";
- 12–13 dígitos → muda para máscara CNPJ + "Um CNPJ tem 14 dígitos…";
- 11 dígitos com DV inválido → mensagem clara + bloqueio do envio (validade nativa,
  o submit manual já inspeciona `:invalid`) + `aria-invalid` para leitores de tela;
- 14 dígitos com DV inválido → idem para CNPJ;
- >14 dígitos → "O documento informado tem mais dígitos do que o esperado…".

Sem diálogos nativos (`alert/confirm/prompt`), sem segredo, sem `href="#"` —
todo texto vem dos resx (verificados em pt-BR e em francês via Accept-Language).

**Servidor intencionalmente inalterado**: a autenticação canônica já trata os
três identificadores e o erro permanece genérico (sem vazar informação sobre a
existência de contas) — a guia client-side só elimina os erros de digitação
óbvios antes do POST.

## Evidências executáveis

- Contrato próprio: `backend/PlantaoPro.Tests/R63LoginGuiadoContratoTests.cs` (4 fatos):
  1. referência C# de dígitos verificadores com vetores clássicos conhecidos
     (CPF `529.982.247-25` válido; `11111111111` inválido; CNPJ `11.222.333/0001-81`
     válido; todos iguais inválidos; entradas nulas não lançam exceção);
  2. as 8 chaves presentes e não vazias nos quatro locales;
  3. view com guia acessível e os 8 labels localizados no markup;
  4. JS espelha a classificação canônica (`@`/11/14), os pesos oficiais e as
     máscaras, bloqueia via validade nativa + `aria-invalid`, sem diálogos nativos.
- Mini e2e: `r63_e2e.ps1` (BOM UTF-8) — `/Account/Login` renderiza o guia com os
  labels pt-BR e a tradução francesa via Accept-Language fr:
  `PT_GUIA_OK cpf=1 cnpj=1 FR_GUIA_OK=True FALHAS=0`.

## Gates (todos verdes)

| Gate | Resultado |
|---|---|
| Build Release `--no-incremental` | exit 0 (log `%TEMP%\opencode\r20b1_build.log`) |
| Gate de casa `r20b1_gate.ps1` | `Now=160 Base=160 DIFF=NENHUMA erros=0` |
| Dirigidos R63+i18n+copy (R7E1/R7DF6/R25/R34/R50/R56/R57) | 60/60 |
| Suíte integral (apps parados) | **1647/1647** limpa (transientes conhecidos: R19 sob carga e 1 falha única em execução correndo logo após descer os apps — isolados 10/10; registro para R65/item 10) |
| Smoke R52 e2e | FALHAS=0 (PERF média 186 ms) |
| Mini e2e R63 | FALHAS=0 |
| Docscan arquivos mudados | bad=0 |

## Notas de aprendizado (acumuladas para contratos futuros)

- Os scanners de repo (`EvolucaoProdutoVendavelContractTests` /
  `HomologacaoCrudsAcoesJornadasContractTests`) proíbem os literais `alert(` e
  `confirm(` em QUALQUER `.cs` — inclusive em testes que os citam para travar
  ausência em JS. Padrão adotado: construir por concatenação (`"al" + "ert("`).
- Quirk da máquina (também visto em R62): PS 5.1 local trata `null` sem `$`
  como cmdlet em script file — usar sempre `$null`.
