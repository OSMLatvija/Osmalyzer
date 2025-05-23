﻿namespace Osmalyzer;

/// <summary>
/// A user-applyable manual resolution to a <see cref="Resolvable"/> reported issue.
/// This is either created runtime as <see cref="RuntimeResolution"/> as potential.
/// Or it's imported as previously-applied as <see cref="ImportedResolution"/>.
/// </summary>
public abstract class Resolution
{
    /// <summary> The problem that this resolved. </summary>
    public Resolvable Problem { get; }

    /// <summary> When was this resolved? </summary>
    public DateTime Timestamp { get; }

    /// <summary> Custom optional user comment to understand specifics of why this was resolved manually </summary>
    public string Comment { get; }
    
    
    protected Resolution(Resolvable problem, DateTime timestamp, string comment)
    {
        Problem = problem;
        Timestamp = timestamp;
        Comment = comment;
    }

}