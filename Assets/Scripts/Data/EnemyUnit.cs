using UnityEngine;

// Runtime wrapper around an EnemyData asset.
public class EnemyUnit : Unit {
    public EnemyData Data { get; }
    public EnemyMoveData CurrentMove { get; private set; }

    public EnemyUnit(EnemyData data) { Data = data; MaxHp = Hp = data.hp; }

    public override string DisplayName => Data.displayName;
    public override int BaseAgi => Data.agi;
    public override int BaseDefenseDc => Data.defenseDc;
    public override int RollDamage() => Mathf.Max(0, (CurrentMove != null
        ? DiceService.Die(CurrentMove.damageDie) + CurrentMove.damageBonus
        : DiceService.Die(Data.damageDie) + Data.damageBonus) + StatusDamageBonus);

    public int AttackModifier => Data.attackMod + (CurrentMove != null ? CurrentMove.attackModifier : 0) + StatusRollMod;
    public bool Attacks => Data.attacks;
    // False on turns where the chosen move is a Defend or Skill move: no clash dice, no attack.
    public bool AttacksThisTurn => Data.attacks && (CurrentMove == null || CurrentMove.moveType == EnemyMoveType.Attack);
    public EnemyMoveType CurrentMoveType => CurrentMove != null ? CurrentMove.moveType : EnemyMoveType.Attack;
    public string CurrentMoveName => CurrentMove != null
        ? (string.IsNullOrEmpty(CurrentMove.moveName) ? "Attack" : CurrentMove.moveName)
        : (string.IsNullOrEmpty(Data.attackName) ? "Attack" : Data.attackName);

    // Weighted random choice. Zero/negative weights are ignored; if all are zero,
    // choose uniformly from the valid, non-null moves.
    public EnemyMoveData ChooseMove() {
        if (Data.moves == null || Data.moves.Count == 0) {
            CurrentMove = null;
            return null;
        }

        float total = 0f;
        int validCount = 0;
        foreach (var move in Data.moves) {
            if (move == null) continue;
            validCount++;
            total += Mathf.Max(0f, move.selectionWeight);
        }
        if (validCount == 0) { CurrentMove = null; return null; }

        if (total <= 0f) {
            int pick = Random.Range(0, validCount);
            foreach (var move in Data.moves) {
                if (move == null) continue;
                if (pick-- == 0) { CurrentMove = move; return move; }
            }
        }

        float roll = Random.value * total;
        foreach (var move in Data.moves) {
            if (move == null) continue;
            roll -= Mathf.Max(0f, move.selectionWeight);
            if (roll < 0f) { CurrentMove = move; return move; }
        }

        // Floating-point edge case: return the last valid move.
        for (int i = Data.moves.Count - 1; i >= 0; i--) {
            if (Data.moves[i] != null) { CurrentMove = Data.moves[i]; return CurrentMove; }
        }
        CurrentMove = null;
        return null;
    }
}
