# Homologação visual v1.71.0

## Escopo
Admin SaaS, Minha Assinatura, Planos, Relatórios, Configurações, Auditoria/LGPD, notificações e dashboard nas oito viewports do runner.

## Estado desta entrega
A validação estática cobre estrutura, acessibilidade, segurança do JavaScript e contrato do smoke. O resultado visual somente pode ser aprovado com aplicação .NET ativa, sessão Playwright válida e APIs integradas. Execute:

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json scripts/ui/run-visual-smoke.sh
```

As imagens serão gravadas em `screenshots/v171/`. Este documento não declara screenshots aprovadas antes dessa execução.
