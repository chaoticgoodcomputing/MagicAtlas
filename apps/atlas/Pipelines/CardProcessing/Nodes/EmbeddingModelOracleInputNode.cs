using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._04_Feature.Schemas;
using MagicAtlas.Data._05_ModelInput.Schemas;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Pipelines.CardProcessing.Nodes;

/// <summary>
/// Flattens refined oracle text into individual entries for embedding model input.
/// Each card produces multiple entries: one per ability (excluding full/raw text).
/// </summary>
public static class CreateEmbeddingModelOracleInputNode
{
  /// <summary>
  /// Creates a function that flattens refined oracle text into embedding model inputs.
  /// </summary>
  /// <returns>
  /// A function that takes refined oracle text and produces flattened entries suitable for embedding.
  /// </returns>
  public static Func<
    IEnumerable<RefinedOracleText>,
    Task<IEnumerable<EmbeddingModelOracleInput>>
  > Create()
  {
    return async (refinedTexts) =>
    {
      var flattened = new List<EmbeddingModelOracleInput>();

      foreach (var card in refinedTexts)
      {
        // Add keyword abilities
        foreach (var ability in card.KeywordAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              TextEntryId = Guid.NewGuid(),
              CardId = card.Id,
              TextType = OracleTextType.KeywordAbility,
              Text = ability.RawText,
            }
          );
        }

        // Add named triggered abilities
        foreach (var ability in card.NamedTriggeredAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              TextEntryId = Guid.NewGuid(),
              CardId = card.Id,
              TextType = OracleTextType.NamedTriggeredAbility,
              Text = ability.RawText,
            }
          );
        }

        // Add triggered abilities
        foreach (var ability in card.TriggeredAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              TextEntryId = Guid.NewGuid(),
              CardId = card.Id,
              TextType = OracleTextType.TriggeredAbility,
              Text = ability.RawText,
            }
          );
        }

        // Add activated abilities
        foreach (var ability in card.ActivatedAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              TextEntryId = Guid.NewGuid(),
              CardId = card.Id,
              TextType = OracleTextType.ActivatedAbility,
              Text = ability.RawText,
            }
          );
        }

        // Add passive abilities
        foreach (var ability in card.PassiveAbilities)
        {
          flattened.Add(
            new EmbeddingModelOracleInput
            {
              TextEntryId = Guid.NewGuid(),
              CardId = card.Id,
              TextType = OracleTextType.PassiveAbility,
              Text = ability.Effect,
            }
          );
        }
      }

      return await Task.FromResult(flattened);
    };
  }
}
