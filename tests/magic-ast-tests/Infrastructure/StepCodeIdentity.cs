using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using Flowthru.Step;

namespace MagicAtlas.Ast.Tests.Infrastructure;

/// <summary>
/// Makes Flowthru's step cache key aware of the code that actually performs the
/// transform — not just the code that <em>declares</em> it (ADR 0004, issue #22).
/// </summary>
/// <remarks>
/// <para>
/// <strong>The hole.</strong> Flowthru composes a step's cache fingerprint from
/// <c>IStepNode.CodeVersion</c> + the step's input fingerprints
/// (<c>CachePlanBuilder.ComposeStepFingerprint</c>). <c>CodeVersion</c> is emitted by
/// <c>StepMetadataGenerator</c> as a SHA-256 prefix over <em>the step class's own
/// normalized source text</em>, and the framework documents that scope explicitly:
/// "The hash covers the step class's own source text only. Cross-assembly type-symbol
/// changes … are not reflected."
/// </para>
/// <para>
/// For MagicAtlas that scope is exactly wrong. <c>ParseCorpusStep</c> is nine lines of
/// wiring around <c>new OracleParser()</c>; the transform lives in <c>MagicAST</c>, a
/// different assembly. Every parser rule this repo lands leaves <c>ParseCorpusStep</c>'s
/// source byte-identical, so its <c>CodeVersion</c> is unchanged, so the cache plan calls
/// the step FRESH and serves the previous <c>parse-records.json</c>. That is the
/// documented "rm parse-records + <c>--only ParseCorpus</c>" force-re-parse recipe: a
/// manual workaround for a code-blind key.
/// </para>
/// <para>
/// <strong>The fix.</strong> At startup every <c>[FlowthruStep]</c> class is
/// re-registered in <see cref="StepMetadataRegistry"/> under
/// <c>{generatedCodeVersion}+{closureDigest}</c>, where the closure digest covers the
/// first-party code the step actually reaches:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <strong>Same-assembly reach</strong> is hashed at method-body granularity: the IL
///     of every method transitively reachable from the step class (including its
///     compiler-generated lambda companions). This catches helper edits — e.g.
///     <c>LossyParseAnalyzer</c>, which <c>ParseCorpusStep</c> calls but whose source the
///     generated <c>CodeVersion</c> does not cover.
///   </description></item>
///   <item><description>
///     <strong>Cross-assembly reach</strong> is hashed at module granularity: the MVID of
///     each first-party assembly (<c>MagicAST*</c> / <c>MagicAtlas*</c>) the walk reaches,
///     plus the first-party assemblies those transitively reference. The walk stops at the
///     assembly boundary — descending into <c>MagicAST</c>'s call graph would buy nothing,
///     since anything reaching <c>OracleParser</c> reaches essentially all of it.
///   </description></item>
/// </list>
/// <para>
/// <strong>Why MVID does not destroy the warm cache.</strong> The obvious objection to any
/// assembly-identity key is that it changes on every recompile, which disables caching.
/// It does not here: .NET SDK builds are deterministic by default, and this repo's builds
/// were verified byte-identical across both an incremental rebuild and a from-scratch
/// rebuild of <c>MagicAST.dll</c>. The MVID therefore changes if and only if the compiler's
/// inputs changed — which is precisely the invalidation signal we want. (It is also
/// path-sensitive, so a git worktree at a different path gets a different MVID; caches are
/// per-worktree and gitignored, so that costs a first cold run in a new worktree and
/// nothing else.)
/// </para>
/// <para>
/// <strong>Why the step's own assembly MVID is deliberately NOT used.</strong> Folding
/// <c>MagicAtlas.Ast.Tests</c>' MVID in would make every NUnit-only edit in this project
/// invalidate the whole corpus parse. The per-method IL walk gives the same safety at the
/// granularity that matters.
/// </para>
/// <para>
/// <strong>Fail-safe.</strong> A step whose generated <c>CodeVersion</c> is absent is left
/// alone — Flowthru treats a null <c>CodeVersion</c> as uncacheable, and this class must
/// never upgrade an "unknown identity" into a confident one.
/// </para>
/// </remarks>
public static class StepCodeIdentity
{
  /// <summary>Separator between the generated code version and the closure digest.</summary>
  public const string Separator = "+";

  /// <summary>
  /// Assembly simple-name prefixes treated as first-party — code this repo owns and
  /// therefore code whose change must invalidate downstream artifacts. Framework and BCL
  /// assemblies are excluded on purpose: they move with an explicit package bump, which is
  /// a deliberate act, and folding them in would tie the cache to the SDK patch level.
  /// </summary>
  private static readonly string[] _firstPartyPrefixes = ["MagicAST", "MagicAtlas"];

  private static readonly ConcurrentDictionary<Type, string> _generatedVersions = new();
  private static readonly ConcurrentDictionary<Type, string> _closureDigests = new();
  private static readonly object _augmentLock = new();
  private static bool _augmented;

  /// <summary>
  /// Re-register every <c>[FlowthruStep]</c> class in every loaded first-party assembly
  /// under its code-closure-aware identity. Idempotent and safe to call from both
  /// <c>Program.Main</c> and test setup; the generated version is remembered per type, so
  /// repeat calls recompose from the original rather than stacking suffixes.
  /// </summary>
  /// <returns>The number of step classes augmented.</returns>
  public static int EnsureAugmented()
  {
    lock (_augmentLock)
    {
      if (_augmented) return _generatedVersions.Count;
      PreloadFirstPartyAssemblies();
      var count = 0;
      foreach (var stepType in EnumerateStepTypes())
      {
        if (Augment(stepType)) count++;
      }
      _augmented = true;
      return count;
    }
  }

  /// <summary>
  /// Force first-party assemblies into the load context before enumerating step classes.
  /// .NET loads assemblies lazily, so at <c>Main</c> entry a referenced project that declares
  /// steps may not be loaded yet — and a step class we never see is a step whose key stays
  /// code-blind. Loading eagerly makes the sweep complete rather than timing-dependent.
  /// </summary>
  private static void PreloadFirstPartyAssemblies()
  {
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var frontier = new Queue<Assembly>();
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      if (seen.Add(assembly.GetName().Name ?? "")) frontier.Enqueue(assembly);
    }
    while (frontier.Count > 0)
    {
      foreach (var reference in frontier.Dequeue().GetReferencedAssemblies())
      {
        if (!IsFirstParty(reference)) continue;
        if (!seen.Add(reference.Name!)) continue;
        try
        {
          frontier.Enqueue(Assembly.Load(reference));
        }
        catch (Exception)
        {
          // A first-party reference that cannot be loaded contributes nothing; the step
          // classes it would have declared simply aren't in this process.
        }
      }
    }
  }

  /// <summary>
  /// Every <c>[FlowthruStep]</c>-decorated class in the loaded first-party assemblies.
  /// Enumerating by attribute rather than by a hand-maintained list is what keeps this
  /// drift-proof: a step added tomorrow is covered without touching this file or its
  /// <c>AddStep</c> call site.
  /// </summary>
  public static IEnumerable<Type> EnumerateStepTypes()
  {
    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      if (!IsFirstParty(assembly.GetName())) continue;
      Type[] types;
      try
      {
        types = assembly.GetTypes();
      }
      catch (ReflectionTypeLoadException ex)
      {
        types = ex.Types.Where(t => t is not null).Select(t => t!).ToArray();
      }
      foreach (var type in types)
      {
        if (type.GetCustomAttributes().Any(a => a.GetType().FullName == "Flowthru.Step.FlowthruStepAttribute"))
        {
          yield return type;
        }
      }
    }
  }

  /// <summary>
  /// The identity <c>FlowBuilder.AddStep</c> will resolve for <paramref name="stepType"/>
  /// right now — i.e. whatever is currently in <see cref="StepMetadataRegistry"/>.
  /// </summary>
  public static string? EffectiveCodeVersion(Type stepType) => StepMetadataRegistry.TryGet(stepType);

  /// <summary>
  /// The source-generated code version for <paramref name="stepType"/>, before augmentation.
  /// Null when the type was never registered (not a <c>[FlowthruStep]</c> class).
  /// </summary>
  public static string? GeneratedCodeVersion(Type stepType) =>
    _generatedVersions.TryGetValue(stepType, out var v) ? v : StepMetadataRegistry.TryGet(stepType);

  /// <summary>
  /// Digest of the first-party code closure reachable from <paramref name="stepType"/>.
  /// Deterministic within a build: recomputing it in the same process, or in a later
  /// process over the same binaries, yields the same string.
  /// </summary>
  public static string ClosureDigest(Type stepType) =>
    _closureDigests.GetOrAdd(stepType, static t => ComputeClosureDigest(t).Digest);

  /// <summary>
  /// First-party assemblies OTHER than <paramref name="stepType"/>'s own that the step's
  /// code closure reaches. Exposed so the gate can assert the walk is not silently empty —
  /// a closure walk that finds nothing would make the invalidation test pass vacuously,
  /// which is the exact failure mode ADR 0004 is guarding against.
  /// </summary>
  public static IReadOnlyList<Assembly> ReachedAssemblies(Type stepType) =>
    ComputeClosureDigest(stepType).Assemblies;

  /// <summary>
  /// Fully-qualified names of the same-assembly types whose method bodies contribute to
  /// <paramref name="stepType"/>'s closure digest. Exposed for the same anti-vacuity reason
  /// as <see cref="ReachedAssemblies"/>: it proves the IL walk descended past the step class
  /// into the helpers the generated <c>CodeVersion</c> is blind to.
  /// </summary>
  public static IReadOnlyCollection<string> ReachedOwnAssemblyTypes(Type stepType) =>
    ComputeClosureDigest(stepType).OwnAssemblyTypes;

  private static bool Augment(Type stepType)
  {
    var generated = StepMetadataRegistry.TryGet(stepType);
    // Fail-safe: no recorded identity means Flowthru already considers the step
    // uncacheable. Never manufacture confidence we don't have.
    if (generated is null) return false;
    _generatedVersions[stepType] = generated;

    var services = StepMetadataRegistry.TryGetServices(stepType);
    StepMetadataRegistry.Register(
      stepType,
      generated + Separator + ClosureDigest(stepType),
      services
    );
    return true;
  }

  private static bool IsFirstParty(AssemblyName name)
  {
    var simple = name.Name;
    if (simple is null) return false;
    return _firstPartyPrefixes.Any(p => simple.StartsWith(p, StringComparison.Ordinal));
  }

  private readonly record struct Closure(
    string Digest,
    IReadOnlyList<Assembly> Assemblies,
    IReadOnlyCollection<string> OwnAssemblyTypes
  );

  /// <summary>
  /// Walk the step class's IL, collecting (a) the bodies of every same-assembly method it
  /// transitively reaches and (b) the identity of every foreign first-party assembly it
  /// touches. Both are folded into one SHA-256 prefix.
  /// </summary>
  private static Closure ComputeClosureDigest(Type stepType)
  {
    var ownAssembly = stepType.Assembly;
    var visited = new HashSet<MethodBase>();
    var queue = new Queue<MethodBase>();
    foreach (var method in EnumerateMethods(stepType))
    {
      if (visited.Add(method)) queue.Enqueue(method);
    }

    // Sorted so the digest does not depend on reflection enumeration order.
    var bodyContributions = new SortedSet<string>(StringComparer.Ordinal);
    var foreignAssemblies = new Dictionary<string, Assembly>(StringComparer.Ordinal);

    while (queue.Count > 0)
    {
      var method = queue.Dequeue();
      var il = TryGetIl(method);
      if (il is null) continue;
      bodyContributions.Add(Describe(method) + "=" + Sha256Hex(il, 16));

      foreach (var member in ResolveTokens(method, il))
      {
        var owner = member as Type ?? member.DeclaringType;
        if (owner is null) continue;
        var assembly = owner.Assembly;
        if (ReferenceEquals(assembly, ownAssembly))
        {
          // Same assembly: descend, so helper classes the step calls are covered at
          // method granularity.
          if (member is MethodBase callee && visited.Add(callee)) queue.Enqueue(callee);
          if (member is Type sameAsmType)
          {
            foreach (var m in EnumerateMethods(sameAsmType))
            {
              if (visited.Add(m)) queue.Enqueue(m);
            }
          }
          continue;
        }
        if (!IsFirstParty(assembly.GetName())) continue;
        // Foreign first-party: record the module, stop descending.
        foreignAssemblies.TryAdd(assembly.GetName().Name!, assembly);
      }
    }

    // Transitively include first-party assemblies the reached ones reference. Coarse on
    // purpose — a second-order dependency change is rare and over-invalidating there is
    // cheap, while missing one would be a silent stale build.
    var expandFrontier = new Queue<Assembly>(foreignAssemblies.Values);
    while (expandFrontier.Count > 0)
    {
      var assembly = expandFrontier.Dequeue();
      foreach (var reference in assembly.GetReferencedAssemblies())
      {
        if (!IsFirstParty(reference)) continue;
        if (reference.Name == ownAssembly.GetName().Name) continue;
        if (foreignAssemblies.ContainsKey(reference.Name!)) continue;
        Assembly loaded;
        try
        {
          loaded = Assembly.Load(reference);
        }
        catch (Exception)
        {
          continue;
        }
        foreignAssemblies[reference.Name!] = loaded;
        expandFrontier.Enqueue(loaded);
      }
    }

    var builder = new StringBuilder();
    foreach (var contribution in bodyContributions)
    {
      builder.Append(contribution).Append('|');
    }
    foreach (var name in foreignAssemblies.Keys.OrderBy(n => n, StringComparer.Ordinal))
    {
      builder
        .Append("asm:")
        .Append(name)
        .Append('=')
        .Append(foreignAssemblies[name].ManifestModule.ModuleVersionId.ToString("N"))
        .Append('|');
    }

    return new Closure(
      Sha256Hex(Encoding.UTF8.GetBytes(builder.ToString()), 16),
      foreignAssemblies.Values.OrderBy(a => a.GetName().Name, StringComparer.Ordinal).ToList(),
      visited
        .Where(m => ReferenceEquals(m.DeclaringType?.Assembly, ownAssembly))
        .Select(m => m.DeclaringType!.FullName!)
        .ToHashSet(StringComparer.Ordinal)
    );
  }

  /// <summary>
  /// Every method and constructor declared on <paramref name="type"/> and on its nested
  /// types, recursively. The nested-type recursion is load-bearing: a step's transform is a
  /// lambda inside <c>Create()</c>, which the C# compiler lowers onto a nested
  /// <c>&lt;&gt;c</c> / <c>&lt;&gt;c__DisplayClass</c> companion — the IL that matters is
  /// on the companion, not on the step class itself.
  /// </summary>
  private static IEnumerable<MethodBase> EnumerateMethods(Type type)
  {
    const BindingFlags flags =
      BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    MethodBase[] declared;
    try
    {
      declared = type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags)).ToArray();
    }
    catch (Exception)
    {
      yield break;
    }
    foreach (var method in declared) yield return method;

    Type[] nested;
    try
    {
      nested = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
    }
    catch (Exception)
    {
      yield break;
    }
    foreach (var child in nested)
    {
      foreach (var method in EnumerateMethods(child)) yield return method;
    }
  }

  private static byte[]? TryGetIl(MethodBase method)
  {
    try
    {
      if (method.IsAbstract || method.ContainsGenericParameters && method.IsGenericMethodDefinition == false)
      {
        // Abstract/open-constructed methods carry no body of their own.
      }
      return method.GetMethodBody()?.GetILAsByteArray();
    }
    catch (Exception)
    {
      return null;
    }
  }

  private static string Describe(MethodBase method) =>
    (method.DeclaringType?.FullName ?? "?") + "::" + method.Name + "/" + method.GetParameters().Length;

  // ---- IL token scanning -------------------------------------------------

  private static readonly OpCode[] _singleByte = new OpCode[0x100];
  private static readonly OpCode[] _multiByte = new OpCode[0x100];

  static StepCodeIdentity()
  {
    foreach (
      var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
    )
    {
      if (field.GetValue(null) is not OpCode opCode) continue;
      var value = unchecked((ushort)opCode.Value);
      if (opCode.Size == 1) _singleByte[value] = opCode;
      else _multiByte[value & 0xFF] = opCode;
    }
  }

  /// <summary>
  /// Decode <paramref name="il"/> and resolve every metadata-token operand back to the
  /// member it names. Resolution failures are skipped rather than thrown: a token we cannot
  /// resolve contributes nothing, and the method-body hash already covers the raw bytes, so
  /// nothing is silently lost from the digest.
  /// </summary>
  private static IEnumerable<MemberInfo> ResolveTokens(MethodBase method, byte[] il)
  {
    var module = method.Module;
    Type[]? typeArgs = null;
    Type[]? methodArgs = null;
    try
    {
      typeArgs = method.DeclaringType?.IsGenericType == true
        ? method.DeclaringType.GetGenericArguments()
        : null;
      methodArgs = method.IsGenericMethodDefinition ? method.GetGenericArguments() : null;
    }
    catch (Exception)
    {
      // Leave both null — ResolveMember falls back to the unbound form.
    }

    var pos = 0;
    while (pos < il.Length)
    {
      var code = il[pos++];
      OpCode opCode;
      if (code == 0xFE)
      {
        if (pos >= il.Length) yield break;
        opCode = _multiByte[il[pos++]];
      }
      else
      {
        opCode = _singleByte[code];
      }

      switch (opCode.OperandType)
      {
        case OperandType.InlineNone:
          break;
        case OperandType.ShortInlineBrTarget:
        case OperandType.ShortInlineI:
        case OperandType.ShortInlineVar:
          pos += 1;
          break;
        case OperandType.InlineVar:
          pos += 2;
          break;
        case OperandType.InlineBrTarget:
        case OperandType.InlineI:
        case OperandType.ShortInlineR:
          pos += 4;
          break;
        case OperandType.InlineI8:
        case OperandType.InlineR:
          pos += 8;
          break;
        case OperandType.InlineSwitch:
        {
          if (pos + 4 > il.Length) yield break;
          var count = BitConverter.ToInt32(il, pos);
          pos += 4 + (4 * count);
          break;
        }
        case OperandType.InlineString:
        case OperandType.InlineSig:
          pos += 4;
          break;
        case OperandType.InlineField:
        case OperandType.InlineMethod:
        case OperandType.InlineTok:
        case OperandType.InlineType:
        {
          if (pos + 4 > il.Length) yield break;
          var token = BitConverter.ToInt32(il, pos);
          pos += 4;
          MemberInfo? member = null;
          try
          {
            member = module.ResolveMember(token, typeArgs, methodArgs);
          }
          catch (Exception)
          {
            member = null;
          }
          if (member is not null) yield return member;
          break;
        }
        default:
          // Unknown/undefined opcode — the stream is no longer trustworthy; the body hash
          // still covers these bytes, so stop scanning rather than misread operands.
          yield break;
      }
    }
  }

  private static string Sha256Hex(byte[] bytes, int hexLength)
  {
    var hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash)[..hexLength].ToLowerInvariant();
  }
}
