namespace Balkana.Data.Infrastructure
{
    public static class GameProfileProviderExtensions
    {
        public static bool IsFaceitProvider(string? provider) =>
            string.Equals(provider, "FACEIT", StringComparison.OrdinalIgnoreCase);

        public static bool IsRiotProvider(string? provider) =>
            string.Equals(provider, "RIOT", StringComparison.OrdinalIgnoreCase);
    }
}
