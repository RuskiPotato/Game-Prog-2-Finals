public struct CheatResult {
    public bool Played;
    public bool SuspicionChecked;
    public bool Noticed;           // failed the Suspicion check
}

public static class CheatRunner {
    public static int AudacityCost(ICheat cheat, ICheatHost host) =>
        cheat.AudacityCost == 0 ? 0
        : host.Hero.CheatCost(cheat.Id, cheat.AudacityCost) + host.Fate.CheatCostBump;

    // Pays the cost and rolls the Suspicion check, without applying the effect.
    public static CheatResult TryPay(ICheat cheat, ICheatHost host) {
        var fate = host.Fate;
        if (!cheat.CanUse(host)) { host.Log($"{cheat.DisplayName}: can't use that right now."); return default; }

        int cost = AudacityCost(cheat, host);
        if (fate.Audacity < cost) { host.Log($"Need {cost} Audacity."); return default; }

        fate.Audacity -= cost;
        bool noticed = false;
        if (cheat.NeedsSuspicionCheck) {
            bool pass = fate.SuspicionCheck(host.Hero.Cunning);
            fate.AddMorale(-3);
            noticed = !pass;
            host.Log(pass ? $"{cheat.DisplayName}: unnoticed." : $"{cheat.DisplayName}: the GM saw that!");
        }
        return new CheatResult { Played = true, SuspicionChecked = cheat.NeedsSuspicionCheck, Noticed = noticed };
    }

    public static CheatResult TryPlay(ICheat cheat, ICheatHost host) {
        var result = TryPay(cheat, host);
        if (result.Played) cheat.Apply(host);
        return result;
    }
}
