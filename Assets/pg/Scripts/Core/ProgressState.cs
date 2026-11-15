using System.Collections.Generic;

public enum GamePhase { Tutorial, FirstEncounter, Normal }

public class ProgressState {
    public GamePhase Phase = GamePhase.Tutorial;
    public bool FateBarRevealed => Phase == GamePhase.Normal;
    public HashSet<string> UnlockedCheats = new HashSet<string>();

    public bool IsUnlocked(string id) => UnlockedCheats.Contains(id);
    public void Unlock(string id) => UnlockedCheats.Add(id);
}
