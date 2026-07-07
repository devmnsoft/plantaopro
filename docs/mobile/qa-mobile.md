# QA Mobile MVP — PlantãoPro App

Data da rodada: 2026-07-07.

## Comandos executados

```bash
cd mobile/PlantaoPro.App
npm install
CI=1 npm run start
```

## Resultado real

- `npm install`: executado com sucesso; dependências já estavam atualizadas.
- `npm run start`: o Expo/Metro iniciou em modo CI, mas falhou com `TypeError: fetch failed` durante inicialização do Expo em ambiente não interativo/rede do executor.

## Telas a validar no dispositivo/emulador

- Login;
- Home;
- Plantões;
- Convites;
- Escalas;
- Pagamentos;
- Notificações;
- Perfil;
- Disponibilidade;
- Preferências.

## Critério de aprovação

A validação mínima exige abrir o app com URL de API configurável por ambiente, autenticar usuário médico de demonstração e navegar pelas telas acima sem import quebrado, tela sem `export default`, `Alert.alert`, URL fixa de produção ou erro de TypeScript em runtime.
