using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class NPCAI : MonoBehaviour
{
    [Header("NPC Kimliği ve Kuralları")]
    public string npcAdi = "Hamdi";
    [TextArea(10, 20)]
    public string npcKisiligi = "Sen huysuz bir bakkalsın. Çok kısa cevaplar ver.";
    
    [Tooltip("ses_referanslari klasöründeki ses dosyasının adı (Örn: hamdi_ses.wav)")]
    public string referansSesi = "varsayilan.wav"; 

    [Header("Arayüz (UI) Bağlantıları")]
    public GameObject sohbetArayuzu; 
    public GameObject etkilesimMetni; 
    public TextMeshProUGUI npcCevapEkrani; 

    [Header("Ses Kayıt (V tuşuna basılı tut)")]
    public KeyCode mikrofonTusu = KeyCode.V;
    private AudioClip kaydedilenSes;
    private string aktifMikrofon;
    private bool kayitYapiyor = false;

    private AudioSource npcAudioSource;
    private bool oyuncuYakinmi = false;
    private bool sohbetAcikmi = false;

    [System.Serializable]
    public class NPCResponse { 
        public string diyalog; 
        public string eylem; 
        public string ses_dosyasi; 
    }

    void Start()
    {
        npcAudioSource = GetComponent<AudioSource>();
        if(sohbetArayuzu != null) sohbetArayuzu.SetActive(false);
        if(etkilesimMetni != null) etkilesimMetni.SetActive(false);

        if (Microphone.devices.Length > 0)
            aktifMikrofon = Microphone.devices[0];
        else
            Debug.LogError("Sistemde mikrofon bulunamadı!");
    }

    void Update()
    {
        if (Cursor.lockState == CursorLockMode.None && !sohbetAcikmi) return;

        if (oyuncuYakinmi && Input.GetKeyDown(KeyCode.T) && !sohbetAcikmi) SohbetiBaslat();
        if (sohbetAcikmi && Input.GetKeyDown(KeyCode.Escape)) SohbetiBitir();

        // SADECE MİKROFON DEVREDE
        if (sohbetAcikmi && aktifMikrofon != null)
        {
            if (Input.GetKeyDown(mikrofonTusu))
            {
                kaydedilenSes = Microphone.Start(aktifMikrofon, false, 15, 44100);
                kayitYapiyor = true;
                if (npcCevapEkrani != null) npcCevapEkrani.text = "[Dinliyor...]";
            }
            else if (Input.GetKeyUp(mikrofonTusu) && kayitYapiyor)
            {
                Microphone.End(aktifMikrofon);
                kayitYapiyor = false;
                if (npcCevapEkrani != null) npcCevapEkrani.text = npcAdi + " düşünüyor...";
                
                byte[] wavBytes = WavUtility.FromAudioClip(kaydedilenSes);
                StartCoroutine(MesajGonderSes(wavBytes));
            }
        }
    }

    IEnumerator MesajGonderSes(byte[] sesDosyasiWav)
    {
        string url = "http://127.0.0.1:5000/chat";
        
        WWWForm form = new WWWForm();
        form.AddField("npc_adi", npcAdi);
        form.AddField("system_prompt", npcKisiligi);
        form.AddField("referans_ses", referansSesi);
        form.AddBinaryData("ses_dosyasi", sesDosyasiWav, "oyuncu_mikrofon.wav", "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            IsleCevabi(request);
        }
    }

    private void IsleCevabi(UnityWebRequest request)
    {
        if (request.result == UnityWebRequest.Result.Success)
        {
            string gelenVeri = request.downloadHandler.text;
            NPCResponse cevap = JsonUtility.FromJson<NPCResponse>(gelenVeri);
            
            if (npcCevapEkrani != null) npcCevapEkrani.text = cevap.diyalog;
            
            if (!string.IsNullOrEmpty(cevap.ses_dosyasi))
            {
                StartCoroutine(SesiIndirVeCal(cevap.ses_dosyasi));
            }
        }
        else
        {
            if (npcCevapEkrani != null) npcCevapEkrani.text = "Sunucuya bağlanılamadı!";
        }
    }

   // Python'un ürettiği sesi oyun içine çekmek
    IEnumerator SesiIndirVeCal(string dosyaYolu)
    {
        // Python'un çalıştığı ana klasörün yolu yeni klasör yapısına göre güncellendi
        string pythonKlasoru = @"C:\Users\enesk\OneDrive\Desktop\Bitirme_YapayZeka_NPC\Python_Backend";
        
        // Klasör ile dosya adını birleştirip Unity'nin anlayacağı formata çeviriyorum
        string tamYol = System.IO.Path.Combine(pythonKlasoru, dosyaYolu).Replace("\\", "/");
        string dosyaUrl = "file:///" + tamYol;

        Debug.Log("Unity şu sesi çalmaya çalışıyor: " + dosyaUrl);

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(dosyaUrl, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                npcAudioSource.clip = clip;
                npcAudioSource.Play();
                Debug.Log("Ses başarıyla çalınıyor kanka!");
            }
            else
            {
                Debug.LogError("Ses dosyası okunamadı: " + www.error + " | Aranan Yol: " + dosyaUrl);
            }
        }
    }

    void SohbetiBaslat()
    {
        sohbetAcikmi = true;
        etkilesimMetni.SetActive(false);
        sohbetArayuzu.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void SohbetiBitir()
    {
        sohbetAcikmi = false;
        sohbetArayuzu.SetActive(false);
        if (oyuncuYakinmi) etkilesimMetni.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            oyuncuYakinmi = true;
            if (!sohbetAcikmi) etkilesimMetni.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            oyuncuYakinmi = false;
            etkilesimMetni.SetActive(false);
            if (sohbetAcikmi) SohbetiBitir(); 
        }
    }
}