namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.References;

/// <summary>
/// Handles the Ulalek, Fused Atrocity triggered-ability effect:
/// "you may pay {COST}. If you do, copy all spells you control, then copy all
/// other activated and triggered abilities you control. You may choose new targets
/// for the copies."
///
/// <para>
/// The effect decomposes as:
/// <list type="bullet">
///   <item><see cref="OptionalEffect"/> (the "you may" wrapper)</item>
///   <item>
///     <see cref="ConditionalPayEffect"/> as the <c>Inner</c> — the mana cost the
///     controller may optionally pay.
///   </item>
///   <item>
///     <see cref="CompositeEffect"/> as the <c>IfYouDo</c> — two
///     <see cref="CopyEffect"/> nodes with <c>MayChooseNewTargets = true</c>:
///     <list type="number">
///       <item>"copy all spells you control" →
///         <see cref="ObjectReferenceKind.Each"/> + filter
///         <c>CardTypes=["spell"], Controller=You, Zone=Stack</c></item>
///       <item>"copy all other activated and triggered abilities you control" →
///         <see cref="ObjectReferenceKind.Each"/> + filter
///         <c>CardTypes=["activatedAbility","triggeredAbility"], Controller=You, Zone=Stack,
///         ExcludeSelf=true</c> — the word "other" excludes the source ability itself
///         (this triggered ability of Ulalek), CR 109.5.</item>
///     </list>
///     Both copies carry <c>MayChooseNewTargets = true</c> because the
///     retarget grant in "You may choose new targets for the copies" applies to all
///     copies produced by this ability. CR 707.10.
///   </item>
/// </list>
/// </para>
///
/// <para>Priority 90: must run BEFORE <see cref="ConditionalPayTriggeredRule"/>
/// (priority 80), whose <c>TryParseIfYouDoEffect</c> cannot handle a composite
/// copy-all consequent and would reject the full text, leaving the ability
/// unparsed. This rule takes priority because it is more specific.</para>
///
/// <para>CR 707.10: "To copy a spell, activated ability, or triggered ability
/// means to put a copy of it onto the stack." The copies are placed onto the
/// stack; the controller then may choose new targets for each copy independently
/// (CR 707.10b / CR 117.7).</para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class CopyAllSpellsAndAbilitiesTriggeredRule : ITriggeredRule
{
  // Full effect text (trailing period stripped by dispatcher):
  // "you may pay {C}{C}. If you do, copy all spells you control, then copy all
  //  other activated and triggered abilities you control. You may choose new
  //  targets for the copies"
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\."
    + @"\s*If\s+you\s+do,\s*copy\s+all\s+spells\s+you\s+control,"
    + @"\s*then\s+copy\s+all\s+other\s+activated\s+and\s+triggered\s+abilities\s+you\s+control\."
    + @"\s*You\s+may\s+choose\s+new\s+targets\s+for\s+the\s+copies\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var costStr = m.Groups["cost"].Value;
    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(costStr);
    if (manaCost is null)
    {
      return false;
    }

    var copySpells = new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["spell"],
          Controller = ControllerFilter.You,
          Zone = Zone.Stack,
        },
      },
      MayChooseNewTargets = true,
    };

    var copyAbilities = new CopyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["activatedAbility", "triggeredAbility"],
          Controller = ControllerFilter.You,
          Zone = Zone.Stack,
          // "all OTHER ... abilities" — exclude the source ability itself (CR 109.5).
          ExcludeSelf = true,
        },
      },
      MayChooseNewTargets = true,
    };

    var ifYouDo = new CompositeEffect
    {
      Effects = [copySpells, copyAbilities],
    };

    effect = new OptionalEffect
    {
      Inner = new ConditionalPayEffect { Cost = manaCost },
      IfYouDo = ifYouDo,
    };
    return true;
  }
}
