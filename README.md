# Microservice AI-NPC (Prototip / Geliştirme Aşamasında)

Bu depo, üniversite bitirme projem için geliştirmekte olduğum sesli etkileşimli Yapay Zeka / NPC sisteminin prototipidir. 

Projenin temel amacı, tamamen yerel (offline) çalışan dil ve ses modellerini bir oyun motoruna entegre etmektir. Şu an için arka plan (backend) mimarisi kısmi mikroservis mantığıyla ayağa kaldırılmıştır; nihai hedef tüm modellerin bağımsız konteynerlerde çalıştığı tam modüler bir yapıdır. Unity (oyun) tarafındaki mekanikler ise geliştirilmeye devam etmektedir.

## Roadmap

- [x] Python Backend (Flask) köprüsünün kurulması
- [x] Dolphin Llama 3 (Ollama) ile NPC karar/diyalog motorunun entegrasyonu
- [x] Faster-Whisper ile Speech-to-Text (STT) entegrasyonu
- [x] Coqui XTTS v2 ile Text-to-Speech (TTS) ve anlık ses klonlama
- [x] Sistemin Docker Compose ile mikroservislere ayrılmaya başlanması (Ağır yük olan TTS'in bağımsız konteynere taşınması)
- [x] Docker üzerinde GPU Passthrough ve model önbellekleme (Volume) optimizasyonları
- [ ] **Whisper (STT) motorunun ana sunucudan koparılıp kendi bağımsız mikroservisine taşınması**
- [ ] **Ana Python sunucusunun sadece bir API Gateway (Trafik Yöneticisi) olarak yapılandırılması**
- [ ] Unity tarafında oyun döngüsünün ve mekaniklerin tamamlanması
- [ ] NPC animasyonları ve eylem (action) sisteminin oyuna yedirilmesi

## Sistem Mimarisi (Faz 1)

Sistem şu an itibariyle mikroservis mimarisine geçiş aşamasındadır:

1. **Unity İstemcisi:** Oyuncunun sesini kaydeder ve backend'e iletir.
2. **Ana Sunucu (API + STT + LLM):** Oyuncunun sesini metne çevirir, NPC'nin sistem promptuna ve hafızasına göre Dolphin-Llama 3'e yeni bir diyalog ve eylem kararı yazdırır. *(Gelecek fazda STT buradan ayrılacaktır).*
3. **TTS Mikroservisi:** Yükü ana sunucudan almak için **tamamen ayrı bir Docker konteynerinde** çalışır. Sadece üretilen diyaloğu alır ve referans sese göre klonlayarak Unity'ye ses dosyasını döndürür.

## Kurulum (Geliştirici Ortamı)

Sistemi NVIDIA GPU (CUDA) destekli bir makinede çalıştırmanız önerilir.

1. **Hazırlık:** Bilgisayarınıza **Docker Desktop** ve **Ollama** kurun.
2. **Modeli Çek:** Terminale `ollama run dolphin-llama3` yazarak NPC'nin beynini indirin. (İşlem bitince terminali kapatabilirsiniz, Ollama arkada çalışsın).
3. **Sistemi Başlat:** `Python_Backend` klasöründe terminal açıp `docker-compose up --build` komutunu girin.
   * *Not: İlk kurulumda modeller (4-5 GB) ineceği için biraz sürebilir, sonraki açılışlar saniyeler sürecektir.*
4. **Oyuna Gir:** Docker'da `Bütün Motorlar HAZIR` yazısını görünce Unity projesini açın ve Play tuşuna basın.
