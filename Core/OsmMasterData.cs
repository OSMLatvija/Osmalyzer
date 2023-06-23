﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using JetBrains.Annotations;
using OsmSharp;
using OsmSharp.Streams;

namespace Osmalyzer
{
    /// <summary>
    /// Full list of OSM data, as loaded.
    /// </summary>
    public class OsmMasterData : OsmData
    {
        [PublicAPI]
        public override IReadOnlyList<OsmElement> Elements => _elements.AsReadOnly();


        private readonly List<OsmElement> _elements;

        
        internal ReadOnlyDictionary<long, OsmNode> NodesById => new ReadOnlyDictionary<long, OsmNode>(_nodesById);
        internal ReadOnlyDictionary<long, OsmWay> WaysById => new ReadOnlyDictionary<long, OsmWay>(_waysById);
        internal ReadOnlyDictionary<long, OsmRelation> RelationsById => new ReadOnlyDictionary<long, OsmRelation>(_relationsById);

        
        private readonly Dictionary<long, OsmNode> _nodesById = new Dictionary<long, OsmNode>();
        private readonly Dictionary<long, OsmWay> _waysById = new Dictionary<long, OsmWay>();
        private readonly Dictionary<long, OsmRelation> _relationsById = new Dictionary<long, OsmRelation>();
        

        public OsmMasterData(string dataFileName)
        {
            _elements = new List<OsmElement>();

            using FileStream fileStream = new FileInfo(dataFileName).OpenRead();

            using PBFOsmStreamSource source = new PBFOsmStreamSource(fileStream);

            // Read the "raw" elements from the file
            
            foreach (OsmGeo element in source)
            {
                OsmElement osmElement = OsmElement.Create(element);

                _elements.Add(osmElement);

                switch (osmElement)
                {
                    case OsmNode osmNode:
                        _nodesById.Add(osmElement.Id, osmNode);
                        break;

                    case OsmWay osmWay:
                        _waysById.Add(osmElement.Id, osmWay);
                        break;
                    
                    case OsmRelation osmRelation:
                        _relationsById.Add(osmElement.Id, osmRelation);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(osmElement));
                }
            }

            // Link and backlink all the elements together
            
            foreach (OsmElement element in _elements)
            {
                switch (element)
                {
                    case OsmNode:
                        break;

                    case OsmWay osmWay:
                        Way rawWay = (Way)osmWay.RawElement;

                        foreach (long rawWayId in rawWay.Nodes)
                        {
                            OsmNode node = NodesById[rawWayId];
                            
                            // Link
                            osmWay.nodes.Add(node);
                            
                            // Backlink
                            if (node.ways == null) 
                                node.ways = new List<OsmNode>();
                            
                            node.ways.Add(node);
                        }

                        break;
                    
                    case OsmRelation osmRelation:
                        foreach (OsmRelationMember member in osmRelation.members)
                        {
                            switch (member.ElementType)
                            {
                                case OsmRelationMember.MemberElementType.Node:
                                    if (NodesById.TryGetValue(member.Id, out OsmNode? node))
                                    {
                                        // Link
                                        member.Element = node;
                                        // Backlink
                                        if (node.relations == null)
                                            node.relations = new List<OsmRelationMember>();
                                        node.relations.Add(member);
                                    }

                                    break;
                                    
                                case OsmRelationMember.MemberElementType.Way:
                                    if (WaysById.TryGetValue(member.Id, out OsmWay? way))
                                    {
                                        // Link
                                        member.Element = way;
                                        // Backlink
                                        if (way.relations == null)
                                            way.relations = new List<OsmRelationMember>();
                                        way.relations.Add(member);
                                    }

                                    break;

                                case OsmRelationMember.MemberElementType.Relation:
                                    if (RelationsById.TryGetValue(member.Id, out OsmRelation? relation))
                                    {
                                        // Link
                                        member.Element = relation;
                                        // Backlink
                                        if (relation.relations == null)
                                            relation.relations = new List<OsmRelationMember>();
                                        relation.relations.Add(member);
                                    }

                                    break;
                                    
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        break;
                }
            }
        }
    }
}