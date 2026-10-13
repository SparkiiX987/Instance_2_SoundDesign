using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SonarTools
{
    /// <summary>
    /// Bascule la vision du jeu entre trois modes, pour pouvoir construire le
    /// niveau sans se battre contre le post-process.
    ///
    /// Le shader d'echolocalisation ecrase toute la couleur de l'ecran : il
    /// s'applique donc aussi a la vue Scene, ou l'on ne voit plus rien et ou
    /// poser un objet devient impossible. Plutot que d'aller decocher le
    /// renderer feature a la main dans PC_Renderer, on passe d'un mode a
    /// l'autre d'un clic.
    ///
    /// Les trois modes se construisent avec ce qui existe deja : l'etat actif
    /// du FullScreenPassRendererFeature, et _TrailFloor sur le materiau, qui
    /// fixe la luminosite minimale des aretes meme sans onde.
    /// </summary>
    public static class SonarVision
    {
        public enum Mode
        {
            /// <summary>Ce que voit le joueur : noir, seules les ondes revelent.</summary>
            Aveugle = 0,

            /// <summary>Aretes visibles en permanence, sans attendre un cri.</summary>
            Semi = 1,

            /// <summary>Post-process coupe : eclairage normal, pour le level design.</summary>
            Normale = 2,
        }

        public const string FeatureName = "FullScreenPassRendererFeature";
        private const string ShaderName = "Custom/StifledEdge_Sonar";

        private const string PrefMode = "SonarVision.Mode";
        private const string PrefSemi = "SonarVision.SemiAmount";

        private static readonly int ID_TrailFloor = Shader.PropertyToID("_TrailFloor");

        // ── Etat ─────────────────────────────────────────────────────

        public static Mode Current
        {
            get => (Mode)EditorPrefs.GetInt(PrefMode, (int)Mode.Aveugle);
            private set => EditorPrefs.SetInt(PrefMode, (int)value);
        }

        /// <summary>Luminosite des aretes en mode semi.</summary>
        public static float SemiAmount
        {
            get => Mathf.Clamp01(EditorPrefs.GetFloat(PrefSemi, 0.35f));
            set => EditorPrefs.SetFloat(PrefSemi, Mathf.Clamp01(value));
        }

        // ── Application ──────────────────────────────────────────────

        public static void Apply(Mode _mode)
        {
            Current = _mode;

            bool  passActive = _mode != Mode.Normale;
            float trailFloor = _mode == Mode.Semi ? SemiAmount : 0f;

            int touchedFeatures = 0;
            foreach (ScriptableRendererFeature feature in FindFeatures())
            {
                // SetActive ecrit dans le champ serialise du renderer : le
                // changement vit donc dans l'asset, pas seulement en memoire.
                // C'est voulu, sinon le mode sauterait au rechargement du
                // domaine, mais cela veut dire qu'il part aussi au commit.
                feature.SetActive(passActive);
                EditorUtility.SetDirty(feature);
                touchedFeatures++;
            }

            Material material = FindMaterial();
            if (material != null)
            {
                Undo.RecordObject(material, "Vision sonar");
                material.SetFloat(ID_TrailFloor, trailFloor);
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssets();

            // Rafraichit la vue Scene, qui ne se redessine pas toute seule
            // quand on modifie un asset sans toucher a la scene.
            SceneView.RepaintAll();

            if (touchedFeatures == 0)
            {
                Debug.LogWarning(
                    $"[Vision sonar] Aucun renderer feature nomme '{FeatureName}' trouve. " +
                    "Verifie le nom dans PC_Renderer.");
            }
            if (material == null)
            {
                Debug.LogWarning(
                    $"[Vision sonar] Aucun materiau utilisant le shader '{ShaderName}' trouve. " +
                    "Le mode Semi n'aura aucun effet.");
            }
        }

        // ── Recherche des assets ─────────────────────────────────────

        /// <summary>
        /// Tous les renderers du projet qui portent le pass, pas seulement
        /// PC_Renderer : si un jour un renderer mobile le reprend, les deux
        /// resteront coherents.
        /// </summary>
        public static List<ScriptableRendererFeature> FindFeatures()
        {
            List<ScriptableRendererFeature> found = new();

            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null) { continue; }

                foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                {
                    if (feature != null && feature.name == FeatureName) { found.Add(feature); }
                }
            }

            return found;
        }

        /// <summary>
        /// Cherche par nom de shader plutot que par nom de fichier : renommer
        /// le materiau ne cassera pas l'outil.
        /// </summary>
        public static Material FindMaterial()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null && mat.shader != null && mat.shader.name == ShaderName) { return mat; }
            }
            return null;
        }

        /// <summary>Etat reel, lu dans les assets et non dans les prefs.</summary>
        public static bool IsPassActive()
        {
            List<ScriptableRendererFeature> features = FindFeatures();
            return features.Count > 0 && features[0].isActive;
        }

        // ── Raccourcis ───────────────────────────────────────────────

        [MenuItem("Tools/Vision sonar/Aveugle (vue du joueur) _F1", priority = 0)]
        private static void MenuAveugle() => Apply(Mode.Aveugle);

        [MenuItem("Tools/Vision sonar/Semi (aretes visibles) _F2", priority = 1)]
        private static void MenuSemi() => Apply(Mode.Semi);

        [MenuItem("Tools/Vision sonar/Normale (level design) _F3", priority = 2)]
        private static void MenuNormale() => Apply(Mode.Normale);

        [MenuItem("Tools/Vision sonar/Aveugle (vue du joueur) _F1", validate = true)]
        private static bool CheckAveugle()
        {
            Menu.SetChecked("Tools/Vision sonar/Aveugle (vue du joueur) _F1", Current == Mode.Aveugle);
            return true;
        }

        [MenuItem("Tools/Vision sonar/Semi (aretes visibles) _F2", validate = true)]
        private static bool CheckSemi()
        {
            Menu.SetChecked("Tools/Vision sonar/Semi (aretes visibles) _F2", Current == Mode.Semi);
            return true;
        }

        [MenuItem("Tools/Vision sonar/Normale (level design) _F3", validate = true)]
        private static bool CheckNormale()
        {
            Menu.SetChecked("Tools/Vision sonar/Normale (level design) _F3", Current == Mode.Normale);
            return true;
        }
    }
}
