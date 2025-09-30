using System.Linq;

namespace Osmalyzer;

public class RelationMustHaveAllMembersDownloaded : OsmFilter
{
    public override bool ForNodesOnly => false;
    public override bool ForWaysOnly => false;
    public override bool ForRelationsOnly => true;
    public override bool TaggedOnly => false;


    internal override bool Matches(OsmElement element)
    {
        return element is OsmRelation relation && relation.Members.All(m => m.Element != null);
    }
}