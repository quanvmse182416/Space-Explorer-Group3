using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Scoring : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string scoreFormat = "Score: {0}";
    [SerializeField] private bool animateScoreChanges = false;
    [SerializeField] private float animationDuration = 0.5f;

    private int displayedScore = 0;
    private int targetScore = 0;
    private float animationTimer = 0f;

    void Start()
    {
        // Get TextMeshProUGUI component if not assigned in inspector
        if (scoreText == null)
        {
            scoreText = GetComponent<TextMeshProUGUI>();
            if (scoreText == null)
            {
                Debug.LogError("No TextMeshProUGUI component found on " + gameObject.name);
            }
        }

        // Initialize with current score
        displayedScore = GameManager.Score;
        targetScore = displayedScore;
        UpdateScoreText();
    }

    void Update()
    {
        // Check if the score in GameManager has changed
        if (targetScore != GameManager.Score)
        {
            targetScore = GameManager.Score;
            
            if (animateScoreChanges)
            {
                // Start animation
                animationTimer = animationDuration;
            }
            else
            {
                // Update immediately
                displayedScore = targetScore;
                UpdateScoreText();
            }
        }

        // Handle score animation
        if (animateScoreChanges && animationTimer > 0)
        {
            animationTimer -= Time.deltaTime;
            
            if (animationTimer <= 0)
            {
                // Animation complete
                displayedScore = targetScore;
            }
            else
            {
                // Animate score counting up/down
                float progress = 1 - (animationTimer / animationDuration);
                displayedScore = Mathf.RoundToInt(Mathf.Lerp(displayedScore, targetScore, progress));
            }
            
            UpdateScoreText();
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, displayedScore);
        }
    }

    // Public method to refresh the score display manually if needed
    public void RefreshScore()
    {
        targetScore = GameManager.Score;
        displayedScore = targetScore;
        UpdateScoreText();
    }
}
