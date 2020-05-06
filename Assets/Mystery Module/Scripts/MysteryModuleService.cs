using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class MysteryModuleService : MonoBehaviour
{
    private string _settingsFile;
    private MysteryModuleSettings _settings;
    private string[] uberSouvenirModuleTypes = new string[] { "SouvenirModule", "ubermodule" };

    void Start()
    {
        name = "Mystery Module Service";

        _settingsFile = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), "MysteryModuleSettings.json");

        if (!File.Exists(_settingsFile))
            _settings = new MysteryModuleSettings();
        else
        {
            try
            {
                _settings = JsonConvert.DeserializeObject<MysteryModuleSettings>(File.ReadAllText(_settingsFile), new StringEnumConverter());
                if (_settings == null)
                    throw new Exception("Settings could not be read. Creating new Settings...");
                Debug.LogFormat(@"[Mystery Module Service] Settings successfully loaded");
            }
            catch (Exception e)
            {
                Debug.LogFormat(@"[Mystery Module Service] Error loading settings file:");
                Debug.LogException(e);
                _settings = new MysteryModuleSettings();
            }
        }

        _settings.Version = 2;
        Debug.LogFormat(@"[Mystery Module Service] Service is active");
        StartCoroutine(GetData());
    }

    public bool MustAutoSolve(string moduleId)
    {
        string setting;
        return _settings.RememberedCompatibilities.TryGetValue(moduleId, out setting) && setting == "RequiresAutoSolve";
    }

    public bool MustNotBeHidden(string moduleId)
    {
        string setting;
        return (_settings.RememberedCompatibilities.TryGetValue(moduleId, out setting) && (setting == "MustNotBeHidden" || setting == "MustNotBeHiddenOrKey"))
            || (!_settings.HideUberSouvenir && uberSouvenirModuleTypes.Contains(moduleId));
    }

    public bool MustNotBeKey(string moduleId)
    {
        string setting;
        return _settings.RememberedCompatibilities.TryGetValue(moduleId, out setting) && (setting == "MustNotBeKey" || setting == "MustNotBeHiddenOrKey");
    }

    IEnumerator GetData()
    {
        using (var http = UnityWebRequest.Get(_settings.SiteUrl))
        {
            // Request and wait for the desired page.
            yield return http.SendWebRequest();

            if (http.isNetworkError)
            {
                Debug.LogFormat(@"[Mystery Module Service] Website {0} responded with error: {1}", _settings.SiteUrl, http.error);
                yield break;
            }

            if (http.responseCode != 200)
            {
                Debug.LogFormat(@"[Mystery Module Service] Website {0} responded with code: {1}", _settings.SiteUrl, http.responseCode);
                yield break;
            }

            var allModules = JObject.Parse(http.downloadHandler.text)["KtaneModules"] as JArray;
            if (allModules == null)
            {
                Debug.LogFormat(@"[Mystery Module Service] Website {0} did not respond with a JSON array at “KtaneModules” key.", _settings.SiteUrl, http.responseCode);
                yield break;
            }

            var compatibilities = new Dictionary<string, string>();

            foreach (JObject module in allModules)
            {
                var id = module["ModuleID"] as JValue;
                if (id == null || !(id.Value is string))
                    continue;
                var compatibility = module["MysteryModule"] as JValue;
                if (compatibility == null || !(compatibility.Value is string))
                    continue;
                compatibilities[(string) id.Value] = (string) compatibility.Value;
            }

            Debug.LogFormat(@"[Mystery Module Service] List successfully loaded:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, compatibilities.Select(kvp => string.Format("[Mystery Module Service] {0} => {1}", kvp.Key, kvp.Value)).ToArray()));
            _settings.RememberedCompatibilities = compatibilities;

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(_settingsFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));
                File.WriteAllText(_settingsFile, JsonConvert.SerializeObject(_settings, Formatting.Indented, new StringEnumConverter()));
            }
            catch (Exception e)
            {
                Debug.LogFormat("[Mystery Module Service] Failed to save settings file:");
                Debug.LogException(e);
            }
        }
    }
}