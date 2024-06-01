using System.Collections.Generic;

namespace Osmalyzer;

public interface IParcelLockerListProvider
{
    IEnumerable<ParcelLocker> ParcelLockers { get; }
}