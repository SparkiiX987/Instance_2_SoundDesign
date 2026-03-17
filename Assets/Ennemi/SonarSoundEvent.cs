using UnityEngine;

/// <summary>
/// Evenement emis par Sonar.cs quand le joueur crie.
/// EnemyPerception s'abonne via un event statique.
/// </summary>
public static class SonarSoundEvent
{
    /// <summary>
    /// Declenche quand le joueur emet une onde.
    /// Vector3 = position de l'emission, float = volume normalise [0..1].
    /// </summary>
    public static event System.Action<Vector3, float> OnSonarEmitted;

    /// <summary>
    /// Appele par Sonar.cs dans EmitWave().
    /// </summary>
    public static void Emit(Vector3 _origin, float _normalizedVolume)
    {
        OnSonarEmitted?.Invoke(_origin, _normalizedVolume);
    }
}
