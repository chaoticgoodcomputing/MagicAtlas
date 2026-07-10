namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// The same-name-count anthem — "Each nontoken creature you control gets +N/+M
/// for each other creature you control with the same name as that creature."
/// (Mirror Box). A per-object continuous effect: every nontoken creature the
/// controller controls is buffed by +N/+M multiplied by the number of OTHER
/// creatures they control that share its name.
///
/// <para>
/// Emits ONE <see cref="StaticAbility"/> with a single <see cref="ModifyPTEffect"/>:
/// <list type="bullet">
/// <item><c>Target</c> = <c>Kind=Each, Filter={CardTypes:["creature"],
/// IsToken:false, Controller:You}</c> — "each nontoken creature you control"
/// (the token predicate on the first-class <see cref="ObjectFilter.IsToken"/>
/// boolean, CR 111).</item>
/// <item><c>PowerModifier</c> / <c>ToughnessModifier</c> = a
/// <see cref="CountQuantity"/> over "other creature you control with the same name
/// as that creature" — <c>CountOf={CardTypes:["creature"], Controller:You,
/// ExcludeSelf:true, SharesNameWith:{Kind:It}}</c>. The per-item increment of +1
/// is carried by the count itself (each counted object contributes one), mirroring
/// <see cref="EnchantedPTForEachRule"/>. "the same name as that creature" is the
/// structured relational <see cref="ObjectFilter.SharesNameWith"/> axis (CR 201.2 —
/// same-name comparison), whose referent "that creature" is the anthem's per-object
/// subject, an anaphoric back-reference mapped to <c>{Kind:It}</c> (Rule 109.2 — a
/// pronoun to a previously-mentioned object). "other" (CR 109.5) is the
/// <see cref="ObjectFilter.ExcludeSelf"/> self-exclusion.</item>
/// </list>
/// </para>
///
/// <para>
/// CR 611.3 (a static ability generates a continuous effect); CR 613.4c (layer 7c,
/// P/T-modifying continuous effects). ANCHORED (^…$) on the exact surface so it
/// claims only this clause. Only the +1-per-item increment is handled (higher
/// per-item multipliers are a distinct shape); the rule declines otherwise so a
/// mismatched buff is never emitted.
/// </para>
/// </summary>
[StaticRule(Priority = 974)]
public sealed class EachNontokenCreatureSameNameCountAnthemRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Each\s+nontoken\s+creature\s+you\s+control\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+other\s+creature\s+you\s+control\s+with\s+the\s+same\s+name\s+as\s+that\s+creature\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = int.Parse(match.Groups["psign"].Value + match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["tsign"].Value + match.Groups["t"].Value);

    // Only the unit (+1) per-item increment is representable by a bare CountQuantity.
    if (Math.Abs(power) > 1 || Math.Abs(toughness) > 1)
    {
      return null;
    }

    var sameNameCount = new CountQuantity
    {
      CountOf = new ObjectFilter
      {
        CardTypes = ["creature"],
        SharesNameWith = new ObjectReference { Kind = ObjectReferenceKind.It },
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
      },
    };

    Quantity powerModifier = power == 0 ? LiteralQuantity.Of(0) : sameNameCount;
    Quantity toughnessModifier = toughness == 0 ? LiteralQuantity.Of(0) : sameNameCount;

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new ModifyPTEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                IsToken = false,
                Controller = ControllerFilter.You,
              },
            },
            PowerModifier = powerModifier,
            ToughnessModifier = toughnessModifier,
          },
        ],
      },
    ];
  }
}
