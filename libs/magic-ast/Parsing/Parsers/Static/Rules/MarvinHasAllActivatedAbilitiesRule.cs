namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Marvin has all activated abilities of creatures you control that don't have
/// the same name as this creature." — the Marvin, Murderous Mimic continuous
/// static ability that grants all activated abilities from other creatures the
/// controller controls to itself.
///
/// <para>
/// <b>CR 613.1f</b> (layer 6 — ability-adding continuous effects): the static
/// ability continuously grants abilities to Marvin while it is on the battlefield.
/// </para>
///
/// <para>
/// <b>CR 602.1</b>: "An activated ability is the only kind of ability that can
/// be activated." The class of abilities granted is identified as "activated."
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent matching substrings of longer ability lines.
/// The card name prefix ("[Name] has all activated abilities …") is matched by
/// a general pattern so this rule generalises to other mimic-style cards with
/// the same oracle shape. Priority 996 (above generic keyword-grant rules at 967)
/// so this specific shape is claimed before a keyword-grant fallback attempts it.
/// </para>
/// </summary>
[StaticRule(Priority = 996)]
public sealed class MarvinHasAllActivatedAbilitiesRule : IStaticRule
{
  // "[CardName] has all activated abilities of creatures you control that don’t
  //  have the same name as this creature."
  // Matches any card name in the subject slot.
  // The apostrophe character class covers:
  //   - U+0027 (APOSTROPHE, ASCII ‘)
  //   - U+2019 (RIGHT SINGLE QUOTATION MARK, curly ‘) — Scryfall oracle text form
  // Using an interpolated string so we can embed ’ explicitly.
  private static readonly Regex Pattern = new(
    $@"^\s*\S+(?:\s+\S+)*\s+has\s+all\s+activated\s+abilities\s+of\s+creatures\s+you\s+control\s+that\s+don['‘’]t\s+have\s+the\s+same\s+name\s+as\s+this\s+creature\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!Pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    // Subject: Self — Marvin itself (the card bearing this ability).
    var subject = ObjectReference.Self();

    // SourceFilter: creatures you control that don't have the same name as this creature.
    // "that don't have the same name as this creature" is the relational name-EXCLUSION
    // predicate — the first-class ObjectFilter.ExcludesNameOf axis (negation sibling of
    // SharesNameWith), whose referent "this creature" is Marvin itself (Self, CR 109).
    var sourceFilter = new ObjectFilter
    {
      CardTypes = ["creature"],
      Controller = ControllerFilter.You,
      ExcludesNameOf = ObjectReference.Self(),
    };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new HasAllAbilitiesOfControlledCreaturesEffect
          {
            Subject = subject,
            AbilityKind = "activated",
            SourceFilter = sourceFilter,
          },
        ],
      },
    ];
  }
}
