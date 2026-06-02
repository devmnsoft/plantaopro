# Checklist de pré-homologação

## Técnico
- [ ] API inicia sem erro.
- [ ] Web inicia sem erro.
- [ ] Swagger abre.
- [ ] `/api/health` retorna sucesso.
- [ ] Build API verde.
- [ ] Build Web verde.
- [ ] Sem binários versionados.
- [ ] Sem `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()` ou `confirm()` nativo nas áreas verificadas.

## Operacional
- [ ] Hospital criado.
- [ ] Especialidade criada.
- [ ] Médico criado.
- [ ] Plantão criado como rascunho.
- [ ] Plantão publicado.
- [ ] Médico solicita plantão.
- [ ] Coordenação confirma escala e vaga reduz.
- [ ] Escala realizada.
- [ ] Financeiro gera pagamento sem duplicidade.
- [ ] Financeiro confirma pagamento.
- [ ] Médico visualiza pagamento confirmado.

## Segurança
- [ ] Médico só vê dados próprios.
- [ ] Usuário comum só vê dados do cliente próprio.
- [ ] Hospital só vê hospital/unidade vinculada.
- [ ] Admin global consegue visão ampla.
- [ ] Acesso negado é amigável.

## Produto
- [ ] Central de Escala reflete pendências.
- [ ] Agenda operacional abre com filtros.
- [ ] Notificações funcionam.
- [ ] Comunicação abre nos contextos previstos.
- [ ] API Mobile MVP aparece no Swagger.
- [ ] Demo comercial tem roteiro validado.
