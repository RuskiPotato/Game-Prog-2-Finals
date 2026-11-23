using System.Collections.Generic;
using UnityEngine;

public sealed class CombatCardInstance : ICard {
    public CombatCardData Data { get; }
    public CombatCardInstance(CombatCardData data) { Data = data; }
    public string Id => Data.Id;
    public string Title => Data.Title;
    public CardKind Kind => Data.Kind;
    public Sprite Art => Data.Art;
    public bool IsAvailable(BattleContext ctx) => Data.IsAvailable(ctx);
    public bool CanPlay(BattleContext ctx) => Data.CanPlay(ctx);
    public string CostText(BattleContext ctx) => Data.CostText(ctx);
    public string DetailText(BattleContext ctx) => Data.DetailText(ctx);
    public void Play(BattleContext ctx, BattleInput input) => input.PressCombatCard(this);
}

public sealed class CardDeck {
    readonly List<CombatCardInstance> drawPile = new List<CombatCardInstance>();
    readonly List<CombatCardInstance> hand = new List<CombatCardInstance>();
    readonly List<CombatCardInstance> discardPile = new List<CombatCardInstance>();
    readonly List<CombatCardInstance> exhausted = new List<CombatCardInstance>();
    readonly int handSize;

    public IReadOnlyList<CombatCardInstance> Hand => hand;
    public int DrawCount => drawPile.Count;
    public int DiscardCount => discardPile.Count;
    public int ExhaustCount => exhausted.Count;

    public CardDeck(IEnumerable<CombatCardData> cards, int handSize) {
        this.handSize = Mathf.Max(1, handSize);
        if (cards != null) foreach (var card in cards) if (card != null) drawPile.Add(new CombatCardInstance(card));
        Shuffle(drawPile);
    }

    public void DrawToHand() {
        while (hand.Count < handSize) {
            if (drawPile.Count == 0) {
                if (discardPile.Count == 0) break;
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
            }
            int last = drawPile.Count - 1;
            hand.Add(drawPile[last]);
            drawPile.RemoveAt(last);
        }
    }

    public bool Play(CombatCardInstance card) {
        int index = hand.IndexOf(card);
        if (index < 0) return false;
        hand.RemoveAt(index);
        discardPile.Add(card);
        return true;
    }

    public void Exhaust(CombatCardInstance card) {
        if (hand.Remove(card)) exhausted.Add(card);
    }

    public List<ICard> BuildVisibleCards(IEnumerable<CardData> coreCards) {
        var result = new List<ICard>();
        if (coreCards != null) foreach (var card in coreCards) {
            if (card is AttackCardData || card is DefendCardData) result.Add(card);
        }
        result.AddRange(hand);
        return result;
    }

    static void Shuffle(List<CombatCardInstance> cards) {
        var rng = Rng.Get(RngStream.Loot);
        for (int i = cards.Count - 1; i > 0; i--) {
            int j = rng.Next(i + 1);
            var temp = cards[i]; cards[i] = cards[j]; cards[j] = temp;
        }
    }
}
