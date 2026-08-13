# Guia de design premium v1.66

## Fundamentos

- **Cores:** navy para confiança e navegação; azul para ação; teal para contexto assistencial; verde, âmbar e vermelho reservados a sucesso, atenção e falha. Fundo clínico frio, cards brancos, texto secundário em cinza frio com contraste AA.
- **Tipografia:** hero público `clamp(2rem, 10vw, 4.35rem)` conforme viewport; login limitado a `clamp(1.9rem, 3vw, 3rem)`; títulos internos moderados e corpo com line-height mínimo de 1.45. Evitar caixa alta em frases.
- **Espaçamento:** unidade base de 4 px; gaps usuais de 12, 16 e 24 px. Seções usam padding fluido, sem margens negativas.
- **Breakpoints:** 575 px (telefone/ações em coluna), 991 px (sidebar vira drawer e grids viram coluna), 1199 px (painéis laterais empilham).

## Componentes

| Componente | Uso | Regras |
|---|---|---|
| `.pp-page` | Raiz autenticada | Sempre dentro de `.pp-content-container`; largura mínima zero. |
| `.pp-page-hero` | Contexto e ação primária | Compacto, uma mensagem, ações agrupadas e borda sutil. |
| `.pp-card` / `.pp-data-card` | Seção de conteúdo | Fundo branco, borda fria e conteúdo real ou empty state. |
| `.pp-action-card` | Navegação acionável | Flex em coluna, ícone alinhado e CTA no rodapé. |
| `.pp-kpi-card` | Indicador real | Label, valor e explicação; jamais fabricar contador. |
| `.pp-form` | Coleta de dados | Summary no topo e seções por contexto. |
| `.pp-form-field` | Campo | Label acima, controle 100%, ajuda e erro abaixo. |
| `.pp-form-grid` | Campos relacionados | Duas colunas no desktop e uma no mobile. |
| `.pp-data-table` | Dados tabulares | Dentro de `.table-responsive`; cabeçalho nítido e ações agrupadas. |
| `.pp-mobile-card` | Alternativa de tabela | Exibir rótulo e valor sem perder semântica/contexto. |
| drawers | Detalhe contextual | `role="dialog"`, `aria-modal`, foco restaurado, Escape e tela inteira no mobile. |
| modal | Confirmação | Montado em `#pp-overlay-root`, começa `hidden`, nunca participa do fluxo. |
| toast | Feedback transitório | Região live; mensagem humana; não cobrir navegação móvel. |
| empty state | Ausência de dados | Explicar o estado real e oferecer somente ação existente. |

## Exemplos

```html
<section class="pp-page">
  <header class="pp-page-hero">…</header>
  <div class="pp-kpi-grid"><article class="pp-kpi-card">…</article></div>
</section>
```

```html
<form class="pp-form" method="post">
  <div class="pp-form-field">
    <label class="pp-form-label" for="campo">Campo</label>
    <input class="pp-form-control" id="campo" aria-describedby="campo-help campo-error">
    <small id="campo-help" class="form-help">Orientação objetiva.</small>
    <span id="campo-error" class="pp-form-error"></span>
  </div>
  <div class="pp-form-actions"><button type="submit" class="btn btn-primary">Salvar</button></div>
</form>
```
