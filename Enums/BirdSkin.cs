namespace FlappyBird.Enums
{
    /// <summary>
    /// Oyuncunun seçebileceği kuş görünüm (deri/skin) seçenekleri.
    /// </summary>
    public enum BirdSkin
    {
        ClassicYellow,  // Klasik Sarı Kuş
        SkyBlue,        // Gök Mavisi Kuş
        RubyRed,        // Yakut Kırmızı Kuş
        RosePink,       // Pamuk Pembe Kuş
        GoldenPhoenix   // Altın Anka Kuş
    }

    public static class BirdSkinExtensions
    {
        public static string GetDisplayName(this BirdSkin skin)
        {
            return skin switch
            {
                BirdSkin.ClassicYellow => "Sarı (Klasik)",
                BirdSkin.SkyBlue => "Mavi (Fırtına)",
                BirdSkin.RubyRed => "Kırmızı (Ateş)",
                BirdSkin.RosePink => "Pembe (Şeker)",
                BirdSkin.GoldenPhoenix => "Altın (Efsane)",
                _ => "Klasik"
            };
        }
    }
}

