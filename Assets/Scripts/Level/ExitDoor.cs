using UnityEngine;
using TMPro;

public class ExitDoor : MonoBehaviour
{
    [Header("Door Settings")]
    [SerializeField] private bool isLocked = true;

    [Header("Visual Feedback")]
    [SerializeField] private Light doorLight;
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;

    [Header("UI Feedback")]
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private float messageDuration = 2f;

    private float messageTimer = 0f;

    public bool IsLocked => isLocked;

    void Start()
    {
        UpdateDoorVisuals();

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;

            if (messageTimer <= 0f && warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
        }
    }

    public void LockDoor()
    {
        isLocked = true;
        UpdateDoorVisuals();
        //Debug.Log("Door locked!");
    }

    public void UnlockDoor()
    {
        isLocked = false;
        UpdateDoorVisuals();
        //Debug.Log("Door unlocked!");
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
        PlayerModel player = other.GetComponent<PlayerModel>();
        if (player == null || player.IsDead)
            return;

        if (isLocked)
        {
            ShowWarningMessage();
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerReachedExit();
            }
        }
    }

    void ShowWarningMessage()
    {
        if (warningText == null)
        {
            //Debug.Log("Door is locked! Defeat all enemies first.");
            return;
        }

        int enemiesRemaining = 0;
        if (GameManager.Instance != null)
        {
            enemiesRemaining = GameManager.Instance.EnemiesRemaining;
        }

        if (enemiesRemaining > 0)
        {
            warningText.text = $"¡Elimina a los {enemiesRemaining} enemigos restantes!";
        }
        else
        {
            warningText.text = "¡Puerta cerrada!";
        }

        warningText.gameObject.SetActive(true);
        messageTimer = messageDuration;

        //Debug.Log($"Door is locked! Defeat all enemies first. Enemies remaining: {enemiesRemaining}");
    }
}