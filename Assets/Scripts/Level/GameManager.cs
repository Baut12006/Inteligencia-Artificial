using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    [Header("UI References")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject introPanel;

    [Header("Game Objects")]
    [SerializeField] private PlayerModel player;
    [SerializeField] private ExitDoor exitDoor;

    [Header("Game State")]
    private bool isGameOver = false;
    private int totalEnemies = 0;
    private int enemiesKilled = 0;
    private bool gameStarted = false;

    private static bool hasSeenIntro = false;

    public bool IsGameOver => isGameOver;
    public int EnemiesRemaining => totalEnemies - enemiesKilled;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (deathPanel != null)
            deathPanel.SetActive(false);

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (introPanel != null)
        {
            if (!hasSeenIntro)
            {
                introPanel.SetActive(true);
            }
            else
            {
                introPanel.SetActive(false);
            }
        }
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.GetComponent<PlayerModel>();
        }

        if (exitDoor == null)
        {
            exitDoor = FindAnyObjectByType<ExitDoor>();
        }

        CountTotalEnemies();

        if (exitDoor != null)
        {
            exitDoor.LockDoor();
        }

        if (introPanel != null && introPanel.activeSelf)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    void CountTotalEnemies()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        totalEnemies = enemies.Length;
        //Debug.Log($"Total enemies in level: {totalEnemies}");
    }

    public void StartGame()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }

        hasSeenIntro = true;
        Time.timeScale = 1f;
        gameStarted = true;

        //Debug.Log("Game started!");
    }

    public void OnPlayerDeath()
    {
        if (isGameOver) return;

        isGameOver = true;
        //Debug.Log("Player died! Game Over!");

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void OnEnemyKilled()
    {
        if (isGameOver) return;

        enemiesKilled++;
        //Debug.Log($"Enemy killed! {enemiesKilled}/{totalEnemies}");

        if (enemiesKilled >= totalEnemies)
        {
            OnAllEnemiesDefeated();
        }
    }

    void OnAllEnemiesDefeated()
    {
        //Debug.Log("All enemies defeated! Exit door unlocked!");

        if (exitDoor != null)
        {
            exitDoor.UnlockDoor();
        }
    }

    public void OnPlayerReachedExit()
    {
        if (isGameOver) return;

        isGameOver = true;
        //Debug.Log("Player reached exit! Victory!");

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f;
        hasSeenIntro = false;
        SceneManager.LoadScene(0);
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}