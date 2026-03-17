using UnityEngine;

[CreateAssetMenu(fileName = "SO_EnemySettings", menuName = "Enemy/SO_EnemySettings")]
public class SO_EnemySettings : ScriptableObject
{
    [Header("Deplacement")]
    public float wanderSpeed    = 1.5f;
    public float investigateSpeed = 2.5f;
    public float chaseSpeed     = 4.5f;
    public float stalkSpeed     = 2.0f;
    public float acceleration   = 3f;
    public float angularSpeed   = 120f;

    [Header("Wander")]
    public float wanderRadius       = 8f;
    public float wanderWaitMin      = 1f;
    public float wanderWaitMax      = 4f;
    public float wanderPointReachedDist = 0.5f;

    [Header("Perception — Ouie")]
    [Tooltip("Rayon d'ecoute de base. Multiplie par l'intensite du cri.")]
    public float hearingBaseRadius  = 20f;

    [Header("Perception — Vue courte (Wander/Investigate)")]
    public float shortVisionRange   = 3f;
    [Range(0f, 180f)]
    public float shortVisionAngle   = 60f;

    [Header("Perception — Vue longue (Chase)")]
    public float longVisionRange    = 15f;
    [Range(0f, 180f)]
    public float longVisionAngle    = 90f;

    [Header("Memoire")]
    [Tooltip("Temps avant d'oublier la derniere position du joueur.")]
    public float memoryDuration     = 6f;

    [Header("Investigate")]
    public float investigateSearchRadius  = 3f;
    public float investigateSearchTime    = 5f;

    [Header("Echolocalisation ennemi")]
    [Tooltip("Intervalle echo en secondes quand il cherche (Wander/Investigate).")]
    public float echoIntervalSearch = 5f;
    [Tooltip("Intervalle echo en secondes quand il chasse (Chase).")]
    public float echoIntervalChase  = 2f;

    [Header("Agressivite progressive")]
    [Tooltip("Temps en secondes pour atteindre l'agressivite maximale.")]
    public float aggressionRampDuration = 120f;
    [Tooltip("Multiplie le rayon d'ecoute au max de l'agressivite.")]
    public float aggressionHearingMult  = 2f;
    [Tooltip("Multiplie la vitesse de chase au max de l'agressivite.")]
    public float aggressionSpeedMult    = 1.5f;

    [Header("Obstacles")]
    public LayerMask obstacleMask;
    public LayerMask playerMask;
}
