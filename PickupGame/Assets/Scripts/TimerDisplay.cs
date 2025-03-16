using UnityEngine;
using TMPro;

public class TimerDisplay : MonoBehaviour
{
    public float timeRemaining = 60f;
    public TextMeshProUGUI timerText;
    private bool timerRunning = true;

    public Canvas Gameover;

    void Update()
    {
        if (GameObject.FindGameObjectsWithTag("Car").Length == 0)
        {
            timerRunning = false;
        }

        if (timerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerText(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                timerRunning = false;
                UpdateTimerText(timeRemaining);
                Gameover.gameObject.SetActive(true);
            }
        }
    }

    void UpdateTimerText(float timeToDisplay)
    {
        int minutes = Mathf.FloorToInt(timeToDisplay / 60);
        int seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
