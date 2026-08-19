namespace Osmalyzer;

[UsedImplicitly]
public abstract class AutotransportaDirekcijaRouteAnalyzer : PublicTransportRouteAnalyzer<AutotransportaDirekcijaAnalysisData>
{
    protected override string Label => "Autotransporta Direkcija - " + AgencyLabel;

    protected override string? GtfsDisambiguatorId => "LV-ATD";
    protected override bool RegionalService => true;
}


public class GalssBussATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA GALSS BUSS";
    protected override string AgencyLabel => "Galss Buss";
    protected override bool IncludeAllVariants => true; // many infrequent variants around Salaspils but all very different
}

public class DaugavpilsAPATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA Daugavpils autobusu parks";
    protected override string AgencyLabel => "Daugavpils AP";
    protected override bool IncludeAllVariants => false;
}

public class LiepajasAPATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS Liepājas autobusu parks";
    protected override string AgencyLabel => "Liepajas AP";
    protected override bool IncludeAllVariants => false;
}

public class TalsuATATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS Talsu autotransports";
    protected override string AgencyLabel => "Talsu AT";
    protected override bool IncludeAllVariants => false;
}

public class LSAATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA Latvijas Sabiedriskais Autobuss";
    protected override string AgencyLabel => "LSA";
    protected override bool IncludeAllVariants => false;
}

public class VentspilsReissATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "Pašvaldības SIA Ventspils reiss";
    protected override string AgencyLabel => "Ventspils reiss";
    protected override bool IncludeAllVariants => false;
}

public class RezeknesAPATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS Rēzeknes autobusu parks";
    protected override string AgencyLabel => "Rezeknes AP";
    protected override bool IncludeAllVariants => false;
}

public class MKAutobussATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS MK Autobuss";
    protected override string AgencyLabel => "MK Autobuss";
    protected override bool IncludeAllVariants => true; // there are only 2 and they are both "primary"
}

public class NORMAAATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA NORMA-A";
    protected override string AgencyLabel => "Norma A";
    protected override bool IncludeAllVariants => false;
}

public class NordekaATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS Nordeka";
    protected override string AgencyLabel => "Nordeka";
    protected override bool IncludeAllVariants => false;
}

public class CATAATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS CATA";
    protected override string AgencyLabel => "CATA";
    protected override bool IncludeAllVariants => false;
}

public class VTUValmieraATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA VTU Valmiera";
    protected override string AgencyLabel => "VTU Valmiera";
    protected override bool IncludeAllVariants => false;
}

public class LuxExpressATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA \"Lux Express Latvia\"";
    protected override string AgencyLabel => "Lux Express";
    protected override bool IncludeAllVariants => false;
}

public class BussiliinidATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "ATG Bussiliinid OÜ";
    protected override string AgencyLabel => "Bussiliinid";
    protected override bool IncludeAllVariants => true; // a single route
}

public class HansabussATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA HANSABUSS Latvija";
    protected override string AgencyLabel => "Hansabuss";
    protected override bool IncludeAllVariants => false;
}

// Not doing train routes yet
// There are no routes for this atm
// public class ViviATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
// {
//     protected override string Agency => "Pasažieru vilciens AS";
//     protected override string AgencyLabel => "Vivi";
// }

// Not doing train routes yet
// There is one route
// public class GulbenesAluksnesBanitisATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
// {
//     protected override string Agency => "Gulbenes-Alūksnes bānītis";
//     protected override string AgencyLabel => "Gulbenes Aluksnes banitis";
// }