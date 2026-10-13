using System.Collections;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Retour sonore et visuel quand le renard meurt et reapparait.
///
/// GameManager se contentait d'un print en console et d'une teleportation au
/// point de spawn. Pour un joueur aveugle, une teleportation sans retour est
/// indistinguable d'un deplacement normal : on ne sait ni qu'on est mort, ni
/// qu'on a reapparu, ni pourquoi le decor a change.
///
/// A deposer sur n'importe quel objet de la scene, typiquement celui qui
/// porte GameManager. Les deux evenements FMOD et le texte sont facultatifs :
/// laisser un champ vide desactive simplement ce retour-la.
/// </summary>
public class DeathFeedback : MonoBehaviour
{
    [Header("Sons FMOD")]
    [Tooltip("Joue a l'instant de la mort.")]
    [SerializeField] private EventReference deathSound;

    [Tooltip("Joue une fois le joueur revenu au point de depart.")]
    [SerializeField] private EventReference respawnSound;

    [Tooltip("Delai entre la mort et le son de reapparition.")]
    [SerializeField] private float respawnSoundDelay = 0.6f;

    [Header("Message a l'ecran")]
    [Tooltip("Laisser vide pour ne rien afficher.")]
    [SerializeField] private string deathMessage = "Piege. Retour au depart.";
    [SerializeField] private float  messageHold  = 1.4f;
    [SerializeField] private float  messageFade  = 0.6f;
    [SerializeField] private int    messageSize  = 44;
    [SerializeField] private Color  messageColor = Color.white;

    private CanvasGroup      _group;
    private TextMeshProUGUI  _text;
    private Coroutine        _routine;

    private void Start()
    {
        EventBus.Subscribe<OnDefeat>(HandleDefeat);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnDefeat>(HandleDefeat);
    }

    private void HandleDefeat(OnDefeat _defeat)
    {
        PlayOneShot(deathSound);
        ShowMessage(deathMessage);

        if (_routine != null) { StopCoroutine(_routine); }
        _routine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // Le son de reapparition doit arriver APRES celui de la mort, sinon
        // les deux se confondent en un seul bruit et l'information est perdue.
        if (respawnSoundDelay > 0f)
        {
            yield return new WaitForSeconds(respawnSoundDelay);
        }
        PlayOneShot(respawnSound);

        if (_group == null) { yield break; }

        yield return new WaitForSeconds(Mathf.Max(messageHold, 0f));

        float t = 0f;
        float d = Mathf.Max(messageFade, 0.01f);
        while (t < d)
        {
            t += Time.deltaTime;
            _group.alpha = 1f - (t / d);
            yield return null;
        }
        _group.alpha = 0f;
    }

    private void PlayOneShot(EventReference _event)
    {
        if (_event.IsNull) { return; }

        // Positionne sur le joueur : un event spatialise reste ainsi audible
        // au bon endroit, et un event 2D ignore simplement la position.
        Vector3 at = transform.position;
        GameManager manager = GameManager.instance;
        if (manager != null && manager.player != null)
        {
            at = manager.player.transform.position;
        }

        RuntimeManager.PlayOneShot(_event, at);
    }

    // ── Affichage ────────────────────────────────────────────────────

    private void ShowMessage(string _message)
    {
        if (string.IsNullOrEmpty(_message)) { return; }

        EnsureUI();
        _text.text    = _message;
        _group.alpha  = 1f;
    }

    /// <summary>
    /// Construit l'affichage au premier usage. Le faire par code evite d'avoir
    /// a creer un canvas et a le brancher dans chaque scene.
    /// </summary>
    private void EnsureUI()
    {
        if (_group != null) { return; }

        GameObject canvasGO = new GameObject("DeathFeedbackCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        // Au-dessus des menus, pour ne pas passer derriere l'ecran de pause.
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _group = canvasGO.AddComponent<CanvasGroup>();
        _group.alpha          = 0f;
        _group.interactable   = false;
        _group.blocksRaycasts = false;

        GameObject textGO = new GameObject("Message");
        textGO.transform.SetParent(canvasGO.transform, false);

        _text = textGO.AddComponent<TextMeshProUGUI>();
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontSize  = messageSize;
        _text.color     = messageColor;
        _text.raycastTarget = false;

        RectTransform rect = _text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -180f);
        rect.sizeDelta        = new Vector2(1200f, 200f);
    }
}
