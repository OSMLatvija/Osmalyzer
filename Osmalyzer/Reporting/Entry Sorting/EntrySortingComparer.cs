using System;
using System.Collections.Generic;

namespace Osmalyzer
{
    /// <summary>
    /// Provides comparison logic between <see cref="SortableReportEntry"/>s that potentially have <see cref="EntrySortingRule"/> defined.
    /// This is used when the <see cref="Report"/> is collecting and providing the final sorted list of entries for display.
    /// </summary>
    public class EntrySortingComparer : IComparer<SortableReportEntry>
    {
        public int Compare(SortableReportEntry? a, SortableReportEntry? b)
        {
            EntrySortingRule? ruleA = a!.SortingRule;
            EntrySortingRule? ruleB = b!.SortingRule;

            if (ruleA == null && ruleB == null)
                return 0;

            if (ruleA != null && ruleB == null)
                return -1;

            if (ruleA == null && ruleB != null)
                return 1;

            if (ruleA!.GetType() != ruleB!.GetType())
                throw new NotImplementedException("Cannot sort with different rules, need to implement sort hierarchy or default behaviour or rule precedence");

            switch (a.SortingRule)
            {
                case SortEntryAsc ascA:
                    SortEntryAsc ascB = (SortEntryAsc)ruleB;
                    return ascA.Value.CompareTo(ascB.Value);
                
                case SortEntryDesc descA:
                    SortEntryDesc descB = (SortEntryDesc)ruleB;
                    return descB.Value.CompareTo(descA.Value);
                
                default:
                    throw new NotImplementedException();
            }
        }
    }
}