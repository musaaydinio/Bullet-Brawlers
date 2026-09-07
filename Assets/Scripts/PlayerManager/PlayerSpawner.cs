using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Karakter Prefab'ý")]
    public GameObject playerPrefab;

    private void Start()
    {
        // Biri oyuna baðlandýðýnda SpawnPlayer metodunu tetikle
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayer;
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        // Karakter oluþturma iþlemi sadece sunucu (Server) tarafýnda yapýlmalýdýr.
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log("OYUNCU BAÐLANDI! Client ID: " + clientId); 

        Vector3 spawnPos = Vector3.zero;

        // Baðlanan oyuncunun kimliðine göre haritadaki baþlangýç pozisyonu belirlenir.
        if (clientId == 0) spawnPos = new Vector3(33.66f, 0f, 13.05f);
        else if (clientId == 1) spawnPos = new Vector3(-33f, 0.3f, -33f);

        Debug.Log("DOÐMA NOKTASI SEÇÝLDÝ: " + spawnPos);
        // Karakter sahnede oluþturulur ve að üzerinde ilgili istemciye sahipliði verilerek spawn edilir.
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}
