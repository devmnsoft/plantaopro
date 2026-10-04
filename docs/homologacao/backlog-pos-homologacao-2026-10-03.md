# Backlog Pós-Homologação — PlantãoPro (2026-10-03)

**Entrega 8 de 8.** Itens que a homologação desta rodada **não** fechou, com causa e caminho de destravamento. Ordem sugerida por impacto.

## 1. OperaçãoBff fora do catálogo do guard (pré-existente) — **médio**
- **O que é**: o proxy `bff/operacao/*` (consumido pelo JS do centro de notificações) não tem entrada em `ControllerModules`; quando a sessão cai, o fetch segue o 302, recebe **HTML de AccessDenied** e falha silenciosamente ao parsear JSON. Não afeta as jornadas da camada IA (que POSTam direto em endpoints Web com CSRF).
- **Recomendação**: registrar `OperationBff` no catálogo (módulo comum) **ou** excluí-lo do guard como BFF de API, com teste de regressão do centro de notificações. Origem: Anexo B do relatório P0.

## 2. Enforcement financeiro por valor da IA (`orcamento_mensal`) — **médio**
- `plantaopro.ai_config.orcamento_mensal` é persistido e exibido, mas o bloqueio por **valor** (vs. a cota de usos já aplicada) não existe ainda (documentado em `docs/ia/README.md` §governança).
- **Tarefa**: contador de custo por uso (tokens × preço do modelo) + CHECK/gate no gateway + estado de máquina `COTA_ESGOTADA`.

## 3. Capturas PNG por módulo/perfil — **baixo/médio (homologação)**
- Bloqueadas só pela ferramenta (exige janela desktop visível; erro `Screenshot needs a visible tab` reiterado após `tabs.focus`). Todos os pontos estão instrumentados (mesmas URL/perfis do roteiro).
- **Tarefa**: reexecutar as 5 telas do roteiro com janela visível e salvar em `docs/homologacao/capturas-2026-10-03/`.

## 4. IA com chave real de provedor — **baixo (credencial comercial)**
- Estados sem chave (`VAZIO`, `NAO_ENCONTRADO`, `NAO_HABILITADO`, `NAO_CONFIGURADO`) todos exercitados; a execução com resposta real do provedor fica para quando houver credencial (Groq/Gemini/DeepSeek). Mocks **não** comprovam homologação — declarado.

## 5. S-08/S-09 — produção (rede/TLS/`pg_hba`/papéis de banco) — **médio (implantação)**
- Exigem acesso ao ambiente produtivo. Risco conhecido: v2305 exige papel com **CREATEROLE** para o bypass de imutabilidade. Validar em pré-produção antes do go-live.

## 6. Mobile (app) — **alto (escopo)**
- Avaliação por execução nunca ocorreu nesta rodada; nada foi declarado concluído. Fazer passadas por perfil com o app real + capturas.

## 7. Leitores de tela reais — **baixo (acessibilidade)**
- Estrutura semântica verificada (details/summary nativos, títulos hierárquicos, labels); teste com NVDA/JAWS/VoiceOver pendente.

## 8. Desvio de contraste do link da marca (4,11:1) — **baixo (decisão aberta)**
- `#1f73f1` sobre canvas `#f4f8fb` levemente abaixo de AA para texto pequeno. Mantido como identidade P5; se o cliente exigir AA total, escurecer links pequenos para ~`#1a63d1` (≈4,6:1).

## 9. XML ABRASF real em fixtures — **baixo**
- Pipeline preserva bytes/hash/encoding comprovado, mas sobre fixture sintética. Se houver XML real de produção anonimizado, rodar o mesmo teste contra ele (não modificar o sintético).

## 10. Flake transitória `IsolamentoCadastros_...` (Npgsql 23503 FK) — **baixo (qualidade de teste)**
- Falhou 1× na suíte pós-WP-S4 (exclusão de `adm360_parceiros` × FK de `adm360_orcamentos`), passou isolada e em re-run completo (794/794). Ajustar limpeza/ordenação do fixture para eliminar a janela de corrida entre tests.

## 11. Landing médico em `MODULO_NAO_CONTRATADO` (MEDICO_AREA) — **baixo (comercial/config)**
- Estado de tenant pré-existente: módulo `MEDICO_AREA` não contratado no tenant Clínica Modelo → landing cai em AccessDenied com razão correta. Contratar/habilitar o módulo ou tratar a rota de landing para módulos não contratados.
