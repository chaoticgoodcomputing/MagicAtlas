using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._04_Feature.Schemas;
using MagicAtlas.Data._05_ModelInput.Schemas;
using Microsoft.ML.Tokenizers;

namespace MagicAtlas.Pipelines.OracleTextEmebdding.Nodes;

/// <summary>
/// Tokenizes oracle text entries using Microsoft.ML.Tokenizers BERT tokenizer.
/// Converts raw text strings into BERT input tensors for ONNX model inference.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Tokenizer:</strong> Microsoft.ML.Tokenizers.BertTokenizer (all-MiniLM-L6-v2 config)
/// </para>
/// <para>
/// <strong>Max Length:</strong> 512 tokens
/// </para>
/// <para>
/// <strong>Filtering:</strong> Drops entries exceeding 512 tokens and logs the count
/// </para>
/// </remarks>
public static class OracleTextTokenizationNode
{
  private const int MaxSequenceLength = 512;

  /// <summary>
  /// Creates a node that tokenizes oracle text entries.
  /// </summary>
  /// <returns>Node function that transforms text into BERT token tensors</returns>
  public static Func<
    (string vocabFile, IEnumerable<EmbeddingModelOracleInput> inputs),
    Task<IEnumerable<TokenizedBertInput>>
  > Create()
  {
    return async (input) =>
    {
      var (vocabFile, oracleInputs) = input;

      // Create BERT tokenizer from vocab file
      var tokenizer = BertTokenizer.Create(
        new MemoryStream(System.Text.Encoding.UTF8.GetBytes(vocabFile))
      );

      var tokenized = new List<TokenizedBertInput>();
      int droppedCount = 0;
      int totalCount = 0;

      foreach (var entry in oracleInputs)
      {
        totalCount++;

        // Encode with max token limit to check if text is too long
        var ids = tokenizer.EncodeToIds(
          entry.Text,
          maxTokenCount: MaxSequenceLength,
          addSpecialTokens: true,
          out string? normalizedText,
          out int charsConsumed
        );

        // Check if we consumed all characters (if not, text was truncated)
        if (charsConsumed < entry.Text.Length)
        {
          droppedCount++;
          continue;
        }

        // Create attention mask (all 1s since we have no padding for single sequences)
        var attentionMask = Enumerable.Repeat(1L, ids.Count).ToArray();

        // Create token type IDs (all 0s for single sequence)
        var tokenTypeIds = Enumerable.Repeat(0L, ids.Count).ToArray();

        tokenized.Add(
          new TokenizedBertInput
          {
            TextEntryId = entry.TextEntryId,
            CardId = entry.CardId,
            TextType = entry.TextType,
            InputIds = ids.Select(id => (long)id).ToArray(),
            AttentionMask = attentionMask,
            TokenTypeIds = tokenTypeIds,
          }
        );
      }

      // Log dropped entries if any
      if (droppedCount > 0)
      {
        Console.WriteLine(
          $"Dropped {droppedCount} of {totalCount} entries due to exceeding {MaxSequenceLength} token limit"
        );
      }

      return await Task.FromResult(tokenized);
    };
  }
}
