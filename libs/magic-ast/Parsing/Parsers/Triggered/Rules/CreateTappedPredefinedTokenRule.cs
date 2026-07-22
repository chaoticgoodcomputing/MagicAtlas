namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "create a tapped [Subtype] token" — creates a predefined artifact token that enters the
/// battlefield tapped (CR 111.10 — predefined tokens; the creating effect "may also modify
/// or add to the predefined characteristics", which is how the "tapped" entry modifier on
/// the create instruction is reconciled with the predefined token definition).
///
/// Handles the "create a tapped Treasure token" form produced by cards such as Nuka-Cola
/// Vending Machine (PIP), and the "create a tapped Powerstone token" form on the
/// enters-the-battlefield Powerstone family (Argothian Opportunist, Koilos Roc, Horned
/// Stoneseeker, Junkyard Genius — BRO/DMU). The "tapped" qualifier is recorded on the
/// <see cref="TokenDefinition.EntersTapped"/> field so consumers know the token enters
/// already tapped rather than untapped. This is a descriptive datum, not an engine tap
/// action (MAST describes what oracle text says, not engine execution).
///
/// Per the CR glossary, a Powerstone token is a colorless artifact token with
/// "{T}: Add {C}. This mana can't be spent to cast a nonartifact spell." (a predefined
/// token, see CR 111.10). As with every predefined token in this rule, the token's
/// predefined activated-ability body is NOT re-asserted here as free text: it is carried by
/// the parenthetical reminder ("It's an artifact with ...") that the trigger parser strips
/// into the ability's Reminder field and discards from the effect AST (CR 207.2: reminder
/// text has no rules meaning). The token is identified structurally by its artifact type +
/// named subtype, and its intrinsic affordance is resolved downstream from that subtype via
/// PredefinedTokens.Registry. No predefined token re-asserts its ability body as free text
/// (ADR-0004 recursive-body de-string, issue #40): Treasure/Food/Clue/Blood/Powerstone all omit it.
///
/// Priority 96: sits above <see cref="CreateTokenRule"/> (default priority 50) so the
/// tapped variant is matched before the plain vanilla path, which would fail anyway because
/// it doesn't recognise "tapped" between the article and the subtype word.
///
/// CR 111 (Tokens), CR 111.10 (predefined tokens), CR 603.2 (triggered ability event matching).
/// </summary>
[TriggeredRule(Priority = 96)]
public sealed class CreateTappedPredefinedTokenRule : ITriggeredRule
{
  // "create a tapped [Subtype] token" — article + "tapped" + named subtype.
  private static readonly Regex _createTappedPattern = new(
    @"^create\s+a\s+tapped\s+(?<subtype>Treasure|Food|Clue|Blood|Powerstone)\s+token\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out MagicAST.AST.Effects.Effect? effect)
  {
    effect = null;

    var m = _createTappedPattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value;
    // Title-case normalize
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();

    var token = subtype switch
    {
      "Treasure" => TokenDefinition.TappedTreasure(),
      "Food" => new TokenDefinition
      {
        Types = ["artifact"],
        Subtypes = ["Food"],
        EntersTapped = true,
      },
      "Clue" => new TokenDefinition
      {
        Types = ["artifact"],
        Subtypes = ["Clue"],
        EntersTapped = true,
      },
      "Blood" => new TokenDefinition
      {
        Types = ["artifact"],
        Subtypes = ["Blood"],
        EntersTapped = true,
      },
      // Powerstone: a colorless artifact predefined token (CR 111.10). Its predefined
      // ability ("{T}: Add {C}. This mana can't be spent to cast a nonartifact spell.") is
      // carried by the discarded parenthetical reminder, not re-asserted as free text here.
      "Powerstone" => new TokenDefinition
      {
        Types = ["artifact"],
        Subtypes = ["Powerstone"],
        EntersTapped = true,
      },
      _ => null,
    };

    if (token is null)
    {
      return false;
    }

    effect = new CreateTokenEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(1),
      Token = token,
    };
    return true;
  }
}
