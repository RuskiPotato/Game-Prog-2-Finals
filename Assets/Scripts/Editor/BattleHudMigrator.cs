#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// One-click move of the old BattleHud Inspector assignments into the two new components.
public static class BattleHudMigrator {
    [MenuItem("Paperboard God/Migrate BattleHud (split)")]
    static void Run() {
        int count = 0;
        foreach (var hud in Object.FindObjectsByType<BattleHud>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            var stats = hud.GetComponent<BattleStatusView>() ?? Undo.AddComponent<BattleStatusView>(hud.gameObject);
            var clash = hud.GetComponent<ClashPresenter>() ?? Undo.AddComponent<ClashPresenter>(hud.gameObject);
            Copy(hud, stats);
            Copy(hud, clash);

            var so = new SerializedObject(hud);
            so.FindProperty("statusView").objectReferenceValue = stats;
            so.FindProperty("clashPresenter").objectReferenceValue = clash;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hud);
            count++;
        }
        Debug.Log($"BattleHud migration finished for {count} HUD(s). Save the scene, check the new components, then delete BattleHud.Legacy.cs and this script.");
    }

    // Copies every same-named, same-typed top-level field from the old BattleHud values into the target component.
    static void Copy(Object source, Object target) {
        var src = new SerializedObject(source);
        var dst = new SerializedObject(target);
        var it = dst.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter)) {
            enter = false;
            if (it.name == "m_Script") continue;
            var from = src.FindProperty(it.name);
            if (from == null || from.propertyType != it.propertyType) continue;
            switch (it.propertyType) {
                case SerializedPropertyType.ObjectReference: it.objectReferenceValue = from.objectReferenceValue; break;
                case SerializedPropertyType.Integer: it.intValue = from.intValue; break;
                case SerializedPropertyType.Boolean: it.boolValue = from.boolValue; break;
                case SerializedPropertyType.Float: it.floatValue = from.floatValue; break;
                case SerializedPropertyType.String: it.stringValue = from.stringValue; break;
                case SerializedPropertyType.Color: it.colorValue = from.colorValue; break;
            }
        }
        dst.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }
}
#endif
