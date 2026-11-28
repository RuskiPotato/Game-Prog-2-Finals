using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Displays one Act at a time as a bottom-to-top, three-lane branching map.
// Assign MapRoot to the map area only (do not include the right-hand details panel).
public class ExplorationMapController : MonoBehaviour
{
    [Header("Map")]
    public RectTransform mapRoot;
    public Button nodeButtonPrefab;
    [Tooltip("Optional. If empty, simple UI Images are created automatically for connections.")]
    public Image connectionPrefab;

    [Header("Map Layout")]
    [Min(24f)] public float nodeWidth = 112f;
    [Min(24f)] public float nodeHeight = 58f;
    [Min(2f)] public float connectionThickness = 5f;
    [Range(0.15f, 0.45f)] public float laneSpread = 0.29f;
    [Min(8f)] public float verticalPadding = 42f;

    [Header("Node Colours")]
    public Color lockedColor = new Color(0.34f, 0.32f, 0.29f, 0.78f);
    public Color availableColor = new Color(0.83f, 0.72f, 0.49f, 1f);
    public Color selectedColor = new Color(1f, 0.88f, 0.48f, 1f);
    public Color visitedColor = new Color(0.42f, 0.48f, 0.39f, 0.85f);
    public Color connectionColor = new Color(0.60f, 0.48f, 0.32f, 0.9f);
    public Color activeConnectionColor = new Color(0.91f, 0.73f, 0.37f, 1f);

    [Header("Header / Details")]
    public TMP_Text actText;
    public TMP_Text resourceText;
    public TMP_Text nodeTitleText;
    public TMP_Text nodeDescriptionText;
    public Button actionButton;
    public TMP_Text actionButtonText;
    public TMP_Text statusText;

    GameManager gm;
    MapNode selected;
    readonly List<GameObject> spawned = new List<GameObject>();
    readonly Dictionary<string, Button> nodeButtons = new Dictionary<string, Button>();
    readonly Dictionary<string, Vector2> nodePositions = new Dictionary<string, Vector2>();

    void Start()
    {
        gm = GameManager.Instance;
        if (gm == null || gm.Run == null)
        {
            if (statusText) statusText.text = "No active run. Start a new game.";
            if (actionButton) actionButton.interactable = false;
            return;
        }

        if (mapRoot == null || nodeButtonPrefab == null)
        {
            Debug.LogError("ExplorationMapController requires Map Root and Node Button Prefab references.", this);
            if (statusText) statusText.text = "Campaign map is missing its Map Root or Node Button Prefab.";
            return;
        }

        if (actionButton)
        {
            actionButton.onClick.RemoveListener(ConfirmNode);
            actionButton.onClick.AddListener(ConfirmNode);
            actionButton.interactable = false;
        }

        DrawMap();
        RefreshHeader();
    }

    void DrawMap()
    {
        ClearMap();

        if (gm == null || gm.Run == null || mapRoot == null) return;

        // Only display the currently active Act. The next Act appears after its boss is defeated.
        int currentAct = gm.Run.CurrentAct;
        List<MapNode> actNodes = gm.Run.Map
            .Where(n => n.act == currentAct)
            .OrderBy(n => n.index)
            .ThenBy(n => n.lane)
            .ToList();

        if (actNodes.Count == 0)
        {
            if (statusText) statusText.text = "No campaign nodes found for this Act.";
            return;
        }

        float width = mapRoot.rect.width;
        float height = mapRoot.rect.height;
        if (width <= 1f || height <= 1f)
        {
            Debug.LogWarning("MapRoot has no usable size. Give its RectTransform a non-zero width and height.", mapRoot);
            return;
        }

        int maxRow = Mathf.Max(1, actNodes.Max(n => n.index));
        float usableHeight = Mathf.Max(1f, height - verticalPadding * 2f);
        float laneOffset = Mathf.Min(width * laneSpread, width * 0.38f);

        foreach (MapNode node in actNodes)
        {
            float x = (node.lane - 1) * laneOffset;
            // Row 0 (start) is at the bottom; the boss (last row) is at the top.
            float y = -height * 0.5f + verticalPadding + (node.index / (float)maxRow) * usableHeight;
            nodePositions[node.id] = new Vector2(x, y);
        }

        DrawConnections(actNodes);
        DrawNodes(actNodes);
        RefreshNodeVisuals();
    }

    void DrawConnections(List<MapNode> actNodes)
    {
        foreach (MapNode node in actNodes)
        {
            if (!nodePositions.TryGetValue(node.id, out Vector2 from)) continue;

            foreach (string nextId in node.nextNodeIds)
            {
                MapNode next = actNodes.Find(n => n.id == nextId);
                if (next == null || !nodePositions.TryGetValue(next.id, out Vector2 to)) continue;

                Image line = connectionPrefab != null
                    ? Instantiate(connectionPrefab, mapRoot)
                    : CreateConnectionImage();

                line.name = "Connection_" + node.id + "_to_" + next.id;
                line.raycastTarget = false;
                line.color = (node.visited || next.unlocked) ? activeConnectionColor : connectionColor;

                RectTransform rt = line.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                Vector2 delta = to - from;
                rt.anchoredPosition = (from + to) * 0.5f;
                rt.sizeDelta = new Vector2(delta.magnitude, connectionThickness);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                line.gameObject.SetActive(true);
                line.transform.SetAsFirstSibling();
                spawned.Add(line.gameObject);
            }
        }
    }

    Image CreateConnectionImage()
    {
        GameObject lineObject = new GameObject("CampaignConnection", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObject.transform.SetParent(mapRoot, false);
        Image image = lineObject.GetComponent<Image>();
        image.sprite = null;
        image.color = connectionColor;
        return image;
    }

    void DrawNodes(List<MapNode> actNodes)
    {
        foreach (MapNode node in actNodes)
        {
            nodeButtonPrefab.gameObject.SetActive(true);
            Button button = Instantiate(nodeButtonPrefab, mapRoot);
            button.name = "Node_" + node.id;
            button.transition = Selectable.Transition.None;

            RectTransform rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = nodePositions[node.id];
            rt.sizeDelta = new Vector2(nodeWidth, nodeHeight);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label)
            {
               
                label.text = NodeLabel(node);
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                label.fontSize = Mathf.Min(label.fontSize, 22f);
            }

            MapNode captured = node;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectNode(captured));
            nodeButtons[node.id] = button;
            spawned.Add(button.gameObject);
        }
    }

    void SelectNode(MapNode node)
    {
        if (node == null || !node.unlocked || node.visited) return;

        selected = node;
        if (nodeTitleText) nodeTitleText.text = $"ACT {node.act + 1}  /  {NodeLabel(node)}";
        if (nodeDescriptionText) nodeDescriptionText.text = Description(node);
        if (actionButtonText) actionButtonText.text = ActionLabel(node);
        if (actionButton) actionButton.interactable = true;
        if (statusText) statusText.text = "Destination selected. Confirm to continue.";
        RefreshNodeVisuals();
    }

    void RefreshNodeVisuals()
    {
        foreach (MapNode node in gm.Run.Map.Where(n => n.act == gm.Run.CurrentAct))
        {
            if (!nodeButtons.TryGetValue(node.id, out Button button) || button == null) continue;

            button.interactable = node.unlocked && !node.visited;
            Image image = button.targetGraphic as Image;
            if (image == null) image = button.GetComponent<Image>();
            if (image != null)
            {
                if (node.visited) image.color = visitedColor;
                else if (selected != null && selected.id == node.id) image.color = selectedColor;
                else if (node.unlocked) image.color = availableColor;
                else image.color = lockedColor;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label) label.color = node.unlocked || node.visited ? new Color(0.16f, 0.13f, 0.10f, 1f) : new Color(0.78f, 0.75f, 0.70f, 1f);
        }
    }

    void ConfirmNode()
    {
        if (selected == null || gm == null || gm.Run == null) return;
        if (!gm.Run.SelectNode(selected.id)) return;

        if (selected.IsCombat)
        {
            gm.LoadScene("Battle");
            return;
        }

        ResolveNonCombatNode(selected);
    }

    void ResolveNonCombatNode(MapNode node)
    {
        FateState fate = gm.Fate;
        switch (node.type)
        {
            case NodeType.Shop:
                const int healCost = 30;
                if (fate.Gold >= healCost)
                {
                    fate.Gold -= healCost;
                    gm.Hero.Heal(12);
                    if (statusText) statusText.text = "Purchased a field treatment. Restored 12 HP.";
                }
                else if (statusText) statusText.text = "Not enough Gold for treatment. The shop visit costs no Gold.";
                break;

            case NodeType.SnackBreak:
                gm.Hero.Heal(gm.Config.snackHeal);
                fate.AddSuspicion(-10);
                fate.AddMorale(10);
                if (statusText) statusText.text = $"Rested. Restored up to {gm.Config.snackHeal} HP and reduced Suspicion.";
                break;

            case NodeType.RulesDispute:
                fate.AddFate(-5);
                fate.Gold += 15;
                fate.AddSuspicion(5);
                if (statusText) statusText.text = "You found a loophole in the rules. +15 Gold, -5 Fate, +5 Suspicion.";
                break;
        }

        gm.Run.CompleteCurrent();
        selected = null;
        if (actionButton) actionButton.interactable = false;
        RefreshHeader();
        DrawMap();
    }

    void RefreshHeader()
    {
        if (gm == null || gm.Run == null) return;
        if (actText) actText.text = $"ACT {gm.Run.CurrentAct + 1}";
        if (resourceText)
            resourceText.text = $"HP {gm.Hero.Hp}/{gm.Hero.MaxHp}    GOLD {gm.Fate.Gold}    FATE {gm.Fate.Fate}    SUSPICION {gm.Fate.Suspicion}";

        if (actionButton) actionButton.interactable = selected != null && selected.unlocked && !selected.visited;
        if (nodeTitleText) nodeTitleText.text = "SELECT A DESTINATION";
        if (nodeDescriptionText) nodeDescriptionText.text = "Choose one of the highlighted connected nodes to continue your journey.";
    }

    void ClearMap()
    {
        foreach (GameObject go in spawned) if (go != null) Destroy(go);
        spawned.Clear();
        nodeButtons.Clear();
        nodePositions.Clear();
    }

    static string NodeLabel(MapNode node)
    {
        switch (node.type)
        {
            case NodeType.Fight: return "FIGHT";
            case NodeType.Elite: return "ELITE";
            case NodeType.Shop: return "SHOP";
            case NodeType.SnackBreak: return "REST";
            case NodeType.RulesDispute: return "EVENT";
            case NodeType.Boss: return "ACT BOSS";
            default: return node.type.ToString().ToUpperInvariant();
        }
    }

    static string Description(MapNode node)
    {
        switch (node.type)
        {
            case NodeType.Fight: return "A standard encounter. Victory grants Gold and unlocks the next connected destinations.";
            case NodeType.Elite: return "A dangerous opponent with better rewards. Prepare before entering.";
            case NodeType.Shop: return "Purchase a field treatment to restore 12 HP for 30 Gold.";
            case NodeType.SnackBreak: return "Recover HP, reduce Suspicion, and restore Morale.";
            case NodeType.RulesDispute: return "Challenge Destiny's rules for Gold at the cost of attracting attention.";
            case NodeType.Boss: return "Defeat this Act's boss to unlock the next Act.";
            default: return "Select this destination to continue.";
        }
    }

    static string ActionLabel(MapNode node)
    {
        if (node.type == NodeType.Shop) return "BUY TREATMENT — 30 GOLD";
        if (node.type == NodeType.Boss) return "CHALLENGE THE BOSS";
        if (node.IsCombat) return "ENTER ENCOUNTER";
        return "RESOLVE NODE";
    }
}
