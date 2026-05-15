FROM pytorch/pytorch:2.2.1-cuda12.1-cudnn8-runtime
ENV DEBIAN_FRONTEND=noninteractive
WORKDIR /app

# Sistem bağımlılıklarını baştan temiz kuralım
RUN apt-get update && apt-get install -y ffmpeg git && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .

# Kütüphane çakışmalarını önlemek için temiz kurulum
RUN pip install --upgrade pip
RUN pip install --no-cache-dir --default-timeout=1000 -r requirements.txt

COPY app.py .
EXPOSE 5001
CMD ["python", "app.py"]