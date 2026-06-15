# API Keys PlantãoPro

## Criação
Use `POST /api/developer/api-keys` com nome e escopos permitidos. A chave aparece apenas uma vez na resposta; copie imediatamente.

## Escopos permitidos
- plantoes:read
- plantoes:write
- medicos:read
- escalas:read
- webhooks:write
- pacientes:read
- agendamentos:read
- consultas:read
- financeiro:read

## Validação
Escopos vazios são rejeitados. Escopos desconhecidos retornam erro 400 com a lista de valores inválidos. A API Key deve ser enviada em integrações servidor-servidor no cabeçalho configurado para integrações.

## Armazenamento seguro
Nunca salve API Key em texto puro. A aplicação persiste apenas hash e não registra chave em logs ou auditoria.
