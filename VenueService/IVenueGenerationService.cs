namespace VenueGenerationService
{
    public interface IVenueGenerationService
    {
        Task ReplaceJs(string tenantId);
        string GenerateHmac(string message, string secretKey);
        bool CryptographicEquals(string a, string b);
        Task<Tuple<List<string>, string>> GetVerifyData(string tenantId);
        string CreateHashWithReverseAlgorythm(string input);
    }
}
