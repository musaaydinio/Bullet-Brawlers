using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Oyun sýrasýnda sunucuyla baðlantý koptuðunda veya host oyundan çýktýðýnda,
// oyuncunun ekranda kalmasýný engellemek için uyarý paneli açýp aðý güvenli þekilde kapatýyoruz.
public class DisconnectHandler : MonoBehaviour
{
    [Header("Að Koptuðunda Açýlacaklar")]
    public GameObject uyariPaneli;
    public GameObject yedekKamera;

    private void Start()
    {
        // Að yöneticisi üzerinden herhangi bir istemcinin baðlantý kopma durumunu dinlemeye baþlýyoruz.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        }
    }

    private void OnDisconnected(ulong clientId)
    {
        // Kopan baðlantý bizim yerel istemcimize aitse gerekli güvenlik önlemlerini alýyoruz.
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("Host oyundan çýktý! Uyarý paneli açýlýyor...");

            // Oyuncu menüde veya uyarý ekranýnda rahat iþlem yapabilsin diye fare imlecini serbest býrakýyoruz.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Arka plandaki aktif að oturumunu sonlandýrýyoruz.
            NetworkManager.Singleton.Shutdown();

            // Karakter objesi yok olacaðý için kamerasýz kalmamak adýna yedek kamerayý devreye sokuyoruz.
            if (yedekKamera != null)
            {
                yedekKamera.SetActive(true);
            }
            // Oyuncuya neden ana menüye döndüðünü veya baðlantýnýn koptuðunu belirten paneli açýyoruz.
            if (uyariPaneli != null)
            {
                uyariPaneli.SetActive(true);
            }
        }
    }

    public void AnaMenuyeDonButonu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}