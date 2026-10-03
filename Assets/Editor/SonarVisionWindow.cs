using UnityEditor;
using UnityEngine;

namespace SonarTools
{
    /// <summary>
    /// Fenetre de bascule de la vision. Tools > Vision sonar > Fenetre.
    /// </summary>
    public class SonarVisionWindow : EditorWindow
    {
        [MenuItem("Tools/Vision sonar/Fenetre", priority = 20)]
        public static void Open()
        {
            SonarVisionWindow window = GetWindow<SonarVisionWindow>("Vision sonar");
            window.minSize = new Vector2(260f, 240f);
            window.Show();
        }

        private void OnEnable()
        {
            // L'etat vit dans des assets que l'on peut modifier ailleurs :
            // on se reaffiche quand la selection change, faute de mieux.
            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            SonarVision.Mode current = SonarVision.Current;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Mode de vision", EditorStyles.boldLabel);
            EditorGUILayout.Space(2f);

            DrawModeButton(SonarVision.Mode.Aveugle, "Aveugle  (F1)",
                "Ce que voit le joueur : noir, seules les ondes revelent le decor.", current);

            DrawModeButton(SonarVision.Mode.Semi, "Semi  (F2)",
                "Aretes visibles en permanence, sans attendre un cri. " +
                "Pour se deplacer dans le niveau en gardant le rendu sonar.", current);

            DrawModeButton(SonarVision.Mode.Normale, "Normale  (F3)",
                "Post-process coupe : eclairage normal, pour poser le blocking.", current);

            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(current != SonarVision.Mode.Semi))
            {
                EditorGUI.BeginChangeCheck();
                float amount = EditorGUILayout.Slider(
                    new GUIContent("Intensite semi", "Luminosite des aretes toujours visibles."),
                    SonarVision.SemiAmount, 0f, 1f);
                if (EditorGUI.EndChangeCheck())
                {
                    SonarVision.SemiAmount = amount;
                    SonarVision.Apply(SonarVision.Mode.Semi);
                }
            }

            EditorGUILayout.Space(8f);

            // Le mode est ecrit dans PC_Renderer et dans le materiau : il
            // part donc au commit et dans le build. Un oubli en mode Normale
            // livrerait le jeu sans son echolocalisation.
            if (current != SonarVision.Mode.Aveugle)
            {
                EditorGUILayout.HelpBox(
                    "Mode de travail actif. Il est ecrit dans PC_Renderer et dans le " +
                    "materiau, donc il partira au commit et dans le build. Repasse en " +
                    "Aveugle avant de livrer.",
                    MessageType.Warning);

                if (GUILayout.Button("Repasser en Aveugle"))
                {
                    SonarVision.Apply(SonarVision.Mode.Aveugle);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Vision du joueur. Pret a livrer.", MessageType.Info);
            }

            EditorGUILayout.Space(6f);
            DrawDiagnostics();
        }

        private void DrawModeButton(SonarVision.Mode _mode, string _label, string _tooltip,
                                    SonarVision.Mode _current)
        {
            bool selected = _mode == _current;

            Color previous = GUI.backgroundColor;
            if (selected) { GUI.backgroundColor = new Color(0.45f, 0.75f, 1f); }

            if (GUILayout.Button(new GUIContent((selected ? "> " : "   ") + _label, _tooltip),
                                 GUILayout.Height(28f)))
            {
                SonarVision.Apply(_mode);
            }

            GUI.backgroundColor = previous;
        }

        private void DrawDiagnostics()
        {
            int      features = SonarVision.FindFeatures().Count;
            Material material = SonarVision.FindMaterial();

            if (features == 0)
            {
                EditorGUILayout.HelpBox(
                    $"Aucun renderer feature nomme '{SonarVision.FeatureName}'. " +
                    "Les boutons n'auront aucun effet.", MessageType.Error);
            }

            if (material == null)
            {
                EditorGUILayout.HelpBox(
                    "Materiau du shader sonar introuvable : le mode Semi sera sans effet.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("Materiau", material.name, EditorStyles.miniLabel);
            }

            EditorGUILayout.LabelField(
                "Pass", SonarVision.IsPassActive() ? "actif" : "coupe", EditorStyles.miniLabel);
        }
    }
}
