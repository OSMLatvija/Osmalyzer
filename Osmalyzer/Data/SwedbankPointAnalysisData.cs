using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace Osmalyzer
{
    [UsedImplicitly]
    public class SwedbankPointAnalysisData : AnalysisData, IPreparableAnalysisData
    {
        public override string Name => "Swebank Points";

        protected override string DataFileIdentifier => "swedbank-points";


        public List<BankPoint> Points { get; private set; } = null!; // only null before prepared


        protected override void Download()
        {
            WebsiteDownloadHelper.Download(
                "https://www.swedbank.lv/finder.xml", 
                cacheBasePath + DataFileIdentifier + @".xml"
            );
        }

        public void Prepare()
        {
            string data = File.ReadAllText(cacheBasePath + DataFileIdentifier + @".xml");

            
            if (data.StartsWith("<items>")) // XML header missing
                data = @"<?xml version=""1.0"" encoding=""UTF-8""?>" + data;


            // Fix invalid (as far as .NET XML is concerned) lat/lon values (0 will be skipped later)
            data = data.Replace("<latitude>null</latitude>", "<latitude>0</latitude>");
            data = data.Replace("<longitude>null</longitude>", "<longitude>0</longitude>");
            
            
            using StringReader reader = new StringReader(data);
            
            XmlSerializer serializer = new XmlSerializer(typeof(RawItems));

            RawItems rawItems = (RawItems)serializer.Deserialize(reader)!;

            if (rawItems.Items.Count == 0) throw new InvalidOperationException();
            
            
            Points = new List<BankPoint>();
            
            List<string> names = new List<string>();
            List<string> repeats = new List<string>();
            
            foreach (RawItem item in rawItems.Items)
            {
                OsmCoord coord = new OsmCoord(item.Latitude, item.Longitude);
                
                if (OsmGeoTools.DistanceBetween(coord, new OsmCoord(0, 0)) < 100) // point at default/0,0 - bad coord
                    continue;


                BankPoint point = new BankPoint(
                    RawTypeToPointType(item.Type),
                    item.Name,
                    item.StreetAddress + (item.City != null ? ", " + item.City : ""),
                    coord
                );
                
                Points.Add(point);
                
                
                string combo = MakeBankPointNameCombo(point); // since we want any diffrent value to not match

                if (!names.Contains(combo))
                    names.Add(combo);
                else if (!repeats.Contains(combo))
                    repeats.Add(combo);
            }
            
            // Disambiguate same-named locations, probably planted at the same time (in the same place)
            
            foreach (string repeat in repeats)
            {
                int id = 1;
                
                foreach (BankPoint bankPoint in Points)
                {
                    if (MakeBankPointNameCombo(bankPoint) == repeat)
                    {
                        bankPoint.Id = id;
                        id++;
                    }
                }
            }
        }
            
        [Pure]
        private static string MakeBankPointNameCombo(BankPoint point)
        {
            return point.TypeString + " " + point.Name + " " + point.Address;
        }

        [Pure]
        private static BankPointType RawTypeToPointType(string rawType)
        {
            // Filiāles - cyan - "branch"
            // Izmaksas bankomāti - yellow - "ATM"
            // Naudas iemaksas un izmaksas bankomāti - orange - "R"
            // Skaidras naudas izmaksa pie pirkuma - purple - not in this data file
            
            return rawType switch
            {
                "branch" => BankPointType.Branch,
                "ATM"    => BankPointType.AtmWithdrawal,
                "R"      => BankPointType.AtmWithdrawalAndDeposit,
                _        => throw new NotImplementedException()
            };
        }

        [XmlRoot("items")]
        public class RawItems
        {
            [XmlElement("item")]
            public List<RawItem> Items { get; set; } = new List<RawItem>();
        }
        
        public class RawItem // has to be public for XML parser
        {
            [XmlElement("type")]
            public string Type { get; set; } = null!;

            [XmlElement("name")]
            public string Name { get; set; } = null!;

            [XmlElement("address")]
            public string StreetAddress { get; set; } = null!;

            [XmlElement("city")]
            public string? City { get; set; }

            [XmlElement("latitude")]
            public double Latitude { get; set; }

            [XmlElement("longitude")]
            public double Longitude { get; set; }
        }
    }
}