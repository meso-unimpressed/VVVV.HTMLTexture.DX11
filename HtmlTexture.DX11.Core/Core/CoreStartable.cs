﻿using System;
using System.Diagnostics;
using System.IO;
using Chromium;

namespace VVVV.HtmlTexture.DX11.Core
{
    public static class HtmlTextureStartable
    {
        public static bool Started { get; private set; }

        public static CfxBrowserProcessHandler BrowserProcessHandler { get; private set; }

        private static void app_OnBeforeCommandLineProcessing(object sender, Chromium.Event.CfxOnBeforeCommandLineProcessingEventArgs e)
        {
            // e.CommandLine.AppendSwitch("enable-speech-input");

            e.CommandLine.AppendSwitch("ignore-gpu-blacklist");
            e.CommandLine.AppendSwitch("enable-experimental-canvas-features");
            e.CommandLine.AppendSwitch("enable-gpu-memory-buffer-video-frames");
            e.CommandLine.AppendSwitch("enable-native-gpu-memory-buffers");
            e.CommandLine.AppendSwitchWithValue("disable-gpu-vsync", "beginframe");
            e.CommandLine.AppendSwitchWithValue("enable-accelerated-vpx-decode", "0x03");

            // DO NOT USE THIS TO BROWSE THE INTERNET
            e.CommandLine.AppendSwitch("no-user-gesture-required");
            e.CommandLine.AppendSwitch("allow-file-access");
            e.CommandLine.AppendSwitch("allow-file-access-from-files");
            e.CommandLine.AppendSwitch("allow-cross-origin-auth-prompt");

            e.CommandLine.AppendSwitchWithValue("disable-features",
                "CrossSiteDocumentBlockingAlways," +
                "CrossSiteDocumentBlockingIfIsolating"
            );

            e.CommandLine.AppendSwitchWithValue("enable-features",
                "OverlayScrollbar"
            );

            //e.CommandLine.AppendSwitchWithValue("enable-blink-features",
            //    "PaintTouchActionRects"
            //);

            e.CommandLine.AppendSwitch("smooth-scrolling");

            e.CommandLine.AppendSwitchWithValue("touch-events", "enabled");
            e.CommandLine.AppendSwitch("disable-pinch");

            // enable pepper flash or system Flash
            if (Directory.Exists(Path.Combine(Globals.AssemblyDir, @"cef\PepperFlash")))
            {
                e.CommandLine.AppendSwitchWithValue("ppapi-flash-version", "19.0.0.201");
                e.CommandLine.AppendSwitchWithValue("ppapi-flash-path", Path.Combine(Globals.AssemblyDir, @"cef\PepperFlash\pepflashplayer.dll"));
            }
            else
            {
                e.CommandLine.AppendSwitch("enable-system-flash");
            }

            // MessageBox.Show(e.CommandLine.CommandLineString);
        }

        // Main entry point when called by vvvv
        public static void Start()
        {
            CfxRuntime.LibCefDirPath = Globals.AssemblyDir;

            var app = new CfxApp();
            app.OnBeforeCommandLineProcessing += app_OnBeforeCommandLineProcessing;

            BrowserProcessHandler = new CfxBrowserProcessHandler();
            BrowserProcessHandler.OnBeforeChildProcessLaunch += (sender, args) =>
            {
                args.CommandLine.AppendSwitchWithValue("vvvv-pid", Process.GetCurrentProcess().Id.ToString());
            };
            app.GetBrowserProcessHandler += (sender, args) => args.SetReturnValue(BrowserProcessHandler);

            var settings = new CfxSettings
            {
                //PackLoadingDisabled = true,
                WindowlessRenderingEnabled = true,
                NoSandbox = true,
                UserDataPath = Path.Combine(Globals.AssemblyDir, "userdata"),
                CachePath = Path.Combine(Globals.AssemblyDir, "cache"),
                BrowserSubprocessPath = Globals.AssemblyLocation,
                LogSeverity = CfxLogSeverity.Disable,
                //SingleProcess = false, // DEBUG
                MultiThreadedMessageLoop = true, // false
                IgnoreCertificateErrors = true,
                RemoteDebuggingPort = 8088,
            };

            Started = true;

            CfxRuntime.Initialize(settings, app, RenderProcess.RenderProcessMain);
        }
        public static void Shutdown()
        {
            CfxRuntime.Shutdown();
        }
    }
}
