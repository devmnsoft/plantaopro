namespace PlantaoPro.Web.Models;

// R72 (rodada 72): bloco reutilizavel de anexos de arquivos binarios (PDF/Word/imagem) —
// pedido do usuario. Usado em Colaboradores (FUNCIONARIO), Parceiros/empresas (EMPRESA)
// e pre-notas fiscais (NOTA_FISCAL). Antes da R72 o unico upload do sistema era a Central XML.
public sealed class AnexosBlocoViewModel
{
    public string Categoria { get; init; } = "";

    public Guid? EntidadeId { get; init; }

    public string Titulo { get; init; } = "Documentos anexados";

    public string Ajuda { get; init; } = "Formatos aceitos: PDF, Word (.doc/.docx) e imagens (JPG/PNG/WebP/GIF). Até 8 MB por arquivo.";
}

// Espelho web do AnexoArquivoResumoDto (Application) — mesma convencao dos demais view models
// espelhados nesta camada (PlantaoPro.Web.Models).
public sealed record AnexoArquivoItemViewModel(
    Guid Id,
    Guid? EntidadeId,
    string Categoria,
    string NomeOriginal,
    string Extensao,
    string TipoMime,
    int Tamanho,
    DateTime CriadoEm,
    string? Observacao);

// Resposta da criacao na API ({ "id": "..." }) — referencia para deserializacao via SendApiAsync.
public sealed record AnexoArquivoCriadoDto(Guid Id);
