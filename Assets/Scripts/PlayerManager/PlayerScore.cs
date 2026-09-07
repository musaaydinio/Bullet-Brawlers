using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class PlayerScore : NetworkBehaviour
{
    // Skor deðerini manipüle edebilecek istemci (Client) hilelerini önlemek için yazma yetkisi sadece sunucuya (Server) verilmiþtir.
    public NetworkVariable<int> killSayisi = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Að üzerinden string veri taþýmak maliyetli olduðu için Unity'nin optimize edilmiþ FixedString32Bytes yapýsý kullanýlmýþtýr.
    public NetworkVariable<FixedString32Bytes> oyuncuNick = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            // Ýstemci tarafýnda, giriþ ekranýndan (Web API login) alýnan ve hafýzaya (PlayerPrefs) kaydedilen kullanýcý adý çekilir.
            // Bulunamazsa istemcinin að üzerindeki benzersiz ID'si ile "Misafir_ID" atanýr.
            string API_Nick = PlayerPrefs.GetString("PlayerName", "Misafir_" + OwnerClientId);
            NickBelirleServerRpc(API_Nick);
        }
    }

    [ServerRpc]
    private void NickBelirleServerRpc(string nick)
    {
        // Gelen string FixedString yapýsýna dönüþtürülerek NetworkVariable'a iþlenir.
        oyuncuNick.Value = new FixedString32Bytes(nick);
    }

    // Skor artýrma talebi oyun içinden tetiklendiðinde çalýþýr.
    public void KillEkle()
    {
        if (IsServer)
        {           
            SkoruArtir();
        }
        else
        {     
            KillEkleServerRpc();
        }
    }

    // Mermiyi atan Client, merminin vurduðu hedefin objesine sahip olmasa bile ona skor ekleyebilmelidir.
    [ServerRpc(RequireOwnership = false)]
    private void KillEkleServerRpc()
    {
        SkoruArtir();
    }

    private void SkoruArtir()
    {
        killSayisi.Value++;

        if (MatchManager._instance!= null)
        {
            MatchManager._instance.KillSiniriKontorl(killSayisi.Value);
        }
    }
}