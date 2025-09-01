using TMPro;
using UnityEngine;

public class ScoreUpdater : MonoBehaviour{
    [SerializeField] private TextMeshProUGUI scoreText;

    public void OnScoreUpdated(int score){
        if (scoreText) scoreText.text = score.ToString();
    }
}
