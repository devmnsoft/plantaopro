namespace PlantaoPro.Tests;

/// <summary>
/// Colecao "saas-operacao-serial": suites do nucleo comercial SaaS que gravam forte nas
/// tabelas canonicas compartilhadas e/ou possuem asserts de varredura GLOBAL:
///  - CobrancaB6Tests: efeitos canonicos de pagamento (faturas_saas, pagamentos_saas)
///    com FOR UPDATE sobre linhas quentes durante webhooks simulados.
///  - ProvisionamentoB5Tests: corridas de provisionamento (clientes/usuarios/tenants,
///    indices unicos arbitros) em SERIALIZABLE.
///  - SaasComercialB4MatrizTests: POST ativar-agendados varre tenant_modulos GLOBAL,
///    entao qualquer AGENDADO vencido semeado por suite concorrente quebra a contagem
///    de idempotencia (visto como flake: Expected 0, Actual 1).
///  - SaasGovernancaB6Rodada4Tests: semeia tenant_modulos AGENDADO vencido (tC) e
///    clientes proprios exatamente dentro da janela lida pelo scan global acima.
/// Em execucao paralela essas gravacoes cruzadas abortavam transacoes alheias com
/// 40001/40P01 (falso-positivo SSI sob ruido legitimo) e deixavam a suíte
/// nao-deterministica. As classes rodam serialmente entre si; demais suites seguem
/// em paralelo. Nao oculta defeito de producao: a concorrencia real de cobranca segue
/// provada dentro de CobrancaB6Tests (indices unicos arbitro + dedupe atomico de
/// webhook na MESMA linha), igual ao convenio ja existente da colecao A360Transmissao.
/// Tambem integram a colecao as suites com assert de AGREGADO GLOBAL impossivel de
/// estabilizar sob churn concorrente: Saude360R4B9PlantoesMeuDiaBiTests mede delta
/// global de KPIs (g0..g1) e ProductivityActionScopingTests insere/remove escalas e
/// agendamentos exatamente dentro de janelas de medicao alheias (flake observado:
/// Escalas delta esperado 2, obtido 0). WS-A3/CotacoesXml partilham TenantSantaCasa
/// e ficam na colecao A360Transmissao preexistente.
/// </summary>
[CollectionDefinition("saas-operacao-serial")]
public sealed class SaasOperacaoSerialCollectionDefinition
{
}
