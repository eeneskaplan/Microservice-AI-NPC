FROM pytorch/pytorch:2.2.1-cuda12.1-cudnn8-runtime

# Kurulumlarda soru sorup zaman kaybı yaşamamak için
ENV DEBIAN_FRONTEND=noninteractive

# Whisper için zorunlu olan 'ffmpeg' programını Linux'a kuruyoruz
RUN apt-get update && apt-get install -y \
    ffmpeg \
    git \
    && rm -rf /var/lib/apt/lists/*


WORKDIR /app

# Ön bellek optimizasyonu için sadece kütüphane listesini kopyalayip ve kurulacak 
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Şimdi tüm kodları içeri kopyala
COPY . .

# Flask'ın dışarıyla iletişimi için
EXPOSE 5000

# Uygulamayı başlatıyor
CMD ["python", "bridge.py"]