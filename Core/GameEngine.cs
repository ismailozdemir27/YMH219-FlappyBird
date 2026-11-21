using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using FlappyBird.Enums;
using FlappyBird.Models;
using FlappyBird.Services;

namespace FlappyBird.Core
{
    /// <summary>
    /// Oyunun tüm mantıksal döngüsünü, fiziğini, çarpışma kontrollerini ve
    /// durum yönetimini üstlenen çekirdek motor sınıfı.
    /// Nesne tabanlı mimarinin merkezi bileşenidir.
    /// </summary>
    public class GameEngine : IDisposable
    {
        public int ScreenWidth { get; }
        public int ScreenHeight { get; }

        public Bird Bird { get; private set; }
        public List<Pipe> Pipes { get; private set; }
        public List<Coin> Coins { get; private set; }
        public Ground Ground { get; private set; }
        public ScoreManager ScoreManager { get; private set; }
        public SoundManager SoundManager { get; private set; }
        public GameSettings Settings { get; private set; }

        public GameState State { get; private set; }
        public GameTheme CurrentTheme { get; set; } = GameTheme.Day;
        public float CurrentSpeed { get; private set; }
        public int Level { get; private set; } = 1;
        public int CollectedCoins { get; private set; } = 0;

        public event Action? OnGameOver;
        public event Action? OnScoreChanged;

        private readonly Random _random = new Random();
        private double _gameTimeSeconds = 0;
        private readonly float _birdStartX = 75;
        private readonly float _birdStartY = 195;
        private bool _hasPlayedRecordFanfare = false;

        // Arka plan bulutları ve yıldızlar
        private readonly List<PointF> _clouds = new List<PointF>();
        private readonly List<PointF> _stars = new List<PointF>();

        public GameEngine(int screenWidth, int screenHeight)
        {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;

            Settings = new GameSettings();
            ScoreManager = new ScoreManager();
            SoundManager = new SoundManager();

            Bird = new Bird(_birdStartX, _birdStartY);
            Ground = new Ground();
            Pipes = new List<Pipe>();
            Coins = new List<Coin>();

            InitializeClouds();
            InitializeStars();
            ResetGame();
        }

        private void InitializeClouds()
        {
            _clouds.Clear();
            _clouds.Add(new PointF(40, 75));
            _clouds.Add(new PointF(185, 135));
            _clouds.Add(new PointF(330, 55));
        }

        private void InitializeStars()
        {
            _stars.Clear();
            for (int i = 0; i < 40; i++)
            {
                float sx = _random.Next(10, ScreenWidth - 10);
                float sy = _random.Next(15, ScreenHeight - 140);
                _stars.Add(new PointF(sx, sy));
            }
        }

        /// <summary>
        /// Oyunu sıfırlar ve bekleme (Ready) moduna alır.
        /// </summary>
        public void ResetGame()
        {
            State = GameState.Ready;
            CurrentSpeed = Settings.BasePipeSpeed;
            Level = 1;
            CollectedCoins = 0;
            _hasPlayedRecordFanfare = false;

            ScoreManager.ResetScore();
            Bird.Reset(_birdStartX, _birdStartY);
            Bird.Gravity = Settings.Gravity;
            Bird.JumpStrength = Settings.JumpStrength;

            Pipes.Clear();
            Coins.Clear();

            // Ekran üzerinde dönecek boru çiftleri ve toplanabilir altınlar
            float startPipeX = ScreenWidth + 80;
            for (int i = 0; i < 3; i++)
            {
                float px = startPipeX + (i * Settings.PipeSpacing);
                Pipe pipe = CreateNewPipe(px);
                Pipes.Add(pipe);

                // Her borunun boşluğuna toplanabilir altın ekle
                float coinY = pipe.TopHeight + (pipe.Gap / 2f);
                Coins.Add(new Coin(pipe.X + (pipe.Width / 2f), coinY));
            }
        }

        /// <summary>
        /// Rastgele açıklık yüksekliğinde yeni bir boru çifti üretir.
        /// </summary>
        private Pipe CreateNewPipe(float x)
        {
            int groundY = ScreenHeight - Ground.Height;
            int minPipeHeight = 55;
            int maxTopHeight = groundY - Settings.PipeGap - minPipeHeight;

            if (maxTopHeight < minPipeHeight)
            {
                maxTopHeight = minPipeHeight + 35;
            }

            int topHeight = _random.Next(minPipeHeight, maxTopHeight);
            return new Pipe(x, topHeight, Settings.PipeGap);
        }

        /// <summary>
        /// Oyuncu zıplama eylemi (Space, Tıklama veya Yukarı Ok tuşu).
        /// </summary>
        public void Flap()
        {
            if (State == GameState.Ready)
            {
                State = GameState.Playing;
                Bird.Jump();
                SoundManager.PlayJump();
            }
            else if (State == GameState.Playing && Bird.IsAlive)
            {
                Bird.Jump();
                SoundManager.PlayJump();
            }
            else if (State == GameState.GameOver)
            {
                ResetGame();
            }
        }

        /// <summary>
        /// Oyunu duraklatır veya devam ettirir.
        /// </summary>
        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Playing;
            }
        }

        /// <summary>
        /// Zorluk derecesini değiştirir ve oyunu yeniden başlatır.
        /// </summary>
        public void ChangeDifficulty(Difficulty difficulty)
        {
            Settings.ApplyDifficulty(difficulty);
            ResetGame();
        }

        /// <summary>
        /// Kuş kostümünü (rengini) bir sonrakine geçirir.
        /// </summary>
        public void CycleBirdSkin()
        {
            var values = (BirdSkin[])Enum.GetValues(typeof(BirdSkin));
            int currentIndex = Array.IndexOf(values, Bird.Skin);
            int nextIndex = (currentIndex + 1) % values.Length;
            Bird.Skin = values[nextIndex];
            SoundManager.PlayScore();
        }

        /// <summary>
        /// Arka plan temasını değiştirir (Gündüz -> Gün Batımı -> Gece).
        /// </summary>
        public void CycleTheme()
        {
            var values = (GameTheme[])Enum.GetValues(typeof(GameTheme));
            int currentIndex = Array.IndexOf(values, CurrentTheme);
            int nextIndex = (currentIndex + 1) % values.Length;
            CurrentTheme = values[nextIndex];
            SoundManager.PlayScore();
        }

        /// <summary>
        /// Timer tarafından yaklaşık 60 FPS frekansla çağrılan ana oyun döngüsü metodu.
        /// </summary>
        public void Update()
        {
            _gameTimeSeconds += 0.016;

            // Bulutların yavaş süzülmesi
            for (int i = 0; i < _clouds.Count; i++)
            {
                float cx = _clouds[i].X - 0.4f;
                if (cx < -120) cx = ScreenWidth + 50;
                _clouds[i] = new PointF(cx, _clouds[i].Y);
            }

            if (State == GameState.Ready)
            {
                // Başlamadan önce kuş nazikçe havada salınır
                Bird.IdleFloat(_birdStartY, _gameTimeSeconds);
                Ground.Update(CurrentSpeed * 0.5f);
                return;
            }

            if (State == GameState.Paused)
            {
                return;
            }

            if (State == GameState.GameOver)
            {
                // Kuş yere düşene kadar yerçekimi devam eder
                int groundY = ScreenHeight - Ground.Height;
                if (Bird.Y + Bird.Height < groundY)
                {
                    Bird.Update(groundY);
                }
                return;
            }

            int groundTopY = ScreenHeight - Ground.Height;

            // Kuş fiziği güncelle
            Bird.Update(groundTopY);

            // Zemin kayması
            Ground.Update(CurrentSpeed);

            // Boruların ve altınların hareketi
            float maxRightX = 0;
            foreach (var p in Pipes)
            {
                if (p.X > maxRightX) maxRightX = p.X;
            }

            Rectangle birdBox = Bird.Hitbox;

            for (int i = 0; i < Pipes.Count; i++)
            {
                Pipe pipe = Pipes[i];
                pipe.Update(CurrentSpeed);

                // Altın hareketi ve toplama denetimi
                if (i < Coins.Count)
                {
                    Coin coin = Coins[i];
                    coin.Update(CurrentSpeed);

                    if (!coin.IsCollected && birdBox.IntersectsWith(coin.Hitbox))
                    {
                        coin.IsCollected = true;
                        CollectedCoins++;

                        // Altın alınca skora +1 ekle ve rekor kontrolü yap
                        bool newBest = ScoreManager.IncrementScore();
                        if (newBest && ScoreManager.CurrentScore > 1 && !_hasPlayedRecordFanfare)
                        {
                            _hasPlayedRecordFanfare = true;
                            SoundManager.PlayRecordFanfare(); // Rekor kırılınca özel zafer sesi!
                        }
                        else
                        {
                            SoundManager.PlayCoin(); // Özel altın çan sesi!
                        }

                        OnScoreChanged?.Invoke();
                    }
                }

                // Skor kontrolü: Kuş borunun ortasını geçtiğinde skor ekle
                if (!pipe.Passed && pipe.X + (pipe.Width / 2) < Bird.X)
                {
                    pipe.Passed = true;
                    bool newBest = ScoreManager.IncrementScore();

                    if (newBest && ScoreManager.CurrentScore > 1 && !_hasPlayedRecordFanfare)
                    {
                        _hasPlayedRecordFanfare = true;
                        SoundManager.PlayRecordFanfare(); // Rekor kırılınca özel zafer sesi!
                    }
                    else
                    {
                        SoundManager.PlayScore();
                    }

                    OnScoreChanged?.Invoke();

                    // Seviye ve hız artışı (Her 5 puanda bir hız %5 artar)
                    int calculatedLevel = (ScoreManager.CurrentScore / 5) + 1;
                    if (calculatedLevel != Level)
                    {
                        Level = calculatedLevel;
                        CurrentSpeed = Settings.BasePipeSpeed + ((Level - 1) * 0.35f);
                    }
                }

                // Ekrandan çıkan boruyu sıraya en sağdan tekrar dahil et (Recycle)
                if (pipe.X + pipe.Width < -10)
                {
                    float nextX = Math.Max(ScreenWidth + 30, maxRightX + Settings.PipeSpacing);
                    Pipe newPipe = CreateNewPipe(nextX);
                    Pipes[i] = newPipe;

                    // Yeni borunun ortasına yeni bir altın koy
                    float coinY = newPipe.TopHeight + (newPipe.Gap / 2f);
                    if (i < Coins.Count)
                    {
                        Coins[i] = new Coin(nextX + (newPipe.Width / 2f), coinY);
                    }
                    else
                    {
                        Coins.Add(new Coin(nextX + (newPipe.Width / 2f), coinY));
                    }

                    maxRightX = nextX;
                }
            }

            // Çarpışma Denetimi (Collision Detection)
            CheckCollisions();
        }

        /// <summary>
        /// Kuşun borulara veya zemine çarpmasını kontrol eder.
        /// </summary>
        private void CheckCollisions()
        {
            int groundY = ScreenHeight - Ground.Height;

            // 1. Zemin çarpışması
            if (Bird.Y + Bird.Height >= groundY)
            {
                TriggerGameOver();
                return;
            }

            // 2. Borular ile çarpışma
            Rectangle birdBox = Bird.Hitbox;
            foreach (var pipe in Pipes)
            {
                if (pipe.CollidesWith(birdBox, ScreenHeight, Ground.Height))
                {
                    TriggerGameOver();
                    return;
                }
            }
        }

        private void TriggerGameOver()
        {
            if (State != GameState.GameOver)
            {
                State = GameState.GameOver;
                Bird.IsAlive = false;
                SoundManager.PlayHit();
                SoundManager.PlayDie();
                OnGameOver?.Invoke();
            }
        }

        /// <summary>
        /// Tüm oyun ekranını GDI+ çift tamponlama ile çizer.
        /// </summary>
        public void Render(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;

            // 1. Gökyüzü arka planı ve tema öğeleri (Güneş, Ay, Yıldızlar, Şehir)
            DrawSky(g);

            // 2. Bulutlar
            DrawClouds(g);

            // 3. Borular (Engeller)
            foreach (var pipe in Pipes)
            {
                pipe.Draw(g, ScreenHeight, Ground.Height);
            }

            // 4. Toplanabilir Altınlar
            foreach (var coin in Coins)
            {
                coin.Draw(g);
            }

            // 5. Zemin
            Ground.Draw(g, ScreenWidth, ScreenHeight);

            // 6. Kuş
            Bird.Draw(g);

            // 7. HUD / Arayüz Bilgileri
            DrawHUD(g);

            // 8. Durum Katmanları (Ready / Paused / Game Over)
            if (State == GameState.Ready)
            {
                DrawReadyOverlay(g);
            }
            else if (State == GameState.Paused)
            {
                DrawPausedOverlay(g);
            }
            else if (State == GameState.GameOver)
            {
                DrawGameOverOverlay(g);
            }
        }

        private void DrawSky(Graphics g)
        {
            Color topColor, bottomColor, cityColor;

            switch (CurrentTheme)
            {
                case GameTheme.Sunset:
                    topColor = Color.FromArgb(235, 95, 55);    // Sıcak kızıl gün batımı
                    bottomColor = Color.FromArgb(255, 195, 115);
                    cityColor = Color.FromArgb(160, 85, 45, 75);
                    break;

                case GameTheme.Night:
                    topColor = Color.FromArgb(12, 18, 48);     // Koyu lacivert gece
                    bottomColor = Color.FromArgb(32, 45, 95);
                    cityColor = Color.FromArgb(210, 15, 20, 38);
                    break;

                default: // Day
                    topColor = Color.FromArgb(112, 197, 206);   // Klasik açık mavi
                    bottomColor = Color.FromArgb(178, 230, 236);
                    cityColor = Color.FromArgb(140, 185, 218, 200);
                    break;
            }

            // Gökyüzü gradyanı
            using (LinearGradientBrush skyBrush = new LinearGradientBrush(
                new Rectangle(0, 0, ScreenWidth, ScreenHeight - Ground.Height),
                topColor,
                bottomColor,
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(skyBrush, 0, 0, ScreenWidth, ScreenHeight);
            }

            // Gece Teması: Yıldızlar ve Hilal Ay
            if (CurrentTheme == GameTheme.Night)
            {
                // Yıldızlar
                using (SolidBrush starBrush = new SolidBrush(Color.FromArgb(220, 255, 255, 255)))
                {
                    foreach (var s in _stars)
                    {
                        g.FillEllipse(starBrush, s.X, s.Y, 2.2f, 2.2f);
                    }
                }

                // Hilal Ay
                int moonX = ScreenWidth - 75;
                int moonY = 45;
                using (SolidBrush moonBrush = new SolidBrush(Color.FromArgb(255, 250, 205)))
                using (SolidBrush cutBrush = new SolidBrush(topColor))
                {
                    g.FillEllipse(moonBrush, moonX, moonY, 36, 36);
                    g.FillEllipse(cutBrush, moonX - 10, moonY - 3, 34, 34); // Hilal kesiği
                }
            }
            // Gün Batımı Teması: Batan Güneş
            else if (CurrentTheme == GameTheme.Sunset)
            {
                int sunX = ScreenWidth - 110;
                int sunY = ScreenHeight - Ground.Height - 115;
                using (SolidBrush sunGlow = new SolidBrush(Color.FromArgb(90, 255, 220, 100)))
                using (SolidBrush sunBody = new SolidBrush(Color.FromArgb(255, 235, 120)))
                {
                    g.FillEllipse(sunGlow, sunX - 12, sunY - 12, 64, 64);
                    g.FillEllipse(sunBody, sunX, sunY, 40, 40);
                }
            }

            // Ufuk çizgisi silüeti (Şehir binaları)
            int cityY = ScreenHeight - Ground.Height - 45;
            using (SolidBrush brush = new SolidBrush(cityColor))
            {
                for (int x = 0; x < ScreenWidth; x += 38)
                {
                    int h = 26 + ((x * 19) % 26);
                    g.FillRectangle(brush, x, cityY + (45 - h), 34, h);

                    // Gece temasında binalarda yanan sıcak pencereler
                    if (CurrentTheme == GameTheme.Night)
                    {
                        using (SolidBrush windowBrush = new SolidBrush(Color.FromArgb(240, 255, 230, 110)))
                        {
                            int winY = cityY + (45 - h) + 6;
                            if ((x % 2 == 0) && winY < cityY + 35)
                            {
                                g.FillRectangle(windowBrush, x + 6, winY, 5, 5);
                                g.FillRectangle(windowBrush, x + 16, winY, 5, 5);
                            }
                        }
                    }
                }
            }
        }

        private void DrawClouds(Graphics g)
        {
            Color cloudColor = CurrentTheme switch
            {
                GameTheme.Sunset => Color.FromArgb(170, 255, 215, 200),
                GameTheme.Night => Color.FromArgb(60, 180, 200, 255),
                _ => Color.FromArgb(210, 255, 255, 255)
            };

            using (SolidBrush cloudBrush = new SolidBrush(cloudColor))
            {
                foreach (var c in _clouds)
                {
                    g.FillEllipse(cloudBrush, c.X, c.Y, 70, 32);
                    g.FillEllipse(cloudBrush, c.X + 20, c.Y - 14, 45, 36);
                    g.FillEllipse(cloudBrush, c.X + 45, c.Y, 50, 28);
                }
            }
        }

        private void DrawHUD(Graphics g)
        {
            // Oyun esnasında büyük retro skor sayısı
            if (State == GameState.Playing)
            {
                string scoreText = ScoreManager.CurrentScore.ToString();
                using (Font font = new Font("Impact", 36, FontStyle.Bold))
                {
                    SizeF size = g.MeasureString(scoreText, font);
                    float x = (ScreenWidth - size.Width) / 2f;
                    float y = 45;

                    // Kalın siyah kontur
                    using (FontFamily ff = new FontFamily("Impact"))
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        path.AddString(scoreText, ff, (int)FontStyle.Bold, 48, new PointF(x, y), StringFormat.GenericDefault);
                        using (Pen outlinePen = new Pen(Color.FromArgb(20, 20, 20), 5))
                        {
                            outlinePen.LineJoin = LineJoin.Round;
                            g.DrawPath(outlinePen, path);
                        }
                        g.FillPath(Brushes.White, path);
                    }
                }
            }

            // Üst bar mini bilgiler: Mod, Tema ve Toplanan Altın Sayısı
            string diffText = $"Mod: {Settings.CurrentDifficulty} | Tema: {CurrentTheme.GetDisplayName()}";
            using (Font miniFont = new Font("Segoe UI", 9, FontStyle.Bold))
            {
                g.DrawString(diffText, miniFont, Brushes.White, 12, 10);

                // Toplanan Altın Sayacı
                string coinHud = $"★ Altın: {CollectedCoins}";
                g.DrawString(coinHud, miniFont, Brushes.Gold, 12, 28);
            }

            // Ses durumu
            string soundText = SoundManager.SoundEnabled ? "Ses: Açık (M)" : "Ses: Kapalı (M)";
            using (Font miniFont = new Font("Segoe UI", 9, FontStyle.Bold))
            {
                SizeF size = g.MeasureString(soundText, miniFont);
                g.DrawString(soundText, miniFont, Brushes.White, ScreenWidth - size.Width - 12, 10);
            }
        }

        private void DrawReadyOverlay(Graphics g)
        {
            int centerX = ScreenWidth / 2;

            // Başlık
            DrawOutlinedText(g, "FLAPPY BIRD", centerX, 80, 36, Color.Gold, Color.Black);

            // Yönerge kutusu
            int boxW = 296;
            int boxH = 158;
            int boxX = centerX - (boxW / 2);
            int boxY = 240;

            using (SolidBrush bannerBrush = new SolidBrush(Color.FromArgb(190, 15, 25, 35)))
            {
                g.FillRoundRectangle(bannerBrush, new Rectangle(boxX, boxY, boxW, boxH), 16);
            }
            using (Pen boxPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
            {
                g.DrawRoundRectangle(boxPen, new Rectangle(boxX, boxY, boxW, boxH), 16);
            }

            using (Font textFont = new Font("Segoe UI", 10.5f, FontStyle.Bold))
            {
                DrawCenteredText(g, "Uçmak için SPACE'e Basın!", textFont, Brushes.White, centerX, boxY + 14);
                DrawCenteredText(g, $"< C: Kuş: {Bird.Skin.GetDisplayName()} >", textFont, Brushes.LightGreen, centerX, boxY + 41);
                DrawCenteredText(g, $"< T: Tema: {CurrentTheme.GetDisplayName()} >", textFont, Brushes.LightSkyBlue, centerX, boxY + 68);
                DrawCenteredText(g, "1: Kolay | 2: Orta | 3: Zor", textFont, Brushes.Yellow, centerX, boxY + 95);
                DrawCenteredText(g, "P: Duraklat | M: Ses Aç/Kapa", textFont, Brushes.WhiteSmoke, centerX, boxY + 122);
            }

            // En yüksek skor bilgisi
            string bestText = $"En Yüksek Skor: {ScoreManager.HighScore}";
            DrawOutlinedText(g, bestText, centerX, 415, 16, Color.White, Color.Black);
        }

        private void DrawPausedOverlay(Graphics g)
        {
            using (SolidBrush dimBrush = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
            {
                g.FillRectangle(dimBrush, 0, 0, ScreenWidth, ScreenHeight);
            }

            DrawOutlinedText(g, "DURAKLATILDI", ScreenWidth / 2, 230, 28, Color.Yellow, Color.Black);
            using (Font subFont = new Font("Segoe UI", 12, FontStyle.Bold))
            {
                DrawCenteredText(g, "Devam etmek için P veya ESC'ye basın", subFont, Brushes.White, ScreenWidth / 2, 280);
            }
        }

        private void DrawGameOverOverlay(Graphics g)
        {
            int centerX = ScreenWidth / 2;

            // Game Over başlığı
            DrawOutlinedText(g, "OYUN BİTTİ", centerX, 90, 34, Color.Crimson, Color.Black);

            // Skor tablosu paneli
            int panelW = 270;
            int panelH = 185;
            int panelX = centerX - (panelW / 2);
            int panelY = 145;

            using (SolidBrush panelBg = new SolidBrush(Color.FromArgb(238, 226, 180)))
            {
                g.FillRoundRectangle(panelBg, new Rectangle(panelX, panelY, panelW, panelH), 14);
            }
            using (Pen borderPen = new Pen(Color.FromArgb(120, 80, 40), 3))
            {
                g.DrawRoundRectangle(borderPen, new Rectangle(panelX, panelY, panelW, panelH), 14);
            }

            // Madalya alanı
            int medalX = panelX + 22;
            int medalY = panelY + 45;
            DrawMedal(g, medalX, medalY, ScoreManager.CurrentScore);

            // Skorlar metinleri
            using (Font labelFont = new Font("Segoe UI", 10, FontStyle.Bold))
            using (Font valFont = new Font("Impact", 20))
            {
                g.DrawString("SKOR", labelFont, Brushes.DarkOrange, panelX + 115, panelY + 18);
                g.DrawString(ScoreManager.CurrentScore.ToString(), valFont, Brushes.Black, panelX + 115, panelY + 36);

                g.DrawString("EN YÜKSEK", labelFont, Brushes.DarkOrange, panelX + 115, panelY + 70);
                g.DrawString(ScoreManager.HighScore.ToString(), valFont, Brushes.Black, panelX + 115, panelY + 88);

                // Toplanan Altın
                g.DrawString($"★ Toplanan Altın: {CollectedCoins}", labelFont, Brushes.SaddleBrown, panelX + 115, panelY + 124);
            }

            // Yeni rekor rozeti
            if (ScoreManager.IsNewHighScore)
            {
                using (SolidBrush newBadge = new SolidBrush(Color.FromArgb(220, 30, 30)))
                {
                    g.FillRoundRectangle(newBadge, new Rectangle(panelX + panelW - 62, panelY + 14, 48, 20), 6);
                }
                using (Font badgeFont = new Font("Segoe UI", 8, FontStyle.Bold))
                {
                    g.DrawString("YENİ!", badgeFont, Brushes.White, panelX + panelW - 55, panelY + 17);
                }
            }

            // Seviye bilgisi
            using (Font infoFont = new Font("Segoe UI", 9, FontStyle.Italic))
            {
                g.DrawString($"Ulaşılan Seviye: {Level}", infoFont, Brushes.DimGray, panelX + 115, panelY + 148);
            }

            // Tekrar başlatma talimatı
            using (Font retryFont = new Font("Segoe UI", 12, FontStyle.Bold))
            {
                DrawCenteredText(g, "Yeniden Başlatmak İçin SPACE veya R", retryFont, Brushes.White, centerX, panelY + panelH + 30);
            }
        }

        private void DrawMedal(Graphics g, int x, int y, int score)
        {
            Color medalColor = Color.DarkGray;
            string medalName = "Yok";

            if (score >= 30)
            {
                medalColor = Color.FromArgb(229, 228, 226); // Platin
                medalName = "Platin";
            }
            else if (score >= 20)
            {
                medalColor = Color.FromArgb(255, 215, 0); // Altın
                medalName = "Altın";
            }
            else if (score >= 10)
            {
                medalColor = Color.FromArgb(192, 192, 192); // Gümüş
                medalName = "Gümüş";
            }
            else if (score >= 5)
            {
                medalColor = Color.FromArgb(205, 127, 50); // Bronz
                medalName = "Bronz";
            }

            int size = 52;
            using (SolidBrush brush = new SolidBrush(medalColor))
            {
                g.FillEllipse(brush, x, y, size, size);
            }
            using (Pen pen = new Pen(Color.FromArgb(80, 50, 20), 2.5f))
            {
                g.DrawEllipse(pen, x, y, size, size);
                g.DrawEllipse(pen, x + 5, y + 5, size - 10, size - 10);
            }

            using (Font font = new Font("Segoe UI", 7, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(medalName, font);
                g.DrawString(medalName, font, Brushes.Black, x + (size - sz.Width) / 2f, y + (size - sz.Height) / 2f);
            }
        }

        private static void DrawOutlinedText(Graphics g, string text, int centerX, int y, int size, Color fill, Color outline)
        {
            using (FontFamily ff = new FontFamily("Impact"))
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddString(text, ff, (int)FontStyle.Bold, size, new PointF(0, 0), StringFormat.GenericDefault);
                RectangleF bounds = path.GetBounds();
                float x = centerX - (bounds.Width / 2f);

                GraphicsState state = g.Save();
                g.TranslateTransform(x - bounds.Left, y);
                using (Pen pen = new Pen(outline, 4.5f))
                {
                    pen.LineJoin = LineJoin.Round;
                    g.DrawPath(pen, path);
                }
                using (SolidBrush brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, path);
                }
                g.Restore(state);
            }
        }

        private static void DrawCenteredText(Graphics g, string text, Font font, Brush brush, int centerX, int y)
        {
            SizeF size = g.MeasureString(text, font);
            g.DrawString(text, font, brush, centerX - (size.Width / 2f), y);
        }

        public void Dispose()
        {
            SoundManager?.Dispose();
        }
    }

    /// <summary>
    /// GDI+ için yuvarlatılmış dikdörtgen çizim eklentisi.
    /// </summary>
    public static class GraphicsExtensions
    {
        public static void FillRoundRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using GraphicsPath path = CreateRoundedRectPath(rect, radius);
            g.FillPath(brush, path);
        }

        public static void DrawRoundRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using GraphicsPath path = CreateRoundedRectPath(rect, radius);
            g.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);

            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
