using System.Net;
using System.Text.RegularExpressions;

namespace MeetingRoomBooking.IntegrationTests.Infrastructure;

internal static class TestFormHelper
{
    public static string ExtractToken(
        string html,
        string uniqueFieldName)
    {
        var regexTimeout = TimeSpan.FromSeconds(1);

        var forms = Regex.Matches(
            html,
            @"<form\b[^>]*>(?<content>.*?)</form\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            regexTimeout);

        var targetForm = forms
            .Cast<Match>()
            .Single(form => Regex.IsMatch(
                form.Groups["content"].Value,
                $@"\bname=""{Regex.Escape(uniqueFieldName)}""",
                RegexOptions.IgnoreCase,
                regexTimeout));

        var tokenInput = Regex.Matches(
                targetForm.Groups["content"].Value,
                @"<input\b[^>]*>",
                RegexOptions.IgnoreCase,
                regexTimeout)
            .Cast<Match>()
            .Single(input => Regex.IsMatch(
                input.Value,
                @"\bname=""__RequestVerificationToken""",
                RegexOptions.IgnoreCase,
                regexTimeout));

        var value = Regex.Match(
            tokenInput.Value,
            @"\bvalue=""([^""]*)""",
            RegexOptions.IgnoreCase,
            regexTimeout);

        Assert.True(
            value.Success,
            "Antiforgery token value was not found.");

        return WebUtility.HtmlDecode(value.Groups[1].Value);
    }
}