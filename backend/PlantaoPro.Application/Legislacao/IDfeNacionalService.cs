using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlantaoPro.Application.Legislacao;

/// <summary>
/// R85 (bloco B4 - DF-e nacional): UF autorizada com o código oficial cUF e os documentos
/// fiscais eletrônicos apoiados na fonte única (parametros_legais SEFAZ_UF_&lt;UF&gt;).
/// </summary>
public sealed record DfeUfInfo(
    string Uf,
    int Cuf,
    string Descricao,
    IReadOnlyList<string> DocumentosApoiados,
    string FonteOficial);

/// <summary>
/// R85: layout oficial de DF-e (NF-e/NFC-e/CF-e SAT/CF-e Serviços) com a versão vigente
/// da fonte única (parametros_legais DFE_LAYOUT_*).
/// </summary>
public sealed record DfeLayoutInfo(
    string Documento,
    string Nome,
    string Versao,
    string FonteOficial);

/// <summary>
/// R85: comando de geração da chave de acesso. RegistroCnpjCpf aceita 11 dígitos (CPF) ou
/// 14 dígitos (CNPJ), com ou sem pontuação. As faixas seguem a composição oficial da chave
/// (série 1..999, número 1..999999999, tipo de emissão 1..9, código numérico 0..99999999).
/// </summary>
public sealed record GerarChaveCommand(
    string Uf,
    DateOnly DataEmissao,
    string RegistroCnpjCpf,
    int Modulo,
    int Serie,
    int NumeroNota,
    int TipoEmissao,
    long CodigoNumerico);

/// <summary>R85: comando de validação de uma chave de acesso recebida (44 dígitos).</summary>
public sealed record ValidarChaveCommand(string Chave);

/// <summary>R85: segmento nomeado da chave de acesso para exibição passo a passo.</summary>
public sealed record ChaveSegmento(string Nome, string Valor);

/// <summary>
/// R85: chave de acesso de 44 dígitos decomposta nos segmentos oficiais + dígito verificador.
/// O campo do emitente é sempre exibido com 14 posições (CPF zerado à esquerda), como na chave.
/// </summary>
public sealed record ChaveAcessoDfe(
    int Cuf,
    string Uf,
    int Ano,
    int Mes,
    string Registro,
    int Modulo,
    int Serie,
    int NumeroNota,
    int TipoEmissao,
    long CodigoNumerico,
    int DigitoVerificador,
    string Chave,
    IReadOnlyList<ChaveSegmento> Segmentos);

/// <summary>
/// R85: resultado de geração/validação. Valida indica o veredito final; Erros lista cada
/// falha individual (faixa, dígito verificador, UF/modelo não apoiado) e DetalheCalculo traz
/// a aritmética módulo 11 (soma ponderada, resto e regra do DV) em texto legível.
/// </summary>
public sealed record DfeChaveAcessoResultado(
    bool Valida,
    ChaveAcessoDfe? Chave,
    IReadOnlyList<string> Erros,
    string DetalheCalculo);

/// <summary>
/// R85 (bloco B4 - DF-e nacional): configuração SEFAZ por UF, layouts oficiais vigentes e
/// chave de acesso com dígito verificador módulo 11. A configuração institucional (cUF e
/// documentos apoiados por UF, versões de layout) lê da fonte única de parâmetros legais;
/// o algoritmo módulo 11 é determinístico e vive no serviço (pesos 2..9 da direita para a
/// esquerda, reiniciando em 2; resto 0 ou 1 resulta em DV 0).
/// </summary>
public interface IDfeNacionalService
{
    /// <summary>Catálogo das 27 UFs autorizadas (ordem crescente de cUF) com documentos apoiados.</summary>
    Task<IReadOnlyList<DfeUfInfo>> ListarUfsAsync(CancellationToken ct);

    /// <summary>Layouts oficiais geridos (NF-e, NFC-e, CF-e SAT, CF-e Serviços) com versão vigente.</summary>
    Task<IReadOnlyList<DfeLayoutInfo>> ListarLayoutsAsync(CancellationToken ct);

    /// <summary>Gera a chave de 44 dígitos (incluindo o DV módulo 11) e devolve a decomposição em segmentos.</summary>
    Task<DfeChaveAcessoResultado> GerarChaveAsync(GerarChaveCommand comando, CancellationToken ct);

    /// <summary>Valida uma chave recebida: formato, cUF/UF do catálogo, DV recalculado, faixas dos segmentos e modelo apoiado na UF.</summary>
    Task<DfeChaveAcessoResultado> ValidarChaveAsync(string chave, CancellationToken ct);
}
