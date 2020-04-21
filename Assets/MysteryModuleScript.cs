using KModkit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

public class MysteryModuleScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMBossModule BossModule;
    public TextMesh Screen;
    public Light LED;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    public KMSelectable NextModule;
    public KMSelectable Failswitch;
    public GameObject Cover;

    sealed class remainingCandidatesInfo
    {
        public List<GameObject> remainingCandidates = new List<GameObject>();
    }

    private static readonly Dictionary<string, remainingCandidatesInfo> _infos = new Dictionary<string, remainingCandidatesInfo>();

    static private string[] _ignore = { "Encryption Bingo", "Cookie Jars", "Hogwarts", "Forget The Colors", "14", "Bamboozling Time Keeper", "Brainf---", "Forget Enigma", "Forget Everything", "Forget It Not", "Forget Me Not", "Forget Me Later", "Forget Perspective", "Forget Them All", "Forget This", "Forget Us Not", "Organization", "Purgatory", "Simon Forgets", "Simon's Stages", "Souvenir", "Tallordered Keys", "The Time Keeper", "The Troll", "The Very Annoying Button", "Timing Is Everything", "Turn The Key", "Ultimate Custom Night", "Übermodule" };
    private string[] bossModules;
    private List<string> keysName = new List<string>();
    private string[] possibleCandidatesName;

    private List<GameObject> keys = new List<GameObject>();
    private string mystifyName;
    private bool FailswitchPressed = false;
    private bool nextStage = false;
    private bool failsolve = false;
    private bool strikeActive = false;

    private Vector3 mystifyScale;

    private GameObject mystify;

    private void Start()
    {
        moduleId = moduleIdCounter++;

        NextModule.OnInteract += delegate
        {
            if (moduleSolved || strikeActive) return false;
            NextModule.AddInteractionPunch();
            if (FailswitchPressed)
            {
                FailswitchPressed = false;
                StartCoroutine(FailSwitchAborted());
                return false;
            }
            if (!nextStage)
            {
                StartCoroutine(StrikeHandler());
                return false;
            }

            if (failsolve)
            {
                UnlockMystery();
                return false;
            }

            Debug.LogFormat(@"[Mystery Module #{0}] Advancing to the next stage after solving {1}", moduleId, keys.First().GetComponent<KMBombModule>().ModuleDisplayName);
            keys.RemoveAt(0);
            nextStage = false;
            SetLED(255, 0, 0);
            SetKey();
            return false;
        };

        Failswitch.OnInteract += delegate
        {
            if (moduleSolved || strikeActive) return false;
            if (FailswitchPressed)
            {
                UnlockMystery();
                Failswitch.AddInteractionPunch();
                Debug.LogFormat(@"[Mystery Module #{0}] 'Failswitch' was pressed - Remaining time was cut in half", moduleId);
                if (failsolve)
                    setScreen("Why would you do that!?", 255, 0, 0);
                TimeRemaining.FromModule(Module, Bomb.GetTime() * 0.75f);
                return false;
            }
            FailswitchPressed = true;
            Debug.LogFormat(@"[Mystery Module #{0}] Defuser was asked if he really wants to press the Fail Switch", moduleId);
            setScreen("Are you sure?", 255, 0, 0);
            return false;
        };

        StartCoroutine(Setup());
    }

    private IEnumerator Setup()
    {
        yield return null;

        bossModules = BossModule.GetIgnoredModules(Module, _ignore);
        if (bossModules == null)
            bossModules = _ignore;

        var serialNumber = Bomb.GetSerialNumber();
        if (!_infos.ContainsKey(serialNumber))
        {
            _infos[serialNumber] = new remainingCandidatesInfo();

            possibleCandidatesName = Bomb.GetSolvableModuleNames().Except(bossModules).Where(n => n != Module.ModuleDisplayName).Distinct().ToArray();

            for (int i = 0; i < transform.parent.childCount; i++)
            {
                var module = transform.parent.GetChild(i).gameObject.GetComponent<KMBombModule>();
                if (module == null)
                    continue;
                var moduleName = module.ModuleDisplayName;

                if (possibleCandidatesName.Any(n => n == moduleName) && !_infos[serialNumber].remainingCandidates.Any(n => n.GetComponent<KMBombModule>().ModuleDisplayName == moduleName))
                    _infos[serialNumber].remainingCandidates.Add(module.gameObject);
            }
        }

        var remainingCandidates = _infos[serialNumber].remainingCandidates;

        if (remainingCandidates.Count == 0)
        {
            Debug.LogFormat(@"[Mystery Module #{0}] No possible candidates for mystifying or using as a key found - Green button can be pressed to solve this module", moduleId);
            nextStage = true;
            SetLED(0, 255, 0);
            setScreen("Free solve :D", 0, 255, 0);
            failsolve = true;
            StopAllCoroutines();
            yield break;
        }

        remainingCandidates.Shuffle();
        mystify = remainingCandidates.First();
        remainingCandidates.RemoveAt(0);

        if (remainingCandidates.Count == 0)
        {
            Debug.LogFormat(@"[Mystery Module #{0}] No candidates left to use as a key after mystifying a module - Green button can be pressed to solve this module", moduleId);
            nextStage = true;
            SetLED(0, 255, 0);
            setScreen("Free solve :D", 0, 255, 0);
            failsolve = true;
            StopAllCoroutines();
            yield break;
        }
        var range = Random.Range(1, remainingCandidates.Count / 2);

        for (int i = 0; i < range; i++)
        {
            keys.Add(remainingCandidates.First());
            keysName.Add(remainingCandidates.First().GetComponent<KMBombModule>().ModuleDisplayName);
            remainingCandidates.RemoveAt(0);
        }

        mystifyName = mystify.GetComponent<KMBombModule>().ModuleDisplayName;

        Debug.LogFormat(@"[Mystery Module #{0}] Boss Modules are: {1}, Keys are: {2}, Mystified module is: {3}", moduleId, bossModules.Join(", "), keysName.Join(", "), mystifyName);

        var maxBounds = GetMaxBounds(mystify);
        var coverBounds = GetMaxBounds(Cover);

        Cover.SetActive(true);

        Cover.transform.position = maxBounds.center;
        Cover.transform.parent = mystify.gameObject.transform;

        var scale = new Vector3(coverBounds.size.x / maxBounds.size.x, coverBounds.size.y / maxBounds.size.y, coverBounds.size.z / maxBounds.size.z);
        Cover.transform.localScale = scale;
        Cover.transform.rotation = mystify.transform.rotation;

        Cover.transform.parent = transform;

        SetKey();
        StartCoroutine(checkSolves());

        mystifyScale = mystify.transform.localScale;
        mystify.transform.localScale = new Vector3(0, 0, 0);

    }

    private void setScreen(string text, byte r, byte g, byte b)
    {
        Screen.text = text;
        Screen.color = new Color32(r, g, b, 255);
    }

    private void SetLED(byte r, byte g, byte b)
    {
        LED.color = new Color32(r, g, b, 255);
    }

    private void UnlockMystery()
    {
        StopAllCoroutines();
        Module.HandlePass();
        Debug.LogFormat(@"[Mystery Module #{0}] The mystery module was {1}", moduleId, failsolve == true ? "unable to find a mystifyable module. You won a free solve :D" : "succsessfully unlocked - Well done!");
        moduleSolved = true;
        LED.color = new Color32(0, 255, 0, 255);
        if (!failsolve)
        {
            setScreen("Mystified module unlocked!", 0, 255, 0);
            mystify.transform.localScale = mystifyScale;
            Cover.SetActive(false);
        }

    }

    private void SetKey()
    {
        if (keys.Count() < 1) UnlockMystery();

        else
        {
            setScreen(keys.First().GetComponent<KMBombModule>().ModuleDisplayName, 255, 255, 255);
        }
    }

    private IEnumerator checkSolves()
    {
        bool red = false;
        while (!moduleSolved)
        {
            if (Bomb.GetSolvedModuleNames().Contains(keys.First().GetComponent<KMBombModule>().ModuleDisplayName))
            {
                SetLED(red ? (byte)255 : (byte)0, red ? (byte)0 : (byte)255, 0);
                red = !red;
                nextStage = true;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator StrikeHandler()
    {
        strikeActive = true;
        setScreen("Strike!", 255, 0, 0);
        Debug.LogFormat(@"[Mystery Module #{0}] You tried to go to the next module without solving the current one - Strike", moduleId);
        Module.HandleStrike();
        yield return new WaitForSeconds(2f);
        SetKey();
        strikeActive = false;
    }

    private IEnumerator FailSwitchAborted()
    {
        setScreen("Failswitch aborted!", 0, 255, 0);
        yield return new WaitForSeconds(2f);
        SetKey();
    }

    Bounds GetMaxBounds(GameObject g)
    {
        var b = new Bounds(g.transform.position, Vector3.zero);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            b.Encapsulate(r.bounds);
        }
        foreach (Collider r in g.GetComponentsInChildren<Collider>())
        {
            b.Encapsulate(r.bounds);
        }
        return b;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} failswitch [press the red button] | !{0} next [press the green button]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        do
        {
            yield return "trycancel";
        } while (strikeActive);

        if (moduleSolved)
        {
            yield return "sendtochaterror The mystified module is already unlocked";
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*(red|fail|failswitch|kill|autosolve|cheat|yes|round)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Failswitch.OnInteract();
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*(green|next|continue|abort|square|go|solve)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            NextModule.OnInteract();
            yield break;
        }

        yield return null;
    }
}