# R4-J — Roteiro de homologação do usuário (Rodada 4)

Objetivo: percorrer, módulo a módulo, o que foi construído na Rodada 4 e registrar o **aceite funcional**. Suíte verde ≠ aceite: o veredito deste roteiro é o que move um módulo de "testado" para "homologado".

**Ambiente sugerido:** stack local (Web `https://localhost:52977`, API `http://localhost:51976`/`51977`) com banco de homologação `plantaopro_test` em v2321 — ou servidor IIS seguindo `docs/deploy/guia-implantacao-iis.md` (P0–P8).
**Credenciais:** `docs/usuarios-teste.md` (5 contas @plantaopro.local) + contas da Santa Casa Demonstração: `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor` (medico/consulta idem, senha no padrão do seed — ver `r4i-fiscal.md`).
**Como registrar:** ao final de cada módulo, marque o veredito (APROVADO / FALHOU / BLOQUEADO / NÃO EXECUTADO — legenda em `r4j-matriz-homologacao.md` §0) e anexe print/log em `docs/evidencias/<data>/aceite-r4-<modulo>.md`. FALHOU deve nomear o passo que falhou e o esperado.
**Regra de ouro (L33/H19):** nenhum item pode ser marcado APROVADO só porque a tela respondeu 200 — use o resultado esperado concreto abaixo.

---

## 1. Acesso e permissões (base — A5/B6)
1. `superadmin@plantaopro.local` entra → cai no contexto de administração global (Sem Tenant).
2. `admin.clinica@plantaopro.local` entra → cai no painel da Clínica Modelo PlantãoPro.
3. `medico@plantaopro.local` tenta abrir Financeiro ou Usuários → recusa **humanizada** (mensagem clara de permissão, não 500, não tela em branco).
4. Em Desenvolvimento, abrir diretamente uma rota de teste (ex.: URL `/MvcSeguroTest`) → comportamento correto do ambiente; em `Production` a mesma rota dá 404 (A5).
*Evidência-fonte:* r4a5-test-auth.md, r4b-governanca-b6.md (S1/S2).

## 2. Governança SaaS (B6)
Com `superadmin@plantaopro.local`:
1. Criar/alterar um tenant sintético com plano → verificar que a vigência do contrato muda o acesso por módulo por relógio (S2): módulo contratado expira → tela some no próximo login.
2. Criar/estornar assinatura → máquina TRIAL→ATIVA funciona (S3); status visível.
3. Tentar excluir/alterar o **último** admin global do ambiente → trava com mensagem (proteção B6 S1).
4. Confirme que `permite_relatorios` fica fechado por padrão em banco existente (v2313) — relatórios só abrem se o plano/flag permitir (exercer no item 5).
*Evidência-fonte:* r4b-governanca-b6.md (S1–S5).

## 3. ADM360 core (B7)
Com `gestor@santacasa-demo.example` (tenant Santa Casa Demonstração, contrato ADM360 ATIVO):
1. Cotações: criar → editar → **cancelar** (botão, C11.4) → **estornar** cancelada (botão, C11.4). Valores exibidos pt-BR (`N2`).
2. Conferência: conferir uma referência existente → resultado visível; referência inválida → erro humanizado.
3. XML: listar/importar/consultar documento do tenant.
4. Com `medico@santacasa-demo.example`: telas de triagem/consulta acessíveis; `consulta@...` (AUDITOR) em modo leitura.
5. Isolamento: nada do tenant Q7/A360 aparece nas listas da Santa Casa (Q7, B7).
*Evidência-fonte:* r4c-adm360-b7.md (T01–T18).

## 4. Fiscal pré-emissão (F1/F2) — MVP A29
Com `gestor@santacasa-demo.example`, dentro do ADM360:
1. Menu/tela de fiscal **aparece** (gate: módulo ADM360 contratado). Para usuários sem ADM360, a navegação não expõe o item.
2. **Configurar**: parâmetros do tenant demonstram estado CONFIGURADO no ambiente (operação/UF/provedor = decisão comercial registrada — pendência P2, não é defeito).
3. **Notas**: lista abre; nota finalizada do E2E consta **CANCELADA** (`NPE-00000060`).
4. **Nova pré-emissão**: criar um pré-documento interno → conferência de referências roda → status real persiste.
5. **Emissão**: botão responde com motivo real (ex.: `PENDENTE_DE_CONFIGURACAO` ou credencial ausente). **Não pode** haver botão que sempre retorna sucesso (L376/L33/H19).
6. **Não espere "NF-e autorizada"** neste MVP (A29): pré-documento + conferência; emissão autorizada é o próximo escopo (P1) após credenciais reais.
*Evidência-fonte:* r4i-fiscal.md (E2E 11 passos).

## 5. Saúde 360 financeiro (B8)
Com `financeiro@plantaopro.local` (Clínica Modelo PlantãoPro, contrato FINANCEIRO ATIVO):
1. Registrar baixa de um recebível → valor pt-BR.
2. Estornar a baixa → estado volta consistentemente.
3. Finalizar um atendimento com pre-autorização → a pre-autorização é considerada na finalização (B8).
4. Fechar caixa do dia → resumo confere com as operações do dia (conferir 1–2 linhas via psql se tiver acesso).
5. `recepcao@` não enxerga a área financeira (recusa humanizada).
*Evidência-fonte:* r4d-saude360-b8.md (T01–T15).

## 6. Plantões, Meu Dia e BI (B9)
1. Com `medico@`: abrir **Meu Dia** → agenda real do dia (não mock).
2. Criar/encerrar um plantão; tentar reabrir/contestar com estorno → estados coerentes; `data_negocio` respeitada (relatório caído na competência certa, fuso do tenant).
3. Com `admin.clinica@`: abrir **BI/Relatórios** → filtro por competência, fuso e escopo; valores conferem com o financeiro do item 5 (1 cruzamento).
4. Se o plano do tenant tiver `permite_relatorios=false` → relatório recusa com mensagem clara (GATE G5).
*Evidência-fonte:* r4e-plantoes-meu-dia-bi.md.

## 7. IA assistente (B10)
Pré: chave de provider real configurada no ambiente (no IIS: `Ai__Providers__*__ApiKey` + `Ai__EncryptionKey` de 64 hex no pool API; local: user-secrets/env).
1. Configurar/checar a chave do provider na UI do tenant → chave salva **cifrada** (nunca legível em banco/UI; máscara na exibição).
2. Enviar pergunta simples → resposta real do modelo (ex.: "Diga apenas: ok").
3. Trocar de provider (Groq ↔ Gemini ↔ DeepSeek) → fallback/defaults documentados; erro de saldo/timeout → mensagem humanizada, sem derrubar a tela.
4. Homologação externa já tem veredito 3/3 (r4f): registre aqui apenas o aceite de experiência de uso.
*Evidência-fonte:* r4f-ia-b10-prontidao-chaves.md.

## 8. Design (C11) — passe visual
1. Teclado: Tab percorre as telas-chave (login → painel → cotação → nova pré-emissão) com **foco visível** na cor da marca (C11.3).
2. Contraste: nenhuma área de texto principal ilegível (tokens AA, C11.3).
3. **Breadcrumb único** na topbar — nenhum breadcrumb duplicado em hero/workspace (C11.4/C11.e–g).
4. Overlays (dropdowns/modais/toasts) na ordem correta de camadas (z-index canônico, C11.a/b) — nada "pula" atrás de painel fixo.
5. Viewports reais: 390px (mobile), 1024px (tablet), ~1600px (desktop) nas telas acima → layout íntegro, sem estouro horizontal.
6. Botões de triagem/estorno do ADM360 funcionam e têm affordance correta (C11.4).
*Evidência-fonte:* r4h-c11-design.md (C11.1–C11.4).

## 9. (Se houver IIS) Implantação viva
Seguir `docs/deploy/guia-implantacao-iis.md` P0→P8 em ordem; registrar passo→esperado→obtido. Destaque novo (F2): o pool **Web** agora exige `ConnectionStrings__Default` e, para emissão, `Fiscal__Credenciais__{referencia}`.
*Evidência-fonte:* r4a2-publicacao-iis.md + guia.

## 10. Registro final
| Módulo | Veredito (aceite) | Observações/passo | Evidência anexada |
|---|---|---|---|
| Acesso/Permissões | | | |
| Governança (B6) | | | |
| ADM360 core (B7) | | | |
| Fiscal (F1/F2) | | | |
| Saúde 360 fin. (B8) | | | |
| Plantões/Meu Dia/BI (B9) | | | |
| IA (B10) | | | |
| Design (C11) | | | |
| IIS vivo (se houver) | | | |

Assinatura do usuário: ____________ · Data: ____/____/______

> Este aceite move os módulos para **homologado**. A **liberação para produção** segue o checklist §6 de `r4j-matriz-homologacao.md` (banco v2321, IIS, auditoria F3, decisão fiscal P2, push explícito).
