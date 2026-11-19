using System.Collections.Generic;
using UnityEngine;

// Lines live in the scrolling road's local space. Each wheel writes only its own ground contacts.
[DefaultExecutionOrder(20)]
public sealed class CartWheelTracks : MonoBehaviour
{
    private const int WheelCount = 4;
    private const float Lifetime = 2.6f;
    private const float Length = 260f;
    private const float Spacing = 4f;
    private const float Width = 9f;
    private static readonly Color TrackColor = new(.52f, .38f, .24f, .55f);

    [SerializeField] private LineRenderer[] tracks;
    [SerializeField] private LineRenderer[] interruptedTracks;
    private readonly bool[] disconnected = new bool[WheelCount];
    private Track[] active, interrupted;
    private int stage;
    private float previousScroll;

    private void OnEnable()
    {
        active = System.Array.ConvertAll(tracks, line => new Track(line));
        interrupted = System.Array.ConvertAll(interruptedTracks, line => new Track(line));
        Clear();
    }

    private void LateUpdate()
    {
        if (active == null || interrupted == null) return;
        if (!CartStations.Running)
        {
            if (stage != 0) Clear();
            return;
        }

        var game = CartStations.Instance;
        float scroll = game.Cart.ScrollDistance;
        if (stage != game.StageNumber || scroll < previousScroll) Clear();
        stage = game.StageNumber;
        previousScroll = scroll;
        float unit = game.WorldPerWebUnit;
        float now = Time.time;
        bool rolling = game.Cart.Speed > .05f;
        for (int i = 0; i < WheelCount; i++)
        {
            var wheel = game.TireAt(i);
            if (wheel == null) continue;
            bool attached = game.IsWheelSecured(i);
            if (!attached && !disconnected[i])
            {
                (active[i], interrupted[i]) = (interrupted[i], active[i]);
                active[i].Clear();
            }
            disconnected[i] = !attached;

            float ground = wheel.bounds.min.y;
            Vector3 contact = transform.InverseTransformPoint(new Vector3(wheel.transform.position.x, ground, transform.position.z));
            if (attached) active[i].Draw(contact, rolling, now, unit);
            interrupted[i].Draw(contact, false, now, unit);
        }
    }

    private void Clear()
    {
        foreach (var track in active) track.Clear();
        foreach (var track in interrupted) track.Clear();
        System.Array.Clear(disconnected, 0, WheelCount);
        stage = 0;
    }

    private sealed class Track
    {
        private readonly LineRenderer line;
        private readonly List<(Vector3 Position, float Time)> samples = new();
        private Vector3[] positions = new Vector3[64];

        public Track(LineRenderer line) => this.line = line;

        public void Clear()
        {
            samples.Clear();
            line.positionCount = 0;
        }

        public void Draw(Vector3 contact, bool recording, float now, float unit)
        {
            float farthest = contact.x + Length * unit;
            int expired = 0;
            while (expired < samples.Count && (now - samples[expired].Time > Lifetime || samples[expired].Position.x > farthest))
                expired++;
            if (expired > 0) samples.RemoveRange(0, expired);

            float spacing = Spacing * unit;
            if (recording && (samples.Count == 0 || (samples[^1].Position - contact).sqrMagnitude >= spacing * spacing))
                samples.Add((contact, now));
            bool extendToContact = recording && samples.Count > 0 && (samples[^1].Position - contact).sqrMagnitude > .000001f;
            int count = samples.Count + (extendToContact ? 1 : 0);
            line.positionCount = count < 2 ? 0 : count;
            if (count < 2) return;

            if (count > positions.Length) positions = new Vector3[Mathf.NextPowerOfTwo(count)];
            for (int i = 0; i < samples.Count; i++) positions[i] = samples[i].Position;
            if (extendToContact) positions[count - 1] = contact;
            line.SetPositions(positions);
            float opacity = Mathf.Clamp01(1f - (now - samples[^1].Time) / Lifetime);
            line.widthMultiplier = Width * unit;
            line.startColor = new Color(TrackColor.r, TrackColor.g, TrackColor.b, 0f);
            line.endColor = new Color(TrackColor.r, TrackColor.g, TrackColor.b, TrackColor.a * opacity);
        }
    }
}
