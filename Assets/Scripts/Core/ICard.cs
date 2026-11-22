using UnityEngine;

public enum CardKind { Action, Cheat }

// Anything that can sit in the hand.
public interface ICard {
    string Id { get; }
    string Title { get; }
    CardKind Kind { get; }
    Sprite Art { get; }
    bool IsAvailable(BattleContext ctx);     
    bool CanPlay(BattleContext ctx);         
    string CostText(BattleContext ctx);
    string DetailText(BattleContext ctx);
    void Play(BattleContext ctx, BattleInput input);
}
