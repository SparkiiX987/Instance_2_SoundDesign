using UnityEngine;

public class S_CinematicDebug : MonoBehaviour
{
    [Header("Debug")]
    public int CinematicIndex = 0;

    private CinematicManager cinematicSystem;

    private void Start()
    {
        cinematicSystem = FindObjectOfType<CinematicManager>();

        if (cinematicSystem == null)
        {
            Debug.LogError("S_CinematicDebug : aucun S_Cinematic trouvé dans la scène.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (cinematicSystem == null)
            {
                Debug.LogError("S_CinematicDebug : impossible de lancer la cinématique, S_Cinematic introuvable.");
                return;
            }

            Debug.Log("S_CinematicDebug : lancement de la cinématique d'index " + CinematicIndex);
            cinematicSystem.PlayCinematic(CinematicIndex);
        }
    }
}