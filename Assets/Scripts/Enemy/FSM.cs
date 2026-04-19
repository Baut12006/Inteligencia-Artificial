using UnityEngine;

public class FSM : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Pursuit,
        Search,
        Alert,
    }

    public EnemyState currentState = EnemyState.Patrol;

    public void UpdateState(bool canSeePlayer, bool isSentry = false)
    {
        switch (currentState)
        {
            case EnemyState.Patrol:

                if (canSeePlayer)
                {
                    currentState = isSentry ? EnemyState.Alert : EnemyState.Pursuit;
                    Debug.Log("Switch to Pursuit");
                }

                break;

            case EnemyState.Pursuit:

                if (!canSeePlayer)
                {
                    currentState = EnemyState.Search;
                    Debug.Log("Switch to Search");
                }

                break;

            case EnemyState.Search:

                if (canSeePlayer)
                {
                    currentState = isSentry ? EnemyState.Alert : EnemyState.Pursuit;
                    Debug.Log("Switch to Pursuit");
                }

                break;

            case EnemyState.Alert:

                if (!canSeePlayer)
                {
                    currentState = EnemyState.Search;
                    Debug.Log("Switch to Alert");
                }

                break;

        }
    }
    
}
