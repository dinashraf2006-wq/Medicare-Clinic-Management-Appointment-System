using System.ComponentModel.DataAnnotations;
using System.Reflection;
using MediCare.Data.Entities;

namespace MediCare.Web.Models.MedicalRecords;

/// <summary>Small helpers used by the Medical Records views (labels, colors, initials, dates).</summary>
public static class MedicalRecordsDisplay
{
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Replace("Dr.", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => (parts[0][..1] + parts[^1][..1]).ToUpperInvariant()
        };
    }

    // Same name always gets the same avatar color.
    private static readonly string[] AvatarColors = { "#6d4ae0", "#2e7d5b", "#c47b12", "#2f80c9", "#8e44ad", "#1f9aa8" };

    public static string AvatarColor(string? name)
    {
        var hash = 0;
        foreach (var c in name ?? string.Empty) hash = (hash * 31 + c) & 0x7fffffff;
        return AvatarColors[hash % AvatarColors.Length];
    }

    /// <summary>"Today", "Yesterday" or "Oct 2, 2026".</summary>
    public static string FriendlyDate(DateTime utc)
    {
        var local = utc.ToLocalTime().Date;
        var today = DateTime.Now.Date;
        if (local == today) return "Today";
        if (local == today.AddDays(-1)) return "Yesterday";
        return local.ToString("MMM d, yyyy");
    }

    public static string TypeBadgeClass(string type) => type switch
    {
        "ECG" => "badge-type-ecg",
        "Echo" => "badge-type-echo",
        "Lab" => "badge-type-lab",
        "Notes" => "badge-type-notes",
        "Imaging" => "badge-type-imaging",
        "Discharge" => "badge-type-discharge",
        _ => "badge-type-report"
    };

    public static string TypeIcon(string type) => type switch
    {
        "Lab" => "bi-eyedropper",
        "Imaging" => "bi-file-earmark-image",
        "ECG" or "Echo" => "bi-activity",
        _ => "bi-file-earmark-text"
    };

    public static string StatusBadgeClass(string status) => status switch
    {
        "Sent" => "badge-status-ok",
        "Refill due" => "badge-status-warn",
        _ => "badge-status-muted"
    };

    public static string DisplayName(this Enum value) =>
        value.GetType().GetMember(value.ToString()).FirstOrDefault()?
             .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();

    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / (1024.0 * 1024):0.#} MB"
    };

    /// <summary>Which times of day a medication is taken, derived from its frequency.</summary>
    public static IReadOnlyList<(string Slot, string Time, bool Active)> DailySchedule(MedicationFrequency frequency)
    {
        var active = frequency switch
        {
            MedicationFrequency.OnceDaily => new[] { true, false, false, false },
            MedicationFrequency.TwiceDaily => new[] { true, false, false, true },
            MedicationFrequency.ThreeTimesDaily => new[] { true, true, false, true },
            MedicationFrequency.FourTimesDaily => new[] { true, true, true, true },
            _ => new[] { false, false, false, false }
        };

        return new List<(string, string, bool)>
        {
            ("Morning", "8:00 AM", active[0]),
            ("Afternoon", "2:00 PM", active[1]),
            ("Evening", "6:00 PM", active[2]),
            ("Night", "10:00 PM", active[3])
        };
    }

    public static IReadOnlyList<string> SplitLines(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
