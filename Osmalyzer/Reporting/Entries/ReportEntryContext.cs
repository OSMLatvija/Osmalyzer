using System;

namespace Osmalyzer
{
    /// <summary>
    /// Used to optionally identify a report entry uniquely when creating it.
    /// Useful so the entry can be referred to later without needing to track it reference, but using, for example, user's own objects.
    /// Used by stuff like <see cref="Report.CancelEntries"/>. 
    /// </summary>
    public class ReportEntryContext : IEquatable<ReportEntryContext>
    {
        private readonly object _context;

        
        public ReportEntryContext(object context)
        {
            _context = context;
        }

        
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            
            if (ReferenceEquals(this, obj))
                return true;
            
            if (obj.GetType() != typeof(ReportEntryContext))
                return false;
            
            return Equals((ReportEntryContext)obj);
        }

        public bool Equals(ReportEntryContext? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            
            if (ReferenceEquals(this, other))
                return true;
            
            return _context.Equals(other._context);
        }

        public override int GetHashCode()
        {
            return _context.GetHashCode();
        }

        public static bool operator ==(ReportEntryContext? left, ReportEntryContext? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ReportEntryContext? left, ReportEntryContext? right)
        {
            return !Equals(left, right);
        }
    }
}