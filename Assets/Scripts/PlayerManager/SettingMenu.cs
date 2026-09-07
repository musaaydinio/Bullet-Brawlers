using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;


public class SettingMenu : NetworkBehaviour
{
    [Header("UI Ayarlarý")]
    public GameObject settingsPanel;
    public string mainMenuSceneName = "MainMenu";

    private NetworkObject parentNetworkObject;

    private void Start()
    {
        // UI bileþeninin baðlý olduðu karakterin að referansý alýnýr
        parentNetworkObject = GetComponentInParent<NetworkObject>();
    }

    private void Update()
    {
        // Menü girdileri sadece karakterin yerel sahibi tarafýndan kontrol edilebilir.
        if (parentNetworkObject != null && !parentNetworkObject.IsOwner) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel.activeSelf)
            {
                PaneliKapat();
            }
            else
            {
                PaneliAc();
            }
        }
    }

    public void PaneliAc()
    {
        settingsPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PaneliKapat()
    {
        settingsPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
  
    public void MenuyeDon()
    {
        if (NetworkManager.Singleton != null)
        {
            // Mevcut að baðlantýsý sonlandýrýlýr.
            NetworkManager.Singleton.Shutdown();

            // Yeni bir oyuna girildiðinde senkronizasyon hatalarýný (bug, hayalet oyuncu) önlemek için 
            // eski NetworkManager objesi tamamen yok edilir.
            Destroy(NetworkManager.Singleton.gameObject);
        }

        // 3. Beklemeden anýnda Ana Menüye dön
        SceneManager.LoadScene(mainMenuSceneName);
    }
}