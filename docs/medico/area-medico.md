# Área do Médico

## Princípios de UX
- Mobile-first.
- Cards grandes e objetivos.
- Botões de ação com área de toque confortável.
- Sem tabelas complexas no celular.
- Feedback por toast e estados vazios claros.

## Blocos principais
- Resumo do médico.
- Plantões disponíveis.
- Convites.
- Minhas escalas.
- Meus pagamentos.
- Disponibilidade.
- Preferências.
- Notificações.
- Perfil.

## Regras de segurança
- Médico só acessa dados vinculados ao próprio usuário.
- Pagamentos exibidos são apenas do próprio médico.
- Convites e escalas de outros médicos não aparecem.
- Atualização de perfil não aceita campos administrativos sensíveis.

## Fluxos críticos
- Solicitar plantão: valida duplicidade, elegibilidade e conflito.
- Aceitar convite: revalida vaga e conflito.
- Recusar convite: registra motivo quando exigido.
- Contestação financeira: exige motivo e bloqueia duplicidade aberta.
