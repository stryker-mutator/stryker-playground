using Stryker.Core.Common.Reporters.Json;

namespace Stryker.Playground.Domain.Reporters;

public static class HtmlReportBuilder
{
    public static string GetHtmlReport(this JsonReport jsonReport)
    {
        using var htmlStream = typeof(HtmlReportBuilder).Assembly
            .GetManifestResourceStream(typeof(HtmlReportBuilder)
                .Assembly.GetManifestResourceNames()
                .Single(m => m.Contains("mutation-report.html")));

        using var jsStream = typeof(HtmlReportBuilder).Assembly
            .GetManifestResourceStream(typeof(HtmlReportBuilder)
                .Assembly.GetManifestResourceNames()
                .Single(m => m.Contains("mutation-test-elements.js")));

        using var htmlReader = new StreamReader(htmlStream);
        using var jsReader = new StreamReader(jsStream);
        
        var fileContent = htmlReader.ReadToEnd();

        fileContent = fileContent.Replace("##REPORT_JS##", jsReader.ReadToEnd());
        fileContent = fileContent.Replace("##REPORT_TITLE##", "Stryker.NET Report");
        fileContent = fileContent.Replace("##REPORT_JSON##", jsonReport.ToJsonHtmlSafe());

        return fileContent;
    }
}