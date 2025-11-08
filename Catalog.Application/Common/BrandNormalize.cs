namespace Catalog.Application.Common;

internal static class BrandNormalize
{
    public static string Normalize(string input) =>
        input.Trim().ToLowerInvariant()
            .Replace('ي', 'ی')
            .Replace('ك', 'ک');
}