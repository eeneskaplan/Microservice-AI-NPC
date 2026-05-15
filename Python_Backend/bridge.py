from flask import Flask, request, jsonify
import requests
import json
import os
import torch
from deep_translator import GoogleTranslator
from faster_whisper import WhisperModel

app = Flask(__name__)

OLLAMA_URL = "[http://host.docker.internal:11434/api/chat](http://host.docker.internal:11434/api/chat)"
MODEL_NAME = "dolphin-llama3"

device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"\n[SİSTEM] Başlatılıyor... Kullanılan Donanım: {device.upper()}", flush=True)

print("[SİSTEM] Whisper yükleniyor...", flush=True)
whisper_model = WhisperModel("base", device=device, compute_type="float16" if device=="cuda" else "int8")

print("[SİSTEM] Bütün Yapay Zeka Motorları HAZIR!\n", flush=True)

def hafizayi_yukle(dosya_adi):
    if os.path.exists(dosya_adi):
        with open(dosya_adi, "r", encoding="utf-8") as f:
            return json.load(f)
    return []

def hafizayi_kaydet(gecmis, dosya_adi):
    with open(dosya_adi, "w", encoding="utf-8") as f:
        json.dump(gecmis, f, ensure_ascii=False, indent=4)

@app.route("/chat", methods=["POST"])
def chat():
    data = request.form
    audio_file = request.files.get("ses_dosyasi")
    
    if audio_file:
        temp_audio_path = "temp_player_voice.wav"
        audio_file.save(temp_audio_path)
        segments, _ = whisper_model.transcribe(temp_audio_path, language="tr")
        user_message_tr = "".join([segment.text for segment in segments])
        print(f"[WHISPER DUYDU]: {user_message_tr}", flush=True)
    else:
        user_message_tr = ""

    # Eğer oyuncu mikrofona hiçbir şey söylemediyse, boşuna işlem yapma
    if not user_message_tr.strip():
        return jsonify({"diyalog": "Seni duyamadım?", "eylem": "idle", "ses_dosyasi": ""})

    npc_adi = data.get("npc_adi", "npc").lower().replace(" ", " ")
    npc_prompt_tr = data.get("system_prompt", "")
    world_context = data.get("world_context", "Etrafta olağandışı bir durum yok.")
    
    # Referans sesi alıyoruz, TTS sunucusuna atıyoruz
    istenen_ses = data.get("referans_ses", "varsayilan.wav") 
    hafiza_dosyasi = f"{npc_adi}_hafiza.json"
    
    try:
        translator_en = GoogleTranslator(source='tr', target='en')
        
        full_prompt_tr = f"{npc_prompt_tr}\n\n[GİZLİ SİSTEM BİLGİSİ - ŞU ANKİ DÜNYA DURUMU]: {world_context}"
        npc_prompt_en = translator_en.translate(full_prompt_tr)
        user_message_en = translator_en.translate(user_message_tr)
        
        sohbet_gecmisi = hafizayi_yukle(hafiza_dosyasi)
        sohbet_gecmisi.append({"role": "user", "content": user_message_en})
        
        if len(sohbet_gecmisi) > 10:
            sohbet_gecmisi = sohbet_gecmisi[-10:]

        messages_list = [
            {"role": "system", "content": npc_prompt_en + "\nIMPORTANT: Answer ONLY in valid JSON format: {\"dialogue\": \"...\", \"action\": \"...\"}"},
            {"role": "user", "content": "Hello"},
            {"role": "assistant", "content": "{\"dialogue\": \"What do you want?\", \"action\": \"idle\"}"}
        ]
        messages_list.extend(sohbet_gecmisi)

        payload = {
            "model": MODEL_NAME,
            "messages": messages_list,
            "format": "json",
            "stream": False,
            "options": {"temperature": 0.4, "num_ctx": 1024, "num_predict": 60}
        }

        print(f"[{npc_adi.upper()}] Ollama'ya bağlanılıyor...", flush=True)
        response = requests.post(OLLAMA_URL, json=payload)
        ai_raw_output = response.json().get("message", {}).get("content", "{}")
        
        sohbet_gecmisi.append({"role": "assistant", "content": ai_raw_output})
        hafizayi_kaydet(sohbet_gecmisi, hafiza_dosyasi)

        # OLLAMA MARKDOWN TEMİZLİĞİ (Çökme sebebi yüksek ihtimalle burasıydı)
        ai_raw_output = ai_raw_output.strip().replace("```json", "").replace("```", "")
        ai_data = json.loads(ai_raw_output)
        
        dialogue_en = ai_data.get("dialogue", "...")
        action_value = ai_data.get("action", "idle")
        
        dialogue_tr = GoogleTranslator(source='en', target='tr').translate(dialogue_en)
        
        if not dialogue_tr or dialogue_tr.strip() == "":
            dialogue_tr = "Hmm..."
            
        print(f"[{npc_adi.upper()}] Llama Cevabı Üretti, Seslendirme İçin TTS Konteynerine Gönderiliyor...", flush=True)
        
        tts_payload = {
            "text": dialogue_tr,
            "npc_adi": npc_adi,
            "referans_ses": istenen_ses
        }
        
        tts_response = requests.post("http://tts_sunucu:5001/generate_audio", json=tts_payload)
        
        if tts_response.status_code == 200:
            ses_yolu = tts_response.json().get("ses_dosyasi", "")
            print(f"[TTS BAŞARILI] Ses Dosyası Oluştu: {ses_yolu}", flush=True)
        else:
            print(f"[HATA] TTS Sunucusu cevap vermedi! (Kod: {tts_response.status_code})", flush=True)
            ses_yolu = ""
        
        print(f"[{npc_adi.upper()}] Soru: {user_message_tr}", flush=True)
        print(f"[{npc_adi.upper()}] Cevap: {dialogue_tr} | Eylem: {action_value}", flush=True)

        return jsonify({
            "diyalog": dialogue_tr,
            "eylem": action_value,
            "ses_dosyasi": ses_yolu
        })

    except Exception as e:
        hata_mesaji = str(e)
        print(f"[KRİTİK HATA] {hata_mesaji}", flush=True)
        # Unity ekranına "ses tellerim koptu" yerine asıl hatayı basıyoruz!
        return jsonify({"diyalog": f"Hata Çıktı: {hata_mesaji}", "eylem": "hata", "ses_dosyasi": ""})

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)