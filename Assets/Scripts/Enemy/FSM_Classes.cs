using UnityEditor.Search;
using UnityEngine;

public class FSM_Classes : MonoBehaviour
{
    public State CurrentState { get; private set; }

    private PatrolState patrolState;
    private PursuitState pursuitState;
    private SearchState searchState;
    private void Awake()
    {
        patrolState = new PatrolState(this);
        pursuitState = new PursuitState(this);

        CurrentState = patrolState;
    }

    public void UpdateState(bool canSeePlayer)
    {
        CurrentState.Update(canSeePlayer);
    }

    public void ChangeToPatrol()
    {
        ChangeState(patrolState);
    }

    public void ChangeToPursuit()
    {
        ChangeState(pursuitState);
    }
    public void ChangeToSearch()
    {
        ChangeState(searchState);
    }
    private void ChangeState(State newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public string GetStateName()
    {
        return CurrentState.GetType().Name;
    }
}

public abstract class State
{
    protected FSM_Classes fsm;

    public State(FSM_Classes fsm)
    {
        this.fsm = fsm;
    }

    public virtual void Enter() { }

    public virtual void Exit() { }

    public abstract void Update(bool canSeePlayer);
}

public class PatrolState : State
{
    public PatrolState(FSM_Classes fsm)
        : base(fsm) { }

    public override void Enter()
    {
        Debug.Log("Entering Patrol");
    }

    public override void Update(bool canSeePlayer)
    {
        if (canSeePlayer)
        {
            fsm.ChangeToPursuit();
        }
    }

    public override void Exit()
    {
        Debug.Log("Exiting Patrol");
    }
}

public class PursuitState : State
{
    public PursuitState(FSM_Classes fsm): base(fsm) { }

    public override void Enter()
    {
        Debug.Log("Entering Pursuit");
    }

    public override void Update(bool canSeePlayer)
    {
        if (!canSeePlayer)
        {
            fsm.ChangeToPatrol();
        }
    }

    public override void Exit()
    {
        Debug.Log("Exiting Pursuit");
    }
}

public class SearchState : State
{
    public SearchState(FSM_Classes fsm) : base(fsm) { }
    public override void Enter()
    {
        Debug.Log("Entering Search");
    }
    public override void Update(bool canSeePlayer)
    {
        
    }
    public override void Exit()
    {
        Debug.Log("Exiting Search");
    }
}
