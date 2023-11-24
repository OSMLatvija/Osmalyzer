using System;

namespace Osmalyzer
{
    public abstract class CorrelatorBatch
    {
    }

    public class MatchedItemCBatch : CorrelatorBatch
    {
    }

    public class MatchedFarItemBatch : CorrelatorBatch
    {
    }

    public class UnmatchedItemBatch : CorrelatorBatch
    {        
    }

    public class UnmatchedOsmBatch : CorrelatorBatch
    {
    }
}