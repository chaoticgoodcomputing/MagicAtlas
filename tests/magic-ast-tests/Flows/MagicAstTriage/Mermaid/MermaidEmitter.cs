using System.Linq;
using System.Text;
using MagicAST;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Mermaid;

/// <summary>
/// Renders <see cref="CardOutputAST"/> instances as Mermaid <c>graph TD</c> diagrams.
/// Per-fixture graphs show discriminator + key scalar values; the aggregate graph shows
/// union AST-node-type paths weighted by fixture count.
/// </summary>
public static class MermaidEmitter
{
    // ──────────────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a single card's AST as a fenced mermaid block (the entire file content).
    /// </summary>
    public static string RenderFixtureGraph(CardOutputAST card)
    {
        var ctx = new WalkerContext();
        WalkCard(card, -1, ctx);
        return WrapInFence("graph TD\n" + ctx.Body);
    }

    /// <summary>
    /// Renders the aggregate node-type path frequency across all cards as a fenced mermaid block.
    /// Edge labels show how many fixtures traverse each (from → to) node-type pair.
    /// </summary>
    public static string RenderAggregateGraph(IEnumerable<CardOutputAST> cards)
    {
        // Accumulate edge frequencies: (fromType, toType) → count
        var edgeCounts = new Dictionary<(string From, string To), int>();

        foreach (var card in cards)
        {
            var pathCtx = new PathContext();
            CollectPaths(card, null, pathCtx, edgeCounts);
        }

        // Build graph
        var sb = new StringBuilder();
        sb.AppendLine("graph TD");

        // Collect all unique node types
        var nodeTypes = new HashSet<string>();
        foreach (var (edge, _) in edgeCounts)
        {
            nodeTypes.Add(edge.From);
            nodeTypes.Add(edge.To);
        }

        // Emit node declarations (just the type name, no IDs needed for aggregate)
        foreach (var t in nodeTypes.OrderBy(x => x))
        {
            var safeId = ToNodeId(t);
            sb.AppendLine($"  {safeId}[{EscapeLabel(t)}]");
        }

        // Emit edges with count labels
        foreach (var (edge, count) in edgeCounts.OrderBy(x => x.Key.From).ThenBy(x => x.Key.To))
        {
            var fromId = ToNodeId(edge.From);
            var toId = ToNodeId(edge.To);
            sb.AppendLine($"  {fromId} --[{count}]--> {toId}");
        }

        return WrapInFence(sb.ToString());
    }

    // ──────────────────────────────────────────────────────────────────────
    // Per-fixture walker
    // ──────────────────────────────────────────────────────────────────────

    private sealed class WalkerContext
    {
        public int Counter;
        public readonly StringBuilder Body = new();

        public int NextId() => Counter++;

        public int EmitNode(string label)
        {
            var id = NextId();
            Body.AppendLine($"  n{id}[{EscapeLabel(label)}]");
            return id;
        }

        public void EmitEdge(int from, int to)
        {
            Body.AppendLine($"  n{from} --> n{to}");
        }
    }

    private static void WalkCard(CardOutputAST card, int parentId, WalkerContext ctx)
    {
        var mainType = card.TypeLine.Types.FirstOrDefault() ?? "?";
        var supertypes = card.TypeLine.Supertypes is { Count: > 0 }
            ? string.Join(" ", card.TypeLine.Supertypes) + " "
            : "";
        var label = $"Card<br/>{card.Name}<br/>{supertypes}{mainType}";
        var id = ctx.EmitNode(label);
        if (parentId >= 0)
            ctx.EmitEdge(parentId, id);

        // Attributes
        foreach (var attr in card.Attributes)
        {
            WalkAttribute(attr, id, ctx);
        }

        // Oracle abilities
        if (card.Oracle.Abilities.Count > 0)
        {
            var oracleId = ctx.EmitNode("Abilities");
            ctx.EmitEdge(id, oracleId);
            foreach (var ability in card.Oracle.Abilities)
            {
                WalkAbility(ability, oracleId, ctx);
            }
        }

        // Multi-faced cards
        if (card.Faces is { Count: > 0 })
        {
            foreach (var face in card.Faces)
            {
                WalkFace(face, id, ctx);
            }
        }
    }

    private static void WalkFace(CardFaceAST face, int parentId, WalkerContext ctx)
    {
        var mainType = face.TypeLine.Types.FirstOrDefault() ?? "?";
        var id = ctx.EmitNode($"Face<br/>{face.Name}<br/>{mainType}");
        ctx.EmitEdge(parentId, id);
        foreach (var attr in face.Attributes)
        {
            WalkAttribute(attr, id, ctx);
        }
        if (face.Oracle.Abilities.Count > 0)
        {
            var oracleId = ctx.EmitNode("Abilities");
            ctx.EmitEdge(id, oracleId);
            foreach (var ability in face.Oracle.Abilities)
            {
                WalkAbility(ability, oracleId, ctx);
            }
        }
    }

    private static void WalkAttribute(CardAttribute attr, int parentId, WalkerContext ctx)
    {
        int id;
        switch (attr)
        {
            case ManaCostAttribute mc:
                var mv = mc.ManaValue.HasValue ? $"<br/>MV: {mc.ManaValue}" : "";
                id = ctx.EmitNode($"ManaCost<br/>{mc.Raw}{mv}");
                ctx.EmitEdge(parentId, id);
                break;

            case CreatureStatsAttribute cs:
                var p = cs.Power is FixedPTValue fp ? fp.Value.ToString() : cs.Power.Raw;
                var t = cs.Toughness is FixedPTValue ft ? ft.Value.ToString() : cs.Toughness.Raw;
                id = ctx.EmitNode($"CreatureStats<br/>{p}/{t}");
                ctx.EmitEdge(parentId, id);
                break;

            case LoyaltyAttribute la:
                id = ctx.EmitNode($"Loyalty<br/>{la.Raw}");
                ctx.EmitEdge(parentId, id);
                break;

            case ColorsAttribute ca:
                if (ca.Colors.Count > 0)
                {
                    id = ctx.EmitNode($"Colors<br/>{string.Join("", ca.Colors)}");
                    ctx.EmitEdge(parentId, id);
                }
                break;

            case DefenseAttribute da:
                id = ctx.EmitNode($"Defense<br/>{da.Defense}");
                ctx.EmitEdge(parentId, id);
                break;

            // Skip ColorIdentity, AdditionalCosts, AlternativeCosts, CostReductions, Layout unless notable
            default:
                break;
        }
    }

    private static void WalkAbility(Ability ability, int parentId, WalkerContext ctx)
    {
        int id;
        switch (ability)
        {
            case SpellAbility sa:
                id = ctx.EmitNode("SpellAbility");
                ctx.EmitEdge(parentId, id);
                foreach (var effect in sa.Effects)
                    WalkEffect(effect, id, ctx);
                break;

            case TriggeredAbility ta:
                var trigLabel = FormatTrigger(ta.Trigger);
                id = ctx.EmitNode($"TriggeredAbility<br/>{trigLabel}");
                ctx.EmitEdge(parentId, id);
                foreach (var effect in ta.Effects)
                    WalkEffect(effect, id, ctx);
                break;

            case ActivatedAbility aa:
                id = ctx.EmitNode("ActivatedAbility");
                ctx.EmitEdge(parentId, id);
                foreach (var effect in aa.Effects)
                    WalkEffect(effect, id, ctx);
                break;

            case StaticAbility sta:
                id = ctx.EmitNode("StaticAbility");
                ctx.EmitEdge(parentId, id);
                foreach (var effect in sta.Effects)
                    WalkEffect(effect, id, ctx);
                break;

            case ModalAbility ma:
                id = ctx.EmitNode($"ModalAbility<br/>Choose {ma.ModeSelection.Minimum}-{ma.ModeSelection.Maximum}");
                ctx.EmitEdge(parentId, id);
                foreach (var mode in ma.Modes)
                    WalkAbility(mode.Ability, id, ctx);
                break;

            case UnparsedAbility ua:
                id = ctx.EmitNode("UnparsedAbility");
                ctx.EmitEdge(parentId, id);
                break;

            default:
                id = ctx.EmitNode(ability.GetType().Name);
                ctx.EmitEdge(parentId, id);
                break;
        }
    }

    private static void WalkEffect(Effect effect, int parentId, WalkerContext ctx)
    {
        int id;
        switch (effect)
        {
            // Zone change
            case DestroyEffect de:
                id = ctx.EmitNode("DestroyEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(de.Target, id, ctx);
                break;

            case ExileEffect ee:
                id = ctx.EmitNode("ExileEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(ee.Target, id, ctx);
                break;

            case ReturnToHandEffect rth:
                id = ctx.EmitNode("ReturnToHandEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(rth.Target, id, ctx);
                break;

            case ReturnToBattlefieldEffect rtb:
                id = ctx.EmitNode("ReturnToBattlefieldEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(rtb.Target, id, ctx);
                break;

            case SacrificeEffect sac:
                id = ctx.EmitNode("SacrificeEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(sac.Target, id, ctx);
                break;

            case MillEffect mill:
                id = ctx.EmitNode("MillEffect");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(mill.Count, id, ctx);
                break;

            case ShuffleEffect:
                id = ctx.EmitNode("ShuffleEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case SearchLibraryEffect sl:
                id = ctx.EmitNode("SearchLibraryEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Card flow
            case DrawCardsEffect dc:
                id = ctx.EmitNode("DrawCardsEffect");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(dc.Count, id, ctx);
                break;

            case DiscardCardsEffect disc:
                id = ctx.EmitNode("DiscardCardsEffect");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(disc.Count, id, ctx);
                break;

            case SurveilEffect surv:
                id = ctx.EmitNode("SurveilEffect");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(surv.Count, id, ctx);
                break;

            case ScryEffect scry:
                id = ctx.EmitNode("ScryEffect");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(scry.Count, id, ctx);
                break;

            case LookAtCardsEffect:
                id = ctx.EmitNode("LookAtCardsEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Damage
            case DealDamageEffect dd:
                id = ctx.EmitNode("DealDamageEffect");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(dd.Amount, id, ctx);
                WalkObjectReference(dd.Target, id, ctx);
                break;

            case PreventDamageEffect:
                id = ctx.EmitNode("PreventDamageEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case LifelinkEffect:
                id = ctx.EmitNode("LifelinkEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Resource
            case GainLifeEffect gl:
                id = ctx.EmitNode("GainLifeEffect");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(gl.Amount, id, ctx);
                break;

            case LoseLifeEffect ll:
                id = ctx.EmitNode("LoseLifeEffect");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(ll.Amount, id, ctx);
                break;

            case AddManaEffect am:
                id = ctx.EmitNode("AddManaEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case CostReductionEffect cre:
                id = ctx.EmitNode("CostReductionEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Counter
            case PutCountersEffect pc:
                id = ctx.EmitNode($"PutCountersEffect<br/>{pc.CounterType}");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(pc.Count, id, ctx);
                break;

            case RemoveCountersEffect rc:
                id = ctx.EmitNode($"RemoveCountersEffect<br/>{rc.CounterType}");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(rc.Count, id, ctx);
                break;

            // Token/Copy
            case CreateTokenEffect ct:
                var tokenLabel = FormatTokenLabel(ct.Token);
                id = ctx.EmitNode($"CreateTokenEffect<br/>{tokenLabel}");
                ctx.EmitEdge(parentId, id);
                WalkQuantity(ct.Count, id, ctx);
                break;

            case CopyEffect:
                id = ctx.EmitNode("CopyEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case CreateEmblemEffect:
                id = ctx.EmitNode("CreateEmblemEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Modification
            case ModifyPTEffect mpt:
                id = ctx.EmitNode("ModifyPTEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(mpt.Target, id, ctx);
                WalkQuantity(mpt.PowerModifier, id, ctx);
                WalkQuantity(mpt.ToughnessModifier, id, ctx);
                break;

            case GainAbilityEffect ga:
                id = ctx.EmitNode("GainAbilityEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(ga.Target, id, ctx);
                break;

            case LoseAbilityEffect la:
                id = ctx.EmitNode("LoseAbilityEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case GainControlEffect gc:
                id = ctx.EmitNode("GainControlEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(gc.Target, id, ctx);
                break;

            case ExchangeCharacteristicEffect:
                id = ctx.EmitNode("ExchangeCharacteristicEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Control
            case TapEffect tap:
                id = ctx.EmitNode("TapEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(tap.Target, id, ctx);
                break;

            case UntapEffect ut:
                id = ctx.EmitNode("UntapEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectReference(ut.Target, id, ctx);
                break;

            case DoesntUntapEffect dut:
                id = ctx.EmitNode("DoesntUntapEffect");
                ctx.EmitEdge(parentId, id);
                if (dut.Target != null)
                    WalkObjectReference(dut.Target, id, ctx);
                break;

            case CounterSpellEffect cs:
                id = ctx.EmitNode("CounterSpellEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Keyword
            case EvasionEffect ev:
                id = ctx.EmitNode("EvasionEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case ReachEffect:
                id = ctx.EmitNode("ReachEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case VigilanceEffect:
                id = ctx.EmitNode("VigilanceEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case TrampleEffect:
                id = ctx.EmitNode("TrampleEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case HasteEffect:
                id = ctx.EmitNode("HasteEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case ProtectionEffect pe:
                id = ctx.EmitNode("ProtectionEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case CantBeCounteredEffect:
                id = ctx.EmitNode("CantBeCounteredEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case PartnerEffect part:
                id = ctx.EmitNode($"PartnerEffect<br/>{part.PartnerType}");
                ctx.EmitEdge(parentId, id);
                break;

            // Combat
            case CombatDamageTimingEffect cdt:
                id = ctx.EmitNode($"CombatDamageTimingEffect<br/>{cdt.Timing}");
                ctx.EmitEdge(parentId, id);
                break;

            case TargetingRestrictionEffect tre:
                id = ctx.EmitNode("TargetingRestrictionEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case EnchantRestrictionEffect ere:
                id = ctx.EmitNode("EnchantRestrictionEffect");
                ctx.EmitEdge(parentId, id);
                WalkObjectFilter(ere.LegalTargets, id, ctx);
                break;

            // Timing
            case TimingModificationEffect tme:
                id = ctx.EmitNode($"TimingModificationEffect<br/>{tme.Modification}");
                ctx.EmitEdge(parentId, id);
                break;

            case CastWithoutPayingEffect:
                id = ctx.EmitNode("CastWithoutPayingEffect");
                ctx.EmitEdge(parentId, id);
                break;

            case CommanderDesignationEffect:
                id = ctx.EmitNode("CommanderDesignationEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Replacement
            case ReplacementEffect rep:
                id = ctx.EmitNode("ReplacementEffect");
                ctx.EmitEdge(parentId, id);
                break;

            // Core
            case CompositeEffect comp:
                id = ctx.EmitNode("CompositeEffect");
                ctx.EmitEdge(parentId, id);
                foreach (var sub in comp.Effects)
                    WalkEffect(sub, id, ctx);
                break;

            case UnparsedEffect:
                id = ctx.EmitNode("UnparsedEffect");
                ctx.EmitEdge(parentId, id);
                break;

            default:
                id = ctx.EmitNode(effect.GetType().Name);
                ctx.EmitEdge(parentId, id);
                break;
        }

        // Walk duration if present (Duration lives on IDurativeEffect trait, not base Effect)
        if (effect is IDurativeEffect dur && dur.Duration != null)
            WalkDuration(dur.Duration, id, ctx);
    }

    private static void WalkObjectReference(ObjectReference objRef, int parentId, WalkerContext ctx)
    {
        var id = ctx.EmitNode($"ObjectReference<br/>Kind: {objRef.Kind}");
        ctx.EmitEdge(parentId, id);

        if (objRef.Filter != null)
            WalkObjectFilter(objRef.Filter, id, ctx);
    }

    private static void WalkObjectFilter(ObjectFilter filter, int parentId, WalkerContext ctx)
    {
        var summary = SummarizeFilter(filter);
        if (string.IsNullOrEmpty(summary))
            return;
        var id = ctx.EmitNode($"ObjectFilter<br/>{summary}");
        ctx.EmitEdge(parentId, id);
    }

    private static void WalkQuantity(Quantity qty, int parentId, WalkerContext ctx)
    {
        int id;
        switch (qty)
        {
            case LiteralQuantity lq:
                id = ctx.EmitNode($"LiteralQuantity<br/>{lq.Value}");
                ctx.EmitEdge(parentId, id);
                break;
            case VariableQuantity vq:
                id = ctx.EmitNode($"VariableQuantity<br/>{vq.Name}");
                ctx.EmitEdge(parentId, id);
                break;
            case DerivedQuantity dq:
                id = ctx.EmitNode($"DerivedQuantity<br/>{dq.DerivedFrom}");
                ctx.EmitEdge(parentId, id);
                break;
            case CountQuantity cq:
                id = ctx.EmitNode($"CountQuantity<br/>{cq.CountOf}");
                ctx.EmitEdge(parentId, id);
                break;
            case UpToQuantity uq:
                id = ctx.EmitNode($"UpToQuantity<br/>max:{uq.Maximum}");
                ctx.EmitEdge(parentId, id);
                break;
            case CalculatedQuantity calq:
                id = ctx.EmitNode($"CalculatedQuantity<br/>{calq.Expression}");
                ctx.EmitEdge(parentId, id);
                break;
            default:
                id = ctx.EmitNode(qty.GetType().Name);
                ctx.EmitEdge(parentId, id);
                break;
        }
    }

    private static void WalkDuration(Duration duration, int parentId, WalkerContext ctx)
    {
        int id;
        switch (duration)
        {
            case UntilTimeDuration ut:
                id = ctx.EmitNode($"Until<br/>{ut.Until.Part} {ut.Until.Edge}");
                ctx.EmitEdge(parentId, id);
                break;
            case AsLongAsDuration al:
                id = ctx.EmitNode($"AsLongAs<br/>{Truncate(al.Condition is MagicAST.AST.Abilities.OtherCondition alOc ? alOc.Text : al.Condition.GetType().Name, 30)}");
                ctx.EmitEdge(parentId, id);
                break;
            case PermanentDuration:
                id = ctx.EmitNode("Permanent");
                ctx.EmitEdge(parentId, id);
                break;
            case UntilLeavesBattlefieldDuration ulb:
                var obj = ulb.Object != null ? $"<br/>{Truncate(ulb.Object, 30)}" : "";
                id = ctx.EmitNode($"UntilLeavesField{obj}");
                ctx.EmitEdge(parentId, id);
                break;
            default:
                id = ctx.EmitNode(duration.GetType().Name);
                ctx.EmitEdge(parentId, id);
                break;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    // Aggregate path collector
    // ──────────────────────────────────────────────────────────────────────

    private sealed class PathContext
    {
        // Intentionally empty — used as marker to distinguish the aggregate walk from the fixture walk
    }

    private static void CollectPaths(
        CardOutputAST card,
        string? parentType,
        PathContext ctx,
        Dictionary<(string, string), int> edgeCounts
    )
    {
        const string cardType = "Card";
        AddEdge(parentType, cardType, edgeCounts);

        foreach (var attr in card.Attributes)
            CollectAttrPaths(attr, cardType, ctx, edgeCounts);

        if (card.Oracle.Abilities.Count > 0)
        {
            AddEdge(cardType, "Abilities", edgeCounts);
            foreach (var ability in card.Oracle.Abilities)
                CollectAbilityPaths(ability, "Abilities", ctx, edgeCounts);
        }
    }

    private static void CollectAttrPaths(
        CardAttribute attr,
        string parent,
        PathContext ctx,
        Dictionary<(string, string), int> edgeCounts
    )
    {
        var typeName = attr.GetType().Name;
        AddEdge(parent, typeName, edgeCounts);
    }

    private static void CollectAbilityPaths(
        Ability ability,
        string parent,
        PathContext ctx,
        Dictionary<(string, string), int> edgeCounts
    )
    {
        var typeName = ability.GetType().Name;
        AddEdge(parent, typeName, edgeCounts);

        switch (ability)
        {
            case SpellAbility sa:
                foreach (var e in sa.Effects)
                    CollectEffectPaths(e, typeName, ctx, edgeCounts);
                break;
            case TriggeredAbility ta:
                AddEdge(typeName, "TriggerCondition", edgeCounts);
                foreach (var e in ta.Effects)
                    CollectEffectPaths(e, typeName, ctx, edgeCounts);
                break;
            case ActivatedAbility aa:
                foreach (var e in aa.Effects)
                    CollectEffectPaths(e, typeName, ctx, edgeCounts);
                break;
            case StaticAbility sta:
                foreach (var e in sta.Effects)
                    CollectEffectPaths(e, typeName, ctx, edgeCounts);
                break;
            case ModalAbility ma:
                foreach (var mode in ma.Modes)
                    CollectAbilityPaths(mode.Ability, typeName, ctx, edgeCounts);
                break;
        }
    }

    private static void CollectEffectPaths(
        Effect effect,
        string parent,
        PathContext ctx,
        Dictionary<(string, string), int> edgeCounts
    )
    {
        var typeName = effect.GetType().Name;
        AddEdge(parent, typeName, edgeCounts);

        // Walk child effects for composite
        if (effect is CompositeEffect comp)
        {
            foreach (var sub in comp.Effects)
                CollectEffectPaths(sub, typeName, ctx, edgeCounts);
        }

        if (effect is IDurativeEffect dur && dur.Duration != null)
            AddEdge(typeName, dur.Duration.GetType().Name, edgeCounts);
    }

    private static void AddEdge(
        string? from,
        string to,
        Dictionary<(string, string), int> edgeCounts
    )
    {
        if (from == null)
            return;
        var key = (from, to);
        edgeCounts[key] = edgeCounts.GetValueOrDefault(key) + 1;
    }

    // ──────────────────────────────────────────────────────────────────────
    // Label helpers
    // ──────────────────────────────────────────────────────────────────────

    private static string FormatTrigger(TriggerCondition trigger)
    {
        return $"{trigger.Timing}<br/>{trigger.Event}";
    }

    private static string FormatTokenLabel(TokenDefinition token)
    {
        var parts = new List<string>();
        if (token.Power != null && token.Toughness != null)
            parts.Add($"{token.Power}/{token.Toughness}");
        if (token.Colors is { Count: > 0 })
            parts.Add(string.Join("", token.Colors));
        if (token.Types is { Count: > 0 })
            parts.Add(string.Join(" ", token.Types));
        if (token.Subtypes is { Count: > 0 })
            parts.Add(string.Join(" ", token.Subtypes));
        return parts.Count > 0 ? string.Join(" ", parts) : "Token";
    }

    private static string SummarizeFilter(ObjectFilter filter)
    {
        var parts = new List<string>();
        if (filter.CardTypes is { Count: > 0 })
            parts.Add($"CardTypes: [{string.Join(",", filter.CardTypes)}]");
        if (filter.Subtypes is { Count: > 0 })
            parts.Add($"Subtypes: [{string.Join(",", filter.Subtypes)}]");
        if (filter.Supertypes is { Count: > 0 })
            parts.Add($"Supertypes: [{string.Join(",", filter.Supertypes)}]");
        if (filter.Characteristics is { Count: > 0 })
            parts.Add($"Char: [{string.Join(",", filter.Characteristics.Select(c => c switch { KeywordCharacteristic k => k.Keyword.ToString(), OtherCharacteristic o => o.Description, _ => c.ToString() }))}]");
        if (filter.Controller.HasValue)
            parts.Add($"Ctrl: {filter.Controller}");
        return string.Join(" ", parts);
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "...";

    // ──────────────────────────────────────────────────────────────────────
    // Mermaid encoding helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Escapes a node label for Mermaid. Mermaid uses [...] for labels; forbidden characters
    /// inside that are: `[`, `]`, `"`. The label may contain <c>&lt;br/&gt;</c> separators
    /// which are preserved — we split on them, escape each content part individually, and rejoin.
    /// </summary>
    private static string EscapeLabel(string label)
    {
        const string sep = "<br/>";
        if (!label.Contains(sep))
            return EscapeLabelPart(label);

        var parts = label.Split(sep);
        return string.Join(sep, parts.Select(EscapeLabelPart));
    }

    private static string EscapeLabelPart(string part) =>
        part.Replace("[", "(")
            .Replace("]", ")")
            .Replace("\"", "'")
            .Replace("<", "(")
            .Replace(">", ")")
            .Replace("{", "(")
            .Replace("}", ")")
            .Replace("|", "I")
            .Replace("#", "");

    /// <summary>
    /// Converts an AST type name to a safe Mermaid node ID (alphanumeric + underscore only).
    /// </summary>
    private static string ToNodeId(string typeName)
    {
        return typeName.Replace(" ", "_").Replace("-", "_");
    }

    private static string WrapInFence(string body)
    {
        return $"```mermaid\n{body.TrimEnd()}\n```\n";
    }
}
