namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice a [type] for each 1 life you lost" — a sacrifice effect whose count
/// scales with the life just lost by the triggering player (Lich's Tomb: "Whenever
/// you lose life, sacrifice a permanent for each 1 life you lost.").
///
/// <para>
/// CR 119.3: "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly." The "for each 1 life you lost"
/// scaling is self-contained (unlike the bare "that many"/"that much" antecedent
/// forms, which need the enclosing trigger's event to disambiguate — see
/// <see cref="DrawThatManyCardsLifeLostRule"/>'s remarks), so this rule is safe to
/// reflection-discover into the generic effect-rule pool: the explicit "life you
/// lost" phrase names its own antecedent regardless of the enclosing trigger.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificePerLifeLostRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^sacrifice\s+(?:a|an)\s+(?<type>[a-zA-Z]+?)s?\s+for\s+each\s+1\s+life\s+you\s+lost\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var cardType = m.Groups["type"].Value.ToLowerInvariant() switch
    {
      "permanent" => "permanent",
      "creature" => "creature",
      "artifact" => "artifact",
      "enchantment" => "enchantment",
      "planeswalker" => "planeswalker",
      "land" => "land",
      _ => null,
    };

    if (cardType is null)
    {
      return false;
    }

    effect = new SacrificeEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.You,
        Filter = new ObjectFilter { CardTypes = [cardType] },
      },
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.LifeLost },
    };
    return true;
  }
}
