using GpsTracking.Domain.Models;

namespace GpsTracking.Domain.Rules;

public sealed class MovementRule
{
    private readonly List<RouteSegment> completed = [];
    private readonly List<GpsSample> active = [];
    private Guid activeId;
    private GpsSample? anchor;
    private GpsSample? latest;
    private int anchorIndex;
    private MovementState state = MovementState.Unknown;

    public MovementSnapshot Movement
    {
        get
        {
            DateTimeOffset? stopStartedAt = null;
            double stopElapsedSeconds = 0;

            if (state is MovementState.PossibleStop or MovementState.Stopped)
            {
                stopStartedAt = anchor?.Timestamp;
            }

            if (anchor is not null && latest is not null)
            {
                var elapsed = latest.Timestamp - anchor.Timestamp;
                stopElapsedSeconds = elapsed.TotalSeconds;
            }

            return new MovementSnapshot(state, stopStartedAt, stopElapsedSeconds);
        }
    }

    public IReadOnlyList<RouteSegment> Segments
    {
        get
        {
            var result = new List<RouteSegment>(completed);
            if (active.Count > 0)
            {
                result.Add(new RouteSegment(activeId, active[0].Timestamp, null,
                    Array.AsReadOnly(active.ToArray())));
            }

            return result.AsReadOnly();
        }
    }

    public void Observe(GpsSample sample, double radiusMeters, int durationSeconds)
    {
        latest = sample;
        if (state == MovementState.Stopped)
        {
            if (WithinAnchor(sample, radiusMeters))
            {
                return;
            }

            StartRoute(sample);
            state = MovementState.Moving;
            return;
        }

        if (active.Count == 0)
        {
            StartRoute(sample);
            return;
        }

        active.Add(sample);
        if (!WithinAnchor(sample, radiusMeters))
        {
            anchor = sample;
            anchorIndex = active.Count - 1;
            state = MovementState.Moving;
        }
        else
        {
            state = MovementState.PossibleStop;
            ConfirmIfReady(durationSeconds);
        }
    }

    public void Reevaluate(double radiusMeters, int durationSeconds)
    {
        if (latest is null)
        {
            return;
        }

        // A completed route is immutable. A narrower radius may establish movement
        // at the latest observed point, but never rewrites the completed boundary.
        if (state == MovementState.Stopped)
        {
            if (!WithinAnchor(latest, radiusMeters))
            {
                StartRoute(latest);
                state = MovementState.Moving;
            }

            return;
        }

        if (active.Count == 0)
        {
            return;
        }
        anchor = active[0];
        anchorIndex = 0;
        for (var index = 1; index < active.Count; index++)
        {
            if (!WithinAnchor(active[index], radiusMeters))
            {
                anchor = active[index];
                anchorIndex = index;
            }
        }

        // A one-point route may be a known resumption after a confirmed stop.
        // Settings changes must not discard that movement evidence.
        if (active.Count > 1)
        {
            state = anchorIndex == active.Count - 1
                ? MovementState.Moving
                : MovementState.PossibleStop;
        }
        // Only the latest candidate is reconsidered, not historical unconfirmed stops.
        ConfirmIfReady(durationSeconds);
    }

    private void StartRoute(GpsSample sample)
    {
        active.Clear();
        active.Add(sample);
        activeId = Guid.NewGuid();
        anchor = sample;
        anchorIndex = 0;
    }

    private bool WithinAnchor(GpsSample sample, double radiusMeters)
    {
        if (anchor is null)
        {
            return false;
        }

        var distanceMeters = GeographicDistance.Meters(
            anchor.Latitude,
            anchor.Longitude,
            sample.Latitude,
            sample.Longitude);

        return distanceMeters <= radiusMeters;
    }

    private void ConfirmIfReady(int durationSeconds)
    {
        if (anchor is null || latest is null || latest.Timestamp - anchor.Timestamp < TimeSpan.FromSeconds(durationSeconds))
        {
            return;
        }

        // An initially stationary point is retained in raw history, not a driven trip.
        if (anchorIndex > 0)
        {
            var points = Array.AsReadOnly(active.Take(anchorIndex + 1).ToArray());
            completed.Add(new RouteSegment(activeId, active[0].Timestamp, anchor.Timestamp, points));
        }

        active.Clear();
        state = MovementState.Stopped;
    }
}
