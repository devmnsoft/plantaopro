# Diagnóstico visual v1.63.0

## `/AdminSaas/Index`

| Problema observado | Causa provável | Arquivos afetados | Correção aplicada | Como validar |
|---|---|---|---|---|
| Topbar sobrepunha os primeiros cards | z-index e altura divergiam entre as camadas históricas de CSS | `_Layout.cshtml`, `_AppTopbar.cshtml`, `v161-medical-experience.css` | Topbar sticky com altura por token e z-index 40; conteúdo permanece no fluxo e dentro do container de 1440 px | Conferir `topbarClear` no smoke em 1024, 1366 e 1920 px |
| Painel lateral estreito e texto quebrado | Grid genérico aceitava coluna lateral de 18 rem e checklist em subgrid | `AdminSaas/Index.cshtml`, `v161-medical-experience.css` | Estrutura explícita `pp-admin-layout`, coluna lateral entre 320 e 420 px e checklist vertical | Abrir a rota em 1024 e 1366 px; abaixo de 1200 px o painel empilha |
| Cards desiguais e áreas sem ritmo | Cards administrativos reutilizavam grid clínico sem limites próprios | `AdminSaas/Index.cshtml`, `v161-medical-experience.css` | KPI mínimo de 220 px, cards de ação de 260 px e altura integral | Redimensionar entre 360 e 1920 px sem overflow horizontal |

## `/` — landing pública

| Problema observado | Causa provável | Arquivos afetados | Correção aplicada | Como validar |
|---|---|---|---|---|
| Headline desproporcional e hero excessivo | Hero genérico sem limite de caracteres nem composição própria | `Views/CommercialDemoWeb/Landing.cshtml`, `v161-medical-experience.css` | Hero público em duas colunas, título limitado a 12ch e escala tipográfica controlada | Abrir `/` em 390, 768, 1366 e 1920 px |
| Cards grandes e espaçamento irregular | Grids genéricos acumulavam estilos de versões anteriores | mesmos arquivos | Grid público explícito de três colunas com gap de 1,25 rem e coluna única abaixo de 992 px | Verificar alinhamento e ausência de overflow no smoke |

## `/Account/Login`

| Problema observado | Causa provável | Arquivos afetados | Correção aplicada | Como validar |
|---|---|---|---|---|
| Painel de marca pesado, benefícios cortados | Shell de 1120 px, painel mínimo histórico e múltiplas classes conflitantes | `Login.cshtml`, `_AuthLayout.cshtml`, `v161-medical-experience.css` | Shell de até 1280 px, colunas equilibradas, conteúdo fluido e benefícios em grid responsivo | Abrir em 360, 390, 768 e 1366 px e rolar até o banner |
| Botão colidia com recuperação | Submit e link eram irmãos soltos no grid do formulário | `Login.cshtml`, `v161-medical-experience.css` | Ambos agora vivem em `pp-login-actions`, com nowrap no desktop e coluna no mobile | Confirmar que botão e link nunca se interceptam |
| Banner de segurança e campos saíam do painel | Card não possuía wrapper com largura máxima própria | mesmos arquivos | `pp-auth-card` interno de até 520 px e controles com largura integral | Smoke verifica shell e seus descendentes sem corte horizontal |
