## Osmalyzer

Parsing OSM data in Latvia against various data sources.

[![Sample output](readme%20preview%20map.jpg)](https://osmlatvija.github.io/Osmalyzer/Swedbank%20Locations%20report.html)   

The output is available at https://osmlatvija.github.io/Osmalyzer/ as a static webpage (JS required).

The report is periodically automatically updated (vai GitHub workflow).

This project aims to parse data that is too complex to parse via tools like Overpass Turbo or visualize without a map and filtered output. This project aims to include Latvia-specific sources and use cases rather than being a generic QA tool.

Suggestions and improvements welcome.

## Manual usage / development

You can download the project and run it locally. (This is a C# .NET solution. The main project ("Osmalyzer") is a console project. Download the whole solution and open it in any compatible IDE (like Visual Studio, Visual Code, Rider, MonoDevelop, etc.) Compile and run as per your IDE (e.g. F5 in Visual Studio). You will need to download and restore/install Nuget packages (i.e. external libraries), depending on how your IDE does this - this should be automatic or prompted the first time you try to import/compile/run the project. The output reports will get generated in the build folder, e.g. "Osmalyzer\bin\Debug\net6.0\output\". Enable/disable individual analyzers/reports in `Runner.Run()`. Some web stuff may fail locally due to some websites being particular.
