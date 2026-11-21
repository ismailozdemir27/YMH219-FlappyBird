using System.Drawing;
using System.Drawing.Drawing2D;

namespace FlappyBird.Models
{
    /// <summary>
    /// Üst ve alt borudan oluşan boru çiftini (engel) temsil eden sınıf.
    /// Nesne tabanlı yapıda kendi koordinatlarını, çarpışma testlerini ve çizimini yönetir.
    /// </summary>
    public class Pipe
    {
        public float X { get; set; }
        public int Width { get; } = 64;
        public int TopHeight { get; set; }
        public int Gap { get; set; }
        public bool Passed { get; set; } = false;

        private readonly int _collarHeight = 24;
        private readonly int _collarExtraWidth = 6;

        public Pipe(float x, int topHeight, int gap)
        {
            X = x;
            TopHeight = topHeight;
            Gap = gap;
            Passed = false;
        }

        /// <summary>
        /// Borunun konumunu sola doğru kaydırır.
        /// </summary>
        public void Update(float speed)
        {
            X -= speed;
        }

        /// <summary>
        /// Üst borunun çarpışma sınırları.
        /// </summary>
        public Rectangle TopBounds => new Rectangle((int)X, 0, Width, TopHeight);

        /// <summary>
        /// Alt borunun çarpışma sınırları.
        /// </summary>
        public Rectangle BottomBounds(int screenHeight, int groundHeight)
        {
            int topY = TopHeight + Gap;
            int bottomHeight = screenHeight - groundHeight - topY;
            if (bottomHeight < 0) bottomHeight = 0;
            return new Rectangle((int)X, topY, Width, bottomHeight);
        }

        /// <summary>
        /// Kuşun bu boru çiftiyle çarpışıp çarpmadığını denetler.
        /// </summary>
        public bool CollidesWith(Rectangle birdHitbox, int screenHeight, int groundHeight)
        {
            if (birdHitbox.IntersectsWith(TopBounds))
            {
                return true;
            }

            Rectangle bottomRect = BottomBounds(screenHeight, groundHeight);
            if (birdHitbox.IntersectsWith(bottomRect))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Boruyu ve başlıklarını (collar) estetik retro gölgelendirmeyle çizer.
        /// </summary>
        public void Draw(Graphics g, int screenHeight, int groundHeight)
        {
            // Renk paleti
            Color pipeGreenLight = Color.FromArgb(122, 212, 53);
            Color pipeGreenMid = Color.FromArgb(94, 178, 38);
            Color pipeGreenDark = Color.FromArgb(49, 120, 20);
            Color pipeBorder = Color.FromArgb(20, 60, 10);

            int x = (int)X;

            // --- 1. ÜST BORU ÇİZİMİ ---
            if (TopHeight > 0)
            {
                // Üst boru gövdesi
                Rectangle topBody = new Rectangle(x, 0, Width, TopHeight - _collarHeight);
                DrawPipeSegment(g, topBody, pipeGreenLight, pipeGreenMid, pipeGreenDark, pipeBorder);

                // Üst boru başlığı (aşağı bakan ağız)
                Rectangle topCollar = new Rectangle(
                    x - (_collarExtraWidth / 2),
                    TopHeight - _collarHeight,
                    Width + _collarExtraWidth,
                    _collarHeight
                );
                DrawPipeSegment(g, topCollar, pipeGreenLight, pipeGreenMid, pipeGreenDark, pipeBorder);
            }

            // --- 2. ALT BORU ÇİZİMİ ---
            int bottomY = TopHeight + Gap;
            int bottomTotalHeight = screenHeight - groundHeight - bottomY;

            if (bottomTotalHeight > 0)
            {
                // Alt boru başlığı (yukarı bakan ağız)
                Rectangle bottomCollar = new Rectangle(
                    x - (_collarExtraWidth / 2),
                    bottomY,
                    Width + _collarExtraWidth,
                    _collarHeight
                );
                DrawPipeSegment(g, bottomCollar, pipeGreenLight, pipeGreenMid, pipeGreenDark, pipeBorder);

                // Alt boru gövdesi
                Rectangle bottomBody = new Rectangle(
                    x,
                    bottomY + _collarHeight,
                    Width,
                    bottomTotalHeight - _collarHeight
                );
                if (bottomBody.Height > 0)
                {
                    DrawPipeSegment(g, bottomBody, pipeGreenLight, pipeGreenMid, pipeGreenDark, pipeBorder);
                }
            }
        }

        private void DrawPipeSegment(Graphics g, Rectangle rect, Color light, Color mid, Color dark, Color border)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;

            // Dikey gölgelendirme (soldan sağa doğru parlaktan koyuya)
            using (LinearGradientBrush brush = new LinearGradientBrush(
                rect,
                light,
                dark,
                LinearGradientMode.Horizontal))
            {
                ColorBlend blend = new ColorBlend(4);
                blend.Colors = new Color[] { light, mid, dark, dark };
                blend.Positions = new float[] { 0f, 0.25f, 0.85f, 1f };
                brush.InterpolationColors = blend;

                g.FillRectangle(brush, rect);
            }

            // Sol taraf parıltı çizgisi
            using (Pen highlightPen = new Pen(Color.FromArgb(160, 255, 255, 255), 1.5f))
            {
                g.DrawLine(highlightPen, rect.Left + 5, rect.Top, rect.Left + 5, rect.Bottom);
            }

            // Dış sınır konturu
            using (Pen borderPen = new Pen(border, 2f))
            {
                g.DrawRectangle(borderPen, rect);
            }
        }
    }
}
