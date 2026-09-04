# PlantãoPro v2.14.6 — Central de Pendências e notificações premium

## Escopo e premissas

Esta rodada aprimora exclusivamente Razor, CSS e JavaScript estático. O SDK .NET não está instalado no ambiente; por isso, conforme a restrição da rodada, não foram alterados controllers, services, DTOs, migrations, banco ou contratos da API. Nenhum dado demonstrativo foi criado: contadores, itens, permissões e destinos continuam vindo dos serviços reais existentes.

## Telas alteradas

### Central de Pendências

- A jornada `/Pendencias` passou a se apresentar como **Central de Pendências**, preservando a fonte real `ProductivityPageViewModel`.
- O resumo exibe os totais reais já entregues pelo backend: abertos, críticos, vencidos, para hoje e resolvidos hoje.
- A lista mantém prioridade, módulo, prazo, contexto e CTA fornecidos pelo serviço, sem criar módulos ou registros.
- O filtro de status agora oferece valores operacionais explícitos, além de prioridade, módulo e prazo.
- Foram refinados estados vazio e de erro, feedback acessível e processamento do adiamento.
- A interface identifica visualmente visão global MNSOFT ou escopo da própria instituição a partir dos papéis autorizados da sessão. A filtragem e autorização permanecem no backend existente.
- Foi incluído o bloco “Como usar esta tela”, com orientação de prioridade, filtros, resolução e segurança de tenant.

### Central de Notificações

- A página completa ganhou hierarquia visual, filtros com nomes humanos, limpeza rápida e orientação “Como usar esta tela”.
- Os tipos contemplados na interface são login, sessão, plantão, escala, financeiro, cliente, usuário, permissão, cobrança, auditoria e sistema. Apenas itens retornados pela API são renderizados.
- O sino existente na topbar e o drawer continuam consumindo exclusivamente `/nao-lidas`; não existe contador sintético em caso de indisponibilidade.
- O drawer recebeu o mesmo catálogo de categorias da página completa.
- Ações de leitura, arquivamento, resolução e “marcar todas” agora comunicam processamento, sucesso e erro por toast/área viva, restaurando os botões mesmo após falha.
- Datas inválidas ou ausentes deixam de interromper a renderização da lista.
- Destinos continuam limitados à mesma origem antes de serem exibidos.

### Login e sessão

- Mantidos os estados humanos já existentes para sessão expirada, usuário bloqueado, instituição bloqueada e funcionalidade sem acesso.
- Mantidos label visível “E-mail, CPF ou CNPJ”, validação inline, `aria-live`, aviso de demora/conexão e restauração segura do botão implementados na jornada atual.
- Identificação visual atualizada para v2.14.6. Nenhuma regra de autenticação ou enumeração de conta foi alterada.

## Padrão de mensagens

| Evento | Canal recomendado | Mensagem orientada ao usuário |
| --- | --- | --- |
| Sucesso em ação | Toast de sucesso | Confirma a ação concluída e o objeto afetado. |
| Erro recuperável | Toast + estado contextual | Explica que nada foi executado e oferece nova tentativa. |
| Atenção | Banner contextual | Informa impacto e próximo passo sem termos técnicos. |
| Informação | Banner/área `aria-live` | Atualiza contexto sem interromper a tarefa. |
| Sessão expirada | Banner no login | Solicita nova autenticação por segurança. |
| Bloqueio de usuário/cliente | Banner no login | Orienta procurar a administração sem confirmar existência da conta. |
| Falha de comunicação | Estado de erro | Preserva os dados, orienta conferir conexão e tentar novamente. |

Mensagens técnicas cruas não são renderizadas deliberadamente. O frontend usa os textos humanos do BFF ou mensagens seguras predefinidas.

## Confirmações e ações críticas

O shell mantém o modal acessível compartilhado para ações marcadas com `data-confirm`, contendo título, impacto, cancelar, confirmar e estados de processamento. Nesta rodada, o adiamento usa `dialog` próprio porque não executa nem cancela a operação relacionada; o texto deixa esse impacto explícito. Não foram introduzidos `alert()`, `confirm()` ou `prompt()`.

## Super Admin, tenant, permissões, auditoria e LGPD

- A sinalização global depende de papéis MNSOFT autorizados presentes na sessão.
- A Central não agrega dados no navegador: filtros e paginação são enviados ao serviço existente, que mantém o escopo de tenant e perfil.
- Usuários comuns não recebem seletor global de clientes nesta camada.
- Acesso assistido, banner persistente, saída de contexto e trilhas globais existentes foram preservados.
- Nenhum bypass de autorização, segredo, payload sensível ou identificador digitado manualmente foi incluído.

## Responsividade e acessibilidade

- Cards usam grades progressivas de cinco, três e duas colunas.
- Filtros e ações se reorganizam em telas menores sem ultrapassar a viewport a 100% de zoom.
- Estados usam `role="alert"`/`aria-live`; botões em processamento usam `aria-busy`.
- CTAs preservam foco, labels claros e áreas de toque adequadas.
- Animações respeitam `prefers-reduced-motion`.

## Validações executadas

- Diagnóstico inicial com Git, inventário .NET e buscas de pendências/notificações/feedback.
- QA estático de links vazios, diálogos nativos proibidos, campos por ID, marcadores de debug e padrões de segredo.
- Verificação de whitespace com `git diff --check`.
- Verificações estáticas do repositório para UI premium, feedback e experiência de formulários.

## Limitações reais

- O SDK .NET 10 não está disponível neste ambiente; build, restore e testes automatizados .NET precisam ser executados em CI ou estação com o SDK 10.
- Não foi possível validar dados em execução sem API/banco configurados. Estados visuais não fabricam dados quando a API está vazia ou indisponível.
- A visão por cliente para Super Admin depende do mecanismo real de contexto assistido já existente; não foi criado um seletor paralelo sem contrato seguro no backend.
- Novos tipos no filtro aparecem como opções de consulta, mas só produzirão resultados quando existirem eventos reais dessas categorias no serviço de notificações.
