using System;

namespace Osmalyzer;

/// <summary>
/// Reported via/batched into <see cref="MatchedLoneOsmBatch"/> (not that there is no "unmatched" version - such elements are simply ignored).
/// If elements are not matched to data items, normally, they are batched into <see cref="UnmatchedItemBatch"/>.
/// However, we can specify with this to instead consider them "lone" elements and report them separately.
/// This is only necessary when not all elements passed to the correlator are automatically "missing";
/// for example, we might match all `shop=*` regardless of other tags,
/// but a "lone" element might also need to have other relevant tags like `name=BoozeCentral`,
/// because not every shop on the map is automatically a problem.
/// Note that this is pointless to do if the callback is always true - i.e. all elements are already matchable without further checks.
/// Similarly, it's also pointless if the callback is always false - i.e. all elements are ignored when not matched.
/// </summary>
public class LoneElementAllowanceParameter : CorrelatorParamater
{
    public Func<OsmElement, bool> AllowanceCallback { get; }
        

    public LoneElementAllowanceParameter(Func<OsmElement, bool> allowanceCallback)
    {
        AllowanceCallback = allowanceCallback;
    }
}