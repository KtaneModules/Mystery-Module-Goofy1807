using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;

using Random = UnityEngine.Random;

public class MysteryModuleScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public TextMesh Screen;
    public Light LED;

    public KMSelectable NextModule;
    public KMSelectable Failswitch;
    public GameObject Cover;
    public GameObject PivotRight;
    public GameObject PivotLeft;

    sealed class remainingCandidatesInfo
    {
        public List<KMBombModule> RemainingCandidateKeys = new List<KMBombModule>();
        public List<KMBombModule> RemainingCandidateMystifiables = new List<KMBombModule>();
    }

    private static readonly Dictionary<string, remainingCandidatesInfo> _infos = new Dictionary<string, remainingCandidatesInfo>();

    private List<KMBombModule> keyModules;
    private KMBombModule mystifiedModule;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private bool FailswitchPressed = false;
    private bool nextStage = false;
    private bool failsolve = false;
    private bool strikeActive = false;

    // Indicates that the unlocking animation is still running — used by Souvenir
    private bool animating = false;

    private Vector3 mystifyScale;

    private void Start()
    {
        moduleId = moduleIdCounter++;
        Debug.LogFormat(@"[Mystery Module #{0}] Version: 2.0", moduleId);
        LED.range *= transform.lossyScale.x;

        NextModule.OnInteract += delegate
        {
            if (moduleSolved || strikeActive)
                return false;
            NextModule.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, NextModule.transform);
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
                StartCoroutine(UnlockMystery());
                return false;
            }

            Debug.LogFormat(@"[Mystery Module #{0}] Advancing to the next stage after solving {1}", moduleId, keyModules[0].ModuleDisplayName);
            keyModules.RemoveAt(0);
            nextStage = false;
            SetLED(255, 0, 0);
            SetKey();
            return false;
        };

        Failswitch.OnInteract += delegate
        {
            if (moduleSolved || strikeActive)
                return false;
            Failswitch.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Failswitch.transform);
            if (FailswitchPressed)
            {
                StartCoroutine(UnlockMystery());
                Debug.LogFormat(@"[Mystery Module #{0}] 'Failswitch' was pressed - Remaining time was cut by a quarter", moduleId);
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

        // Find the Mystery Module Service and obtain the list of compatibilities
        var mmService = FindObjectOfType<MysteryModuleService>();
        if (mmService == null)
        {
            Debug.LogFormat(@"[Mystery Module #{0}] Catastrophic problem: Mystery Module Service is not present.");
            goto mustAutoSolve;
        }

        var offendingModule = Bomb.GetSolvableModuleIDs().FirstOrDefault(n => mmService.MustAutoSolve(n));
        if (offendingModule != null)
        {
            Debug.LogFormat(@"[Mystery Module #{0}] The module {1} ruins the mood! Green button can be pressed to solve this module.", moduleId, offendingModule);
            goto mustAutoSolve;
        }

        var serialNumber = Bomb.GetSerialNumber();
        remainingCandidatesInfo inf;
        if (!_infos.ContainsKey(serialNumber))
        {
            inf = new remainingCandidatesInfo();
            _infos[serialNumber] = inf;

            for (int i = 0; i < transform.parent.childCount; i++)
            {
                var module = transform.parent.GetChild(i).gameObject.GetComponent<KMBombModule>();
                if (module == null)
                    continue;

                if (!mmService.MustNotBeHidden(module.ModuleType))
                    inf.RemainingCandidateMystifiables.Add(module);
                if (!mmService.MustNotBeKey(module.ModuleType))
                    inf.RemainingCandidateKeys.Add(module);
            }
        }
        else
            inf = _infos[serialNumber];

        var mystifiableCandidates = inf.RemainingCandidateMystifiables.Where(m =>
        {
            // We need to make sure that Mystery Modules do not hide themselves in a cycle.
            // This code relies on the fact that we haven’t chosen a mystified module yet (mystsifiedModule for us is null).
            MysteryModuleScript scr;
            var my = m;
            while ((scr = my.GetComponent<MysteryModuleScript>()) != null && scr.mystifiedModule != null)
                my = scr.mystifiedModule;

            return my != Module;
        }).ToArray();

        if (mystifiableCandidates.Length == 0)
        {
            Debug.LogFormat(@"[Mystery Module #{0}] No possible candidate found to mystify - Green button can be pressed to solve this module.", moduleId);
            goto mustAutoSolve;
        }

        mystifiedModule = mystifiableCandidates[Random.Range(0, mystifiableCandidates.Length)];
        inf.RemainingCandidateMystifiables.Remove(mystifiedModule);
        inf.RemainingCandidateKeys.Remove(mystifiedModule); // A mystified module cannot be a key anymore (for this or any other Mystery Module)

        if (inf.RemainingCandidateKeys.Count == 0)
        {
            Debug.LogFormat(@"[Mystery Module #{0}] No possible candidates for using as a key found - Green button can be pressed to solve this module.", moduleId);
            goto mustAutoSolve;
        }

        // Select a number of key modules
        keyModules = inf.RemainingCandidateKeys.ToList().Shuffle()
            .Take(Random.Range(1, Mathf.Max(2, inf.RemainingCandidateKeys.Count / (Bomb.GetSolvableModuleIDs().Count(m => m == Module.ModuleType) + 1))))
            .ToList();

        // These key modules can no longer be mystified by any other Mystery Module
        foreach (var km in keyModules)
            inf.RemainingCandidateMystifiables.Remove(km);

        Debug.LogFormat(@"[Mystery Module #{0}] Keys are: {1}, mystified module is: {2}", moduleId, keyModules.Select(km => km.ModuleDisplayName).Join(", "), mystifiedModule.ModuleDisplayName);

        var maxBounds = GetMaxBounds(mystifiedModule.gameObject);
        var coverBounds = GetMaxBounds(Cover);

        Cover.SetActive(true);

        Cover.transform.position = maxBounds.center;
        Cover.transform.parent = mystifiedModule.gameObject.transform;

        var scale = new Vector3(coverBounds.size.x / maxBounds.size.x, coverBounds.size.y / maxBounds.size.y, coverBounds.size.z / maxBounds.size.z);
        Cover.transform.localScale = scale;
        Cover.transform.rotation = mystifiedModule.transform.rotation;
        yield return null;
        Cover.transform.parent = transform.parent;

        SetKey();
        StartCoroutine(checkSolves());

        mystifyScale = mystifiedModule.transform.localScale;
        mystifiedModule.transform.localScale = new Vector3(0, 0, 0);
        yield break;

        mustAutoSolve:
        nextStage = true;
        SetLED(0, 255, 0);
        setScreen("Free solve :D", 0, 255, 0);
        failsolve = true;
        StopAllCoroutines();
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

    private IEnumerator UnlockMystery()
    {
        animating = true;
        Module.HandlePass();
        Debug.LogFormat(@"[Mystery Module #{0}] The mystery module was {1}", moduleId, failsolve ? "unable to find a mystifyable module. You won a free solve :D" : "successfully unlocked - Well done!");
        moduleSolved = true;
        LED.color = new Color32(0, 255, 0, 255);
        if (!failsolve)
        {
            setScreen("Mystified module unlocked!", 0, 255, 0);

            var duration = 2f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                mystifiedModule.transform.localScale = Vector3.Lerp(new Vector3(0, 0, 0), mystifyScale, elapsed / duration);
                PivotRight.transform.localEulerAngles = new Vector3(0, 0, -90 * elapsed / duration);
                PivotLeft.transform.localEulerAngles = new Vector3(0, 0, 90 * elapsed / duration);
            }
            mystifiedModule.transform.localScale = mystifyScale;
            Destroy(Cover);
        }
        animating = false;
        StopAllCoroutines();
    }

    private void SetKey()
    {
        if (keyModules.Count() == 0)
            StartCoroutine(UnlockMystery());
        else
            setScreen(keyModules[0].ModuleDisplayName, 255, 255, 255);
    }

    private IEnumerator checkSolves()
    {
        bool red = false;
        while (!moduleSolved)
        {
            if (Bomb.GetSolvedModuleIDs().Contains(keyModules[0].ModuleType))
            {
                SetLED(red ? (byte) 255 : (byte) 0, red ? (byte) 0 : (byte) 255, 0);
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
        Debug.LogFormat(@"[Mystery Module #{0}] You tried to go to the next module without solving the current one - Strike!", moduleId);
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

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        StartCoroutine(UnlockMystery());
        while (!moduleSolved)
            yield return true;
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} failswitch [press the red button] | !{0} next [press the green button]";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if (moduleSolved)
        {
            yield return "sendtochaterror The mystified module is already unlocked";
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*(red|fail|failswitch|kill|autosolve|cheat|yes|round)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            do
                yield return "trycancel";
            while (strikeActive);
            Failswitch.OnInteract();
            yield break;
        }

        if (Regex.IsMatch(command, @"^\s*(green|next|continue|abort|square|go|solve)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            do
                yield return "trycancel";
            while (strikeActive);
            NextModule.OnInteract();
            yield break;
        }

        yield return null;
    }
}