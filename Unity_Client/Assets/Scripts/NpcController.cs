using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

[System.Serializable]
public class FlaskResponse {
    public string status;
    public string output; 
    public string message; // Python hata verdiyse buraya düşecek
}

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
    public string apiUrl = "[http://127.0.0.1:5000/chat](http://127.0.0.1:5000/chat)"; 

    public void SoruyuGonder() {
        if (!string.IsNullOrEmpty(inputField.text)) {
            StartCoroutine(PostRequest(apiUrl, inputField.text));
        }
    }

    IEnumerator PostRequest(string url, string message) {
        outputText.text = "Barney düşünüyor...";
        
        string jsonPayload = "{\"message\":\"" + message + "\"}";
        
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            FlaskResponse flaskRes = JsonUtility.FromJson<FlaskResponse>(request.downloadHandler.text);
            
            // Eğer Python "success" döndüyse işle:
            if (flaskRes.status == "success") {
                // Model markdown koyduysa (```json) onları temizle:
                string cleanJson = flaskRes.output.Replace("```json", "").Replace("```", "").Trim();
                
                try {
                    NpcData npcData = JsonUtility.FromJson<NpcData>(cleanJson);
                    outputText.text = npcData.diyalog + "\n<color=yellow>[Eylem: " + npcData.eylem + "]</color>";
                } catch (System.Exception e) {
                    // JSON bozuksa ekrana ham metni bas
                    outputText.text = "<color=red>Model JSON'ı bozdu!</color>\nGelen Ham Veri:\n" + cleanJson;
                }
            } else {
                // Python'dan hata döndüyse (Ollama kapalıysa vb.)
                outputText.text = "<color=red>Sunucu Hatası: </color>" + flaskRes.message;
            }
            
        } else {
            outputText.text = "<color=red>Bağlantı Hatası: " + request.error + "</color>";
        }
    }
}