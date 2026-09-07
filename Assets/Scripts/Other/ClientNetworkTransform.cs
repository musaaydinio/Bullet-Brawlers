using Unity.Netcode.Components;

public class ClientNetworkTransform : NetworkTransform
{
    // Netcode paketinin Server Authoritative devre dýþý býrakýp, 
    // hareket yetkisini tamamen istemciye devrederek multiplayer senkronizasyonunu saðlýyoruz.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}