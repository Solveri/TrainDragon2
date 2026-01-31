using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class NetworkPerformanceData
{
    public string networkName;
    public float averageReward;
    public float successRate;
    public float averageTime;
    public float smoothness;
    public int totalEpisodes;
    public int successfulEpisodes;
    public string timestamp;
}

[System.Serializable]
public class TopNetworksData
{
    public List<NetworkPerformanceData> networks = new List<NetworkPerformanceData>();
}

public class NeuralNetworkSaver : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private string saveFolderName = "TrainedDragons";
    [SerializeField] private int topNetworksToSave = 10;

    [Header("Tracking")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float saveInterval = 300f;
    [SerializeField] private int minEpisodesBeforeSave = 10;

    private string savePath;
    private float lastSaveTime;
    private Dictionary<string, NetworkPerformanceData> networkPerformances = new Dictionary<string, NetworkPerformanceData>();
    private string currentSessionId;

    private void Start()
    {
        savePath = Path.Combine(Application.persistentDataPath, saveFolderName);
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        currentSessionId = $"Session_{System.DateTime.Now:yyyyMMdd_HHmmss}";

        Debug.Log($"=== NEURAL NETWORK SAVER INITIALIZED ===");
        Debug.Log($"Save path: {savePath}");
        Debug.Log($"Session ID: {currentSessionId}");
        Debug.Log($"Auto-save: {autoSave} (every {saveInterval}s)");

        lastSaveTime = Time.time;
        LoadPreviousNetworks();
    }

    private void Update()
    {
        if (autoSave && Time.time - lastSaveTime >= saveInterval)
        {
            SaveTopNetworks();
            lastSaveTime = Time.time;
        }
    }

    public void TrackNetworkPerformance(string networkId, float reward, bool success, float time, float smoothness)
    {
        if (!networkPerformances.ContainsKey(networkId))
        {
            networkPerformances[networkId] = new NetworkPerformanceData
            {
                networkName = networkId,
                averageReward = 0f,
                successRate = 0f,
                averageTime = 0f,
                smoothness = 0f,
                totalEpisodes = 0,
                successfulEpisodes = 0,
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        NetworkPerformanceData data = networkPerformances[networkId];

        int n = data.totalEpisodes;
        data.averageReward = (data.averageReward * n + reward) / (n + 1);
        data.averageTime = (data.averageTime * n + time) / (n + 1);
        data.smoothness = (data.smoothness * n + smoothness) / (n + 1);

        data.totalEpisodes++;
        if (success)
        {
            data.successfulEpisodes++;
        }
        data.successRate = (float)data.successfulEpisodes / data.totalEpisodes;
    }

    public void SaveTopNetworks()
    {
        if (networkPerformances.Count == 0)
        {
            Debug.LogWarning("No network data to save yet");
            return;
        }

        List<NetworkPerformanceData> sortedNetworks = new List<NetworkPerformanceData>(networkPerformances.Values);

        sortedNetworks.RemoveAll(n => n.totalEpisodes < minEpisodesBeforeSave);

        sortedNetworks.Sort((a, b) =>
        {
            float scoreA = CalculateOverallScore(a);
            float scoreB = CalculateOverallScore(b);
            return scoreB.CompareTo(scoreA);
        });

        int saveCount = Mathf.Min(topNetworksToSave, sortedNetworks.Count);
        TopNetworksData topNetworks = new TopNetworksData();

        for (int i = 0; i < saveCount; i++)
        {
            topNetworks.networks.Add(sortedNetworks[i]);
        }

        string jsonPath = Path.Combine(savePath, $"top_networks_{currentSessionId}.json");
        string json = JsonUtility.ToJson(topNetworks, true);
        File.WriteAllText(jsonPath, json);

        string latestPath = Path.Combine(savePath, "top_networks_latest.json");
        File.WriteAllText(latestPath, json);

        Debug.Log($"=== SAVED TOP {saveCount} NETWORKS ===");
        Debug.Log($"File: {jsonPath}");

        for (int i = 0; i < Mathf.Min(3, saveCount); i++)
        {
            var network = sortedNetworks[i];
            Debug.Log($"#{i + 1}: Score={CalculateOverallScore(network):F2}, " +
                      $"Success={network.successRate:P0}, " +
                      $"AvgReward={network.averageReward:F2}, " +
                      $"Episodes={network.totalEpisodes}");
        }
    }

    private float CalculateOverallScore(NetworkPerformanceData data)
    {
        float rewardScore = Mathf.Clamp(data.averageReward / 100f, 0f, 1f);
        float successScore = data.successRate;
        float timeScore = Mathf.Clamp01(1f - (data.averageTime / 60f));
        float smoothnessScore = Mathf.Clamp01(data.smoothness);

        return (rewardScore * 0.35f) +
               (successScore * 0.40f) +
               (timeScore * 0.15f) +
               (smoothnessScore * 0.10f);
    }

    public void ManualSave()
    {
        SaveTopNetworks();
    }

    public TopNetworksData LoadTopNetworks(string filename = "top_networks_latest.json")
    {
        string jsonPath = Path.Combine(savePath, filename);

        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            TopNetworksData data = JsonUtility.FromJson<TopNetworksData>(json);
            Debug.Log($"Loaded {data.networks.Count} network records from {filename}");
            return data;
        }

        Debug.LogWarning($"No saved data found at {jsonPath}");
        return new TopNetworksData();
    }

    private void LoadPreviousNetworks()
    {
        TopNetworksData previous = LoadTopNetworks();

        foreach (var network in previous.networks)
        {
            if (!networkPerformances.ContainsKey(network.networkName))
            {
                networkPerformances[network.networkName] = network;
            }
        }

        if (previous.networks.Count > 0)
        {
            Debug.Log($"Loaded {previous.networks.Count} previous network records");
        }
    }

    public string GetSavePath()
    {
        return savePath;
    }

    [ContextMenu("Display Top Networks")]
    public void DisplayTopNetworks()
    {
        TopNetworksData data = LoadTopNetworks();

        Debug.Log("=== TOP PERFORMING NETWORKS ===");
        for (int i = 0; i < data.networks.Count; i++)
        {
            NetworkPerformanceData network = data.networks[i];
            float score = CalculateOverallScore(network);

            Debug.Log($"\n#{i + 1} - {network.networkName}");
            Debug.Log($"  Overall Score: {score:F3}");
            Debug.Log($"  Avg Reward: {network.averageReward:F2}");
            Debug.Log($"  Success Rate: {network.successRate:P1} ({network.successfulEpisodes}/{network.totalEpisodes})");
            Debug.Log($"  Avg Time: {network.averageTime:F1}s");
            Debug.Log($"  Smoothness: {network.smoothness:F2}");
            Debug.Log($"  Timestamp: {network.timestamp}");
        }
    }

    [ContextMenu("Export Summary Report")]
    public void ExportSummaryReport()
    {
        TopNetworksData data = LoadTopNetworks();

        string reportPath = Path.Combine(savePath, $"report_{currentSessionId}.txt");

        using (StreamWriter writer = new StreamWriter(reportPath))
        {
            writer.WriteLine("=== DRAGON TRAINING SUMMARY REPORT ===");
            writer.WriteLine($"Generated: {System.DateTime.Now}");
            writer.WriteLine($"Session: {currentSessionId}");
            writer.WriteLine($"Total Networks Tracked: {networkPerformances.Count}");
            writer.WriteLine($"\n=== TOP {data.networks.Count} NETWORKS ===\n");

            for (int i = 0; i < data.networks.Count; i++)
            {
                NetworkPerformanceData network = data.networks[i];
                float score = CalculateOverallScore(network);

                writer.WriteLine($"Rank #{i + 1}");
                writer.WriteLine($"Network: {network.networkName}");
                writer.WriteLine($"Overall Score: {score:F3}");
                writer.WriteLine($"Average Reward: {network.averageReward:F2}");
                writer.WriteLine($"Success Rate: {network.successRate:P1}");
                writer.WriteLine($"Successful Episodes: {network.successfulEpisodes}/{network.totalEpisodes}");
                writer.WriteLine($"Average Completion Time: {network.averageTime:F1}s");
                writer.WriteLine($"Flight Smoothness: {network.smoothness:F2}");
                writer.WriteLine($"Trained: {network.timestamp}");
                writer.WriteLine();
            }
        }

        Debug.Log($"Summary report exported to: {reportPath}");
    }

    private void OnApplicationQuit()
    {
        if (networkPerformances.Count > 0)
        {
            SaveTopNetworks();
            Debug.Log("Auto-saved networks before quitting");
        }
    }
}