using System;

namespace Osmalyzer
{
    public class LoneElementAllowanceCallbackParameter : CorrelatorParamater
    {
        public Func<OsmElement, bool> AllowanceCallback { get; }
        

        public LoneElementAllowanceCallbackParameter(Func<OsmElement, bool> allowanceCallback)
        {
            AllowanceCallback = allowanceCallback;
        }
    }
}