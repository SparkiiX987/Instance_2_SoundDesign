using DG.Tweening;
using UnityEngine;

/// <summary>
/// Robinet qui goutte : une goutte se forme sous le bec, tombe, et emet une
/// onde sonar a l'endroit ou elle touche.
///
/// Sert de repere de navigation. Chaque impact revele brievement l'evier et
/// ce qui l'entoure ; le joueur peut s'orienter vers ce rythme regulier.
///
/// La goutte part d'une sphere. Pendant qu'elle grossit sous le bec, elle se
/// deforme en vraie goutte, base ronde et sommet effile accroche au robinet,
/// puis s'etire en tombant et s'ecrase a l'impact.
///
/// A placer exactement au bout du bec : c'est de ce point que pend la goutte.
/// </summary>
public class WaterDripper : MonoBehaviour
{
    [Header("Accroche")]
    [Tooltip("Cherche le plafond au-dessus du point et y accroche la goutte. Permet " +
             "de poser une fuite n'importe ou dans une piece, sans modeliser de robinet : " +
             "il suffit de placer l'objet quelque part entre le sol et le plafond.")]
    [SerializeField] private bool  attachToCeiling    = true;
    [SerializeField] private float maxCeilingDistance = 10f;

    [Header("Goutte")]
    [Tooltip("Objet utilise comme goutte, avec un MeshFilter centre ; une sphere convient. " +
             "Vide : une sphere est creee automatiquement. Son materiau doit etre OPAQUE : " +
             "le post-process ne dessine que ce qui ecrit dans la depth.")]
    [SerializeField] private GameObject dropPrefab;

    [Tooltip("Diametre de la goutte formee, en metres. Une vraie goutte fait 4 a 5 mm ; " +
             "il faut l'exagerer pour que la detection d'aretes la dessine.")]
    [SerializeField] private float dropSize = 0.05f;

    [Header("Rythme")]
    [Tooltip("Temps pendant lequel la goutte grossit sous le bec avant de tomber.")]
    [SerializeField] private float formDuration = 0.8f;

    [Tooltip("Pause entre l'impact d'une goutte et la formation de la suivante.")]
    [SerializeField] private float pauseBetweenDrops = 1.2f;

    [Tooltip("Variation aleatoire de la pause, en proportion. Un robinet qui goutte " +
             "n'est jamais metronomique, et une regularite parfaite sonne mecanique.")]
    [Range(0f, 0.5f)] [SerializeField] private float pauseJitter = 0.25f;

    [Header("Forme")]
    [Tooltip("Etirement du sommet de la goutte. 0 = la goutte reste une sphere.")]
    [Range(0f, 2f)] [SerializeField] private float elongation = 0.9f;

    [Tooltip("Largeur restante pres du sommet. Plus c'est bas, plus la pointe est fine.")]
    [Range(0.05f, 1f)] [SerializeField] private float tipWidth = 0.3f;

    [Header("Chute")]
    [Tooltip("9,81 pour une chute reelle. Une goutte qui tombe de 30 cm met un quart de " +
             "seconde : baisser la valeur si elle passe trop vite pour etre lue.")]
    [SerializeField] private float gravity = 9.81f;

    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float maxFallDistance = 5f;

    [Tooltip("Point d'impact impose. Utile quand la cible (un recipient sans " +
             "collider, par exemple) ne peut pas etre trouvee par un rayon : " +
             "deplacer ce point dans la Scene view pour viser exactement l'ouverture. " +
             "Laisser vide pour chercher le sol au rayon, comme avant.")]
    [SerializeField] private Transform impactPoint;

    [Header("Onde")]
    [Tooltip("Emetteur deplace au point d'impact et declenche a chaque goutte. " +
             "Sa portee, sa vitesse et sa teinte se reglent sur lui ; son declenchement " +
             "automatique et son cooldown sont coupes au demarrage.")]
    [SerializeField] private S_ToySonarEmitter impactEmitter;

    private Transform _drop;
    private Renderer  _renderer;
    private Mesh      _mesh;
    private Vector3[] _sphere;   // sommets source, ramenes sur une sphere de rayon 1
    private Vector3[] _work;
    private Sequence  _cycle;

    // ── Cycle de vie ─────────────────────────────────────────────────

    private void Awake()
    {
        if (!BuildDrop()) { enabled = false; return; }

        if (attachToCeiling) { transform.position = ResolveSpawn(); }

        if (impactEmitter != null)
        {
            impactEmitter.SetExternallyDriven();
        }
        else
        {
            Debug.LogWarning($"[WaterDripper] '{name}' n'a pas d'emetteur d'impact : " +
                             "la goutte tombera sans emettre d'onde.", this);
        }
    }

    private void OnEnable()
    {
        if (_drop != null) { StartCycle(); }
    }

    private void OnDisable()
    {
        _cycle?.Kill();
        if (_renderer != null) { _renderer.enabled = false; }
    }

    private void OnDestroy()
    {
        _cycle?.Kill();
        if (_drop != null) { Destroy(_drop.gameObject); }
        if (_mesh != null) { Destroy(_mesh); }
    }

    // ── Construction de la goutte ────────────────────────────────────

    private bool BuildDrop()
    {
        GameObject go = dropPrefab != null
            ? Instantiate(dropPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = $"Goutte ({name})";

        // La goutte ne doit ni arreter le rayon qui cherche le point d'impact,
        // ni pousser le joueur. On desactive avant de detruire : Destroy est
        // differe a la fin de la frame, trop tard pour le premier rayon.
        foreach (Collider c in go.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
            Destroy(c);
        }

        MeshFilter filter = go.GetComponentInChildren<MeshFilter>();
        _renderer = go.GetComponentInChildren<Renderer>();
        if (filter == null || filter.sharedMesh == null || _renderer == null)
        {
            Debug.LogError($"[WaterDripper] La goutte de '{name}' n'a pas de mesh affichable.", this);
            Destroy(go);
            return false;
        }

        // Copie du mesh : la deformation ne doit pas toucher l'asset partage.
        _mesh = Instantiate(filter.sharedMesh);
        _mesh.MarkDynamic();
        filter.mesh = _mesh;

        Vector3[] source = _mesh.vertices;
        float radius = 0f;
        foreach (Vector3 v in source) { radius = Mathf.Max(radius, v.magnitude); }
        radius = Mathf.Max(radius, 1e-5f);

        _sphere = new Vector3[source.Length];
        _work   = new Vector3[source.Length];
        for (int i = 0; i < source.Length; i++) { _sphere[i] = source[i] / radius; }

        _drop = go.transform;
        _renderer.enabled = false;
        return true;
    }

    /// <summary>
    /// Deforme la sphere en goutte. La moitie basse reste ronde ; la moitie
    /// haute s'effile vers une pointe en s'etirant vers le haut.
    /// Le mesh resultant a un rayon de 0,5, comme la sphere primitive d'Unity :
    /// une echelle de dropSize donne donc une goutte de dropSize de diametre.
    /// </summary>
    /// <param name="_morph">0 = sphere, 1 = goutte.</param>
    private void ApplyMorph(float _morph)
    {
        for (int i = 0; i < _sphere.Length; i++)
        {
            _work[i] = Deform(_sphere[i], _morph);
        }

        _mesh.vertices = _work;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    /// <summary>
    /// Position d'un point de la sphere unite une fois deforme en goutte,
    /// dans un repere de rayon 0,5. Sert au mesh et au gizmo : les deux ne
    /// peuvent donc pas diverger.
    /// </summary>
    private Vector3 Deform(Vector3 _n, float _morph)
    {
        float up = Mathf.Clamp01(_n.y);   // 0 sur la moitie basse, 1 au sommet

        float squeeze = Mathf.Lerp(1f, Mathf.Lerp(1f, tipWidth, up), _morph);
        float stretch = 1f + elongation * up * _morph;

        return new Vector3(_n.x * squeeze, _n.y * stretch, _n.z * squeeze) * 0.5f;
    }

    // ── Cycle d'une goutte ───────────────────────────────────────────

    private void StartCycle()
    {
        _cycle?.Kill();

        Vector3 spawn = transform.position;

        bool    hasGround;
        Vector3 impact;
        if (impactPoint != null)
        {
            // Cible imposee : pas de recherche de sol, la goutte y tombe
            // toujours, meme si rien ne se trouve sous le point d'accroche.
            hasGround = true;
            impact    = impactPoint.position;
        }
        else
        {
            // Le rayon part un peu sous le bec pour ne pas toucher le robinet.
            hasGround = Physics.Raycast(
                spawn + Vector3.down * dropSize * 2f, Vector3.down,
                out RaycastHit hit, maxFallDistance, groundMask, QueryTriggerInteraction.Ignore);
            impact = hasGround ? hit.point : spawn + Vector3.down * maxFallDistance;
        }

        // Positions de debut et de fin de chute, connues des maintenant : la
        // goutte formee pend sous le bec, la goutte etiree touche le sol par
        // sa base.
        const float fallStretch = 1.2f;
        float   topAtFull  = 0.5f * (1f + elongation) * dropSize;
        Vector3 hangCenter = spawn  + Vector3.down * topAtFull;
        Vector3 landCenter = impact + Vector3.up   * (0.5f * dropSize * fallStretch);

        // Chute libre : h = g t^2 / 2. Ease.InQuad suit exactement cette loi.
        float height   = Mathf.Max(hangCenter.y - landCenter.y, 0.001f);
        float fallTime = Mathf.Sqrt(2f * height / Mathf.Max(gravity, 0.01f));

        float pause = pauseBetweenDrops * (1f + Random.Range(-pauseJitter, pauseJitter));

        _cycle = DOTween.Sequence()
            .AppendInterval(Mathf.Max(pause, 0f))
            .AppendCallback(BeginForming)
            .Append(DOVirtual.Float(0f, 1f, Mathf.Max(formDuration, 0.01f), Form))

            // Chute, en s'etirant sous la vitesse.
            .Append(_drop.DOMove(landCenter, fallTime).SetEase(Ease.InQuad))
            .Join(_drop.DOScale(new Vector3(0.85f, fallStretch, 0.85f) * dropSize, fallTime)
                       .SetEase(Ease.InQuad))

            // Impact : l'onde part, la goutte s'ecrase puis disparait.
            .AppendCallback(() => OnImpact(impact, hasGround))
            .Append(_drop.DOScale(new Vector3(1.8f, 0.15f, 1.8f) * dropSize, 0.06f))
            .Join(_drop.DOMoveY(impact.y + 0.075f * dropSize, 0.06f))
            .Append(_drop.DOScale(Vector3.zero, 0.12f))
            .AppendCallback(() => _renderer.enabled = false)

            .OnComplete(StartCycle);
    }

    private void BeginForming()
    {
        ApplyMorph(0f);
        Form(0f);
        _renderer.enabled = true;
    }

    /// <summary>
    /// La goutte grossit sous le bec en restant accrochee par son sommet.
    /// Elle gonfle vite puis ralentit, et ne prend sa forme de goutte que vers
    /// la fin, quand elle devient assez lourde pour s'etirer.
    /// </summary>
    private void Form(float _t)
    {
        float grow  = 1f - (1f - _t) * (1f - _t);   // Ease.OutQuad
        float morph = _t * _t;                       // Ease.InQuad

        float scale = Mathf.Lerp(0.2f, 1f, grow) * dropSize;
        ApplyMorph(morph);

        _drop.localScale = Vector3.one * scale;

        // Sommet colle au bec : on recule le centre de la hauteur du sommet.
        float top = 0.5f * (1f + elongation * morph) * scale;
        _drop.position = transform.position + Vector3.down * top;
    }

    private void OnImpact(Vector3 _impact, bool _hasGround)
    {
        // Sans surface sous le bec, il n'y a rien a reveler.
        if (!_hasGround || impactEmitter == null) { return; }

        impactEmitter.transform.position = _impact;
        impactEmitter.TriggerWave();
    }

    /// <summary>
    /// Point d'ou pend la goutte : juste sous le plafond si l'accroche est
    /// active et qu'un plafond est trouve, sinon la position de l'objet.
    /// On redescend d'un centimetre pour que la goutte ne demarre pas dans
    /// le plafond, et que le rayon vers le sol n'en parte pas de l'interieur.
    /// </summary>
    private Vector3 ResolveSpawn()
    {
        Vector3 origin = transform.position;
        if (!attachToCeiling) { return origin; }

        bool hit = Physics.Raycast(origin, Vector3.up, out RaycastHit info,
            maxCeilingDistance, groundMask, QueryTriggerInteraction.Ignore);

        return hit ? info.point + Vector3.down * 0.01f : origin;
    }

    // ── Aide au placement ────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Vector3 spawn = ResolveSpawn();
        Gizmos.color = new Color(0.4f, 0.75f, 1f, 1f);
        if (spawn != transform.position) { Gizmos.DrawLine(transform.position, spawn); }
        Gizmos.DrawWireSphere(spawn + Vector3.down * dropSize * 0.5f, dropSize * 0.5f);

        Vector3 end;
        if (impactPoint != null)
        {
            // Cible imposee : pas de rayon, juste un trait direct vers elle.
            end = impactPoint.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(spawn, end);
            Gizmos.DrawWireSphere(end, dropSize);
            Gizmos.DrawWireSphere(end, dropSize * 3f);
        }
        else
        {
            bool hit = Physics.Raycast(
                spawn + Vector3.down * dropSize * 2f, Vector3.down,
                out RaycastHit info, maxFallDistance, groundMask, QueryTriggerInteraction.Ignore);

            Gizmos.color = hit ? Color.cyan : Color.red;
            end = hit ? info.point : spawn + Vector3.down * maxFallDistance;
            Gizmos.DrawLine(spawn, end);
            if (hit) { Gizmos.DrawWireSphere(end, dropSize); }
        }

        DrawDropOutline(spawn);
    }

    /// <summary>
    /// Silhouette de la goutte formee, pendue sous son point d'accroche, a sa
    /// taille reelle. Deux profils croises et la ceinture la plus large
    /// suffisent a lire le volume.
    /// </summary>
    private void DrawDropOutline(Vector3 _spawn)
    {
        const int segments = 48;

        // Meme placement que la fin de Form : le sommet colle au point d'accroche.
        float   top    = 0.5f * (1f + elongation) * dropSize;
        Vector3 center = _spawn + Vector3.down * top;

        Gizmos.color = new Color(0.55f, 0.85f, 1f, 1f);

        for (int plane = 0; plane < 3; plane++)
        {
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= segments; i++)
            {
                float   a = i / (float)segments * Mathf.PI * 2f;
                Vector3 n = plane switch
                {
                    0 => new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f),   // profil face
                    1 => new Vector3(0f, Mathf.Sin(a), Mathf.Cos(a)),   // profil cote
                    _ => new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)),   // ceinture
                };

                Vector3 point = center + Deform(n, 1f) * dropSize;
                if (i > 0) { Gizmos.DrawLine(prev, point); }
                prev = point;
            }
        }
    }
}
