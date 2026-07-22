namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Imperative "Deal N damage to target [type] or [type]." — the bare-imperative
/// spell form of a disjunctive-target burn effect (Tear: "Deal 2 damage to target
/// enchantment or creature."). The spell itself is the damage source (CR 120.1: an
/// object that deals damage is the source of that damage), modeled as
/// <see cref="ObjectReferenceKind.Self"/>.
///
/// <para>
/// The disjunction "X or Y" is a single target whose legal-type set is the union
/// {X, Y} — one <see cref="ObjectReferenceKind.Target"/> with a two-element
/// <see cref="ObjectFilter.CardTypes"/>, the same shape
/// <see cref="SelfDealsDamageToTypeDisjunctionRule"/> (Lava Axe: "[Self] deals …")
/// and <see cref="Triggered.Rules.ItDealsDamageToTargetTypeDisjunctionRule"/> ("it
/// deals …") produce. This rule is the missing <b>imperative</b> sibling: no
/// grammatical subject precedes the verb ("Deal …" rather than "[Name] deals …"),
/// which the subject-anchored sibling rules do not match.
/// </para>
///
/// <para>
/// The "or" disjunction is REQUIRED (kept narrow to the two-type imperative form);
/// bare single-type imperative burn is out of scope for this rule.
/// </para>
/// </summary>
[SpellRule(Priority = 60)]
public sealed class DealDamageToTargetTypeDisjunctionImperativeSpellRule : ISpellRule
{
  private const string TypeGroup = "player|creature|artifact|enchantment|land|planeswalker|permanent";

  private static readonly Regex _pattern = new(
    $@"^Deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+target\s+(?<type1>{TypeGroup})\s+or\s+(?<type2>{TypeGroup})\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var amount = SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value);

    var cardTypes = new List<string>
    {
      m.Groups["type1"].Value.ToLowerInvariant(),
      m.Groups["type2"].Value.ToLowerInvariant(),
    };

    effect = new DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = cardTypes },
      },
    };
    return true;
  }
}
