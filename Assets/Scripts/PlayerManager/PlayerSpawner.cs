using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Karakter Prefab'ý")]
    public GameObject playerPrefab;

    [Header("Oyun Sahnesi Adý")]
    public string gameSceneName = "GameScene"; // Inspector'da sahne adýnla birebir ayný olmalý

    private void Awake()
    {
        // Unity'nin kendi garantili sahne eventine abone oluyoruz
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    // Sahne deðiþip GameScene yüklendiði AN burasý otomatik çalýþýr
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                Debug.Log("GameScene sahnesi yüklendi, baðlý tüm oyuncular doðuruluyor...");
                SpawnAllPlayers();
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // Oyun zaten oynanýrken sonradan biri katýlýrsa doður
        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            SpawnPlayer(clientId);
        }
    }

    private void SpawnAllPlayers()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // Eðer bu oyuncunun karakteri sahnede henüz yoksa doður
            if (client.PlayerObject == null)
            {
                SpawnPlayer(client.ClientId);
            }
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Vector3 spawnPos = Vector3.zero;

        if (clientId == 0) spawnPos = new Vector3(33.66f, 0f, 13.05f);
        else if (clientId == 1) spawnPos = new Vector3(-33f, 0.3f, -33f);
        else spawnPos = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));

        Debug.Log($"OYUNCU DOÐURULDU -> Client ID: {clientId} | Pozisyon: {spawnPos}");

        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}