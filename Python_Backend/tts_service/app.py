from flask import Flask, request, jsonify
from TTS.api import TTS
import torch
import os

app = Flask(__name__)
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"[TTS SİSTEMİ] Başlatılıyor... Donanım: {device}")

tts = TTS("tts_models/multilingual/multi-dataset/xtts_v2", progress_bar=False).to(device)
print("[TTS SİSTEMİ] HAZIR!")

@app.route("/generate_audio", methods=["POST"])
def generate_audio():
    data = request.json
    text = data.get("text")
    npc_adi = data.get("npc_adi")
    istenen_ses = data.get("referans_ses", "varsayilan.wav")

    referans_yolu = os.path.join("ses_referanslari", istenen_ses)
    if not os.path.exists(referans_yolu):
        referans_yolu = os.path.join("ses_referanslari", "varsayilan.wav")

    ses_dosyasi_adi = f"{npc_adi}_ses.wav"
    ses_klasoru = "audio_output"
    os.makedirs(ses_klasoru, exist_ok=True)
    ses_yolu = os.path.join(ses_klasoru, ses_dosyasi_adi)

    # Sesi üret ve kaydet
    tts.tts_to_file(text=text, file_path=ses_yolu, speaker_wav=referans_yolu, language="tr")

    return jsonify({"status": "success", "ses_dosyasi": ses_yolu})

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5001) # 5001 portunda çalışacak