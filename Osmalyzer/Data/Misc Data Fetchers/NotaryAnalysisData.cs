using System.Globalization;
using Newtonsoft.Json;

namespace Osmalyzer;

[UsedImplicitly]
public class NotaryAnalysisData : AnalysisData, IUndatedAnalysisData
{
    public override string Name => "Notary offices";

    public override string ReportWebLink => @"https://www.latvijasnotars.lv/notaries_map";

    public override bool NeedsPreparation => true;


    protected override string DataFileIdentifier => "notary-offices";


    public List<NotaryOfficeData> Offices { get; private set; } = null!; // only null before prepared


    private string DataFileName => Path.Combine(CacheBasePath, DataFileIdentifier + @".json");


    protected override void Download()
    {
        // The site has a clean JSON API that returns all notary data
        WebsiteDownloadHelper.Download(
            "https://www.latvijasnotars.lv/notaries.json",
            DataFileName,
            new Dictionary<string, string>
            {
                { "Cache-Control", "no-cache" },
                { "Pragma",        "no-cache" },
            }
        );
    }

    protected override void DoPrepare()
    {
        Offices = [];

        string source = File.ReadAllText(DataFileName);

        dynamic notaries = JsonConvert.DeserializeObject(source)!;

        foreach (dynamic notary in notaries)
        {
            // {
            //   "id": 203,
            //   "first_name": "Firstname",
            //   "last_name": "Lastname",
            //   "full_name": "Firstname Lastname",
            //   "full_name_gen": "Firstnames Lastnames",
            //   "email": "Firstname.Lastname@LatvijasNotars.lv",
            //   "office_number": "+371 27041471",
            //   "extra_number": null,
            //   "notary_link": "/firstname.lastname",
            //   "image_url": "https://latvijasnotars-uploads.s3.eu-central-1.amazonaws.com/...",
            //   "county_court": "Zemgales apgabaltiesa",
            //   "known_languages": [ "lv", "ru", "en" ],
            //   "active": true,
            //   "is_test_notary": false,
            //   "gender": "female",
            //   "application_for_appointment": false,
            //   "messenger_enabled": true,
            //   "online_operations": true,
            //   "education": null,
            //   "further_education": null,
            //   "attestation": null,
            //   "honorary_titles": null,
            //   "former_names": null,
            //   "emergency_notice": "",
            //   "closest_absence": null,
            //   "absence_interval_lv": null,
            //   "absence_interval_en": null,
            //   "absence_replacement_link_lv": null,
            //   "absence_replacement_link_en": null,
            //   "working_time_today": {
            //     "id": 2045, "weekday": 1,
            //     "time_from": "2000-01-01T12:00:00.000+02:00", "time_until": "2000-01-01T17:00:00.000+02:00",
            //     "pause_from": null, "pause_until": null,
            //     "registering_mandatory": false, "closed": false, "notes": null,
            //     "nis_uid": "2293", "created_at": "...", "updated_at": "...",
            //     "notaries_place_of_practice_id": 293
            //   },
            //   "places_of_practice": [
            //     {
            //       "id": 293,
            //       "street": "Pasta iela 47",
            //       "address": "Pasta iela 47, Jelgava",
            //       "full_address": "Pasta iela 47, Jelgava, LV-3001",
            //       "city": "Jelgava",
            //       "county_court": "Zemgales apgabaltiesa",
            //       "gps_latitude": "56.648370",
            //       "gps_logitude": "23.725169",    <- note: API typo, "logitude" not "longitude"
            //       "active": true,
            //       "primary": true,
            //       "transfer_information_lv": null,
            //       "transfer_information_en": null,
            //       "transfer_notary_id": null,
            //       "appointments_open_from_date": "2026-03-04",
            //       "appointments_open_until_date": "2026-11-09",
            //       "working_times": [
            //         { "id": 2045, "weekday": 1, "open": true,  "time_from": "2000-01-01T12:00:00.000+02:00", "time_until": "2000-01-01T17:00:00.000+02:00", "time_from_minutes": 720.0,  "time_until_minutes": 1020.0, "pause_from": null,                          "pause_until": null,                          "time_options": ["12.00", ...] },
            //         { "id": 2049, "weekday": 5, "open": true,  "time_from": "2000-01-01T09:00:00.000+02:00", "time_until": "2000-01-01T14:00:00.000+02:00", "time_from_minutes": 540.0,  "time_until_minutes": 840.0,  "pause_from": null,                          "pause_until": null,                          "time_options": ["9.00", ...]  },
            //         { "id": 2050, "weekday": 6, "open": false, "time_from": null,                            "time_until": null,                            "time_from_minutes": null,   "time_until_minutes": null,   "pause_from": null,                          "pause_until": null,                          "time_options": []             },
            //         { "id": 2051, "weekday": 7, "open": false, "time_from": null,                            "time_until": null,                            "time_from_minutes": null,   "time_until_minutes": null,   "pause_from": null,                          "pause_until": null,                          "time_options": []             }
            //       ]
            //     },
            //     {
            //       "id": 294,
            //       "active": false, "primary": false,
            //       "transfer_information_lv": "...", "transfer_information_en": "...", "transfer_notary_id": "10",
            //       "appointments_open_from_date": null, "appointments_open_until_date": null,
            //       "working_times": [ ... ]
            //     }
            //   ],
            //   "assistants": [
            //     { "id": 921, "first_name": "Firstname", "last_name": "Lastname", "email": "...", "notary_id": 203, "nis_uid": 1654, "gender": "female", "image_url": null, "education": null, "full_name_gen": "...", "full_name_dat": "...", "full_name_aku": "..." }
            //   ],
            //   "employees": [
            //     { "id": 968, "first_name": "Firstname", "last_name": "Lastname", "email": "...", "notary_id": 203, "nis_uid": 1552, "gender": "female", "image_url": null, "education": null, "full_name_gen": "...", "full_name_dat": "...", "full_name_aku": "..." }
            //   ]
            // }

            bool notaryActive = (bool)notary.active;
            if (!notaryActive)
                continue;

            bool isTestNotary = (bool)notary.is_test_notary;
            if (isTestNotary)
                continue;

            string fullName = (string)notary.full_name;
            string? phone = (string?)notary.office_number;
            string? email = (string?)notary.email;
            string notaryLinkSuffix = (string)notary.notary_link;
            string website = "https://www.latvijasnotars.lv" + notaryLinkSuffix;

            List<string> langs = [ ];
            foreach (string lang in notary.known_languages)
                langs.Add(lang);

            if (langs.Count == 0) throw new Exception("Notary " + fullName + " does not have any known languages listed");
            if (!langs.Contains("lv")) throw new Exception("Notary " + fullName + " does not have expected Latvian language in known_languages");

            foreach (dynamic place in notary.places_of_practice)
            {
                bool placeActive = (bool)place.active;
                if (!placeActive)
                    continue;

                string? latStr = (string?)place.gps_latitude;
                string? lonStr = (string?)place.gps_logitude; // note: API has typo "logitude"
                if (latStr == null || lonStr == null)
                    continue;

                double lat = double.Parse(latStr, CultureInfo.InvariantCulture);
                double lon = double.Parse(lonStr, CultureInfo.InvariantCulture);

                string address = (string)place.address;
                string fullAddress = (string)place.full_address;
                string city = (string)place.city;
                string countyCourt = (string)place.county_court;

                string? openingHours = ParseOpeningHours(place.working_times);

                Offices.Add(
                    new NotaryOfficeData(
                        new OsmCoord(lat, lon),
                        fullName,
                        address,
                        fullAddress,
                        city,
                        countyCourt,
                        phone,
                        email,
                        openingHours,
                        langs,
                        website
                    )
                );
            }
        }
        
        if (Offices.Count == 0)
            throw new Exception("No offices parsed from notary data");
        
        CultureInfo latvianCulture = new CultureInfo("lv-LV"); // to sort letters alphabetically
        Offices.Sort((o1, o2) => string.Compare(o1.Name, o2.Name, latvianCulture, CompareOptions.None));
    }


    [Pure]
    private static string? ParseOpeningHours(dynamic workingTimes)
    {
        List<(int weekday, string hours)> parsedOpen = [];
        List<int> parsedClosed = [];

        foreach (dynamic wt in workingTimes)
        {
            int weekday = (int)wt.weekday;

            bool open = (bool)wt.open;
            if (!open)
            {
                parsedClosed.Add(weekday);
                continue;
            }

            string? timeFrom = (string?)wt.time_from;
            string? timeUntil = (string?)wt.time_until;
            if (timeFrom == null || timeUntil == null)
                continue;

            // Times are like "2000-01-01T09:00:00.000+02:00"
            DateTime from = DateTime.Parse(timeFrom, CultureInfo.InvariantCulture);
            DateTime until = DateTime.Parse(timeUntil, CultureInfo.InvariantCulture);

            string? pauseFrom = (string?)wt.pause_from;
            string? pauseUntil = (string?)wt.pause_until;

            if (pauseFrom != null && pauseUntil != null)
            {
                DateTime pFrom = DateTime.Parse(pauseFrom, CultureInfo.InvariantCulture);
                DateTime pUntil = DateTime.Parse(pauseUntil, CultureInfo.InvariantCulture);

                string hoursWithPause =
                    $"{from:HH:mm}-{pFrom:HH:mm},{pUntil:HH:mm}-{until:HH:mm}";

                parsedOpen.Add((weekday, hoursWithPause));
            }
            else
            {
                string hours = $"{from:HH:mm}-{until:HH:mm}";
                parsedOpen.Add((weekday, hours));
            }
        }

        if (parsedOpen.Count == 0)
            return null;

        // Sort by weekday (API weekday: 1=Mon, 7=Sun)
        parsedOpen.Sort((a, b) => a.weekday.CompareTo(b.weekday));
        parsedClosed.Sort();

        List<string> dayHours = [];

        foreach ((int weekday, string hours) in parsedOpen)
            dayHours.Add(WeekdayToOsm(weekday) + " " + hours);

        foreach (int weekday in parsedClosed)
            dayHours.Add(WeekdayToOsm(weekday) + " off");

        // Merge open hours ranges first, then closed ranges; they naturally won't cross since off != time
        List<string> merged = OsmOpeningHoursHelper.MergeSequentialWeekdaysWithSameTimes(dayHours);

        return string.Join("; ", merged);


        [Pure]
        static string WeekdayToOsm(int weekday) =>
            weekday switch
            {
                1 => "Mo",
                2 => "Tu",
                3 => "We",
                4 => "Th",
                5 => "Fr",
                6 => "Sa",
                7 => "Su",
                _ => throw new Exception("Unknown weekday: " + weekday)
            };
    }
}

