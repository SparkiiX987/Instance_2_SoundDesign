using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// A deposer sur un bouton qui doit ramener au menu principal.
///
/// Le bouton « Main menu » de l'ecran de credits de fin n'a pas ete ajoute
/// dans imageEnd.prefab mais directement dans la scene, par-dessus
/// l'instance du prefab. Son onClick etait donc vide et cliquer dessus ne
/// faisait rien. Ce composant se branche tout seul au demarrage : rien a
/// glisser dans l'inspecteur, et la liaison ne peut pas se perdre lors d'une
/// reserialisation de la scene.
/// </summary>
[RequireComponent(typeof(Button))]
public class ReturnToMainMenuButton : MonoBehaviour
{
    [Tooltip("Index du menu principal dans les Build Settings.")]
    [SerializeField] private int mainMenuSceneIndex = 0;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ReturnToMainMenu);
    }

    public void ReturnToMainMenu()
    {
        // L'ecran de fin s'affiche apres que GameManager a coupe les entrees
        // et verrouille le curseur : il faut le rendre avant de quitter, sinon
        // le menu principal est injouable a la souris.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}
