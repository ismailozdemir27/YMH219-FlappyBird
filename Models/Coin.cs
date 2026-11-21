using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FlappyBird.Models
{
    /// <summary>
    /// Boruların arasında toplanabilen altın/yıldız nesnesi.
    /// Kendi hareketini, dönme animasyonunu ve çarpışma kontrolünü yönetir.
    /// </summary>
    public class Coin
    {
        public float X { get; set; }
        public float BaseY { get; set; }
        public float Y { get; private set; }
        public int Size { get; } = 24;
        public bool IsCollected { get; set; } = false;

        private int _tick = 0;

        public Coin(float x, float y)
        {
            X = x;
            BaseY = y;
            Y = y;
            IsCollected = false;
        }

        /// <summary>
        /// Altını sola kaydırır ve yukarı-aşağı süzülme animasyonunu işletir.
        /// </summary>
        public void Update(float speed)
        {
            X -= speed;
            _tick++;

            // Nazikçe yukarı-aşağı salınım
            Y = BaseY + (float)(Math.Sin(_tick * 0.12) * 5.0);
        }

        /// <summary>
        /// Toplama çarpışma alanı.
        /// </summary>
        public Rectangle Hitbox => new Rectangle(
            (int)X - (Size / 2),
            (int)Y - (Size / 2),
            Size,
            Size
        );

        /// <summary>
        /// Parlayan ve kendi etrafında 3D dönen altın parayı çizer.
        /// </summary>
        public void Draw(Graphics g)
        {
            if (IsCollected) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 3D Dönme efekti (Genişliği sinüs dalgasıyla daraltıp açarak)
            float spinFactor = (float)Math.Abs(Math.Cos(_tick * 0.1));
            if (spinFactor < 0.15f) spinFactor = 0.15f;

            float drawWidth = Size * spinFactor;
            float drawHeight = Size;
            float drawX = X - (drawWidth / 2f);
            float drawY = Y - (drawHeight / 2f);

            RectangleF coinRect = new RectangleF(drawX, drawY, drawWidth, drawHeight);

            // Dış ışıltı halkası
            using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(70, 255, 230, 80)))
            {
                g.FillEllipse(glowBrush, X - (Size * 0.75f), Y - (Size * 0.75f), Size * 1.5f, Size * 1.5f);
            }

            // Altın gövde (Gradient)
            using (LinearGradientBrush goldBrush = new LinearGradientBrush(
                coinRect,
                Color.FromArgb(255, 245, 120),
                Color.FromArgb(220, 160, 10),
                LinearGradientMode.Vertical))
            {
                g.FillEllipse(goldBrush, coinRect);
            }

            // Kenar konturu
            using (Pen borderPen = new Pen(Color.FromArgb(170, 110, 0), 1.5f))
            {
                g.DrawEllipse(borderPen, coinRect);
            }

            // İç yıldız / desen (Para genişken görünür)
            if (spinFactor > 0.5f)
            {
                using (SolidBrush starBrush = new SolidBrush(Color.FromArgb(255, 255, 240)))
                {
                    float starSize = 8 * spinFactor;
                    PointF[] starPoints = new PointF[]
                    {
                        new PointF(X, Y - starSize),
                        new PointF(X + (starSize * 0.35f), Y - (starSize * 0.35f)),
                        new PointF(X + starSize, Y),
                        new PointF(X + (starSize * 0.35f), Y + (starSize * 0.35f)),
                        new PointF(X, Y + starSize),
                        new PointF(X - (starSize * 0.35f), Y + (starSize * 0.35f)),
                        new PointF(X - starSize, Y),
                        new PointF(X - (starSize * 0.35f), Y - (starSize * 0.35f))
                    };
                    g.FillPolygon(starBrush, starPoints);
                }
            }
        }
    }
}
