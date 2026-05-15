using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

[System.Serializable]
public class NpcData {
    public string diyalog;
    public string eylem;
}

public class NpcController : MonoBehaviour {
    [Header("UI Elemanları")]
    public TMP_InputField inputField;
    public TextMeshProUGUI outputText;
    
    [Header("API Ayarları")]
    // SADECE temiz linki buraya yazıyoruz
    public string apiUrl = "http://127.0.0.1:5000/chat"; 

    public void SoruyuGonder() {
        if (!string.IsNullOrEmpty(inputField.text)) {
            StartCoroutine(PostRequest(apiUrl, inputField.text));
        }
    }

    IEnumerator PostRequest(string url, string message) {
        outputText.text = "Barney düşünüyor...";
        
        // JSON verisini güvenli oluşturuyoruz
        string jsonPayload = "{\"message\":\"" + message + "\"}";
        
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            string json = request.downloadHandler.text;
            Debug.Log("Backend'den Gelen: " + json); // Console'u kontrol et!

            try {
                // Eğer model markdown (```json) eklediyse temizliyoruz
                string cleanJson = json.Replace("```json", "").Replace("```", "").Trim();
                NpcData npcData = JsonUtility.FromJson<NpcData>(cleanJson);
                
                outputText.text = npcData.diyalog + "\n<color=yellow>[Eylem: " + npcData.eylem + "]</color>";
            } catch (System.Exception e) {
                // JSON formatı hatalıysa ham metni bas ki ne olduğunu görelim
                outputText.text = "Gelen veri işlenemedi. Ham veri: " + json;
            }
        } else {
            outputText.text = "<color=red>Bağlantı Hatası: " + request.error + "</color>";
            Debug.LogError("Detaylı Hata: " + request.downloadHandler.text);
        }
    }
}