# Limites e Bloqueios

O `AssinaturaGuardService` valida assinatura, status contratual, recursos e limites antes de ações operacionais.

## Validações disponíveis

- Cadastrar médico.
- Cadastrar hospital.
- Cadastrar usuário.
- Publicar plantão.
- Enviar convite.
- Usar Mobile, BI, API, integrações e relatórios avançados.

## Como testar

1. Crie plano com limite baixo.
2. Crie assinatura ativa para o cliente.
3. Cadastre recursos até atingir o limite.
4. Tente cadastrar novo recurso.
5. Confirme resposta amigável, registro em `cliente_bloqueios` e alerta em `cliente_alertas`.
