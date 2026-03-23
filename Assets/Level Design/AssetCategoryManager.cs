using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ─────────────────────────────────────────────
//  Niveau 3 — Asset individuel
// ─────────────────────────────────────────────
[System.Serializable]
public class Asset3DEntry
{
    public string assetName;
    public GameObject sceneObject;
    public bool ignore = false;
}

// ─────────────────────────────────────────────
//  Niveau 2 — Catégorie avec matériau + assets
// ─────────────────────────────────────────────
[System.Serializable]
public class AssetCategory
{
    public string categoryName;
    public Material categoryMaterial;
    public List<Asset3DEntry> assets = new();
}

// ─────────────────────────────────────────────
//  Niveau 1 — Groupe contenant des catégories
// ─────────────────────────────────────────────
[System.Serializable]
public class AssetGroup
{
    public string groupName;
    public List<AssetCategory> categories = new();
}

// ─────────────────────────────────────────────
//  Composant principal — liste mère des groupes
// ─────────────────────────────────────────────
public class AssetCategoryManager : MonoBehaviour
{
    [Header("Groupes d'assets 3D")]
    public List<AssetGroup> groups = new();

    void Start()
    {
        foreach (AssetGroup group in groups)
        {
            foreach (AssetCategory cat in group.categories)
            {
                if (cat.categoryMaterial == null) continue;

                foreach (Asset3DEntry entry in cat.assets)
                {
                    if (entry.sceneObject == null || entry.ignore) continue;

                    Renderer[] renderers = entry.sceneObject.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers)
                        r.material = cat.categoryMaterial;
                }
            }
        }
    }

    public GameObject GetAsset(string groupName, string categoryName, string assetName)
    {
        AssetGroup group = groups.Find(g => g.groupName == groupName);
        if (group == null)
        {
            Debug.LogWarning($"[AssetCategoryManager] Groupe introuvable : {groupName}");
            return null;
        }

        AssetCategory cat = group.categories.Find(c => c.categoryName == categoryName);
        if (cat == null)
        {
            Debug.LogWarning($"[AssetCategoryManager] Catégorie introuvable : {categoryName}");
            return null;
        }

        Asset3DEntry entry = cat.assets.Find(a => a.assetName == assetName);
        if (entry == null)
        {
            Debug.LogWarning($"[AssetCategoryManager] Asset introuvable : {assetName}");
            return null;
        }

        return entry.sceneObject;
    }
}


// ═══════════════════════════════════════════════════════════════════════════
//  CUSTOM EDITOR — Inspector amélioré (Unity Editor uniquement)
// ═══════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
[CustomEditor(typeof(AssetCategoryManager))]
public class AssetCategoryManagerEditor : Editor
{
    private List<bool> _groupFoldouts = new();
    private List<List<bool>> _categoryFoldouts = new();

    public override void OnInspectorGUI()
    {
        AssetCategoryManager manager = (AssetCategoryManager)target;

        EditorGUILayout.LabelField("Asset Category Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        while (_groupFoldouts.Count < manager.groups.Count)
        {
            _groupFoldouts.Add(true);
            _categoryFoldouts.Add(new List<bool>());
        }

        for (int g = 0; g < manager.groups.Count; g++)
        {
            AssetGroup group = manager.groups[g];

            while (_categoryFoldouts[g].Count < group.categories.Count)
                _categoryFoldouts[g].Add(true);

            // ── Niveau 1 : Groupe ─────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            GUIStyle groupHeader = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };

            _groupFoldouts[g] = EditorGUILayout.Foldout(
                _groupFoldouts[g],
                string.IsNullOrEmpty(group.groupName) ? $"Groupe {g + 1}" : group.groupName,
                true,
                groupHeader
            );

            GUIStyle redBtn = new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red } };
            if (GUILayout.Button("✕", redBtn, GUILayout.Width(24)))
            {
                manager.groups.RemoveAt(g);
                _groupFoldouts.RemoveAt(g);
                _categoryFoldouts.RemoveAt(g);
                EditorUtility.SetDirty(target);
                break;
            }

            EditorGUILayout.EndHorizontal();

            if (_groupFoldouts[g])
            {
                EditorGUI.indentLevel++;

                group.groupName = EditorGUILayout.TextField("Nom du groupe", group.groupName);
                EditorGUILayout.Space(4);

                for (int c = 0; c < group.categories.Count; c++)
                {
                    AssetCategory cat = group.categories[c];

                    // ── Niveau 2 : Catégorie ──────────────────────────────
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    EditorGUILayout.BeginHorizontal();

                    _categoryFoldouts[g][c] = EditorGUILayout.Foldout(
                        _categoryFoldouts[g][c],
                        string.IsNullOrEmpty(cat.categoryName) ? $"Catégorie {c + 1}" : cat.categoryName,
                        true,
                        EditorStyles.foldoutHeader
                    );

                    if (GUILayout.Button("✕", redBtn, GUILayout.Width(24)))
                    {
                        group.categories.RemoveAt(c);
                        _categoryFoldouts[g].RemoveAt(c);
                        EditorUtility.SetDirty(target);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();

                    if (_categoryFoldouts[g][c])
                    {
                        EditorGUI.indentLevel++;

                        cat.categoryName = EditorGUILayout.TextField("Nom de catégorie", cat.categoryName);
                        cat.categoryMaterial = (Material)EditorGUILayout.ObjectField(
                            "Matériau", cat.categoryMaterial, typeof(Material), false);

                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Assets 3D", EditorStyles.boldLabel);

                        // ── Niveau 3 : Assets ─────────────────────────────
                        for (int a = 0; a < cat.assets.Count; a++)
                        {
                            Asset3DEntry asset = cat.assets[a];

                            EditorGUILayout.BeginHorizontal();

                            GUI.color = asset.ignore ? new Color(1f, 0.4f, 0.4f) : Color.white;
                            asset.ignore = EditorGUILayout.Toggle(asset.ignore, GUILayout.Width(16));
                            GUI.color = Color.white;

                            EditorGUI.BeginDisabledGroup(asset.ignore);
                            asset.assetName = EditorGUILayout.TextField(asset.assetName, GUILayout.Width(110));
                            asset.sceneObject = (GameObject)EditorGUILayout.ObjectField(
                                asset.sceneObject, typeof(GameObject), true);
                            EditorGUI.EndDisabledGroup();

                            if (GUILayout.Button("−", GUILayout.Width(20)))
                            {
                                cat.assets.RemoveAt(a);
                                EditorUtility.SetDirty(target);
                                break;
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        if (GUILayout.Button("+ Ajouter un asset"))
                        {
                            cat.assets.Add(new Asset3DEntry { assetName = "NouvelAsset" });
                            EditorUtility.SetDirty(target);
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                if (GUILayout.Button("＋ Nouvelle catégorie"))
                {
                    group.categories.Add(new AssetCategory { categoryName = "NouvelleCategorie" });
                    _categoryFoldouts[g].Add(true);
                    EditorUtility.SetDirty(target);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("＋ Nouveau groupe", GUILayout.Height(30)))
        {
            manager.groups.Add(new AssetGroup { groupName = "NouveauGroupe" });
            _groupFoldouts.Add(true);
            _categoryFoldouts.Add(new List<bool>());
            EditorUtility.SetDirty(target);
        }
    }
}
#endif