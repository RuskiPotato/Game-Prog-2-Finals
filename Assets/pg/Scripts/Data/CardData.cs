using UnityEngine;

// Abstract ScriptableObject base for every card. AttackCardData, DefendCardData and CheatData derive from it.
public abstract class CardData : ScriptableObject, ICard {
    [Header("Card")]
    public Sprite art;

    public abstract string Id { get; }
    public abstract string Title { get; }
    public abstract CardKind Kind { get; }
    public Sprite Art => art;

    public virtual bool IsAvailable(BattleContext ctx) => true;
    public abstract bool CanPlay(BattleContext ctx);
    public abstract string CostText(BattleContext ctx);
    public abstract string DetailText(BattleContext ctx);
    public abstract void Play(BattleContext ctx, BattleInput input);
}
