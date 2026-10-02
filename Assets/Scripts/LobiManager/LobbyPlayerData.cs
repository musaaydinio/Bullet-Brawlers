using System;
using Unity.Netcode;
using Unity.Collections;

// Sahne geçiþleri arasýnda Relay katýlým kodunu (Join Code) bellekte tutan statik veri sýnýfý
public static class LobbyDataHolder
{
    public static string CurrentJoinCode = "";
}

// Netcode aðý üzerinde taþýnacak ve senkronize edilecek oyuncu verisi
// INetworkSerializable: Verinin að paketi olarak dönüþtürülebilmesini saðlar
// IEquatable: NetworkList içindeki deðiþikliklerin (delta) doðru karþýlaþtýrýlabilmesini saðlar
public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData>
{
    public ulong clientId;

    // Að üzerinden string gönderirken bellek boyutunu sabitlemek ve garbage collection yükünü azaltmak için FixedString32Bytes kullanýmý
    public FixedString32Bytes playerName;

    public bool isReady;
    public bool isHost;

    public LobbyPlayerData(ulong id, string name, bool ready, bool host)
    {
        clientId = id;
        playerName = name;
        isReady = ready;
        isHost = host;
    }

    // Aða veri yazma (Serialize) ve aðdan veri okuma (Deserialize) iþlemlerini yöneten serileþtirme metodu
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref isReady);
        serializer.SerializeValue(ref isHost);
    }

    // NetworkList üzerindeki veri deðiþimlerinin tespit edilmesi için eþitlik karþýlaþtýrmasý
    public bool Equals(LobbyPlayerData other)
    {
        return clientId == other.clientId &&
               playerName == other.playerName &&
               isReady == other.isReady &&
               isHost == other.isHost;
    }
}
