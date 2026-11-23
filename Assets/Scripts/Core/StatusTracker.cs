using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public sealed class ActiveStatus {
    public EnemyStatusEffectType Type;
    public int Amount;
    public int TurnsLeft;
}

// Timed buffs/debuffs on one unit. Re-applying the same status refreshes it (longest duration, strongest amount) instead of stacking.
public sealed class StatusTracker {
    readonly List<ActiveStatus> active = new List<ActiveStatus>();
    public IReadOnlyList<ActiveStatus> All => active;

    public void Add(EnemyStatusEffectType type, int amount, int turns) {
        if (turns < 1) turns = 1;
        foreach (var s in active) {
            if (s.Type != type) continue;
            if (amount > s.Amount) s.Amount = amount;
            if (turns > s.TurnsLeft) s.TurnsLeft = turns;
            return;
        }
        active.Add(new ActiveStatus { Type = type, Amount = amount, TurnsLeft = turns });
    }

    public int Total(EnemyStatusEffectType type) {
        int sum = 0;
        foreach (var s in active) if (s.Type == type) sum += s.Amount;
        return sum;
    }

    public bool Remove(EnemyStatusEffectType type) => active.RemoveAll(s => s.Type == type) > 0;
    public void Clear() => active.Clear();

    // Called once at the end of every round.
    public void Tick() {
        foreach (var s in active) s.TurnsLeft--;
        active.RemoveAll(s => s.TurnsLeft <= 0);
    }

    public static string NameOf(EnemyStatusEffectType type) =>
        Regex.Replace(type.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

    public string Describe() {
        if (active.Count == 0) return "";
        var sb = new StringBuilder();
        for (int i = 0; i < active.Count; i++) {
            if (i > 0) sb.Append("  |  ");
            var s = active[i];
            sb.Append(NameOf(s.Type)).Append(' ').Append(s.Amount).Append(" (").Append(s.TurnsLeft).Append("t)");
        }
        return sb.ToString();
    }
}
