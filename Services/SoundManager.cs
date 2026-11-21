using System;
using System.IO;
using System.Media;

namespace FlappyBird.Services
{
    /// <summary>
    /// Oyun ses efektlerini bellekte prosedürel olarak üreten ve çalan ses yöneticisi.
    /// Harici dosya bağımlılığı olmadan %100 taşınabilir 8-bit retro sesler sunar.
    /// </summary>
    public class SoundManager : IDisposable
    {
        public bool SoundEnabled { get; set; } = true;

        private SoundPlayer? _jumpPlayer;
        private SoundPlayer? _scorePlayer;
        private SoundPlayer? _hitPlayer;
        private SoundPlayer? _diePlayer;
        private SoundPlayer? _coinPlayer;
        private SoundPlayer? _recordPlayer;

        public SoundManager()
        {
            InitializeSounds();
        }

        private void InitializeSounds()
        {
            try
            {
                // 1. Zıplama sesi (Yükselen retro ton)
                byte[] jumpWav = GenerateWav(sampleRate: 22050, durationMs: 90, (t, dur) =>
                {
                    double freq = 400.0 + (t / dur) * 450.0;
                    return Math.Sin(2.0 * Math.PI * freq * t) * (1.0 - (t / dur) * 0.4);
                });
                _jumpPlayer = new SoundPlayer(new MemoryStream(jumpWav));
                _jumpPlayer.LoadAsync();

                // 2. Skor sesi (İki tonlu parlak boru geçme sesi)
                byte[] scoreWav = GenerateWav(sampleRate: 22050, durationMs: 140, (t, dur) =>
                {
                    double freq = t < 0.07 ? 988.0 : 1319.0;
                    double env = 1.0 - (t / dur) * 0.5;
                    return Math.Sin(2.0 * Math.PI * freq * t) * env;
                });
                _scorePlayer = new SoundPlayer(new MemoryStream(scoreWav));
                _scorePlayer.LoadAsync();

                // 3. Altın / Yıldız Toplama Sesi (Parlak sihirli çan tınısı)
                byte[] coinWav = GenerateWav(sampleRate: 22050, durationMs: 160, (t, dur) =>
                {
                    double freq = t < 0.08 ? 1568.0 : 2093.0; // G6 -> C7
                    double env = Math.Exp(-7.0 * (t / dur));
                    return (Math.Sin(2.0 * Math.PI * freq * t) + 0.3 * Math.Sin(4.0 * Math.PI * freq * t)) * env;
                });
                _coinPlayer = new SoundPlayer(new MemoryStream(coinWav));
                _coinPlayer.LoadAsync();

                // 4. Yeni En Yüksek Skor / Rekor Fanfarı (Yükselen 4 tonlu zafer melodisi C5-E5-G5-C6)
                byte[] recordWav = GenerateWav(sampleRate: 22050, durationMs: 420, (t, dur) =>
                {
                    double freq;
                    if (t < 0.10) freq = 523.25;       // C5
                    else if (t < 0.20) freq = 659.25;  // E5
                    else if (t < 0.30) freq = 783.99;  // G5
                    else freq = 1046.50;               // C6 Zirve tonu

                    double env = 1.0 - (t / dur) * 0.35;
                    return (Math.Sin(2.0 * Math.PI * freq * t) + 0.25 * Math.Sin(4.0 * Math.PI * freq * t)) * env;
                });
                _recordPlayer = new SoundPlayer(new MemoryStream(recordWav));
                _recordPlayer.LoadAsync();

                // 5. Çarpma sesi (Tok vuruş)
                byte[] hitWav = GenerateWav(sampleRate: 22050, durationMs: 120, (t, dur) =>
                {
                    double freq = 180.0 - (t / dur) * 110.0;
                    double env = Math.Exp(-12.0 * (t / dur));
                    return Math.Sin(2.0 * Math.PI * freq * t) * env;
                });
                _hitPlayer = new SoundPlayer(new MemoryStream(hitWav));
                _hitPlayer.LoadAsync();

                // 6. Düşüş / Game Over sesi (Düşen hüzünlü ton)
                byte[] dieWav = GenerateWav(sampleRate: 22050, durationMs: 250, (t, dur) =>
                {
                    double freq = 420.0 - (t / dur) * 310.0;
                    double env = 1.0 - (t / dur);
                    return Math.Sin(2.0 * Math.PI * freq * t) * env;
                });
                _diePlayer = new SoundPlayer(new MemoryStream(dieWav));
                _diePlayer.LoadAsync();
            }
            catch
            {
                // Ses sistemi başlatılamazsa sessiz modda devam eder
            }
        }

        public void PlayJump()
        {
            if (!SoundEnabled || _jumpPlayer == null) return;
            try { _jumpPlayer.Play(); } catch { }
        }

        public void PlayScore()
        {
            if (!SoundEnabled || _scorePlayer == null) return;
            try { _scorePlayer.Play(); } catch { }
        }

        public void PlayCoin()
        {
            if (!SoundEnabled || _coinPlayer == null) return;
            try { _coinPlayer.Play(); } catch { }
        }

        public void PlayRecordFanfare()
        {
            if (!SoundEnabled || _recordPlayer == null) return;
            try { _recordPlayer.Play(); } catch { }
        }

        public void PlayHit()
        {
            if (!SoundEnabled || _hitPlayer == null) return;
            try { _hitPlayer.Play(); } catch { }
        }

        public void PlayDie()
        {
            if (!SoundEnabled || _diePlayer == null) return;
            try { _diePlayer.Play(); } catch { }
        }

        /// <summary>
        /// Bellek içi PCM WAV ses dosyası bayt dizisi oluşturur.
        /// </summary>
        private static byte[] GenerateWav(int sampleRate, int durationMs, Func<double, double, double> sampleGenerator)
        {
            int numSamples = (int)(sampleRate * (durationMs / 1000.0));
            short[] samples = new short[numSamples];
            double durationSec = durationMs / 1000.0;

            for (int i = 0; i < numSamples; i++)
            {
                double t = (double)i / sampleRate;
                double val = sampleGenerator(t, durationSec);
                val = Math.Clamp(val, -1.0, 1.0);
                samples[i] = (short)(val * 30000);
            }

            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            int subChunk2Size = numSamples * 2; // 16-bit mono
            int chunkSize = 36 + subChunk2Size;

            // RIFF chunk descriptor
            writer.Write("RIFF"u8.ToArray());
            writer.Write(chunkSize);
            writer.Write("WAVE"u8.ToArray());

            // "fmt " sub-chunk
            writer.Write("fmt "u8.ToArray());
            writer.Write(16);                 // Subchunk1Size (16 for PCM)
            writer.Write((short)1);           // AudioFormat (1 for PCM)
            writer.Write((short)1);           // NumChannels (1: Mono)
            writer.Write(sampleRate);         // SampleRate
            writer.Write(sampleRate * 2);     // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
            writer.Write((short)2);           // BlockAlign (NumChannels * BitsPerSample/8)
            writer.Write((short)16);          // BitsPerSample

            // "data" sub-chunk
            writer.Write("data"u8.ToArray());
            writer.Write(subChunk2Size);

            // Audio data
            for (int i = 0; i < numSamples; i++)
            {
                writer.Write(samples[i]);
            }

            return ms.ToArray();
        }

        public void Dispose()
        {
            _jumpPlayer?.Dispose();
            _scorePlayer?.Dispose();
            _hitPlayer?.Dispose();
            _diePlayer?.Dispose();
            _coinPlayer?.Dispose();
            _recordPlayer?.Dispose();
        }
    }
}
