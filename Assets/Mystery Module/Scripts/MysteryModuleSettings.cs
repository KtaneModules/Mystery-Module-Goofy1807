using System.Collections.Generic;

public sealed class MysteryModuleSettings
{
    public string SiteUrl = @"https://ktane.timwi.de/json/raw";

    public Dictionary<string, string> RememberedCompatibilities = new Dictionary<string, string>();

    public bool HideUberSouvenir = true;

    public int Version = 2;
}