public interface ICheat {
    string Id { get; }
    string DisplayName { get; }
    int AudacityCost { get; }
    bool NeedsSuspicionCheck { get; }
    bool CanUse(ICheatHost host);
    void Apply(ICheatHost host);
    string CostLabel(ICheatHost host);      // e.g. "1 AUD, Susp DC 8"
}
