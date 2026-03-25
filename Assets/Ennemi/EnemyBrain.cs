using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyPerception))]
public class EnemyBrain : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private SO_EnemySettings settings;

    [Header("References")]
    [Tooltip("Vide = trouve automatiquement le GameObject tague 'Player'.")]
    [SerializeField] private Transform playerTransform;

   
    private enum State { Wander, Investigate, Stalk, Chase }

    private State          _state = State.Wander;
    private NavMeshAgent   _agent;
    private EnemyPerception _perception;

    
    private EnemyEcholocation _echolocation;
    private float _aggressionLevel;
    private float _aggressionTimer;

    
    private Vector3 _wanderTarget;
    private float   _wanderWaitTimer;
    private bool    _waitingAtPoint;

    
    private Vector3 _investigateTarget;
    private float   _investigateTimer;
    private bool    _investigateSearching;

    
    private Vector3 _lastKnownPlayerPos;
    private float   _lostPlayerTimer;
    
    private void Awake()
    {
        _agent         = GetComponent<NavMeshAgent>();
        _perception    = GetComponent<EnemyPerception>();
        _echolocation  = GetComponent<EnemyEcholocation>();

        
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                UnityEngine.Debug.Log("[EnemyBrain] Joueur trouve automatiquement.");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[EnemyBrain] Aucun GameObject tague 'Player' trouve !");
            }
        }
    }

    private void Start()
    {
        if (_echolocation != null)
        {
            _echolocation.OnPlayerDetectedByEcho += OnEchoDetectedPlayer;
        }
        SetState(State.Wander);
    }

    private void OnDestroy()
    {
        if (_echolocation != null)
        {
            _echolocation.OnPlayerDetectedByEcho -= OnEchoDetectedPlayer;
        }
    }

   
    private void OnEchoDetectedPlayer(Vector3 _playerPos)
    {
        _lastKnownPlayerPos = _playerPos;
        if (_state != State.Chase)
        {
            SetState(State.Chase);
        }
    }

    private void Update()
    {
        UpdateAggression();
        _perception.AggressionLevel = _aggressionLevel;

        switch (_state)
        {
            case State.Wander:      UpdateWander();      break;
            case State.Investigate: UpdateInvestigate(); break;
            case State.Stalk:       UpdateStalk();       break;
            case State.Chase:       UpdateChase();       break;
        }
    }

   

    private void UpdateAggression()
    {
        _aggressionTimer  += Time.deltaTime;
        _aggressionLevel   = Mathf.Clamp01(
            _aggressionTimer / settings.aggressionRampDuration);
    }

   
    private void UpdateWander()
    {
        
        if (_perception.CanSeePlayer)  { SetState(State.Chase);       return; }
        if (_perception.HeardSound)    { SetState(State.Investigate);  return; }

        if (_waitingAtPoint)
        {
            _wanderWaitTimer -= Time.deltaTime;
            if (_wanderWaitTimer <= 0f) { _waitingAtPoint = false; }
            return;
        }

     
        if (!_agent.pathPending && _agent.remainingDistance <= settings.wanderPointReachedDist)
        {
            _waitingAtPoint  = true;
            _wanderWaitTimer = Random.Range(settings.wanderWaitMin, settings.wanderWaitMax);
            return;
        }

       
        if (!_agent.hasPath || _agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            SetNewWanderPoint();
        }
    }

    private void SetNewWanderPoint()
    {
        
        Vector3 biasDir = Vector3.zero;
        if (playerTransform != null)
        {
            biasDir = (playerTransform.position - transform.position).normalized;
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
           
            Vector3 randDir  = Random.insideUnitSphere;
            randDir.y        = 0f;
            randDir          = Vector3.Lerp(randDir.normalized, biasDir, _aggressionLevel * 0.4f)
                               .normalized;
            Vector3 candidate = transform.position + randDir * Random.Range(3f, settings.wanderRadius);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _wanderTarget = hit.position;
                _agent.SetDestination(_wanderTarget);
                return;
            }
        }
    }

   

    private void UpdateInvestigate()
    {
        if (_perception.CanSeePlayer) { SetState(State.Chase); return; }

        _investigateTimer -= Time.deltaTime;
        if (_investigateTimer <= 0f)
        {
            _perception.ClearSoundMemory();
            SetState(State.Wander);
            return;
        }

        
        if (!_investigateSearching &&
            !_agent.pathPending &&
            _agent.remainingDistance <= settings.wanderPointReachedDist)
        {
            _investigateSearching = true;
        }

        if (_investigateSearching)
        {
            
            if (!_agent.hasPath || _agent.remainingDistance <= settings.wanderPointReachedDist)
            {
                Vector3 searchPoint = _investigateTarget
                    + Random.insideUnitSphere * settings.investigateSearchRadius;
                searchPoint.y = _investigateTarget.y;

                if (NavMesh.SamplePosition(searchPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    _agent.SetDestination(hit.position);
                }
            }
        }

       
        if (_perception.HeardSound)
        {
            _agent.SetDestination(_perception.LastSoundOrigin);
            _investigateTarget    = _perception.LastSoundOrigin;
            _investigateSearching = false;
            _investigateTimer     = settings.investigateSearchTime;
            _perception.ClearSoundMemory();
        }
    }

   

    private void UpdateStalk()
    {
     
        if (_perception.CanSeePlayer) { SetState(State.Chase); return; }
        if (_perception.HeardSound)   { SetState(State.Investigate); return; }

        _lostPlayerTimer -= Time.deltaTime;
        if (_lostPlayerTimer <= 0f)
        {
            SetState(State.Wander);
            return;
        }

       
        if (!_agent.pathPending &&
            _agent.remainingDistance <= settings.wanderPointReachedDist)
        {
           
            _lostPlayerTimer = Mathf.Min(_lostPlayerTimer, 2f);
        }
    }

   

    private void UpdateChase()
    {
        if (_perception.CanSeePlayer)
        {
            
            _lastKnownPlayerPos = playerTransform.position;
            _agent.SetDestination(_lastKnownPlayerPos);
        }
        else
        {
            
            SetState(State.Stalk);
        }
    }

    

    private void SetState(State _newState)
    {
        
        if (_state == State.Chase && _newState != State.Chase && _echolocation != null)
        {
            _echolocation.StopEcholocation();
        }

        _state = _newState;
        _perception.SetChaseMode(_newState == State.Chase);

        switch (_newState)
        {
            case State.Wander:
                _agent.speed        = settings.wanderSpeed
                    * Mathf.Lerp(1f, settings.aggressionSpeedMult, _aggressionLevel);
                _agent.acceleration = settings.acceleration;
                
                if (_echolocation != null) { _echolocation.StartEcholocation(settings.echoIntervalSearch); }
                SetNewWanderPoint();
                break;

            case State.Investigate:
                _agent.speed              = settings.investigateSpeed;
                _investigateTarget        = _perception.LastSoundOrigin;
                _investigateTimer         = settings.investigateSearchTime;
                _investigateSearching     = false;
                _agent.SetDestination(_investigateTarget);
                _perception.ClearSoundMemory();
                UnityEngine.Debug.Log("[EnemyBrain] → Investigate");
                break;

            case State.Stalk:
                _agent.speed        = settings.stalkSpeed;
                _lastKnownPlayerPos = playerTransform.position;
                _lostPlayerTimer    = settings.memoryDuration;
                _agent.SetDestination(_lastKnownPlayerPos);
                UnityEngine.Debug.Log("[EnemyBrain] → Stalk");
                break;

            case State.Chase:
                _agent.speed        = settings.chaseSpeed
                    * Mathf.Lerp(1f, settings.aggressionSpeedMult, _aggressionLevel);
                _agent.acceleration = settings.acceleration * 2f;
                if (_echolocation != null) { _echolocation.StartEcholocation(settings.echoIntervalChase); }
                UnityEngine.Debug.Log("[EnemyBrain] → Chase !");
                break;
        }
    }

    

    private void OnDrawGizmosSelected()
    {
        switch (_state)
        {
            case State.Wander:
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_wanderTarget, 0.3f);
                break;
            case State.Investigate:
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(_investigateTarget, 0.3f);
                break;
            case State.Stalk:
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(_lastKnownPlayerPos, 0.3f);
                break;
            case State.Chase:
                Gizmos.color = Color.red;
                if (playerTransform != null)
                    Gizmos.DrawLine(transform.position, playerTransform.position);
                break;
        }
    }
}
