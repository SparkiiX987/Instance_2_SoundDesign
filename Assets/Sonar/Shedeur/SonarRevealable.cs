using UnityEngine;

/// <summary>
/// A placer sur chaque objet avec le shader SonarSurface.
/// Stocke localement le moment ou il a ete touche par l'onde.
/// Le fade est calcule dans le shader a partir de ce temps.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class SonarRevealable : MonoBehaviour
{
    private Renderer             _renderer;
    private MaterialPropertyBlock _mpb;

    private static readonly int ID_RevealTime  = Shader.PropertyToID("_RevealTime");
    private static readonly int ID_ERevealTime = Shader.PropertyToID("_ERevealTime");

    private float _revealTime  = -9999f;
    private float _eRevealTime = -9999f;
    private bool  _dirty       = false;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();
        PushMPB();
    }

    /// <summary>Appele quand l'onde du JOUEUR touche cet objet.</summary>
    public void RevealByPlayer()
    {
        _revealTime = Time.time;
        _dirty      = true;
    }

    /// <summary>Appele quand l'onde de l'ENNEMI touche cet objet.</summary>
    public void RevealByEnemy()
    {
        _eRevealTime = Time.time;
        _dirty       = true;
    }

    private void Update()
    {
        if (_dirty)
        {
            PushMPB();
            _dirty = false;
        }
    }

    private void PushMPB()
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(ID_RevealTime,  _revealTime);
        _mpb.SetFloat(ID_ERevealTime, _eRevealTime);
        _renderer.SetPropertyBlock(_mpb);
    }
}