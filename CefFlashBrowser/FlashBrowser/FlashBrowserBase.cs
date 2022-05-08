﻿using CefFlashBrowser.Models.Data;
using CefFlashBrowser.Utils;
using CefFlashBrowser.Views.Dialogs.JsDialogs;
using CefFlashBrowser.WinformCefSharp4WPF;
using CefSharp;
using System;
using System.Diagnostics;
using System.IO;

namespace CefFlashBrowser.FlashBrowser
{
    public abstract class FlashBrowserBase : ChromiumWebBrowser
    {
        private static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string EmptyExePath = Path.Combine(BaseDirectory, @"CefFlashBrowser.EmptyExe.exe");
        public static readonly string CachePath = Path.Combine(BaseDirectory, @"Caches\");
        public static readonly string FlashPath = Path.Combine(BaseDirectory, @"Assets\Plugins\pepflashplayer.dll");

        public FlashBrowserBase()
        {
            IsBrowserInitializedChanged += FlashBrowserIsBrowserInitializedChanged;
        }

        private void FlashBrowserIsBrowserInitializedChanged(object sender, EventArgs e)
        {
            if (IsBrowserInitialized)
            {
                Cef.UIThreadTaskFactory.StartNew(() =>
                {
                    var requestContext = GetBrowser().GetHost().RequestContext;
                    var flag = requestContext.SetPreference("profile.default_content_setting_values.plugins", 1, out string err);

                    if (!flag)
                    {
                        var title = LanguageManager.GetString("title_error");
                        JsAlertDialog.ShowDialog(err, title);
                    }
                });
            }
        }

        /// <summary>
        /// This method should be called when the program starts
        /// </summary>
        public static void InitCefFlash()
        {
            if (Cef.IsInitialized)
                return;

            Environment.SetEnvironmentVariable("ComSpec", EmptyExePath); //Remove black popup window

            CefSettings settings = new CefSettings()
            {
                Locale = GlobalData.Settings.Language,
                CachePath = CachePath
            };

#if !DEBUG
            settings.LogSeverity = LogSeverity.Disable;
#endif

            settings.CefCommandLineArgs["enable-system-flash"] = "1";
            settings.CefCommandLineArgs["ppapi-flash-path"] = FlashPath;
            settings.CefCommandLineArgs["autoplay-policy"] = "no-user-gesture-required";

            if (GlobalData.Settings.FakeFlashVersionSetting.Enable)
            {
                settings.CefCommandLineArgs["ppapi-flash-version"] = GlobalData.Settings.FakeFlashVersionSetting.FlashVersion;
            }
            else
            {
                settings.CefCommandLineArgs["ppapi-flash-version"] = FileVersionInfo.GetVersionInfo(FlashPath).FileVersion.Replace(',', '.');
            }

            if (GlobalData.Settings.UserAgentSetting.EnableCustom)
            {
                settings.UserAgent = GlobalData.Settings.UserAgentSetting.UserAgent;
            }

            if (GlobalData.Settings.ProxySettings.EnableProxy)
            {
                var proxySettings = GlobalData.Settings.ProxySettings;
                CefSharpSettings.Proxy = new ProxyOptions(proxySettings.IP, proxySettings.Port, proxySettings.UserName, proxySettings.Password);
            }

            Cef.Initialize(settings);
        }
    }
}
