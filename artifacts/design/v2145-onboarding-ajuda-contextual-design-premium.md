# PlantãoPro v2.14.5 — onboarding, ajuda contextual e design premium

## Escopo

Evolução visual estática da experiência autenticada, preservando autenticação, autorização, isolamento por tenant, auditoria e LGPD existentes. Nenhum endpoint, regra de negócio, esquema de dados ou bypass de segurança foi alterado.

## Telas alteradas

- **Central de Ajuda**: novo cabeçalho contextual, busca segura, atalhos sem digitação de ID, manuais por rotina, recomendações por perfil e início de tour.
- **Primeiros passos**: orientação inicial específica para Super Admin, administrador do cliente, gestor operacional, financeiro, médico/profissional e perfil genérico.
- **Dúvidas frequentes**: respostas sobre acesso, módulos, escalas, financeiro, sessão, bloqueio e isolamento de tenant; foi removida a orientação para carregar massa de demonstração.
- **Onboarding SaaS**: checklist visual completo em onze etapas, legenda de estados, progresso e próximo passo recomendado.
- **Login**: identificação visual da versão 2.14.5. Foram preservados o campo “E-mail, CPF ou CNPJ”, Caps Lock, recuperação de senha, mensagens de sessão e o mecanismo que libera novamente o botão após 15 segundos sem navegação.

## Componentes de ajuda

- Bloco recolhível **Como usar esta tela** nas telas tocadas, com objetivo, ações, obrigatoriedade, status, cuidados e indicação de quando procurar o administrador.
- Cards de manuais curtos e atalhos amigáveis para clientes, usuários/perfis, escalas, plantões, profissionais, financeiro, relatórios e FAQ.
- Conteúdo apresentado conforme o perfil já resolvido pela aplicação. Os links não elevam permissão: filtros, guardas de rota e tenant permanecem responsáveis pela autorização efetiva.
- Avisos explícitos para não inserir dados pessoais na busca nem compartilhar senha.

## Onboarding e checklist

A visão de implantação cobre: dados do cliente; unidades/hospitais; especialidades; usuários; perfis e permissões; médicos/profissionais; escalas; plantões; financeiro; notificações; e revisão final. A legenda contempla **concluído**, **pendente**, **atenção** e **bloqueado**. Antes da seleção real de um cliente, o primeiro item aparece disponível e os dependentes aparecem bloqueados, evitando progresso fictício.

A entrada usa links de navegação e o fluxo existente, sem solicitar identificadores manualmente e sem criar dados simulados.

## Tours guiados

Foi criado um componente JavaScript leve, sem dependência nova, acionado por atributos `data-tour-*`. Ele oferece destaque do elemento atual, título e descrição, progresso, Voltar, Próximo/Concluir, Escape para sair e “Não mostrar novamente” persistido no navegador. O modal usa semântica de diálogo, foco visível e contraste adequado.

Foram preparados tours para a Central de Ajuda e para o onboarding. A preferência “não mostrar novamente” fica apenas no `localStorage`; nenhum dado pessoal é armazenado.

## Super Admin e perfis

- O Super Admin recebe orientação sobre contexto global MNSOFT, clientes, troca explícita de contexto, bloqueio/desbloqueio, cobranças, auditoria, módulos e banner persistente de acesso assistido.
- O administrador do cliente recebe roteiro de equipe, perfis, módulos, operação e pendências.
- Gestor operacional, financeiro e médico/profissional recebem jornadas específicas.
- Textos reforçam que navegação e ajuda não ampliam permissões e que clientes permanecem isolados em seus tenants.

## Design e responsividade

O CSS v2.14.5 adiciona superfícies claras, hierarquia visual, cards equilibrados, estados acessíveis, foco de teclado e layouts que passam de oito para quatro, duas ou uma coluna conforme a largura. Em telas pequenas, ações e resumos passam para coluna e evitam estouro. Não foi adicionado arquivo binário.

## Comandos e resultados

- Diagnóstico solicitado (`pwd`, status, remotes, informações do .NET e buscas com `rg`): executado.
- `dotnet --info` e `dotnet --list-sdks`: indisponíveis (`dotnet: command not found`). Conforme a restrição da rodada, as alterações ficaram limitadas a Razor, CSS, JavaScript e documentação.
- `git diff --check`: executado sem erros.
- Buscas proibidas e de possíveis segredos: executadas; resultados existentes fora dos arquivos tocados foram mantidos e nenhum novo caso foi introduzido.
- Builds e testes .NET: não executados porque o SDK não está instalado no ambiente.

## Limitações reais

Sem o SDK .NET não foi possível compilar, executar testes automatizados ou iniciar a aplicação para captura visual. Por isso não houve alteração em controllers, APIs ou modelos, nem screenshot. A persistência futura do progresso por cliente continua dependente do backend já existente; esta rodada entrega a visualização inicial honesta e os componentes preparados, sem simular conclusão.
