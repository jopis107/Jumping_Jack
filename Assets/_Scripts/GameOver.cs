using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI leaderboardScoreText;

    private void Start(){
        Time.timeScale = 1f;
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void StopGame(int score){
        if (leaderboardScoreText) leaderboardScoreText.text = score.ToString();
        if (gameOverPanel) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ReloadScene(){
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
