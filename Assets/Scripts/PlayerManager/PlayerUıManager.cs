using System.Collections;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerUıManager : NetworkBehaviour
{
    [Header("UI Panelleri")]
    public GameObject scoreboardPanel;
    public GameObject odulPanel;

    [Header("Metinler")]
    public TMP_Text skorListeText;
    public TMP_Text sureText;
    public TextMeshProUGUI odulText;
    public TextMeshProUGUI liderText;

    [Header("Başlangıç Efektleri")]
    public TextMeshProUGUI baslangicText; 
    public AudioClip baslangicSesi;

    private void Start()
    {
        if(scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if(odulPanel != null) odulPanel.SetActive(false);

        // Skor tablosunu Update içinde her karede güncellemek yerine, performansı korumak için saniyede bir kez çalıştırır.
        InvokeRepeating(nameof(SkorTablosuGuncellle), 0.5f, 1f);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            StartCoroutine(BaslangicEkraniRoutine());
        }
    }
    private IEnumerator BaslangicEkraniRoutine()
    {
        // Scene yüklendikten sonra 5 saniye bekle
        yield return new WaitForSeconds(6f);

        // Ekrana yazıyı getir
        if (baslangicText != null) baslangicText.gameObject.SetActive(true);

        if (baslangicSesi != null)
        {
            AudioSource.PlayClipAtPoint(baslangicSesi, transform.position);
        }

        yield return new WaitForSeconds(2f);

        // Süre dolduğunda yazıyı ekrandan yok et
        if (baslangicText != null) baslangicText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // UI işlemleri sadece karakterin kendi sahibinde çalışmalıdır.
        if (!IsOwner) return;

        // MatchManager üzerinden ağda senkronize edilen süreyi alıp UI üzerinde formatlayarak gösterir.
        if (MatchManager._instance != null && sureText != null)
        {
            float sure = MatchManager._instance.kalansure.Value;
            if (sure < 0) sure = 0; 
           
            int dakika = Mathf.FloorToInt(sure / 60);
            int saniye = Mathf.FloorToInt(sure % 60);
         
            sureText.text = string.Format("{0:00}:{1:00}", dakika, saniye);
        }

        // Maç bittiyse skor tablosu işlemleri ve diğer UI girdileri kilitlenir.
        if (MatchManager._instance != null && MatchManager._instance.macBitti.Value) return;

        // Oyuncu 'Tab' tuşuna basılı tuttuğu sürece skor tablosunu aktif eder.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            scoreboardPanel.SetActive(true);
            SkorTablosuGuncellle(); 
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            scoreboardPanel.SetActive(false);
        }
    }
    private void SkorTablosuGuncellle()
    {
        // Sahadaki tüm oyuncuların skor referansları toplanır ve LINQ kullanılarak kill sayısına göre büyükten küçüğe (Descending) sıralanır.
        PlayerScore[] tumOyunucular = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);

        tumOyunucular=tumOyunucular.OrderByDescending(n=>n.killSayisi.Value).ToArray();

        Debug.Log("Tablo güncelleniyor! Bulunan oyuncu sayısı: " + tumOyunucular.Length);

        string tabloIcerigi = "OYUNCU NICK\t\tKILL SAKIYI\n";
        tabloIcerigi += "--------------------------------------\n";

        //Sıralanmış dizinin ilk elemanı(en yüksek killi olan oyuncu) lider olarak belirlenir.
        if (tumOyunucular.Length > 0 && liderText != null)
        {           
            var liderOyuncu = tumOyunucular[0];

            if (liderOyuncu.killSayisi.Value > 0)
            {
                liderText.text = $" LİDER: {liderOyuncu.oyuncuNick.Value} - {liderOyuncu.killSayisi.Value}";
            }
            else
            {
                liderText.text = " LİDER: Henüz Kİmse Ölmedi!";
            }
        }

        // Web API'den çekilip ağ değişkenine yazılan gerçek oyuncu kullanıcı adları (oyuncuNick.Value) dinamik olarak tabloya işlenir.
        foreach (var p in tumOyunucular)
        {
            //OwnerClientId (Oyuncu 0) YERİNE, API'den gelip ağda senkronize olan gerçek nicki yazdırıyoruz!
            tabloIcerigi += $"{p.oyuncuNick.Value.ToString()} \t\t\t {p.killSayisi.Value}\n";
        }

        if (skorListeText != null)
        {
            skorListeText.text = tabloIcerigi;
        }
    }

    // Maç sona erdiğinde MatchManager tarafından tetiklenen final ekranı.
    public void OdulPaneliniAc(int kill, int coin)
    {
        if (!IsOwner) return;
        // Ödül ekranında butonlara tıklanabilmesi için fare imleci serbest bırakılır ve görünür yapılır.
        Cursor.lockState = CursorLockMode.None;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if (odulPanel != null) odulPanel.SetActive(true);

        if (odulText != null) odulText.text = $"Kill: {kill}\nKazanılan Coin: {coin}";

        // Maç bittiği için arka planda karakterin hareket etmesi veya fizik hesaplamaları engellenir.
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }
    }

    public void MenuyeDonButonu()
    {
        // Sunucu ve istemci ağ bağlantısı güvenli bir şekilde kapatılır ve ana menü sahnesine dönülür.
        NetworkManager.Singleton.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
