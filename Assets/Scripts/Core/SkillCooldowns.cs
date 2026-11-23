using System.Collections.Generic;

// Per-fight cooldown bookkeeping. Skills use cooldowns, never mana. Cooldown N: used in round R, ready again in round R+N.
public sealed class SkillCooldowns {
    readonly Dictionary<SkillData, int> remaining = new Dictionary<SkillData, int>();

    public int Remaining(SkillData skill) =>
        skill != null && remaining.TryGetValue(skill, out int r) ? r : 0;
    public bool IsReady(SkillData skill) => Remaining(skill) <= 0;

    public void Start(SkillData skill) {
        if (skill == null || skill.cooldown <= 0) return;
        remaining[skill] = skill.cooldown;
    }

    public void Tick() {
        foreach (var key in new List<SkillData>(remaining.Keys)) {
            int next = remaining[key] - 1;
            if (next <= 0) remaining.Remove(key); else remaining[key] = next;
        }
    }

    public void Reset() => remaining.Clear();
}
