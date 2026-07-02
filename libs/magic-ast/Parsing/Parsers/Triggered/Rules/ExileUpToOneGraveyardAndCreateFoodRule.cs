namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "exile up to one target card from a graveyard and create a Food token" —
/// the compound triggered-effect pattern from Hazel's Brewmaster. Produces a
/// <see cref="CompositeEffect"/> of an <see cref="ExileEffect"/> (up to one
/// target graveyard card) followed by a <see cref="CreateTokenEffect"/> (Food).
///
/// <para>
/// CR 701.13a: "To exile an object, move it to the exile zone from wherever it is."
/// CR 111.10b: "A Food token is a colorless Food artifact token with
/// '{2}, {T}, Sacrifice this token: You gain 3 life.'"
/// CR 602 / 603: the exile and token-creation are bundled into a single effect
/// body separated by "and"; both effects resolve together at ability resolution.
/// </para>
///
/// Anchored (^…$) to prevent matching inside a longer "exile…and…" body that
/// carries additional clauses a more-specific rule should handle.
/// </summary>
[TriggeredRule]
public sealed class ExileUpToOneGraveyardAndCreateFoodRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^exile\s+up\s+to\s+one\s+target\s+card\s+from\s+a\s+graveyard\s+and\s+create\s+a\s+Food\s+token$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new CompositeEffect
    {
      Effects = new List<Effect>
      {
        new ExileEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["card"],
              Zone = Zone.Graveyard,
            },
            Quantity = new UpToQuantity { Maximum = 1, Minimum = 0 },
          },
        },
        new CreateTokenEffect
        {
          Player = ObjectReference.You(),
          Count = LiteralQuantity.Of(1),
          Token = TokenDefinition.Food(),
        },
      },
    };
    return true;
  }
}
