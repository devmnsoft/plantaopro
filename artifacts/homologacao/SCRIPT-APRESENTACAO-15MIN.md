# Roteiro de Apresentação — Homologação Administrativo 360 (≤15 min)

Público: time + stakeholders do cliente. Tom: direto, evidência primeiro.
Cenário: apps no ar (`https://localhost:52977` Web · `https://localhost:51977` API), navegador aberto, psql/PowerShell prontos em segundo plano.
Fallback: se algo falhar ao vivo, exibir screenshots de `screenshots/` e o relatório — todos os itens críticos têm captura.

---

## Bloco 1 — Contexto e problema (2 min)

- "O gestor do cliente não conseguia abrir o Administrativo 360: toda rota caía em AccessDenied `CATALOGO_NAO_CONFIGURADO` antes mesmo da autorização rodar."
- "Além da tela, havia um defeito no contrato (trigger) e um defeito silencioso no seed de perfis. Três causas raiz, três correções, tudo provado com execução real."
- Mostrar: `artifacts/homologacao/RELATORIO-HOMOLOGACAO-ADM360.md` (veredito APROVADO).

## Bloco 2 — Corrições (3 min)

1. **Guard Web**: catálogo `SaasRouteGuardFilter` ganhou as 4 chaves dos controladores ADM360 → a rota passa para o `[Authorize]` e a permissão decide.
2. **Trigger v2199**: `adm360_validar_tenant()` resolvia campos contra a tabela errada; nova migration corrige por tabela de disparo. v2190 intocada, checksum no manifest.
3. **Seed 122**: unicidade global de `perfis.nome` descartava perfis por tenant; nomes agora sufixados pela identidade do cliente; legacy intacto; seed reaplicável.

Frase-chave: "Nada hardcodado: o acesso vem de perfil + contrato, e o menu e a API leem o mesmo sinal."

## Bloco 3 — Demo ao vivo: login + tela do gestor (4 min)

1. Navegador → `https://localhost:52977` → `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor`.
2. Sidebar → **Administrativo 360** (aparece por causa do módulo contratado + permissão) → abrir `/Administrativo360/Index`.
3. Mostrar na tela: tenant "Santa Casa Demonstração", PERFIL ADMINISTRADOR CLIENTE, MODO Tenant.
4. (Opcional, se tempo) Abrir a sidebar de novo e destacar que os módulos não contratados **não** aparecem como ADM360.

## Bloco 4 — Demo ao vivo: consulta só lê + revogação ao vivo (4 min)

1. Segunda aba (ou logout) → `consulta@santacasa-demo.example` / mesma senha → a mesma home ADM360 abre (leitura). Sem ação de escrita disponível.
2. Terminal (psql): inativar o grant do AUDITOR —
   `UPDATE plantaopro.perfil_permissoes SET reg_status='I' WHERE id='ee2ec9fe-ca35-4184-91ea-d6d29e0769c8';`
   → chamar o dashboard pela API com o mesmo token → **403** e linha `ACESSO_NEGADO` na auditoria.
3. Restaurar (`reg_status='A'`) → **200** com o mesmo token.
4. Frase-chave: "Revogação vale sem relogin: a sessão é revalidada contra o banco a cada request."

## Bloco 5 — Evidências finais e próximos passos (2 min)

- Mostrar os 3 screenshots commitados em `artifacts/homologacao/screenshots/`.
- Matriz: 15 APROVADO · 1 BLOQUEADO (POST /Account/*) · 2 NÃO EXECUTADO (escrita via UI; cross-tenant app-a-app) · 1 FALHOU (home do AUDITOR — fora do crítico).
- Pendências por impacto: `PENDENCIAS-ROADMAP.md` — nenhuma bloqueia o MVP.
- Encerrar: "MVP homologado com execução real; roadmap atualizado está no repositório."

---

## Perguntas previstas (ter respostas prontas)

- **"E se o cliente cancelar o módulo?"** → a linha de `tenant_modulos` sai do conjunto ATIVO/habilitado; guard + autorização barram; o menu some porque lê a mesma fonte.
- **"Resetou senha do usuário?"** → Não silenciosamente: provisionamento preserva hash; reset só pelo script dedicado com confirmação literal.
- **"Podemos rodar isso em produção?"** → Migrations e seeds seguem as regras (história preservada, checksums); faltam decidir registro em `schema_migrations` no runner e os itens de médio impacto da lista.
