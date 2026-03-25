using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialStep", menuName = "Tutorial/Step")]
public class TutorialStep : ScriptableObject
{
    public StepType stepType;

    [Tooltip("Indices des CanvasGroups à afficher pour cette étape")]
    public List<int> canvasGroupIndices = new();

    [Tooltip("Index du sprite principal (imagesList[0])")]
    public int spriteIndex;

    [Tooltip("Sprites supplémentaires : paires (imageIndex, spriteIndex)")]
    public List<Vector2Int> extraSprites = new();

    [Tooltip("Textes à remplir avec les touches bindées")]
    public List<TextSlotBinding> textSlots = new();


    public string inputAction;
}