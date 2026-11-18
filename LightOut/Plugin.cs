using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using BeatSaberMarkupLanguage.Settings;
using IPA;
using IPALogger = IPA.Logging.Logger;
using System.IO.Ports;
using BS_Utils;
using BS_Utils.Utilities;
using BeatSaberMarkupLanguage.Util;
using SiraUtil.Zenject;

namespace LightOut
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        public string Name => "LightToSerialRelight";
        public string Version => "1.3.0";

        internal static bool gameCoreJustLoaded = false;
        public static IPA.Logging.Logger log;
        public bool eventAdded = false;
        public static Plugin thisPlugin;

        [Init]
        public Plugin(IPA.Logging.Logger logger, Zenjector zenjector)
        {
            log = logger;
            zenjector.Install(Location.Player, container => container.BindInterfacesTo<EventListener>().AsSingle());
            log.Info("Initialized LightToSerialRelight");
            thisPlugin = this;
        }

        [OnStart]
        public void OnStart()
        {
            log.Info("Starting LightToSerialRelight");
            // Stay subscribed: a soft restart (settings OK) rebuilds the main menu and
            // destroys our DontDestroyOnLoad host, so the menu must be re-added every time.
            MainMenuAwaiter.MainMenuInitializing += AddSettingsMenu;
        }

        private Settings _menuHost;

        private void AddSettingsMenu()
        {
            if (_menuHost != null)
            {
                try { BSMLSettings.Instance.RemoveSettingsMenu(_menuHost); }
                catch (Exception e) { log.Debug(e); }
            }
            // Also clears a stale registration whose host Unity already destroyed.
            try { BSMLSettings.Instance.RemoveSettingsMenu(Settings.instance); }
            catch (Exception e) { log.Debug(e); }

            _menuHost = Settings.instance;
            BSMLSettings.Instance.AddSettingsMenu("LightOut", "LightOut.UI.settings.bsml", _menuHost);

            // Connect automatically instead of waiting for the user to press OK.
            if (_menuHost._isModEnabled && (Settings.arduinoPort == null || !Settings.arduinoPort.IsOpen))
            {
                _menuHost.OpenConnection();
            }
        }

        // Kept for Settings.UpdateConnection; the event listener is now installed via SiraUtil
        // and checks the connection itself when a level starts.
        public void BSEvents_menuSceneLoaded()
        {
            Plugin.log.Info("Connection settings applied");
        }

        [OnExit]
        public void OnExit()
        {
            Settings.instance.CloseConnection();
        }

    }
}