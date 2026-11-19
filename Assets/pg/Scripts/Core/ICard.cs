using UnityEngine;

public enum CardKind { Action, Cheat }

// Anything that can sit in the hand. Each card knows how to describe itself and how to play itself,
// so the hand and the controller never need to switch on card type.
public interface ICard {
    string Id { get; }
    string Title { get; }
    CardKind Kind { get; }
    Sprite Art { get; }
    bool IsAvailable(BattleContext ctx);     // shown in the hand at all (e.g. cheat unlocked)
    bool CanPlay(BattleContext ctx);         // clickable right now
    string CostText(BattleContext ctx);
    string DetailText(BattleContext ctx);
    void Play(BattleContext ctx, BattleInput input);
}
