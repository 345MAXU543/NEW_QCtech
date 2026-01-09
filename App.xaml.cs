using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media.Imaging;

namespace NEW_QCtech
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            GifCache.Preload(
                "UP",
                "pack://application:,,,/Resources/ICON/UPgif.gif");
        }

        public static class GifCache
        {
            private static readonly Dictionary<string, BitmapImage> _cache = new();

            public static void Preload(string key, string uri)
            {
                if (_cache.ContainsKey(key))
                    return;

                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(uri, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad; // ⭐關鍵
                img.EndInit();
                img.Freeze(); // ⭐讓 UI 使用安全、效能更好

                _cache[key] = img;
            }

            public static BitmapImage Get(string key)
            {
                return _cache[key];
            }
        }

    }

}
