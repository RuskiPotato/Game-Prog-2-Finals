using System.Collections.Generic;
using System.Linq;

public class RunState
{
    public readonly int Seed;
    public readonly List<MapNode> Map;
    public int Savestates;
    public string SelectedNodeId { get; private set; }

    // The active Act must advance when its boss is defeated, even if the player
    // left optional branches in the previous Act unexplored.
    public int CurrentAct { get; private set; }

    public RunState(int seed, List<MapNode> map, int savestates = 0)
    {
        Seed = seed;
        Map = map;
        Savestates = savestates;
        CurrentAct = 0;

        MapNode start = Map.Find(n => n.act == 0 && n.index == 0 && n.lane == 1);
        if (start != null) start.unlocked = true;
    }

    public bool TrySpendSavestate()
    {
        if (Savestates <= 0) return false;
        Savestates--;
        return true;
    }

    public MapNode Current => string.IsNullOrEmpty(SelectedNodeId) ? null : Map.Find(n => n.id == SelectedNodeId);
    public bool Finished => Map.Count > 0 && Map.Where(n => n.type == NodeType.Boss).All(n => n.visited);
    public IEnumerable<MapNode> AvailableNodes => Map.Where(n => n.act == CurrentAct && n.unlocked && !n.visited);

    public bool SelectNode(string id)
    {
        MapNode node = Map.Find(n => n.id == id);
        if (node == null || node.act != CurrentAct || !node.unlocked || node.visited) return false;
        SelectedNodeId = id;
        return true;
    }

    public void CompleteCurrent()
    {
        MapNode node = Current;
        if (node == null) return;

        node.visited = true;
        foreach (string nextId in node.nextNodeIds)
        {
            MapNode next = Map.Find(n => n.id == nextId);
            if (next != null && !next.visited) next.unlocked = true;
        }

        if (node.type == NodeType.Boss)
        {
            int nextAct = node.act + 1;
            if (Map.Exists(n => n.act == nextAct)) CurrentAct = nextAct;
        }

        SelectedNodeId = null;
    }
}
