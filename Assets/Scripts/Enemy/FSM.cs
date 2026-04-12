using UnityEngine;

public class FSM : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Pursuit
    }

    public EnemyState currentState = EnemyState.Patrol;

    public void UpdateState(bool canSeePlayer)
    {
        switch (currentState)
        {
            case EnemyState.Patrol:

                if (canSeePlayer)
                {
                    currentState = EnemyState.Pursuit;
                    Debug.Log("Switch to Pursuit");
                }

                break;

            case EnemyState.Pursuit:

                if (!canSeePlayer)
                {
                    currentState = EnemyState.Patrol;
                    Debug.Log("Switch to Patrol");
                }

                break;
        }
    }
}
