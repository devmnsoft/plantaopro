# Componentes refatorados na v1.55

## App shell

`_Layout` agora expressa diretamente as três regiões estruturais: sidebar, main shell e conteúdo/rodapé. A camada `v155-medical-experience.css` centraliza apenas os contratos finais da versão, sem `!important`, enquanto `plantaopro.css` voltou a ser somente um manifesto de imports, sem duplicação.

## Navegação

- `_AppTopbar`: breadcrumb com reset local, título compacto e ações responsivas.
- `_UserMenu`: trigger e painel sem dependência de lista ou Bootstrap dropdown; mostra identidade, perfil e tenant já presentes nas claims.
- `v155-medical-ui.js`: abertura acessível, Escape, clique fora, setas e restauração de foco.
- `_AppSidebar` e `_AppFooter`: nomes canônicos no shell e versão atualizada.

## Autenticação

O login usa superfície navy de confiança, logo contido, benefícios que não são cortados, painel branco e formulário com ritmo fixo. Senha, Caps Lock, validação e loading permanecem progressivos e compreensíveis.

## Formulários e SaaS

O novo contrato normaliza labels, controles, grids, seções, erros e ações. Planos usam grid elástico e cards de mesma altura. Onboarding, Admin SaaS e B2B permanecem ligados a dados reais e passam por gates dedicados.

## Regressão

`check-layout-regression.py` passou a exigir os nomes v1.55, dropdown sem lista e ausência de `!important`; `check-saas-ui.py` documenta a versão atual e preserva as verificações de dados reais, wizard, cards, botões e feedback.
