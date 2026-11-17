using System.Collections.Generic;
#if !UNITY_WEBGL
using System.IO;
#endif
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(200)]
public class GlitchRecorder : MonoBehaviour
{
    public bool recording = true;
    public float recordSeconds = 2f;
    public float bodySpeedFactor = 1.5f;
    public float stretchFactor = 1.5f;
    public float aimErrorFraction = 0.8f;
    public float aimErrorSeconds = 0.5f;
    public float jumpDistance = 0.4f;
    public int maxDumps = 5;
    public float minSecondsBetweenDumps = 1f;

    struct Entry
    {
        public float time, aimError, pull, charge, stretch;
        public Vector2 bodyPos, bodyVel, hammerPos;
        public bool hammerHit, bodyHit, burst, deadzone, mouseValid, recovered;
        public int sticky;
    }

    const string ReasonError = "hammer stuck far from the cursor in the air";
    const string ReasonSpeed = "body speed too high";
    const string ReasonStretch = "hammer stretched far from the body";
    const string ReasonRecovered = "hammer was found inside a wall";
    const string ReasonJump = "body position jumped";
    const string ReasonNaN = "NaN or infinity";

    PlayerController p;
    Entry[] ring;
    int head;
    int count;
    float badErrorTime;
    Vector2 prevPos;
    bool havePrev;
    float lastDump = -100f;
    readonly List<string> dumps = new List<string>();
    string path;

    void Start()
    {
#if UNITY_WEBGL
        recording = false;
#endif
        p = GetComponent<PlayerController>();
        ring = new Entry[Mathf.CeilToInt(recordSeconds / Time.fixedDeltaTime) + 1];
#if !UNITY_WEBGL
        path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "glitch_log.txt"));
#endif
    }

    public void NotifyTeleport()
    {
        havePrev = false;
    }

    void FixedUpdate()
    {
        if (!recording || p == null || !p.enabled || p.Aim == null)
        {
            return;
        }

        Rigidbody2D bodyRb = p.BodyRb;
        Rigidbody2D hammerRb = p.HammerRb;
        ContactTracker contacts = p.Contacts;

        Entry e;
        e.time = Time.fixedTime;
        e.aimError = p.HammerAimError;
        e.pull = p.LastPull.magnitude;
        e.charge = p.ChargePool;
        e.bodyPos = bodyRb.position;
        e.bodyVel = bodyRb.linearVelocity;
        e.hammerPos = hammerRb.position;
        e.stretch = (e.hammerPos - e.bodyPos).magnitude;
        e.hammerHit = contacts.HammerOnBlock;
        e.bodyHit = contacts.BodyOnBlock;
        e.burst = p.SuperJumpActive;
        e.deadzone = p.Aim.InDeadzone;
        e.mouseValid = p.Aim.MouseValid;
        e.recovered = p.HammerRecovered;
        e.sticky = p.SurfaceFx.StickyState;

        ring[head] = e;
        head = (head + 1) % ring.Length;
        if (count < ring.Length)
        {
            count++;
        }

        string reason = Detect(e);
        prevPos = e.bodyPos;
        havePrev = true;
        if (reason != null)
        {
            TryDump(reason);
        }
    }

    string Detect(Entry e)
    {
        if (Safe.Bad(e.aimError) || Safe.Bad(e.pull) || Safe.Bad(e.bodyPos) || Safe.Bad(e.bodyVel) || Safe.Bad(e.hammerPos))
        {
            return ReasonNaN;
        }

        float scale = p.Scale;
        if (e.recovered)
        {
            return ReasonRecovered;
        }

        if (e.aimError > aimErrorFraction * p.hammer.reach * scale && !e.hammerHit)
        {
            badErrorTime += Time.fixedDeltaTime;
            if (badErrorTime > aimErrorSeconds)
            {
                badErrorTime = 0f;
                return ReasonError;
            }
        }
        else
        {
            badErrorTime = 0f;
        }

        if (e.stretch > stretchFactor * p.hammer.reach * scale)
        {
            return ReasonStretch;
        }
        if (e.bodyVel.magnitude > bodySpeedFactor * p.limits.safetyMaxSpeed)
        {
            return ReasonSpeed;
        }
        float jump = jumpDistance * scale;
        if (havePrev && (e.bodyPos - prevPos).sqrMagnitude > jump * jump)
        {
            return ReasonJump;
        }
        return null;
    }

    void TryDump(string reason)
    {
        float now = Time.realtimeSinceStartup;
        if (now - lastDump < minSecondsBetweenDumps)
        {
            return;
        }
        lastDump = now;

        StringBuilder sb = new StringBuilder(count * 120 + 200);
        sb.Append("=== glitch at t=").Append(Time.fixedTime.ToString("F2")).Append(" reason: ").AppendLine(reason);
        sb.AppendLine("time aimError pull stretch bodyPos bodyVel hammerPos hammerHit bodyHit sticky burst charge deadzone mouseValid recovered");
        int start = (head - count + ring.Length) % ring.Length;
        for (int i = 0; i < count; i++)
        {
            Entry e = ring[(start + i) % ring.Length];
            sb.Append(e.time.ToString("F3")).Append(' ')
              .Append(e.aimError.ToString("F2")).Append(' ')
              .Append(e.pull.ToString("F0")).Append(' ')
              .Append(e.stretch.ToString("F2")).Append(' ')
              .Append('(').Append(e.bodyPos.x.ToString("F2")).Append(',').Append(e.bodyPos.y.ToString("F2")).Append(") ")
              .Append('(').Append(e.bodyVel.x.ToString("F2")).Append(',').Append(e.bodyVel.y.ToString("F2")).Append(") ")
              .Append('(').Append(e.hammerPos.x.ToString("F2")).Append(',').Append(e.hammerPos.y.ToString("F2")).Append(") ")
              .Append(e.hammerHit ? 1 : 0).Append(' ')
              .Append(e.bodyHit ? 1 : 0).Append(' ')
              .Append(e.sticky).Append(' ')
              .Append(e.burst ? 1 : 0).Append(' ')
              .Append(e.charge.ToString("F2")).Append(' ')
              .Append(e.deadzone ? 1 : 0).Append(' ')
              .Append(e.mouseValid ? 1 : 0).Append(' ')
              .Append(e.recovered ? 1 : 0).AppendLine();
        }

        dumps.Add(sb.ToString());
        while (dumps.Count > maxDumps)
        {
            dumps.RemoveAt(0);
        }

#if !UNITY_WEBGL
        try
        {
            File.WriteAllText(path, string.Join("\n", dumps));
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("glitch log could not be written: " + ex.Message);
            return;
        }
#endif
        Debug.Log("glitch recorded: " + reason);
    }
}
