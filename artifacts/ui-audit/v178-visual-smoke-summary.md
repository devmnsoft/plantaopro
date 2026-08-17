# Smoke visual v1.78.0

O contrato cobre 23 rotas em oito viewports e grava evidências em `artifacts/ui-audit/screenshots/v178/` e `v178-visual-smoke-results.json`.

Novos checks: `clinicalMvpJourneyVisible`, `patientContextVisible`, `nextActionVisible`, `triageRulesVisible`, `consultationBillingActionHonest`, `financialJourneyHonest`, `actionsWithoutBackendDisabled`, `noFakeValues` e `noBrokenLinks`. Os checks v177 foram preservados, incluindo `profileDashboardVisible`.

Execução autenticada: `PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json scripts/ui/run-visual-smoke.sh`. Screenshots não são declarados aprovados sem runtime.
