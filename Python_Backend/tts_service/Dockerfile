FROM pytorch/pytorch:2.2.1-cuda12.1-cudnn8-runtime
ENV DEBIAN_FRONTEND=noninteractive
WORKDIR /app
COPY requirements.txt .

# Pip kendi kendini güncellemesi ve bağlantı kopmalarında 1000 saniye beklesim
RUN python -m pip install --upgrade pip
RUN pip install --no-cache-dir --default-timeout=1000 -r requirements.txt

COPY app.py .
EXPOSE 5001
CMD ["python", "app.py"]