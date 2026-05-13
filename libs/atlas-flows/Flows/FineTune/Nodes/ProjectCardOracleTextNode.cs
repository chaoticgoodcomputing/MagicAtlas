using MagicAtlas.Data._03_Primary.Schemas;
using Flowthru.Step;

namespace MagicAtlas.Flows.FineTune.Nodes;

/// <summary>
/// Projects the commander-legal <see cref="CardCoreData"/> set down to the three scalar fields
/// the training-pair builder needs (card_id, name, full oracle text with parentheticals).
/// Cards with no oracle text or null oracle text are dropped — they contribute nothing to the
/// training corpus.
/// </summary>
[FlowthruStep]
public static class ProjectCardOracleTextNode
{
  public static Func<IEnumerable<CardCoreData>, Task<IEnumerable<CardOracleText>>> Create() =>
    cards => Task.FromResult<IEnumerable<CardOracleText>>(
      cards
        .Where(c => !string.IsNullOrWhiteSpace(c.OracleText))
        .Select(c => new CardOracleText
        {
          CardId = c.Id,
          Name = c.Name,
          OracleText = c.OracleText!,
        })
        .ToList()
    );
}
