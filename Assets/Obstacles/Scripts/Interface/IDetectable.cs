using UnityEngine;

/// <summary>
/// Interface a implementer sur tout objet detectable par le radar.
/// </summary>
public interface IDetectable
{
    Vector3 GetPosition();
    string GetDetectableTag();
    bool IsActive();

    /// <summary>
    /// Appele par RadarSystem quand l'onde touche cet objet.
    /// normalizedProximity : 0 = loin, 1 = proche.
    /// </summary>
    void OnProb(float normalizedProximity);
}
