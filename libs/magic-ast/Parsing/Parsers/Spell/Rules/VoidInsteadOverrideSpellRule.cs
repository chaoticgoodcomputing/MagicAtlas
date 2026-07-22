namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// The Edge of Eternities <em>Void</em> trailing conditional-magnitude override —
/// Plasma Bolt ("Plasma Bolt deals 2 damage to any target. Void — Plasma Bolt deals
/// 3 damage instead if &lt;void&gt;.") and Tragic Trajectory ("Target creature gets
/// -2/-2 until end of turn. Void — That creature gets -10/-10 until end of turn
/// instead if &lt;void&gt;.").
///
/// <para>
/// The word "instead" makes the second magnitude a REPLACEMENT of the first, not an
/// addition (CR 207.2c: "void" is an ability word with no rules meaning — the printed
/// event-history disjunction is the whole condition, byte-identical on every Void
/// card). So the two printed oracle lines are ONE spell ability whose damage / P-T
/// magnitude is conditionally overridden, never two independent abilities (which would
/// read additively). The <see cref="ClauseSplitter"/> joins the "Void — … instead if
/// &lt;void&gt;" continuation paragraph onto its base spell line so the whole
/// two-sentence text reaches this rule as a single clause; this rule stitches it into
/// a single <see cref="ConditionalEffect"/>, mirroring the established "instead"
/// idiom (Then = the overridden magnitude, Else = the base magnitude — see
/// <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.CreateTokenOrInsteadIfConditionRule"/>
/// and Emiel the Blessed's counter override).
/// </para>
///
/// <para>
/// The condition is parsed through <see cref="MagicAST.Parsing.ConditionParser"/>,
/// which already recognises the fixed Void disjunction and returns a
/// <see cref="MagicAST.AST.Abilities.VoidCondition"/> marker. Reference-not-resolution
/// (ADR 0004): MAST records the printed conditional override; the engine reads the
/// this-turn game history and applies the correct magnitude.
/// </para>
/// </summary>
[SpellRule(Priority = 95)]
public sealed class VoidInsteadOverrideSpellRule : ISpellRule
{
  private const string VoidDisjunction =
    @"a\s+nonland\s+permanent\s+left\s+the\s+battlefield\s+this\s+turn\s+or\s+a\s+spell\s+was\s+warped\s+this\s+turn";

  // "<subject> deals <base> damage to any target. Void — <subject> deals <override>
  // damage instead if <void disjunction>". Singleline so "." (implicit in \s) and the
  // joining newline between the two sentences are spanned.
  private static readonly Regex DamageOverride = new(
    @"^.+?\s+deals?\s+(?<base>\d+)\s+damage\s+to\s+any\s+target\.\s*Void\s+—\s+.+?\s+deals?\s+(?<over>\d+)\s+damage\s+instead\s+if\s+"
      + VoidDisjunction
      + @"$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
  );

  // "Target creature gets <bP>/<bT> until end of turn. Void — That creature gets
  // <oP>/<oT> until end of turn instead if <void disjunction>".
  private static readonly Regex ModifyPtOverride = new(
    @"^Target\s+creature\s+gets\s+(?<bp>[+-]\d+)/(?<bt>[+-]\d+)\s+until\s+end\s+of\s+turn\.\s*Void\s+—\s+That\s+creature\s+gets\s+(?<op>[+-]\d+)/(?<ot>[+-]\d+)\s+until\s+end\s+of\s+turn\s+instead\s+if\s+"
      + VoidDisjunction
      + @"$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
  );

  // The void disjunction the two regexes strip off with a trailing " instead if …"
  // — fed verbatim to the shared ConditionParser, which returns a VoidCondition.
  private const string VoidConditionPhrase =
    "a nonland permanent left the battlefield this turn or a spell was warped this turn";

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var dm = DamageOverride.Match(text);
    if (dm.Success)
    {
      var condition = ConditionParser.Parse(VoidConditionPhrase);
      DealDamageEffect Deal(int amount) =>
        new()
        {
          Amount = LiteralQuantity.Of(amount),
          Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
          Source = ObjectReference.Self(),
        };

      effect = new ConditionalEffect
      {
        Condition = condition,
        Then = Deal(int.Parse(dm.Groups["over"].Value)),
        Else = Deal(int.Parse(dm.Groups["base"].Value)),
      };
      return true;
    }

    var mm = ModifyPtOverride.Match(text);
    if (mm.Success)
    {
      var condition = ConditionParser.Parse(VoidConditionPhrase);
      // The spell picks a single target ("target creature"); both branches apply to
      // that one target, only the magnitude differs — so both reference the same
      // target creature (the "instead" branch's "that creature" is the same object).
      ModifyPTEffect Mod(int power, int toughness) =>
        new()
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter { CardTypes = ["creature"] },
          },
          PowerModifier = LiteralQuantity.Of(power),
          ToughnessModifier = LiteralQuantity.Of(toughness),
          Duration = UntilTimeDuration.EndOfTurn,
        };

      effect = new ConditionalEffect
      {
        Condition = condition,
        Then = Mod(int.Parse(mm.Groups["op"].Value), int.Parse(mm.Groups["ot"].Value)),
        Else = Mod(int.Parse(mm.Groups["bp"].Value), int.Parse(mm.Groups["bt"].Value)),
      };
      return true;
    }

    return false;
  }
}
