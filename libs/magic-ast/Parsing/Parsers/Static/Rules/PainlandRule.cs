namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 961)]
public sealed class PainlandRule : IStaticRule
{
  private static readonly Regex _painlandPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+you\s+may\s+pay\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\.\s+If\s+you\s+don'?t,\s+it\s+enters\s+tapped\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _painlandPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amountText = match.Groups["amount"].Value.ToLowerInvariant();
    if (!StaticRuleHelpers.TryParseSmallCount(amountText, out var amount))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects = [MagicAST.AST.Effects.Core.EffectWrap.Optional(new MagicAST.AST.Effects.Keyword.PayLifeEffect {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount)}, true, ifYouDoNot: new MagicAST.AST.Effects.Control.TapEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          })],
      },
    ];
  }
}
