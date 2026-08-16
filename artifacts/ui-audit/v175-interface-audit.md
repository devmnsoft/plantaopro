# Auditoria de interface — v1.75.0

## Escopo revisado

A varredura estrutural cobre as 23 rotas do smoke: landing, login, cadastro, planos e os módulos autenticados administrativos, clínicos, operacionais e financeiros. O runner testa oito viewports de 360×800 a 1920×1080.

## Ajustes desta rodada

- Login preserva card proporcional, toggle acessível, Caps Lock, validação e estado ocupado.
- Cadastro recebeu estado de alterações não salvas, prevenção de duplo envio, feedback de progresso e rodapé de ação responsivo.
- A camada `v175-product-polish.css` melhora contenção, responsividade e movimento reduzido sem `!important`.
- Smoke passou a nomear explicitamente os checks exigidos para cards, shell, forms, drawers, links, login, self-service e honestidade financeira.

## Limite da homologação

A auditoria estática foi executada. A avaliação visual real permanece bloqueada pela ausência de `dotnet`; consequentemente não há aprovação de runtime ou screenshots nesta máquina.
