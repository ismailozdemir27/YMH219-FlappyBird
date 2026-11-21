namespace FlappyBird.Enums
{
    /// <summary>
    /// Oyunun görsel gökyüzü ve arka plan tema seçenekleri.
    /// </summary>
    public enum GameTheme
    {
        Day,     // Gündüz: Canlı açık mavi gökyüzü, beyaz bulutlar
        Sunset,  // Gün Batımı: Sıcak kızıl-turuncu gökyüzü, gün batımı güneşi
        Night    // Gece: Koyu lacivert gökyüzü, parıldayan yıldızlar, hilal ay ve ışıklı şehir
    }

    public static class GameThemeExtensions
    {
        public static string GetDisplayName(this GameTheme theme)
        {
            return theme switch
            {
                GameTheme.Day => "Gündüz",
                GameTheme.Sunset => "Gün Batımı",
                GameTheme.Night => "Gece",
                _ => "Gündüz"
            };
        }
    }
}
