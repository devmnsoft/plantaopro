# Roteiro de Homologação — COORDENACAO

## Cenários

### 1) Login
- **Objetivo do teste:** Validar login da coordenação.
- **Usuário de teste:** coordenacao@plantaopro.com
- **Pré-condições:** Usuário ativo.
- **Passo a passo:** Acessar login, autenticar e validar redirecionamento.
- **Resultado esperado:** Sessão autenticada.
- **Resultado obtido:**
- **Status:** Pendente
- **Observações:**

### 2) Operação diária
- **Objetivo do teste:** Cobrir ciclo operacional completo.
- **Usuário de teste:** coordenacao@plantaopro.com
- **Pré-condições:** Hospitais, médicos e especialidades ativos.
- **Passo a passo:**
  1. Criar hospital.
  2. Criar médico.
  3. Criar especialidade.
  4. Criar plantão.
  5. Publicar plantão.
  6. Confirmar escala.
  7. Recusar escala (com justificativa).
  8. Cancelar escala (com justificativa).
  9. Substituir médico.
  10. Marcar plantão como realizado.
- **Resultado esperado:** Fluxos concluídos com toast e auditoria.
- **Resultado obtido:**
- **Status:** Pendente
- **Observações:**
