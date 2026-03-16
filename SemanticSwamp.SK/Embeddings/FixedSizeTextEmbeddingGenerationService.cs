using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace SemanticSwamp.SK.Embeddings;

/// <summary>
/// Wraps an underlying <see cref="ITextEmbeddingGenerationService"/> and forces all returned
/// vectors to a fixed size (by truncating or padding with zeros).
///
/// This exists because the Qdrant schema for <c>DocumentUploadRAGEntry</c> is currently fixed to
/// 384 dimensions via an attribute. Local embeddings are naturally 384-dim, while cloud embedding
/// models may return larger vectors.
/// </summary>
public sealed class FixedSizeTextEmbeddingGenerationService : ITextEmbeddingGenerationService
{
    private readonly ITextEmbeddingGenerationService _inner;
    private readonly int _dimensions;

    public FixedSizeTextEmbeddingGenerationService(ITextEmbeddingGenerationService inner, int dimensions)
    {
        _inner = inner;
        _dimensions = dimensions;
    }

    public IReadOnlyDictionary<string, object?> Attributes => _inner.Attributes;

    public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IList<string> data,
        Kernel? kernel = null,
        CancellationToken cancellationToken = default)
    {
        var vectors = await _inner.GenerateEmbeddingsAsync(data, kernel, cancellationToken).ConfigureAwait(false);

        if (vectors.Count == 0)
        {
            return vectors;
        }

        var resized = new List<ReadOnlyMemory<float>>(vectors.Count);

        foreach (var v in vectors)
        {
            if (v.Length == _dimensions)
            {
                resized.Add(v);
                continue;
            }

            var buffer = new float[_dimensions];

            var toCopy = Math.Min(v.Length, _dimensions);
            v.Span[..toCopy].CopyTo(buffer);

            resized.Add(buffer);
        }

        return resized;
    }
}
