// Display helper for the Suspicion meter. Thresholds follow the GDD: 0-29 / 30-59 / 60-89 / 90+.
// Pure C#, no Unity types. The rules themselves still live in FateState.
public enum SuspicionTier { Unnoticed, Watching, Suspicious, Caught }

public static class SuspicionInfo {
    public static SuspicionTier TierOf(int suspicion) {
        if (suspicion >= 90) return SuspicionTier.Caught;
        if (suspicion >= 60) return SuspicionTier.Suspicious;
        if (suspicion >= 30) return SuspicionTier.Watching;
        return SuspicionTier.Unnoticed;
    }

    public static string Name(int suspicion) => TierOf(suspicion).ToString();

    // Same formula the Suspicion check uses: DC 8 + Suspicion/10.
    public static int CheckDc(int suspicion) => 8 + suspicion / 10;
}
