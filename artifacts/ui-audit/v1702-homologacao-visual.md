# Homologação visual v1.70.2

## Escopo

O runner cobre as 22 rotas definidas para a recuperação da v1.70, em oito viewports entre 360×800 e 1920×1080. Ele verifica overflow horizontal, recorte de cards, tabelas responsivas, shell desktop, labels de campos, modais inicialmente ocultos e overlays fora do fluxo.

Também exercita a Command Palette, o drawer de notificações (abertura, Escape e retorno de foco) e o estado real ou vazio de Minha Assinatura.

## Execução

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json \
scripts/ui/run-visual-smoke.sh
```

Sem uma sessão válida, somente as rotas públicas podem ser auditadas com `PLANTAOPRO_PUBLIC_ONLY=1`. A homologação runtime não foi declarada como aprovada neste artefato porque exige aplicação e API ativas.
