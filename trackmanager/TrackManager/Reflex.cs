﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TrackManagement;
using TrackManager.Steam;
using System.Linq;

namespace TrackManager
{
    public class Reflex
    {
        public Reflex()
        {
            lock (m_trackLocker)
            {
                m_tracks = HttpUtility.Get<Track[]>("https://spptqssmj8.execute-api.us-east-1.amazonaws.com/test/tracks?validation=valid");
            }
            m_uiFiles = new string[]
            {
                  "MXTables_DLC008.dx9.database"
                , "MXTables_DLC008.dx9.package"
                , "MXTables_DLC008a.dx9.database"
                , "MXTables_DLC008a.dx9.package"
                , "MXUI_DLC008.dx9.database"
                , "MXUI_DLC008.dx9.package"
            };
            m_reflexWasRunningLastFrame = false;
            m_nextTrackPollTime = TimeUtility.DateTimeToUnixTimeStamp(DateTime.UtcNow) + TrackPollTimeInSeconds;
        }

        public static string InstallPath { get; private set; } = string.Empty;
        public static string DatabasePath { get; private set; } = string.Empty;
        public static string LocalImagePath { get; private set; } = string.Empty;
        public static string LocalTrackPath { get; private set; } = string.Empty;
        public static string LocalSettingsPath { get; private set; } = string.Empty;
        public const int SlotCount = 8;

        public void ValidateInstallation()
        {
            InstallPath = GetInstallPath();
            DatabasePath = InstallPath + @"\Database";
            LocalImagePath = Environment.CurrentDirectory + @"\Images";
            LocalTrackPath = Environment.CurrentDirectory + @"\Tracks";
            LocalSettingsPath = Environment.CurrentDirectory + @"\Settings";

            if (Directory.Exists(LocalImagePath) == false)
            {
                Directory.CreateDirectory(LocalImagePath);
            }

            if (Directory.Exists(LocalTrackPath) == false)
            {
                Directory.CreateDirectory(LocalTrackPath);
            }

            if (Directory.Exists(LocalSettingsPath) == false)
            {
                Directory.CreateDirectory(LocalSettingsPath);
            }

            Console.WriteLine("Detected Mx vs Atv Reflex install path: " + InstallPath);

            if (BetaSlotsInstalled() == false)
            {
                Console.WriteLine("Installing beta slots...");
                InstallBetaSlots();
                Console.WriteLine("Beta slots install complete.");
            }
            else
            {
                Console.WriteLine("Beta slots already installed.");
            }
        }

        public void DownloadImages()
        {
            lock (m_trackLocker)
            {
                var serverTracks = m_tracks.Select(t => t.TrackName.Trim()).ToArray();
                var localImages = GetImageFilesOnDisk();
                var newImages = serverTracks.Except(localImages).ToArray();

                if (newImages.Length > 0)
                {
                    Console.WriteLine(string.Format("Preparing to download {0} new preview images...", newImages.Length));
                }

                foreach (var image in newImages)
                {
                    Console.WriteLine(string.Format("Downloading preview image \"{0}\"", image));
                    TrackInstaller.DownloadImage(image);
                }

                if (newImages.Length > 0)
                {
                    Console.WriteLine("Preview image downloads complete.");
                }
            }
        }

        public void InstallRandomTracksOnFirstRun()
        {
            if(LocalSettings.TrackSettingsExist() == false)
            {
                Console.WriteLine("First time run detected. Preparing to install an initial set of random tracks.");

                var trackSets = new string[] { TrackType.National, TrackType.Supercross, TrackType.FreeRide };
                foreach (var set in trackSets)
                {
                    TrackInstaller.EnqueueRandomRandomTracks(set);

                    //Cant install multiple sets at once so block until each one is complete.
                    while (TrackInstaller.InstallQueueIsEmpty == false)
                    {
                        TrackInstaller.ProcessDownloadQueue();
                    }
                }
            }
        }

        public void Process()
        {
            ProcessOverlayInjection();
            TrackInstaller.ProcessDownloadQueue();
            Sharing.Process();
            PollTracks();
        }

        public static string[] GetImageFilesOnDisk()
        {
            var files = Directory.GetFiles(LocalImagePath);
            return files.Select(t => Path.GetFileNameWithoutExtension(t.Trim())).ToArray();
        }

        public static string[] GetTracksOnDisk()
        {
            var files = Directory.GetFiles(LocalTrackPath);
            return files.Select(t => Path.GetFileNameWithoutExtension(t.Trim())).ToArray();
        }

        public static void SetOverlayVisibility(bool visible)
        {
            lock (m_overlayLocker)
            {
                m_overlayVisible = visible;
            }
        }

        public static Track[] GetTracks()
        {
            lock(m_trackLocker)
            {
                return m_tracks;
            }
        }

        public static bool OverlayIsVisible()
        {
            lock(m_overlayLocker)
            {
                return m_overlayVisible;
            }
        }

        private void PollTracks()
        {
            if(TimeUtility.DateTimeToUnixTimeStamp(DateTime.UtcNow) > m_nextTrackPollTime)
            {
                Console.WriteLine("Polling tracks...");
                lock (m_trackLocker)
                {
                    m_tracks = HttpUtility.Get<Track[]>("https://spptqssmj8.execute-api.us-east-1.amazonaws.com/test/tracks?validation=valid");
                }
                m_nextTrackPollTime = TimeUtility.DateTimeToUnixTimeStamp(DateTime.UtcNow) + TrackPollTimeInSeconds;
            }
        }

        private void ProcessOverlayInjection()
        {
            bool reflexIsRunning = System.Diagnostics.Process.GetProcessesByName("MXReflex").Length > 0;

            if(m_reflexWasRunningLastFrame == false && reflexIsRunning)
            {
                Console.WriteLine("Injecting track management overlay into the MXReflex process.");
                string injectorProcess = "Overlay/Injector.exe";
#if DEBUG
                injectorProcess = "../../../../injector/Debug/Injector.exe";
#endif
                System.Diagnostics.Process.Start(injectorProcess);

                Console.WriteLine("Press f11 on your keyboard to toggle the track management overlay on and off while in game.");
            }
            else if(m_reflexWasRunningLastFrame == true && reflexIsRunning == false)
            {
                Console.WriteLine("The MXReflex process was closed.");
                Console.WriteLine("Waiting for you to launch MX vs. ATV Reflex...");
            }

            m_reflexWasRunningLastFrame = reflexIsRunning;
        }

        #region BetaSlots
        private bool BetaSlotsInstalled()
        {
            if (File.Exists(string.Format(@"{0}\{1}", InstallPath, "MX07Leaderboards.bxml")) == false)
            {
                return false;
            }

            foreach(var uiFile in m_uiFiles)
            {
                if (File.Exists(string.Format(@"{0}\Database\{1}", InstallPath, uiFile)) == false)
                {
                    return false;
                }
            }
            return true;
        }

        private void InstallBetaSlots()
        {
            string betaSlotUrl = "https://s3.amazonaws.com/reflextracks/BetaSlots";
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(string.Format("{0}/{1}", betaSlotUrl, "MX07Leaderboards.bxml"), string.Format(@"{0}\{1}", InstallPath, "MX07Leaderboards.bxml"));

                foreach (var uiFile in m_uiFiles)
                {
                    client.DownloadFile(string.Format("{0}/{1}", betaSlotUrl, uiFile), string.Format(@"{0}\{1}", DatabasePath, uiFile));
                }
            }
        }
        #endregion


        #region InstallPath
        private string GetInstallPath()
        {
            string installPath = string.Empty;
            var steamInstallPath = GetSteamInstallPath(@"SOFTWARE\Valve\Steam");

            //If we didn't find it in the 32bit registry path search the 64 bit registry path
            if (steamInstallPath.Length == 0)
            {
                steamInstallPath = GetSteamInstallPath(@"SOFTWARE\Wow6432Node\Valve\Steam");
            }

            if (steamInstallPath.Length > 0)
            {
                installPath = GetInstallPath(steamInstallPath);
                if(installPath.Length == 0)
                {
                    var libraryFolders = GetLibraryFolders(steamInstallPath);
                    foreach(var folder in libraryFolders)
                    {
                        installPath = GetInstallPath(folder);
                        if(installPath.Length > 0)
                        {
                            break;
                        }
                    }
                }

                if(installPath.Length == 0)
                {
                    throw new Exception("Unable to detect MX vs. ATV Reflex install folder. Please ensure you have installed it via Steam.");
                }
            }
            else
            {
                throw new Exception("Unable to detect Steam install location. Please ensure Steam and MX vs. ATV Reflex are installed.");
            }

            return installPath;
        }

        private string GetSteamInstallPath(string steamKey)
        {
            string path = string.Empty;
            RegistryKey key = Registry.LocalMachine.OpenSubKey(steamKey);
            if(key != null)
            {
                object installpath = key.GetValue("InstallPath");
                if(installpath != null)
                {
                    path = installpath.ToString();
                }
            }

            return path;
        }

        private string[] GetLibraryFolders(string steamInstallPath)
        {
            List<string> libraryFolders = new List<string>();
            string libraryFoldersManifest = string.Format(@"{0}\{1}\libraryfolders.vdf", steamInstallPath, "steamapps");

            if (File.Exists(libraryFoldersManifest))
            {
                var reader = new AcfReader(libraryFoldersManifest);
                if (reader.CheckIntegrity())
                {
                    var manifestObject = reader.ACFFileToStruct();
                    for (int i = 1; ; ++i) //infinite loop
                    {
                        if (manifestObject.SubACF["LibraryFolders"].SubItems.ContainsKey(i.ToString()))
                        {
                            libraryFolders.Add(manifestObject.SubACF["LibraryFolders"].SubItems[i.ToString()].Replace(@"\\", @"\"));
                        }
                        else // no more steam libraries
                        {
                            break;
                        }
                    }
                }
            }

            return libraryFolders.ToArray();
        }

        private string GetInstallPath(string steamInstallPath)
        {
            string installPath = string.Empty;
            string basePath = string.Format(@"{0}\{1}", steamInstallPath, "steamapps");

            string[] files = Directory.GetFiles(basePath, "*.acf");

            foreach(var file in files)
            {
                var reader = new AcfReader(file);
                if (reader.CheckIntegrity())
                {
                    var manifestObject = reader.ACFFileToStruct();
                    if(manifestObject.SubACF["AppState"].SubItems["name"] == ReflexNameInSteam)
                    {
                        installPath = string.Format(@"{0}\steamapps\common\{1}", steamInstallPath, manifestObject.SubACF["AppState"].SubItems["installdir"]);
                        break;
                    }
                }
            }

            return installPath;
        }
        #endregion

        private static Track[] m_tracks;
        private static bool m_overlayVisible = false;
        private bool m_reflexWasRunningLastFrame;
        private long m_nextTrackPollTime;
        private const int TrackPollTimeInSeconds = 60*5; //five minutes
        private static readonly object m_trackLocker = new object();
        private static readonly object m_overlayLocker = new object();
        private const string ReflexNameInSteam = "MX vs. ATV Reflex";
        private readonly string[] m_uiFiles;
    }
}
