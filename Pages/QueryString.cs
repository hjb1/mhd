using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace mhd.Pages;

internal static class QueryString
{
    public static string? Get(NavigationManager nav, string name)
    {
        if (nav == null || string.IsNullOrEmpty(nav.Uri))
        {
            return null;
        }

        try
        {
            var query = QueryOf(nav);
            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            var parsed = QueryHelpers.ParseQuery(query);
            if (parsed.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.ToString().Trim();
            }
        }
        catch
        {
        }

        return null;
    }

    private static string QueryOf(NavigationManager nav)
    {
        try
        {
            return nav.ToAbsoluteUri(nav.Uri).Query;
        }
        catch
        {
            var raw = nav.Uri;
            var i = raw.IndexOf('?');
            return i >= 0 ? raw[i..] : string.Empty;
        }
    }
}
