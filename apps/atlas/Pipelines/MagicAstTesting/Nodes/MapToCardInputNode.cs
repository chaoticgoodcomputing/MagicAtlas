using MagicAST;
using MagicAtlas.Data._03_Primary.Schemas;

namespace MagicAtlas.Pipelines.MagicAstTesting.Nodes;

/// <summary>
/// Pipeline node that maps CardCoreData to CardInputDTO for MagicAST processing.
/// Extracts only the fields relevant for AST generation.
/// </summary>
public static class MapToCardInputNode
{
  /// <summary>
  /// Creates a transform function that maps CardCoreData to CardInputDTO.
  /// </summary>
  public static Func<IEnumerable<CardCoreData>, Task<IEnumerable<CardInputDTO>>> Create()
  {
    return async (coreDataItems) =>
    {
      var inputs = coreDataItems.Select(MapToCardInput).ToList();
      return await Task.FromResult(inputs);
    };
  }

  /// <summary>
  /// Maps a single CardCoreData to CardInputDTO.
  /// </summary>
  private static CardInputDTO MapToCardInput(CardCoreData coreData)
  {
    return new CardInputDTO
    {
      Name = coreData.Name,
      ManaCost = coreData.ManaCost,
      TypeLine = ReconstructTypeLine(coreData),
      OracleText = coreData.OracleText,
      Power = coreData.Power,
      Toughness = coreData.Toughness,
      Loyalty = coreData.Loyalty,
    };
  }

  /// <summary>
  /// Reconstructs the type line string from Types and Subtypes.
  /// </summary>
  /// <remarks>
  /// Uses em dash (—) as separator to match standard MTG formatting.
  /// Handles cases where subtypes may be empty.
  /// </remarks>
  private static string ReconstructTypeLine(CardCoreData coreData)
  {
    var mainTypes = string.Join(" ", coreData.Types);

    if (coreData.Subtypes.Count > 0)
    {
      var subtypes = string.Join(" ", coreData.Subtypes);
      return $"{mainTypes} — {subtypes}";
    }

    return mainTypes;
  }
}
