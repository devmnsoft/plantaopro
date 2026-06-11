# Execução local — PlantãoPro

Este guia padroniza a execução local da API e do Web para evitar conflitos de bind no Windows, especialmente `SocketException (10013)` em portas ocupadas, bloqueadas ou reservadas.

## Portas padrão

### API (`PlantaoPro.Api`)

- HTTP: `http://localhost:51976`
- HTTPS: `https://localhost:51977`
- Swagger: `/swagger`

### Web (`PlantaoPro.Web`)

- HTTP: `http://localhost:52976`
- HTTPS: `https://localhost:52977`
- Login: `/Account/Login`

## Executar a API em HTTP

```powershell
dotnet run --project backend/PlantaoPro.Api/PlantaoPro.Api.csproj --urls "http://localhost:51976"
```

Acesse:

```text
http://localhost:51976/swagger
```

## Executar o Web em HTTP

```powershell
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj --urls "http://localhost:52976"
```

Acesse:

```text
http://localhost:52976/Account/Login
```

## Executar com HTTPS local

Antes de usar HTTPS, garanta que o certificado de desenvolvimento do .NET esteja confiável no Windows:

```powershell
dotnet dev-certs https --trust
```

Depois, execute pelos profiles `https` do Visual Studio ou use as portas HTTPS padrão:

- API: `https://localhost:51977/swagger`
- Web: `https://localhost:52977/Account/Login`

## Configuração Web -> API

No arquivo `backend/PlantaoPro.Web/appsettings.Development.json`, o Web deve apontar para a API local:

```json
"ApiSettings": {
  "BaseUrl": "https://localhost:51977"
}
```

Para usar HTTP temporariamente, altere para:

```json
"ApiSettings": {
  "BaseUrl": "http://localhost:51976"
}
```

Se a chave legada `PlantaoProApi:BaseUrl` existir, mantenha-a alinhada com a mesma URL usada em `ApiSettings:BaseUrl`.

## Visual Studio — startup múltiplo

1. Abra a solution do PlantãoPro.
2. Clique com o botão direito na solution e selecione **Configure Startup Projects**.
3. Selecione **Multiple startup projects**.
4. Defina **Start** para:
   - `PlantaoPro.Api`
   - `PlantaoPro.Web`
5. Confirme que os profiles selecionados usam as portas padrão deste documento.

## Resolver `SocketException (10013)`

O erro indica que o Windows recusou o bind da aplicação na porta solicitada. As causas mais comuns são:

- Outro processo já está usando a porta.
- A porta está em uma faixa reservada/excluída pelo Windows.
- Uma configuração antiga de `launchSettings.json` ainda aponta para uma porta problemática.
- Certificado HTTPS local corrompido ou não confiável.

### Verificar processo preso na porta

Substitua a porta conforme necessário:

```cmd
netstat -ano | findstr :52976
netstat -ano | findstr :52977
netstat -ano | findstr :51976
netstat -ano | findstr :51977
```

Se precisar investigar uma porta antiga ou problemática, substitua `<PORTA>` pelo número da porta:

```cmd
netstat -ano | findstr :<PORTA>
```

### Matar processo preso

Depois de identificar o PID no `netstat`, finalize o processo:

```cmd
taskkill /PID <PID> /F
```

### Verificar portas reservadas pelo Windows

```cmd
netsh interface ipv4 show excludedportrange protocol=tcp
```

Se uma porta estiver dentro de uma faixa excluída, use as portas padrão definidas neste documento ou ajuste a reserva no Windows com privilégios administrativos.

### Limpar certificado HTTPS local

Se houver erro de certificado HTTPS local:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Em seguida, reinicie o Visual Studio ou o terminal e execute novamente API/Web.

## Checklist rápido

- API HTTP abre em `http://localhost:51976/swagger`.
- API HTTPS abre em `https://localhost:51977/swagger`.
- Web HTTP abre em `http://localhost:52976/Account/Login`.
- Web HTTPS abre em `https://localhost:52977/Account/Login`.
- `PlantaoPro.Web` aponta para `https://localhost:51977` em desenvolvimento.
- Nenhum profile local usa porta antiga problemática.
