using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    // AudioClip'i byte dizisine (.wav formatına) dönüştüren fonksiyon
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // 1. WAV Dosyası Başlığı (Header)
                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(0); // Dosya boyutu için yer tutucu
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16); // Subchunk1Size (PCM için 16)
                writer.Write((ushort)1); // Ses Formatı (PCM için 1)
                writer.Write((ushort)clip.channels); // Kanal Sayısı
                writer.Write(clip.frequency); // Örnekleme Hızı (Sample Rate)
                writer.Write(clip.frequency * clip.channels * 2); // Byte Hızı
                writer.Write((ushort)(clip.channels * 2)); // Block Align
                writer.Write((ushort)16); // Bits Per Sample (16-bit)
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(0); // Veri boyutu için yer tutucu

                // 2. Ses Verisini Dönüştürme (Float'tan 16-bit PCM'e)
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                short[] intData = new short[samples.Length];
                byte[] bytesData = new byte[samples.Length * 2];
                int rescaleFactor = 32767; // Float'ı 16-bit tam sayıya genişlet

                for (int i = 0; i < samples.Length; i++)
                {
                    intData[i] = (short)(samples[i] * rescaleFactor);
                    byte[] byteArr = BitConverter.GetBytes(intData[i]);
                    byteArr.CopyTo(bytesData, i * 2);
                }

                writer.Write(bytesData);

                // 3. Dosya ve Veri Boyutlarını Güncelleme
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write((int)(stream.Length - 8)); // Toplam Dosya Boyutu
                writer.Seek(40, SeekOrigin.Begin);
                writer.Write((int)(stream.Length - 44)); // Sadece Veri Boyutu

                return stream.ToArray();
            }
        }
    }
}