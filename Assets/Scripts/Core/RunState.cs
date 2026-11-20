using System.Collections.Generic;

public class RunState {
    public readonly int Seed;
    public readonly List<MapNode> Map;
    public int Cursor;
    public int Savestates;       // Flashback tokens left; fixed per run, never refilled

    public RunState(int seed, List<MapNode> map, int savestates = 0) { Seed = seed; Map = map; Savestates = savestates; }

    public bool TrySpendSavestate() {
        if (Savestates <= 0) return false;
        Savestates--;
        return true;
    }

    public MapNode Current => Cursor < Map.Count ? Map[Cursor] : null;
    public bool Finished => Cursor >= Map.Count;
    public int CurrentAct => Current != null ? Current.act : Map[Map.Count - 1].act;

    public void CompleteCurrent() {
        if (Current == null) return;
        Current.visited = true;
        Cursor++;
    }
}
