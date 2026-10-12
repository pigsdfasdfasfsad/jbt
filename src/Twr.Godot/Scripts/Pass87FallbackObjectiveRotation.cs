using System;

namespace Twr.Godot;

/// <summary>
/// Pass87 deterministic, source-neutral objective marker scheduling for
/// approximate public maps. Original owner source maps bypass this policy.
/// </summary>
public static class Pass87FallbackObjectiveRotation
{
    public static int GetIndex(int markerCount,int wave,int objectiveIndex)
    {
        if(markerCount<=0)
            throw new ArgumentOutOfRangeException(nameof(markerCount));
        if(wave<=0 || objectiveIndex<0)
            throw new ArgumentOutOfRangeException(
                "Objective wave/index must be non-negative and wave starts at 1");
        // A stride of N-1 visits every one of N markers on successive
        // waves, including N=2, rather than repeatedly using marker zero.
        // Within a wave the two optional objectives choose separate
        // anchors when at least two exist.
        var stride=markerCount>1 ? markerCount-1 : 1;
        var starting=(long)(wave-1)*stride;
        return (int)((starting+objectiveIndex)%markerCount);
    }
}
