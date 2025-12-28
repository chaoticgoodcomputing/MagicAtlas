using MagicAtlas.Data._04_Feature.Schemas;
using MagicAtlas.Data._05_ModelInput.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace MagicAtlas.Pipelines.OracleTextEmebdding.Nodes;

/// <summary>
/// Generates sentence embeddings using ONNX Runtime and mean pooling.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Model:</strong> sentence-transformers/all-MiniLM-L6-v2
/// </para>
/// <para>
/// <strong>Architecture:</strong> BERT-base with 384-dimensional sentence embeddings
/// </para>
/// <para>
/// <strong>Processing:</strong>
/// </para>
/// <list type="number">
/// <item>Load ONNX model into InferenceSession</item>
/// <item>Create OrtValue tensors from tokenized input</item>
/// <item>Run inference to get token-level embeddings</item>
/// <item>Apply mean pooling to generate sentence embeddings</item>
/// </list>
/// <para>
/// <strong>Output:</strong> 384-dimensional vector per oracle text entry
/// </para>
/// </remarks>
public static class ApplyEmbeddingModelToOracleTextNode
{
  private const int EmbeddingDimension = 384;

  /// <summary>
  /// Creates a node that generates sentence embeddings from tokenized BERT input.
  /// </summary>
  /// <returns>Node function that transforms tokenized input into embeddings</returns>
  public static Func<
    (
      byte[] modelBytes,
      IEnumerable<TokenizedBertInput> inputs,
      IEnumerable<EmbeddingModelOracleInput> originalEmbeddingText
    ),
    Task<IEnumerable<OracleTextEmbedding>>
  > Create()
  {
    return async (input) =>
    {
      var (modelBytes, tokenizedInputs, originalEmbeddingText) = input;

      // Write model bytes to temporary file for InferenceSession
      var tempModelPath = Path.GetTempFileName();
      try
      {
        await File.WriteAllBytesAsync(tempModelPath, modelBytes);

        // Initialize ONNX Runtime session
        using var session = new InferenceSession(tempModelPath);

        var embeddings = new List<OracleTextEmbedding>();

        foreach (var tokenizedInput in tokenizedInputs)
        {
          var embedding = GenerateEmbedding(session, tokenizedInput);
          embeddings.Add(embedding);
        }

        var embeddingsWithText = embeddings.Join(
          originalEmbeddingText,
          e => e.TextEntryId,
          o => o.TextEntryId,
          (e, o) => e with { Text = o.Text }
        );

        return embeddingsWithText;
      }
      finally
      {
        // Clean up temporary model file
        if (File.Exists(tempModelPath))
        {
          File.Delete(tempModelPath);
        }
      }
    };
  }

  /// <summary>
  /// Generates a sentence embedding for a single tokenized input.
  /// </summary>
  private static OracleTextEmbedding GenerateEmbedding(
    InferenceSession session,
    TokenizedBertInput input
  )
  {
    var sequenceLength = input.InputIds.Length;

    // Create OrtValue tensors from input arrays
    using var inputIdsOrtValue = OrtValue.CreateTensorValueFromMemory(
      input.InputIds,
      new long[] { 1, sequenceLength }
    );

    using var attentionMaskOrtValue = OrtValue.CreateTensorValueFromMemory(
      input.AttentionMask,
      new long[] { 1, sequenceLength }
    );

    using var tokenTypeIdsOrtValue = OrtValue.CreateTensorValueFromMemory(
      input.TokenTypeIds,
      new long[] { 1, sequenceLength }
    );

    // Create input dictionary
    var inputs = new Dictionary<string, OrtValue>
    {
      { "input_ids", inputIdsOrtValue },
      { "attention_mask", attentionMaskOrtValue },
      { "token_type_ids", tokenTypeIdsOrtValue },
    };

    // Run inference
    using var runOptions = new RunOptions();
    using var outputs = session.Run(runOptions, inputs, session.OutputNames);

    // Extract token embeddings (shape: [1, sequence_length, 384])
    var tokenEmbeddings = outputs[0].GetTensorDataAsSpan<float>();

    // Apply mean pooling to get sentence embedding
    var sentenceEmbedding = ApplyMeanPooling(tokenEmbeddings, input.AttentionMask, sequenceLength);

    return new OracleTextEmbedding
    {
      TextEntryId = input.TextEntryId,
      CardId = input.CardId,
      TextType = input.TextType,
      Text = string.Empty, // Will be filled by join with original text
      Embedding = sentenceEmbedding,
      EmbeddingDimension = EmbeddingDimension,
    };
  }

  /// <summary>
  /// Applies mean pooling over token embeddings to generate sentence embedding.
  /// </summary>
  /// <remarks>
  /// Averages token embeddings across the sequence dimension, respecting the attention mask
  /// to exclude padding tokens from the average.
  /// </remarks>
  private static float[] ApplyMeanPooling(
    ReadOnlySpan<float> tokenEmbeddings,
    long[] attentionMask,
    int sequenceLength
  )
  {
    var sentenceEmbedding = new float[EmbeddingDimension];

    // Calculate mean for each embedding dimension
    for (int dim = 0; dim < EmbeddingDimension; dim++)
    {
      float sum = 0f;
      int count = 0;

      for (int token = 0; token < sequenceLength; token++)
      {
        // Only include tokens where attention mask is 1 (valid tokens)
        if (attentionMask[token] == 1)
        {
          // Token embeddings are stored as [batch, sequence, embedding]
          // For batch size 1: index = token * EmbeddingDimension + dim
          sum += tokenEmbeddings[token * EmbeddingDimension + dim];
          count++;
        }
      }

      sentenceEmbedding[dim] = count > 0 ? sum / count : 0f;
    }

    return sentenceEmbedding;
  }
}
