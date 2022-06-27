using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HoloScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public List<KMSelectable> buttons;
    public Renderer[] bgs;
    public Renderer[] screens;
    public Renderer[] bsymbs;
    public Renderer[] leds;
    public Material[] screenmats;
    public Material[] symbols;
    public Material[] ledmats;
    public Material sol;
    public Transform[] axes;
    public GameObject matstore;

    private readonly string[] symbnames = new string[] { "Apple", "Atom", "Boat", "Bolt", "Bowling Ball", "Bucket", "Aries", "Chessboard",
        "Clock", "Close", "Crosshair", "Crosslet", "Dollar", "Duality", "Fire", "Fleur-de-lis", "Gear", "Heart", "Knight", "Leaf", "Link",
        "Lock", "Mercury", "Meteor", "Moon", "Hash", "Ozone", "Peace", "Plane", "Planet", "Plus", "Puzzle Piece", "Question Mark", "Radioactive",
        "Recycling", "Rook", "Smile", "Spade", "Spiral", "Star", "Sun", "Target", "Taurus", "Tractor", "Tree", "Triforce", "Uranus", "Yinyang"};
    private int[] symbselect = new int[32];
    private int[,][] dispsymbs = new int[5, 2][];
    private bool[] lightside = new bool[16];
    private int[][] ans = new int[5][];
    private int stage;
    private bool start;
    private bool blink;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        symbselect = Enumerable.Range(0, 48).ToArray().Shuffle().Take(32).ToArray();
        Debug.LogFormat("[Holographic Memory #{0}] The light sides of the tiles display the images:\n[Holographic Memory #{0}] {1}", moduleID, string.Join("\n[Holographic Memory #" + moduleID + "] ", Enumerable.Range(0, 4).Select(x => string.Join(", ", Enumerable.Range(0, 4).Select(y => symbnames[symbselect[4 * x + y]]).ToArray())).ToArray()));
        Debug.LogFormat("[Holographic Memory #{0}] The dark sides of the tiles display the images:\n[Holographic Memory #{0}] {1}", moduleID, string.Join("\n[Holographic Memory #" + moduleID + "] ", Enumerable.Range(0, 4).Select(x => string.Join(", ", Enumerable.Range(0, 4).Select(y => symbnames[symbselect[16 + 4 * x + y]]).ToArray())).ToArray()));
        foreach (Transform x in axes)
            x.Rotate(0, Random.Range(0, 360f), 0);
        foreach (Renderer s in screens)
            s.enabled = false;
        StartCoroutine("Display");
        matstore.SetActive(false);
        foreach (KMSelectable button in buttons)
        {
            int b = buttons.IndexOf(button);
            button.OnInteract = delegate ()
            {
                if(!moduleSolved && !blink)
                {
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
                    button.AddInteractionPunch(0.6f);
                    if(!start)
                        StartCoroutine(T(0));
                    else
                    {
                        Debug.LogFormat("[Holographic Memory #{0}] Pressed {1}{2}-{3}", moduleID, "ABCD"[b%4], "1234"[b/4], lightside[b] ? "light" : "dark");
                        if (b % 4 == ans[stage][1] && b / 4 == ans[stage][0] && ((lightside[b] ? 0 : 1) == ans[stage][2]))
                        {
                            leds[stage].material = ledmats[2];
                            if (stage > 3)
                                StartCoroutine(T(2));
                            else
                                StartCoroutine(T(3));
                        }
                        else
                            StartCoroutine(T(1));
                    }
                }
                return false;
            };
        }
    }

    private void Setup()
    {
        Debug.LogFormat("[Holographic Memory #{0}] Stage {1}:", moduleID, stage + 1);
        List<int>[] prev = new List<int>[2] { new List<int> { }, new List<int> { } };
        for (int i = 0; i < stage * 2; i++)
            prev[i % 2].Add(i / 2);
        int p = Random.Range(0, 2);
        for (int i = 0; i < 2; i++)
        {
            if (prev[0].Count() > 0 && Random.Range(0, 4) == 0)
            {
                int r = prev[0].PickRandom();
                prev[0].Remove(r);
                screens[1 - i].material = symbols[r + 52];
                dispsymbs[stage, i] = ans[r];
                Debug.LogFormat("[Holographic Memory #{0}] The image displayed on the {1} screen is: Solid {2} ({3}{4}-{5})", moduleID, i == 0 ? "left" : "top", "1234"[r], "ABCD"[ans[r][1]], "1234"[ans[r][0]], ans[r][2] == 0 ? "light" : "dark");
            }
            else if(prev[1].Count() > 0 && Random.Range(0, 3) == 0)
            {
                int r = prev[1].PickRandom();
                prev[1].Remove(r);
                screens[1 - i].material = symbols[r + 48];
                dispsymbs[stage, i] = dispsymbs[r, 1 - i];
                Debug.LogFormat("[Holographic Memory #{0}] The image displayed on the {1} screen is: Empty {2} ({3}{4}-{5})", moduleID, i == 0 ? "left" : "top", "1234"[r], "ABCD"[dispsymbs[stage, i][1]], "1234"[dispsymbs[stage, i][0]], dispsymbs[stage, i][2] == 0 ? "light" : "dark");
            }
            else
            {
                int[] s = new int[3];
                if (stage < 1)
                    s = new int[3] { Random.Range(0, 4), Random.Range(0, 4), p };
                else
                    s = new int[3] { Random.Range(0, 4), Random.Range(0, 4), Random.Range(0, 2) };
                dispsymbs[stage, i] = s;
                int v = s[2] * 16 + s[0] * 4 + s[1];
                screens[1 - i].material = symbols[symbselect[v]];
                Debug.LogFormat("[Holographic Memory #{0}] The image displayed on the {1} screen is: {2} ({3}{4}-{5})", moduleID, i == 0 ? "left" : "top", symbnames[symbselect[v]], "ABCD"[dispsymbs[stage, i][1]], "1234"[dispsymbs[stage, i][0]], dispsymbs[stage, i][2] == 0 ? "light" : "dark");
            }
        }
        int[] m = new int[3]
        {
            dispsymbs[stage, 0][0],
            dispsymbs[stage, 1][1],
            dispsymbs[stage, 0][2] == dispsymbs[stage, 1][2] ? dispsymbs[stage, 0][2] : 1 - ans[stage - 1][2]
        };
        ans[stage] = m;
        int n = m[2] * 16 + m[0] * 4 + m[1];
        Debug.LogFormat("[Holographic Memory #{0}] The target image is: {4} ({1}{2}-{3})", moduleID, "ABCD"[m[1]], "1234"[m[0]], m[2] == 0 ? "light" : "dark", symbnames[symbselect[n]]);
    }

    private IEnumerator Display()
    {
        while (!moduleSolved)
        {
            for(int i = 0; i < 16; i++)
            {
                float x = axes[i].right.z;
                x = Mathf.Clamp(x + 0.5f, 0, 1);
                bgs[i].material.color = new Color(x, x, x);
                if (x > 0.5f)
                {
                    lightside[i] = true;
                    if (!start)
                        bsymbs[i].material = symbols[symbselect[i]];
                }
                else
                {
                    lightside[i] = false;
                    if(!start)
                        bsymbs[i].material = symbols[symbselect[i + 16]];
                }
            }
            yield return null;
        }
    }

    private IEnumerator Blink(Renderer g)
    {
        for(int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(Random.Range(0.1f, 0.2f));
            g.enabled ^= true;
        }
    }

    private IEnumerator T(int s)
    {
        switch (s)
        {
            case 0:
                Audio.PlaySoundAtTransform("Stage", transform);
                blink = true;
                foreach (Renderer g in bsymbs)
                    StartCoroutine(Blink(g));
                yield return new WaitForSeconds(1);
                Setup();
                foreach (Renderer g in screens)
                    StartCoroutine(Blink(g));
                blink = false;
                start = true;
                leds[0].material = ledmats[1];
                break;
            case 1:
                Audio.PlaySoundAtTransform("Stage", transform);
                module.HandleStrike();
                stage = 0;
                foreach (Renderer l in leds)
                    l.material = ledmats[0];
                blink = true;
                foreach (Renderer g in screens)
                    StartCoroutine(Blink(g));
                yield return new WaitForSeconds(1);
                foreach (Renderer g in bsymbs)
                    StartCoroutine(Blink(g));
                blink = false;
                start = false;
                break;
            case 2:
                moduleSolved = true;
                module.HandlePass();
                Audio.PlaySoundAtTransform("Solve", transform);
                foreach (Renderer g in bgs)
                    StartCoroutine(Wave(g));
                foreach (Renderer g in screens)
                    StartCoroutine(Blink(g));
                yield return new WaitForSeconds(1);
                Vector3 sc = screens[0].transform.localScale;
                screens[0].transform.localScale = new Vector3(sc.x * 3.5f, sc.y, sc.z);
                screens[0].material = sol;
                screens[0].enabled = true;
                break;
            default:
                stage++;
                Audio.PlaySoundAtTransform("Stage", transform);
                blink = true;
                foreach (Renderer g in screens)
                    StartCoroutine(Blink(g));
                yield return new WaitForSeconds(1);
                Setup();
                foreach (Renderer g in screens)
                    StartCoroutine(Blink(g));
                blink = false;
                leds[stage].material = ledmats[1];
                break;
        }
    }

    private IEnumerator Wave(Renderer g)
    {
        float e = Random.Range(0.5f, 1.5f);
        float a = (Mathf.Asin(g.material.color.r) - 1) * 2;
        if (a > 180f)
            e *= -1;
        while (true)
        {
            float x = (Mathf.Sin(a) + 1) / 2;
            g.material.color = new Color(x, x, x);
            a += Time.deltaTime * e;
            a %= 360;
            yield return null;
        }
    }
}
