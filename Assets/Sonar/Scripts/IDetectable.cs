using UnityEngine;

/// <summary>
/// Interface a implementer sur tout objet detectable par le sonar.
/// Chaque objet gere lui-meme son son via OnProb().
/// </summary>
public interface IDetectable
{
    /// <summary>Retourne la position mondiale de l'objet.</summary>
    Vector3 GetPosition();

    /// <summary>Retourne un identifiant de categorie (ex: "enemy", "wall", "pickup").</summary>
    string GetDetectableTag();

    /// <summary>Indique si l'objet est actuellement actif/detectable.</summary>
    bool IsActive();

    /// <summary>
    /// Appele par le Sonar quand l'onde touche cet objet.
    /// normalizedProximity : 0 = loin, 1 = tres proche du joueur.
    /// </summary>
    void OnProb(float _normalizedProximity);
}
