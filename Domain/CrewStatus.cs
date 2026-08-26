using System;
using System.Collections.Generic;

namespace mhd.Domain;

public static class CrewStatus
{
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1"] = "Escaped / Evaded",
        ["2"] = "Interned",
        ["3"] = "Killed in Action",
        ["4"] = "Missing in Action",
        ["5"] = "Parachuted",
        ["6"] = "Prisoner Of War",
        ["7"] = "Repatriated",
        ["8"] = "Wounded in Action",
        ["9"] = "Died of Wounds",
        ["10"] = "Hospitalized-Released",
        ["11"] = "Wounded Sent Home",
        ["12"] = "Aborted Mission",
        ["13"] = "Prisoner of War / Deceased",
        ["14"] = "Planned Abortive",
        ["15"] = "Mission Recalled",
        ["16"] = "Ditched",
        ["17"] = "Ditched / Returned",
        ["19"] = "",
        ["20"] = "Aircraft Scrubbed",
        ["21"] = "Did Not Take Off",
        ["22"] = "Returned To Military Base",
        ["23"] = "Flew w/Another Group",
        ["24"] = "Interned / Repatriated",
        ["25"] = "Shot Down",
        ["Killed In Action"] = "Killed in Action",
        ["Killed in Action"] = "Killed in Action",
        ["Missing in Action"] = "Missing in Action",
        ["Prisoner Of War"] = "Prisoner Of War",
        ["Prisoner of War"] = "Prisoner Of War",
        ["Died of Wounds"] = "Died of Wounds",
        ["Wounded in Action"] = "Wounded in Action",
        ["Escaped / Evaded"] = "Escaped / Evaded",
        ["Interned"] = "Interned",
        ["Parachuted"] = "Parachuted",
        ["Ditched"] = "Ditched",
        ["Shot Down"] = "Shot Down",
        ["Aborted Mission"] = "Aborted Mission",
        ["Interned / Repatriated"] = "Interned / Repatriated",
    };

    public static string Display(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Trim() is "0" or "False" or "false")
        {
            return string.Empty;
        }

        var key = raw.Trim();
        return Labels.TryGetValue(key, out var label) ? label : key;
    }

    public static bool IsKia(string? raw)
    {
        var text = Display(raw);
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.Contains("Killed in Action", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsKiaFlag(string? personnelKia)
    {
        if (string.IsNullOrWhiteSpace(personnelKia))
        {
            return false;
        }

        var value = personnelKia.Trim();
        if (value is "0" or "False" or "false" or "-0")
        {
            return false;
        }

        if (value is "1" or "-1" or "True" or "true" or "Yes" or "YES" or "KIA")
        {
            return true;
        }

        return IsKia(value);
    }
}
