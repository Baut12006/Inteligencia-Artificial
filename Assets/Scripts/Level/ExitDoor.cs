using UnityEngine;

public class ExitDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private bool isLocked = true;

    [Header("Visual Feedback")]
    [SerializeField] private Light doorLight;
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;


    public bool IsLocked => isLocked;

    void Start()
    {
        UpdateDoorVisuals();
    }

    public void LockDoor()
    {
        isLocked = true;
        UpdateDoorVisuals();
        Debug.Log("Door locked!");
    }

    public void UnlockDoor()
    {
        isLocked = false;
        UpdateDoorVisuals();

        Debug.Log("Door unlocked!");
    }

    void UpdateDoorVisuals()
    {
        if (doorLight != null)
        {
            doorLight.color = isLocked ? lockedColor : unlockedColor;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLocked)
        {
            Debug.Log("Door is locked! Defeat all enemies first.");
            return;
        }

        PlayerModel player = other.GetComponent<PlayerModel>();
        if (player != null && !player.IsDead)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerReachedExit();
            }
        }
    }
}