# PlantãoPro v1.50.1 — bloqueio do remote Git

Data da verificação: 2026-08-09 (UTC)

## Estado encontrado

- Branch local: `work`.
- Commit local: `2d5133a3a9ada57e28cf59689ee2f76d2a52c759`.
- Branches disponíveis antes e depois da tentativa: somente `work`.
- O repositório não tinha nenhum remote configurado.

## Configuração tentada

```bash
git remote add origin https://github.com/devmnsoft/plantaopro.git
git fetch origin
```

O remote `origin` foi adicionado para fetch e push, mas o fetch terminou com código 128:

```text
fatal: unable to access 'https://github.com/devmnsoft/plantaopro.git/': CONNECT tunnel failed, response 403
```

## Impacto

Não foi possível obter ou validar a branch remota `main`, executar `git pull origin main` nem criar com segurança a branch `codex/v150-template-premium-runtime-validado` a partir da `main`. É necessário liberar no proxy o acesso HTTPS ao GitHub ou fornecer localmente uma cópia atualizada da branch `main`.
