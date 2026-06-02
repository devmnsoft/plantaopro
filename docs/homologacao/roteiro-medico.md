# Roteiro de homologação — Médico

## Escopo
Validar a Área do Médico em experiência mobile-first, com foco em plantões disponíveis, convites, escalas, pagamentos e notificações.

## Passos
1. Entrar como `MEDICO`.
2. Ver resumo: próximo plantão, convites pendentes, escalas confirmadas, pagamentos pendentes, valor a receber e notificações não lidas.
3. Abrir Plantões Disponíveis.
4. Solicitar um plantão compatível e validar toast de sucesso.
5. Tentar solicitar o mesmo plantão novamente e validar bloqueio de duplicidade.
6. Simular plantão conflitante e validar bloqueio amigável.
7. Abrir Convites, aceitar convite válido e recusar outro com motivo quando exigido.
8. Abrir Minhas Escalas e validar status atualizado após confirmação da coordenação.
9. Abrir Meus Pagamentos e validar pagamento confirmado.
10. Abrir Notificações e marcar item como lido.
11. Atualizar Perfil, Disponibilidade e Preferências.

## Critérios de aceite
- Médico só vê dados próprios.
- Telas funcionam bem em celular, sem tabelas horizontais ruins.
- Botões são grandes e ações críticas usam confirmação amigável.
- Não há dados sensíveis de outros médicos, clientes ou hospitais.
