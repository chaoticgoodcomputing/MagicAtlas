namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a "Reconfigure [cost]" oracle line into two <see cref="ActivatedAbility"/>
/// nodes, one per ability defined by CR 702.151a.
///
/// <para>
/// CR 702.151a (verbatim): "Reconfigure represents two activated abilities.
/// Reconfigure [cost] means "[Cost]: Attach this permanent to another target
/// creature you control. Activate only as a sorcery" and "[Cost]: Unattach
/// this permanent. Activate only if this permanent is attached to a creature
/// and only as a sorcery."
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000)
/// so the two-ability decomposition takes precedence over the single-ability
/// keyword combinator path. The activation condition "only if this permanent is
/// attached to a creature" is a runtime-state guard with no existing structured
/// node; it is omitted per the descriptive-not-engine doctrine.
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class ReconfigureStaticRule : IStaticRule
{
  // Matches: "Reconfigure {cost}" with optional trailing reminder text.
  // The cost group captures one or more mana symbols, e.g. "{R}" or "{4}".
  private static readonly Regex _pattern = new(
    @"^\s*Reconfigure\s+(?<cost>(?:\{[^}]+\})+)\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var costStr = match.Groups["cost"].Value;
    ManaCost cost;
    try
    {
      var parsed = new ManaCostParser().Parse(costStr);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      cost = new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // Ability 1 (CR 702.151a, first clause):
    // "[Cost]: Attach this permanent to another target creature you control.
    //  Activate only as a sorcery."
    var attachAbility = new ActivatedAbility
    {
      KeywordSource = "Reconfigure",
      Costs = [cost],
      Effects =
      [
        new AttachEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.You,
            },
          },
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
      Reminder = reminder,
    };

    // Ability 2 (CR 702.151a, second clause):
    // "[Cost]: Unattach this permanent. Activate only as a sorcery."
    // The activation guard "only if this permanent is attached to a creature"
    // has no existing structured node and is omitted (no free-text, per contract).
    var unattachAbility = new ActivatedAbility
    {
      KeywordSource = "Reconfigure",
      Costs = [cost],
      Effects = [new UnattachEffect()],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
    };

    return [attachAbility, unattachAbility];
  }
}
