using Flowthru.Abstractions;

namespace MagicAtlas.Data.Enums.Card;

/// <summary>
/// Security stamp types on physical cards.
/// </summary>
public enum SecurityStamp
{
  [SerializedEnum("oval")]
  Oval,

  [SerializedEnum("triangle")]
  Triangle,

  [SerializedEnum("acorn")]
  Acorn,

  [SerializedEnum("circle")]
  Circle,

  [SerializedEnum("arena")]
  Arena,

  [SerializedEnum("heart")]
  Heart,
}
