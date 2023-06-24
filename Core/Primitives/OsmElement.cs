using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using OsmSharp;

namespace Osmalyzer
{
    public abstract class OsmElement
    {
        [PublicAPI]
        public abstract OsmElementType ElementType { get; }


        [PublicAPI]
        public long Id { get; } 


        [PublicAPI]
        public bool HasTags => RawElement.Tags != null && RawElement.Tags.Count > 0;
        
        [PublicAPI]
        public IReadOnlyList<OsmRelationMember>? Relations => relations?.AsReadOnly();
        

        internal OsmGeo RawElement { get; }

        // todo: not keep this eventually, only grab the data
        
        
        internal List<OsmRelationMember>? relations;


        protected OsmElement(OsmGeo rawElement)
        {
            RawElement = rawElement;

            Id = rawElement.Id!.Value;
        }


        internal static OsmElement Create(OsmGeo element)
        {
            switch (element.Type)
            {
                case OsmGeoType.Node:     return new OsmNode(element);
                case OsmGeoType.Way:      return new OsmWay(element);
                case OsmGeoType.Relation: return new OsmRelation(element);
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        
        [Pure]
        public string? GetValue(string key)
        {
            return RawElement.Tags.ContainsKey(key) ? RawElement.Tags.GetValue(key) : null;
        }
        
        [Pure]
        public bool HasKey(string key)
        {
            return RawElement.Tags.ContainsKey(key);
        }
        
        [Pure]
        public bool HasValue(string key, string value)
        {
            return 
                RawElement.Tags.ContainsKey(key) &&
                RawElement.Tags.GetValue(key) == value;
        }


        /// <summary>
        /// Speeding up collections with hashing, basically dictionaries
        /// </summary>
        public override int GetHashCode()
        {
            return (int)Id ^ (int)(Id >> 32);
        }


        public enum OsmElementType
        {
            Node,
            Way,
            Relation
        }
    }
}