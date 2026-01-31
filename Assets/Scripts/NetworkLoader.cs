using UnityEngine;
using Unity.MLAgents.Policies;

public class NetworkLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DragonAgent dragonAgent;
    [SerializeField] private NeuralNetworkSaver networkSaver;

    [Header("Mode Control")]
    [SerializeField] private bool useHeuristicOnStart = false;
    [SerializeField] private bool showDebugInfo = true;

    private BehaviorParameters behaviorParams;
    private float lastReward;
    private int episodeCount;

    private void Awake()
    {
        if (dragonAgent == null || dragonAgent != null)
            dragonAgent = GetComponent<DragonAgent>();

        behaviorParams = GetComponent<BehaviorParameters>();
    }
    private void Start()
    {
      

        if (useHeuristicOnStart)
            SwitchToHeuristicMode();
        else
            SwitchToTrainingMode();

        Debug.Log("=== NETWORK LOADER INITIALIZED ===");
        Debug.Log($"Current mode: {behaviorParams.BehaviorType}");
        Debug.Log($"Model loaded: {(behaviorParams.Model != null ? behaviorParams.Model.name : "None")}");
    }

    private void Update()
    {
        if (showDebugInfo && Input.GetKeyDown(KeyCode.I))
        {
            DisplayInfo();
        }

        // Quick mode switching
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SwitchToHeuristicMode();
        }
        else if (Input.GetKeyDown(KeyCode.F2))
        {
            SwitchToTrainingMode();
        }
        else if (Input.GetKeyDown(KeyCode.F3))
        {
            SwitchToInferenceMode();
        }
    }

    public void SwitchToTrainingMode()
    {
        if (behaviorParams != null)
        {
            behaviorParams.BehaviorType = BehaviorType.Default;
            Debug.Log("=== SWITCHED TO TRAINING MODE ===");
            Debug.Log("The agent will now learn from rewards/penalties");
            Debug.Log("Training data will be sent to ML-Agents if connected");
        }
    }

    public void SwitchToInferenceMode()
    {
        if (behaviorParams != null)
        {
            if (behaviorParams.Model == null)
            {
                Debug.LogWarning("=== NO MODEL ASSIGNED ===");
                Debug.LogWarning("Please assign a trained .onnx model in Behavior Parameters!");
                Debug.LogWarning("See instructions for how to load trained models.");
                return;
            }

            behaviorParams.BehaviorType = BehaviorType.InferenceOnly;
            Debug.Log("=== SWITCHED TO INFERENCE MODE ===");
            Debug.Log($"Using model: {behaviorParams.Model.name}");
            Debug.Log("The agent will use the trained neural network");
        }
    }

    public void SwitchToHeuristicMode()
    {
        if (behaviorParams != null)
        {
            behaviorParams.BehaviorType = BehaviorType.HeuristicOnly;
            Debug.Log("=== SWITCHED TO MANUAL CONTROL MODE ===");
            Debug.Log("Controls:");
            Debug.Log("  Q - Left Wing Flap");
            Debug.Log("  E - Right Wing Flap");
            Debug.Log("  SPACE - Speed/Tail Movement");
            Debug.Log("  F1/F2/F3 - Switch modes");
        }
    }

    public void DisplayInfo()
    {
        Debug.Log("\n=== DRAGON AGENT STATUS ===");
        Debug.Log($"Mode: {behaviorParams.BehaviorType}");
        Debug.Log($"Model: {(behaviorParams.Model != null ? behaviorParams.Model.name : "None")}");
        Debug.Log($"Episodes completed: {episodeCount}");

        if (dragonAgent != null)
        {
            Debug.Log($"Cumulative Reward: {dragonAgent.GetCumulativeReward():F2}");
            Debug.Log($"Smoothness: {dragonAgent.GetSmoothness():F2}");
            Debug.Log($"Obstacle Hits: {dragonAgent.GetObstacleHits()}");
        }

        Debug.Log("\nKEYBOARD SHORTCUTS:");
        Debug.Log("  F1 - Manual Control");
        Debug.Log("  F2 - Training Mode");
        Debug.Log("  F3 - Inference Mode");
        Debug.Log("  I  - Show this info");
    }

    public string GetLoadedNetworkInfo()
    {
        if (behaviorParams != null && behaviorParams.Model != null)
        {
            return $"Model: {behaviorParams.Model.name}\nMode: {behaviorParams.BehaviorType}";
        }
        return "No model loaded";
    }

    public void LoadBestNetwork()
    {
        if (networkSaver == null)
        {
            networkSaver = FindFirstObjectByType<NeuralNetworkSaver>();
        }

        if (networkSaver != null)
        {
            TopNetworksData data = networkSaver.LoadTopNetworks();

            if (data.networks.Count > 0)
            {
                Debug.Log($"Best network found: {data.networks[0].networkName}");
                Debug.Log($"Success rate: {data.networks[0].successRate:P0}");
                Debug.Log("To use this network:");
                Debug.Log("1. Find the corresponding .onnx file in your training results");
                Debug.Log("2. Copy it to Assets/ML-Agents/Models/");
                Debug.Log("3. Assign it to Behavior Parameters > Model");
                Debug.Log("4. Click 'Switch to Inference Mode' button");
            }
            else
            {
                Debug.LogWarning("No network performance data found yet");
            }
        }
    }
}