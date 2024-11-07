using UnityEngine;
using UnityEditor;
using UnityEngine.Networking; // Utilisation directe de UnityWebRequest
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // Pour la gestion asynchrone

namespace Pathfinding {
    /// <summary>Handles update checking for the A* Pathfinding Project</summary>
    [InitializeOnLoad]
    public static class AstarUpdateChecker
    {
        static UnityWebRequest updateCheckDownload;

        static System.DateTime _lastUpdateCheck;
        static bool _lastUpdateCheckRead;

        static System.Version _latestVersion;
        static System.Version _latestBetaVersion;

        /// <summary>Description of the latest update of the A* Pathfinding Project</summary>
        static string _latestVersionDescription;

        static bool hasParsedServerMessage;

        const double updateCheckRate = 1F;
        const string updateURL = "http://www.arongranberg.com/astar/version.php";

        public static System.DateTime lastUpdateCheck
        {
            get
            {
                try
                {
                    if (_lastUpdateCheckRead) return _lastUpdateCheck;

                    _lastUpdateCheck = System.DateTime.Parse(EditorPrefs.GetString("AstarLastUpdateCheck", "1/1/1971 00:00:01"), System.Globalization.CultureInfo.InvariantCulture);
                    _lastUpdateCheckRead = true;
                }
                catch (System.FormatException)
                {
                    lastUpdateCheck = System.DateTime.UtcNow;
                    Debug.LogWarning("Invalid DateTime string encountered when loading from preferences");
                }
                return _lastUpdateCheck;
            }
            private set
            {
                _lastUpdateCheck = value;
                EditorPrefs.SetString("AstarLastUpdateCheck", _lastUpdateCheck.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        public static System.Version latestVersion
        {
            get
            {
                RefreshServerMessage();
                return _latestVersion ?? AstarPath.Version;
            }
            private set
            {
                _latestVersion = value;
            }
        }

        public static System.Version latestBetaVersion
        {
            get
            {
                RefreshServerMessage();
                return _latestBetaVersion ?? AstarPath.Version;
            }
            private set
            {
                _latestBetaVersion = value;
            }
        }

        public static string latestVersionDescription
        {
            get
            {
                RefreshServerMessage();
                return _latestVersionDescription ?? "";
            }
            private set
            {
                _latestVersionDescription = value;
            }
        }

        static Dictionary<string, string> astarServerData = new Dictionary<string, string> {
            { "URL:modifiers", "http://www.arongranberg.com/astar/docs/modifiers.php" },
            { "URL:astarpro", "http://arongranberg.com/unity/a-pathfinding/astarpro/" },
            { "URL:documentation", "http://arongranberg.com/astar/docs/" },
            { "URL:findoutmore", "http://arongranberg.com/astar" },
            { "URL:download", "http://arongranberg.com/unity/a-pathfinding/download" },
            { "URL:changelog", "http://arongranberg.com/astar/docs/changelog.php" },
            { "URL:tags", "http://arongranberg.com/astar/docs/tags.php" },
            { "URL:homepage", "http://arongranberg.com/astar/" }
        };

        static AstarUpdateChecker()
        {
            EditorApplication.update += UpdateCheckLoop;
            EditorBase.getDocumentationURL = () => GetURL("documentation");
        }

        static void RefreshServerMessage()
        {
            if (!hasParsedServerMessage)
            {
                var serverMessage = EditorPrefs.GetString("AstarServerMessage");
                if (!string.IsNullOrEmpty(serverMessage))
                {
                    ParseServerMessage(serverMessage);
                    ShowUpdateWindowIfRelevant();
                }
            }
        }

        public static string GetURL(string tag)
        {
            RefreshServerMessage();
            string url;
            astarServerData.TryGetValue("URL:" + tag, out url);
            return url ?? "";
        }

        public static async void CheckForUpdatesNow()
        {
            lastUpdateCheck = System.DateTime.UtcNow.AddDays(-5);
            EditorApplication.update -= UpdateCheckLoop;
            EditorApplication.update += UpdateCheckLoop;
            await DownloadVersionInfoAsync(); // Appel asynchrone
        }

        static void UpdateCheckLoop()
        {
            if (!CheckForUpdates())
            {
                EditorApplication.update -= UpdateCheckLoop;
            }
        }

        static bool CheckForUpdates()
        {
            if (updateCheckDownload != null && updateCheckDownload.isDone)
            {
                if (!string.IsNullOrEmpty(updateCheckDownload.error))
                {
                    Debug.LogWarning("Error checking for updates to the A* Pathfinding Project: " + updateCheckDownload.error);
                    updateCheckDownload = null;
                    return false;
                }
                UpdateCheckCompleted(updateCheckDownload.downloadHandler.text);
                updateCheckDownload.Dispose();
                updateCheckDownload = null;
            }

            var offsetMinutes = (Application.isPlaying && Time.time > 60) || AstarPath.active != null ? -20 : 20;
            var minutesUntilUpdate = lastUpdateCheck.AddDays(updateCheckRate).AddMinutes(offsetMinutes).Subtract(System.DateTime.UtcNow).TotalMinutes;
            if (minutesUntilUpdate < 0)
            {
                DownloadVersionInfoAsync();
            }

            return updateCheckDownload != null || minutesUntilUpdate < 10;
        }

        static async Task DownloadVersionInfoAsync()
        {
            string query = updateURL + "?v=" + AstarPath.Version + "&pro=0" + "&check=" + updateCheckRate + "&unityversion=" + Application.unityVersion;
            updateCheckDownload = UnityWebRequest.Get(query);
            updateCheckDownload.SendWebRequest();
            lastUpdateCheck = System.DateTime.UtcNow;
        }

        static void UpdateCheckCompleted(string result)
        {
            EditorPrefs.SetString("AstarServerMessage", result);
            ParseServerMessage(result);
            ShowUpdateWindowIfRelevant();
        }

        static void ParseServerMessage(string result)
        {
            if (string.IsNullOrEmpty(result)) return;

            hasParsedServerMessage = true;
            string[] splits = result.Split('|');
            latestVersionDescription = splits.Length > 1 ? splits[1] : "";

            if (splits.Length > 4)
            {
                var fields = splits.Skip(4).ToArray();
                for (int i = 0; i < (fields.Length / 2) * 2; i += 2)
                {
                    string key = fields[i];
                    string val = fields[i + 1];
                    astarServerData[key] = val;
                }
            }

            try
            {
                latestVersion = new System.Version(astarServerData["VERSION:branch"]);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Could not parse version: " + ex);
            }

            try
            {
                latestBetaVersion = new System.Version(astarServerData["VERSION:beta"]);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Could not parse beta version: " + ex);
            }
        }

        static void ShowUpdateWindowIfRelevant () {
#if !ASTAR_ATAVISM
			try {
				System.DateTime remindDate;
				var remindVersion = new System.Version(EditorPrefs.GetString("AstarRemindUpdateVersion", "0.0.0.0"));
				if (latestVersion == remindVersion && System.DateTime.TryParse(EditorPrefs.GetString("AstarRemindUpdateDate", "1/1/1971 00:00:01"), out remindDate)) {
					if (System.DateTime.UtcNow < remindDate) {
						// Don't remind yet
						return;
					}
				} else {
					EditorPrefs.DeleteKey("AstarRemindUpdateDate");
					EditorPrefs.DeleteKey("AstarRemindUpdateVersion");
				}
			} catch {
				Debug.LogError("Invalid AstarRemindUpdateVersion or AstarRemindUpdateDate");
			}

			var skipVersion = new System.Version(EditorPrefs.GetString("AstarSkipUpToVersion", AstarPath.Version.ToString()));

			if (AstarPathEditor.FullyDefinedVersion(latestVersion) != AstarPathEditor.FullyDefinedVersion(skipVersion) && AstarPathEditor.FullyDefinedVersion(latestVersion) > AstarPathEditor.FullyDefinedVersion(AstarPath.Version)) {
				EditorPrefs.DeleteKey("AstarSkipUpToVersion");
				EditorPrefs.DeleteKey("AstarRemindUpdateDate");
				EditorPrefs.DeleteKey("AstarRemindUpdateVersion");

				AstarUpdateWindow.Init(latestVersion, latestVersionDescription);
			}
#endif
		}
	}
}
