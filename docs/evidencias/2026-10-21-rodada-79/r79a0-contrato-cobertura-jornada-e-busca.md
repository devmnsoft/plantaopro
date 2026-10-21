# R79 - Contrato de cobertura da jornada critica + busca da Ajuda por termos das telas (bloco B3)

## Escopo do plano

R79 do plano R66a0: "contrato: zero pagina sem artigo correspondente + busca da
Ajuda cobrindo termos usados nas guias das telas".

Decisao registrada (consequencia direta dos numeros medidos na R75 e dos lotes
das R76-R78): o universo real de paginas contaveis tem gap **517** apos os tres
lotes - muito acima da aproximacao original do plano (~294). Fechar o zero
literal em uma rodada exigiria centenas de artigos. O bloco B3 fecha entao com:

1. **Zero de cobertura travado para a jornada critica** - as 41 etapas que um
   usuario percorre no dia a dia (jornada clinica ponta a ponta, nucleo
   administrativo/fiscal, assinatura self-service, onboarding e a propria ajuda)
   nao podem ficar sem artigo, travado por lista literal em teste.
2. **Busca cobrindo os termos das telas** - o nome da tela alvo de cada artigo
   (modulo + acao) entra no indice de busca, com contrato funcional que verifica
   artigo por artigo.
3. O **zero geral** segue mediado por `GapMaximoTravado` no contrato R75
   (hoje 517) e sera consumido pelos lotes das rodadas seguintes, em ordem de
   dominio, ate poder ser zerado de fato com evidencia.

## O que mudou

### Busca (backend/PlantaoPro.Web/Controllers/AjudaController.cs)

`FiltrarArtigos` agora tambem casa o termo contra `PaginaDoArtigo`
(`Controller + " " + Action`). Exemplo concreto: digitando "Inadimplencia" ou
"TitulosPagar" a busca encontra o artigo correspondente mesmo quando o rotulo
da aba nao aparece literalmente no titulo. Titulo/resumo/conteudo/passos
continuam no indice; nada foi removido.

### Contrato novo (backend/PlantaoPro.Tests/R79ManualJornadaContratoTests.cs)

2 fatos:

- `Jornada_critica_tem_artigo_em_cada_etapa`: lista literal de 41 pares
  (controller/acao, case-insensitive) cobrindo: paciente (buscar/cadastro/ficha/
  prontuario), agendamento (agenda/novo/check-in), chamada, triagem
  (fila/nova/finalizar), consulta (lista/atendimento), prescricao
  (modulo/edicao/impressao), faturamento clinico, nucleo administrativo
  (panorama, cadastros, produtos, inventarios, transferencias, contas a pagar,
  despesa, fluxo de caixa, vendas, fechamento de caixa, apuracao e conferencia
  de fechamentos, sincronizacao DF-e), usuarios/perfis, assinatura e faturas,
  alteracao de senha, notificacoes, onboarding (empresa/plano/usuario/envio) e
  a central de ajuda. Falhou de proporsito antes do lote R78 estar completo
  (validado durante a rodada).
- `Busca_localiza_cada_artigo_pelo_nome_da_tela`: instancia o controller real e,
  para cada par (controller, acao) dos 80 artigos distintos, chama `Index(termo,
  "TODOS")` com termo = acao (ou controlador quando a acao e Index) e exige que
  o resultado contenha o artigo. Contrato funcional (nao so scanner de fonte):
  se a clausula da pagina sair do indice de busca, todos os pares falham.

A suite cresce 1694 -> 1696.

## Numeros apurados

Sem mudanca de pagina ou de artigo nesta rodada; inventario mantido como esta
(apos o rerun da R78):

```
RESUMO|RETURNS_VIEW_BRUTOS=321|ACOES_EFETIVAS_RESOLVIDAS=674|FILE_OK=616|ARTIGOS=82|COBERTAS=80|FORA_ESCOPO=19|GAP_SEM_ARTIGO=517|ACO_AUX=29|POTE_QUEBRA=29
```

- Jornada critica: **41/41 etapas cobertas** (zero na jornada).
- Gap geral: 517 (inalterado nesta rodada). `GapMaximoTravado` permanece 517.
- Artigos: 82; pagina: 80.

## Proximos passos do bloco B3 (rodadas seguintes)

Consumir a fila restante de 517 em ordem de dominio (clinico ~45, administrativo/
financeiro ~77, SaaS/Centrais 32, depois Relatorios/Bi/portais/SaaS operacional/
seguranca/LGPD e afins). Cada lote segue o fluxo ja consolidado: recon fiel das
telas -> artigos -> rerun do r75_gap.ps1 -> teto atualizado -> gates -> push. O
zero geral vira contrato quando a fila terminar, com a mudanca explicita no
`GapMaximoTravado` para 0.

## Gates desta rodada

- Build A/B Release sem novos avisos e sem erros.
- Suite completa com apps parados (total agora 1696 com os 2 fatos novos).
- Docscan sobre os arquivos novos/alterados com delta zero.
- Higiene reexecutada apos este documento.
