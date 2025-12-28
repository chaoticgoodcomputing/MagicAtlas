using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.OracleTextEmebdding.Nodes;

namespace MagicAtlas.Pipelines.OracleTextEmebdding;

/// <summary>
/// Atlas Analysis pipeline: Generates semantic embeddings for card oracle text.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Transform raw card text into vector embeddings for downstream ML tasks
/// </para>
/// <para>
/// <strong>Pipeline Flow:</strong>
/// </para>
/// <code>
/// Layer 0 (Seed):       MiniLmOnnxModel (ONNX file)
///                       EmbeddingModelOracleInput (CSV from CardProcessing)
///                                 ↓
/// Layer 3 (Transform):  TokenizeOracleText (BERT tokenization)
///                                 ↓
/// Layer 3 (Intermediate): TokenizedOracleText (in-memory tensors)
///                                 ↓
/// Layer 4 (Transform):  GenerateOracleTextEmbeddings (ONNX inference + mean pooling)
///                                 ↓
/// Layer 4 (Output):     OracleTextEmbeddings (Parquet with 384-dim vectors)
/// </code>
/// <para>
/// <strong>Downstream Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Semantic search: Find mechanically similar cards</item>
/// <item>Clustering: Group cards by gameplay mechanics</item>
/// <item>Recommendations: Suggest cards for deck building</item>
/// <item>Classification: Predict card archetypes from text</item>
/// </list>
/// <para>
/// <strong>Model Requirements:</strong>
/// </para>
/// <para>
/// Requires all-MiniLM-L6-v2 ONNX model at:
/// Data/_06_Models/PreTrained/all-MiniLM-L6-v2/model.onnx
/// </para>
/// <para>
/// See: docs/guides/using-onnx-models-from-huggingface.md
/// </para>
/// <para>
/// <strong>Implementation Details:</strong>
/// </para>
/// <list type="bullet">
/// <item>Tokenization: BertUncasedBaseTokenizer with 512 token limit</item>
/// <item>Inference: ONNX Runtime with OrtValue tensors</item>
/// <item>Pooling: Mean pooling over token embeddings (respecting attention mask)</item>
/// <item>Output: 384-dimensional sentence embeddings</item>
/// </list>
/// </remarks>
public static class OracleTextEmebdding
{
  /// <summary>
  /// Creates the Atlas Analysis pipeline.
  /// </summary>
  /// <param name="catalog">MagicAtlas data catalog</param>
  /// <returns>Configured pipeline</returns>
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      // Node 1: Tokenization
      pipeline.AddNode(
        label: "TokenizeOracleText",
        description: """
        Tokenizes oracle text entries using Microsoft.ML.Tokenizers BERT tokenizer.
        Converts raw text strings into BERT input tensors.

        Tokenizer: Microsoft.ML.Tokenizers.BertTokenizer (all-MiniLM-L6-v2 vocab)
        Max Length: 512 tokens (longer entries are dropped with logging)
        Output: input_ids, attention_mask, token_type_ids for each entry
        """,
        transform: OracleTextTokenizationNode.Create(),
        input: (catalog.MiniLmVocabFile, catalog.EmbeddingModelOracleInput),
        output: catalog.TokenizedOracleText
      );

      // Node 2: ONNX Inference + Mean Pooling
      pipeline.AddNode(
        label: "GenerateOracleTextEmbeddings",
        description: """
        Generates sentence embeddings using ONNX Runtime.
        Applies mean pooling to convert token embeddings to sentence embeddings.

        Model: all-MiniLM-L6-v2 (384-dimensional embeddings)
        Input: Tokenized BERT tensors + ONNX model bytes
        Output: 384-dimensional sentence embedding vectors
        """,
        transform: ApplyEmbeddingModelToOracleTextNode.Create(),
        input: (
          catalog.MiniLmOnnxModel,
          catalog.TokenizedOracleText,
          catalog.EmbeddingModelOracleInput
        ),
        output: catalog.OracleTextEmbeddings
      );
    });
  }
}
