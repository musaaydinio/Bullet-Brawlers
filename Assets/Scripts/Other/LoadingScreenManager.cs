using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("Yükleme Paneli Ayarlarý")]
    public GameObject loadingPanel;
    public TextMeshProUGUI yuzdeText;
    public float beklemeSuresi = 5f; 

    private void Start()
    {
        // Sahne aktif olur olmaz yükleme animasyonunu baþlatýyoruz.
        StartCoroutine(LoadingEfekti());
    }

    private IEnumerator LoadingEfekti()
    {
        loadingPanel.SetActive(true);
        yuzdeText.text = "%0";

        float gecenSure = 0f;

        // Belirlediðimiz bekleme süresi dolana kadar döngüyü çalýþtýrýyoruz.
        while (gecenSure < beklemeSuresi)
        {
            gecenSure += Time.deltaTime;

            // Geçen süreyi toplam süreye bölüp 0 ile 1 arasýna sýkýþtýrýyoruz,
            // ardýndan 100 ile çarparak gerçek bir yüzde deðeri elde ediyoruz.
            float yuzde = Mathf.Clamp01(gecenSure / beklemeSuresi) * 100f;

            // Küsüratlý rakamlarý yuvarlayarak tam sayý olarak ekrana basýyoruz.
            yuzdeText.text = "%" + Mathf.RoundToInt(yuzde).ToString();

            yield return null;
        }
        // Süre bittiðinde sayýyý garanti olarak %100'e sabitliyoruz.
        yuzdeText.text = "%100";

        // Yükleme paneli pat diye kapanmasýn, oyuncu %100 yazýsýný kýsa bir an görsün diye yarým saniye daha bekletiyoruz.
        yield return new WaitForSeconds(0.5f);

        loadingPanel.SetActive(false);
    }
}
