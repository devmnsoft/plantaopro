using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B16 — §29 item 8: "`consultas.especialidade` text + FK `especialidade_id` coexistindo."
///
/// RESULTADO DA VALIDAÇÃO (fonte canônica: DDL em banco + consumidores em código, sem regra inventada):
///
///   A "coexistência text + id" que a matriz apontou para `consultas` está, de fato, distribuída assim
///   (confirmado via <c>information_schema.columns</c> + varredura de TODOS os 89 usos de `especialidade_id`):
///
///   • `agendamentos` carrega AS DUAS: `especialidade` TEXT + `especialidade_id` uuid.
///   • `consultas` carrega SÓ `especialidade_id` uuid (NÃO tem coluna text); tabela vazia no DB de teste.
///
///   CONSUMIDORES (código de produção):
///     - ESCRITA da clínica (<c>Saude360ClinicalService</c>): o INSERT do agendamento grava
///       <c>especialidade</c> (TEXTO); o UPDATE também grava/atualiza o TEXTO. Nenhum INSERT/UPDATE da
///       clínica popula `especialidade_id`; o serviço inteiro NÃO referencia `especialidade_id` uma única vez.
///       A escrita da <c>consulta</c> não materializa especialidade (nem texto nem id).
///     - EXIBIÇÃO da clínica (<c>CentralAtendimentoService</c>): mostra
///       <c>coalesce(a.especialidade, e.nome, …)</c> → o TEXTO é PRIMÁRIO; o nome resolvido por id
///       (<c>e.nome</c>, via <c>left join … on e.id=a.especialidade_id</c>) é APENAS fallback (que na prática
///       nunca acerta, pois o id nunca é populado na clínica).
///
///   CANONICIDADE:
///     - No subdomínio CLÍNICA (agendamentos/consultas), a representação OPERACIONAL/canônica de especialidade
///       é o TEXTO livre <c>agendamentos.especialidade</c>. As colunas `especialidade_id` de agendamentos/consultas
///       são DORMANTES (forward-schema): não são populadas nem lidas como fonte primária — alimentam apenas um
///       fallback defensivo de exibição.
///     - A representação POR ID da especialidade é, sim, CANÔNICA e operacional NO DOMÍNIO DE PLANTÕES/MÉDICOS
///       (<c>plantoes.especialidade_id</c>, <c>medicos</c>, <c>medico_especialidades</c>, ...): todos os demais usos
///       de `especialidade_id` vivem ali, não na clínica.
///
///   CONCLUSÃO: não há duplo-mestre nem leitura conflitante (o id clínico dorme; o texto manda na clínica).
///   Migrar a clínica para o id equivaleria a normalizar texto-livre → mestre (+ backfill + picker de UI):
///   decisão de modelo de dados fora do rito (§29: sem regra inventada / sem deleção antecipada). Esta fatia
///   ENTREGUE a validação + um CONTRATO anti-conflusão/drift que fixa: (1) a escrita da clínica popula o TEXTO
///   e nunca o id; (2) a exibição da clínica trata o TEXTO como primário (id só como fallback); (3) o id de
///   especialidade é canônico no domínio de plantões/médicos, não na clínica. Zero mudança de produção.
/// </summary>
public sealed class R8B16EspecialidadeClinicaContratoTests
{
    private static string Api(string file) =>
        Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Api", file);

    [Fact]
    public void EscritaDaClinica_PopulaTextoDoAgendamento_NuncaOId()
    {
        var clinica = File.ReadAllText(Api("Saude360ClinicalService.cs"));

        // O serviço de escrita da clínica não referencia NENHUMA coluna especialidade_id.
        Assert.DoesNotContain("especialidade_id", clinica);

        // O agendamento grava a especialidade como TEXTO (coluna entre tipo e status).
        Assert.Contains("data_inicio,data_fim,tipo,especialidade,status", clinica);
        // O update do agendamento também atualiza o TEXTO — nunca o id.
        Assert.Contains("especialidade=coalesce(nullif(@Especialidade,''),especialidade)", clinica);

        // A escrita da consulta não materializa especialidade (nem texto nem id).
        var m = Regex.Match(clinica, @"insert into plantaopro\.consultas\(([^)]*)\)");
        Assert.True(m.Success, "INSERT da clínica em plantaopro.consultas não encontrado.");
        Assert.DoesNotContain("especialidade", m.Groups[1].Value);
    }

    [Fact]
    public void ExibicaoDaClinica_TratatextoComoPrimario_IdApenasFallback()
    {
        var central = File.ReadAllText(Api("CentralAtendimentoService.cs"));

        // A exibição prioriza o TEXTO (a.especialidade); o nome por id (e.nome) só entra como fallback.
        Assert.True(
            Regex.IsMatch(central, @"coalesce\(a\.especialidade\s*,\s*e\.nome"),
            "A exibição da clínica deveria tratar o texto (a.especialidade) como primário sobre o nome por id (e.nome).");

        // O id alimenta a exibição SOMENTE via este left join (fonte secundária/fallback).
        Assert.Contains("left join plantaopro.especialidades e on e.id=a.especialidade_id", central);
    }

    [Fact]
    public void CanonicidadeDoId_EhDoDominioDePlantoesEMedicos_NaoDaClinica()
    {
        var data = File.ReadAllText(Api("Data.cs"));

        // Por contraste: a representação POR ID é operacional no domínio de plantões/médicos
        // (join plantões→especialidades por id), separando-a do campo textual da clínica.
        Assert.Contains("join plantaopro.especialidades esp on esp.id=p.especialidade_id", data);
    }
}
