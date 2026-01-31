using UnityEngine;
using Unity.MLAgents;
using UnityEngine.UI;
using TMPro;

public class TrainingUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DragonAgent dragonAgent;
    [SerializeField] private NeuralNetworkSaver networkSaver;
    [SerializeField] private NetworkLoader networkLoader;

    [Header("UI Elements")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI episodeText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private TMP_Dropdown modeDropdown;

    [Header("Stats Tracking")]
    private int episodeCount = 0;
    private float totalReward = 0f;
    private int successCount = 0;
    private float bestReward = float.MinValue;

    private StatsRecorder statsRecorder;

    private void Start()
    {
        if (dragonAgent == null)
        {
            dragonAgent = FindObjectOfType<DragonAgent>();
        }

        statsRecorder = Academy.Instance.StatsRecorder;

        // Setup UI
        SetupUI();

        // Subscribe to academy events
        Academy.Instance.OnEnvironmentReset += OnEnvironmentReset;
    }

    private void SetupUI()
    {
        if (uiCanvas == null)
        {
            GameObject canvasObj = new GameObject("TrainingUI");
            uiCanvas = canvasObj.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create UI elements if they don't exist
        if (statsText == null)
        {
            statsText = CreateTextElement("StatsText", new Vector2(10, -10), new Vector2(400, 200), TextAlignmentOptions.TopLeft);
        }

        if (saveButton == null)
        {
            saveButton = CreateButton("SaveButton", new Vector2(-10, -10), "Save Top Networks");
            saveButton.onClick.AddListener(() => {
                if (networkSaver != null)
                {
                    networkSaver.ManualSave();
                    Debug.Log("Networks saved manually!");
                }
            });
        }

        if (loadButton == null)
        {
            loadButton = CreateButton("LoadButton", new Vector2(-10, -60), "Load Best Network");
            loadButton.onClick.AddListener(() => {
                if (networkLoader != null)
                {
                    //networkLoader.LoadNetwork(1);
                }
            });
        }
    }

    private void Update()
    {
        UpdateStatsDisplay();
    }

    private void UpdateStatsDisplay()
    {
        if (statsText == null || dragonAgent == null) return;

        float avgReward = episodeCount > 0 ? totalReward / episodeCount : 0f;
        float successRate = episodeCount > 0 ? (float)successCount / episodeCount * 100f : 0f;

        string stats = $"<b>Training Statistics</b>\n\n";
        stats += $"Episodes: {episodeCount}\n";
        stats += $"Current Reward: {dragonAgent.GetCumulativeReward():F2}\n";
        stats += $"Average Reward: {avgReward:F2}\n";
        stats += $"Best Reward: {bestReward:F2}\n";
        stats += $"Success Rate: {successRate:F1}%\n";
        stats += $"Successes: {successCount}\n\n";

        // Add network info if loaded
        if (networkLoader != null)
        {
            stats += networkLoader.GetLoadedNetworkInfo();
        }

        statsText.text = stats;

        // Update progress slider if available
        if (progressSlider != null)
        {
            progressSlider.value = Mathf.Clamp01(successRate / 100f);
        }
    }

    private void OnEnvironmentReset()
    {
        episodeCount++;

        float episodeReward = dragonAgent.GetCumulativeReward();
        totalReward += episodeReward;

        if (episodeReward > bestReward)
        {
            bestReward = episodeReward;
        }

        // Check if episode was successful (high reward = success)
        if (episodeReward > 8f)
        {
            successCount++;
        }

        // Record stats for TensorBoard
        if (statsRecorder != null)
        {
            statsRecorder.Add("Environment/Episode Reward", episodeReward);
            statsRecorder.Add("Environment/Success Rate", episodeCount > 0 ? (float)successCount / episodeCount : 0f);
        }
    }

    private TextMeshProUGUI CreateTextElement(string name, Vector2 anchorPosition, Vector2 sizeDelta, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(uiCanvas.transform, false);

        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = anchorPosition;
        rectTransform.sizeDelta = sizeDelta;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 16;
        text.color = Color.white;
        text.alignment = alignment;

        return text;
    }

    private Button CreateButton(string name, Vector2 anchorPosition, string buttonText)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(uiCanvas.transform, false);

        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = anchorPosition;
        rectTransform.sizeDelta = new Vector2(200, 40);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        Button button = buttonObj.AddComponent<Button>();

        // Create button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = buttonText;
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return button;
    }

    private void OnDestroy()
    {
        Academy.Instance.OnEnvironmentReset -= OnEnvironmentReset;
    }
}