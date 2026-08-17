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
}

public class DaugavpilsAPATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA Daugavpils autobusu parks";
    protected override string AgencyLabel => "Daugavpils AP";
}

public class LiepajasAPATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS Liepājas autobusu parks";
    protected override string AgencyLabel => "Liepajas AP";
}

public class ViviATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "Pasažieru vilciens AS";
    protected override string AgencyLabel => "Vivi";
}

public class TalsuATATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS Talsu autotransports";
    protected override string AgencyLabel => "Talsu AT";
}

public class LSAATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA Latvijas Sabiedriskais Autobuss";
    protected override string AgencyLabel => "LSA";
}

public class VentspilsReissATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "Pašvaldības SIA Ventspils reiss";
    protected override string AgencyLabel => "Ventspils reiss";
}

public class RezeknesAPATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS Rēzeknes autobusu parks";
    protected override string AgencyLabel => "Rezeknes AP";
}

public class MKAutobussATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS MK Autobuss";
    protected override string AgencyLabel => "MK Autobuss";
}

public class NORMAAATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA NORMA-A";
    protected override string AgencyLabel => "Norma A";
}

public class NordekaATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS Nordeka";
    protected override string AgencyLabel => "Nordeka";
}

public class CATAATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "AS CATA";
    protected override string AgencyLabel => "CATA";
}

public class VTUValmieraATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA VTU Valmiera";
    protected override string AgencyLabel => "VTU Valmiera";
}

public class LuxExpressATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA \"Lux Express Latvia\"";
    protected override string AgencyLabel => "Lux Express";
}

public class BussiliinidATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "ATG Bussiliinid OÜ";
    protected override string AgencyLabel => "Bussiliinid";
}

public class HansabussATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "SIA HANSABUSS Latvija";
    protected override string AgencyLabel => "Hansabuss";
}

public class GulbenesAluksnesBanitisATDRouteAnalyzer : AutotransportaDirekcijaRouteAnalyzer
{
    protected override string Agency => "Gulbenes-Alūksnes bānītis";
    protected override string AgencyLabel => "Gulbenes Aluksnes banitis";
}