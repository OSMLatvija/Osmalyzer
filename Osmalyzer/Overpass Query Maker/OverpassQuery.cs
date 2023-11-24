using System;
using System.Collections.Generic;
using System.Web;

namespace Osmalyzer;

public class OverpassQuery
{
    private readonly List<OverpassRule> _rules = new List<OverpassRule>();


    public void AddRule(OverpassRule rule)
    {
        _rules.Add(rule);
    }

    public string GetQueryLink()
    {
        string query = "";

        query += "[out:json][timeout:25];" + Environment.NewLine;
            
        query += "{{geocodeArea:Latvia}}->.searchArea;" + Environment.NewLine;
            
        query += "nwr";
        // todo: element type rule

        foreach (OverpassRule rule in _rules)
        {
            switch (rule)
            {
                case HasKeyOverpassRule hk:
                    query += "[\"" + hk.Key + "\"]";   
                    break;
                    
                case HasValueOverpassRule hv:
                    query += "[\"" + hv.Key + "\"=\"" + hv.Value + "\"]";   
                    break;
                    
                case DoesNotHaveKeyOverpassRule dhk:
                    query += "[!\"" + dhk.Key + "\"]";   
                    break;
                    
                case DoesNotHaveValueOverpassRule dhv:
                    query += "[\"" + dhv.Key + "\"!=\"" + dhv.Value + "\"]";   
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(rule));
            }
        }
            
        // todo: quotes optional if no special chars
            
        query += "(area.searchArea);" + Environment.NewLine;
            
        query += "out geom;" + Environment.NewLine;
            
        return "https://overpass-turbo.eu/?Q=" + HttpUtility.UrlEncode(query);
    }
}