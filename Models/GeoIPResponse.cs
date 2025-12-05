namespace CommonLibrary.Models
{

    public class GeoIPResponse
    {
        public Continent continent { get; set; }
        public Country country { get; set; }
        public RegisteredCountry registeredCountry { get; set; }
        //  public RepresentedCountry representedCountry { get; set; }
        public Traits traits { get; set; }
        public City city { get; set; }
        public Location location { get; set; }
        public Postal postal { get; set; }
        public List<Subdivision> subdivisions { get; set; }
    }

    public class City
    {
        public int geonameId { get; set; }
        public string name { get; set; }
    }

    public class Continent
    {
        public string code { get; set; }
        public int geonameId { get; set; }
        public string name { get; set; }
    }

    public class Country
    {
        public int geonameId { get; set; }
        public bool isInEuropeanUnion { get; set; }
        public string isoCode { get; set; }
        public string name { get; set; }
    }

    public class Location
    {
        public int accuracyRadius { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string timeZone { get; set; }
    }

    public class Postal
    {
        public string code { get; set; }
    }

    public class RegisteredCountry
    {
        public int geonameId { get; set; }
        public bool isInEuropeanUnion { get; set; }
        public string isoCode { get; set; }
        public string name { get; set; }
    }

    public class RepresentedCountry
    {
        public int? geonameId { get; set; }
        public bool isInEuropeanUnion { get; set; }
        public string isoCode { get; set; }
        public string name { get; set; }
    }

    public class Subdivision
    {
        public int geonameId { get; set; }
        public string isoCode { get; set; }
        public string name { get; set; }
    }

    public class Traits
    {
        public bool isAnonymous { get; set; }
        public bool isAnonymousProxy { get; set; }
        public bool isAnonymousVpn { get; set; }
        public bool isHostingProvider { get; set; }
        public bool isLegitimateProxy { get; set; }
        public bool isPublicProxy { get; set; }
        public bool isResidentialProxy { get; set; }
        public bool isSatelliteProvider { get; set; }
        public bool isTorExitNode { get; set; }
        public string ipAddress { get; set; }
        public string network { get; set; }
    }


}
