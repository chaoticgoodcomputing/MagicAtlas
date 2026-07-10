namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Damage;

/// <summary>
/// Recognises the global damage-prevention lock static "Damage can't be
/// prevented." (Leyline of Punishment) — a rules-of-the-game continuous effect
/// (CR 611.1) written as a plain static statement (CR 604.1), that nullifies every
/// prevention effect (CR 615.1). Emits a single <see cref="StaticAbility"/>
/// carrying one <see cref="CantPreventDamageEffect"/>.
/// </summary>
[StaticRule(Priority = 971)]
public sealed class CantPreventDamageRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Damage\s+can'?t\s+be\s+prevented\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new CantPreventDamageEffect()],
      },
    ];
  }
}
