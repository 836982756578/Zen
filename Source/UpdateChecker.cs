// Zen Fence - Otomatik Güncelleme Kontrolcüsü
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Zen
{
    public static class UpdateChecker
    {
        public const string CurrentVersion = "v1.0.1";
        private const string ApiUrl = "https://api.github.com/repos/836982756578/Zen/releases/latest";
        private const string ReleasesPageUrl = "https://github.com/836982756578/Zen/releases";

        private static DispatcherTimer _periodicTimer;
        private static bool _balloonHandlerAttached = false;
        private static string _latestReleaseUrl = ReleasesPageUrl;

        public static void Start(NotifyIcon trayIcon)
        {
            if (trayIcon == null) return;

            // Hook balloon click once
            if (!_balloonHandlerAttached)
            {
                trayIcon.BalloonTipClicked += (s, e) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(_latestReleaseUrl) { UseShellExecute = true });
                    }
                    catch { }
                };
                _balloonHandlerAttached = true;
            }

            // Initial check delayed 6 seconds after launch so app starts instantly
            DispatcherTimer initTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            initTimer.Tick += (s, e) =>
            {
                initTimer.Stop();
                CheckForUpdateAsync(trayIcon, isManual: false);
            };
            initTimer.Start();

            // Periodic check every 4 hours while running
            _periodicTimer = new DispatcherTimer { Interval = TimeSpan.FromHours(4) };
            _periodicTimer.Tick += (s, e) => CheckForUpdateAsync(trayIcon, isManual: false);
            _periodicTimer.Start();
        }

        public static void CheckForUpdateAsync(NotifyIcon trayIcon, bool isManual = false)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    // Ensure TLS 1.2 is enabled for GitHub API
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // Tls12

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiUrl);
                    request.UserAgent = "ZenFence-UpdateChecker";
                    request.Accept = "application/vnd.github.v3+json";
                    request.Timeout = 6000;
                    request.ReadWriteTimeout = 6000;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        Dictionary<string, object> data = serializer.Deserialize<Dictionary<string, object>>(json);

                        if (data != null && data.ContainsKey("tag_name"))
                        {
                            string latestTag = data["tag_name"].ToString().Trim();
                            string htmlUrl = data.ContainsKey("html_url") ? data["html_url"].ToString() : ReleasesPageUrl;
                            _latestReleaseUrl = htmlUrl;

                            bool hasUpdate = IsNewer(latestTag, CurrentVersion);

                            if (hasUpdate)
                            {
                                if (trayIcon != null)
                                {
                                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        trayIcon.BalloonTipTitle = "🚀 Zen Fence: Yeni Sürüm Mevcut! (" + latestTag + ")";
                                        trayIcon.BalloonTipText = "Yeni bir güncelleme yayınlandı.\nİndirmek için buraya tıklayın.";
                                        trayIcon.BalloonTipIcon = ToolTipIcon.Info;
                                        trayIcon.ShowBalloonTip(10000);
                                    }));
                                }

                                if (isManual)
                                {
                                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        MessageBoxResult res = System.Windows.MessageBox.Show(
                                            "🚀 Zen Fence Yeni Sürüm Mevcut! (" + latestTag + ")\n\n" +
                                            "Mevcut Sürüm: " + CurrentVersion + "\n" +
                                            "Yeni Sürüm: " + latestTag + "\n\n" +
                                            "Yeni sürümü indirmek için indirme sayfasını açmak ister misiniz?\n\nGeliştirici: Serkan ŞENTÜRK",
                                            "Zen Fence Güncelleme",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Information);

                                        if (res == MessageBoxResult.Yes)
                                        {
                                            try
                                            {
                                                Process.Start(new ProcessStartInfo(_latestReleaseUrl) { UseShellExecute = true });
                                            }
                                            catch { }
                                        }
                                    }));
                                }
                            }
                            else if (isManual)
                            {
                                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    System.Windows.MessageBox.Show(
                                        "Zen Fence güncel! 🎉\n\nŞu anda en son sürümü (" + CurrentVersion + ") kullanıyorsunuz.\n\nGeliştirici: Serkan ŞENTÜRK",
                                        "Zen Fence Güncelleme Kontrolü",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                                }));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (isManual)
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            System.Windows.MessageBox.Show(
                                "Güncelleme kontrolü sırasında internet sunucusuna bağlanılamadı:\n" + ex.Message + "\n\nLütfen internet bağlantınızı kontrol edin.",
                                "Zen Fence Güncelleme Kontrolü",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }));
                    }
                }
            });
        }

        private static bool IsNewer(string latestTag, string currentTag)
        {
            try
            {
                string cleanLat = latestTag.TrimStart('v', 'V').Trim();
                string cleanCur = currentTag.TrimStart('v', 'V').Trim();

                Version vLat;
                Version vCur;

                if (Version.TryParse(cleanLat, out vLat) && Version.TryParse(cleanCur, out vCur))
                {
                    return vLat > vCur;
                }

                return string.Compare(cleanLat, cleanCur, StringComparison.OrdinalIgnoreCase) > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
