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

    [Header("Game Objects")]
    [SerializeField] private PlayerModel player;
    [SerializeField] private ExitDoor exitDoor;

    [Header("Game State")]
    private bool isGameOver = false;
    private int totalEnemies = 0;
    private int enemiesKilled = 0;

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
    }

    void CountTotalEnemies()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        totalEnemies = enemies.Length;
        Debug.Log($"Total enemies in level: {totalEnemies}");
    }

    public void OnPlayerDeath()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("Player died! Game Over!");

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
        Debug.Log($"Enemy killed! {enemiesKilled}/{totalEnemies}");

        if (enemiesKilled >= totalEnemies)
        {
            OnAllEnemiesDefeated();
        }
    }

    void OnAllEnemiesDefeated()
    {
        Debug.Log("All enemies defeated! Exit door unlocked!");

        if (exitDoor != null)
        {
            exitDoor.UnlockDoor();
        }
    }

    public void OnPlayerReachedExit()
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log("Player reached exit! Victory!");

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

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("No more levels! Restarting from first level.");
            SceneManager.LoadScene(0);
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Quitting game...");
        Application.Quit();

    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
