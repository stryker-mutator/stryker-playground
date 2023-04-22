using Stryker.Core.Common.Mutants;
using Stryker.Core.Common.Options;
using Stryker.Core.Common.ProjectComponents;

namespace Stryker.Core.Common.Reporters.Json
{
    public partial class JsonReporter : IReporter
    {
        private readonly StrykerOptions _options;

        public JsonReporter(StrykerOptions options)
        {
            _options = options;
        }

        public void OnAllMutantsTested(IReadOnlyProjectComponent reportComponent)
        {
            var mutationReport = JsonReport.Build(_options, reportComponent);
            var filename = _options.ReportFileName + ".json";
            var reportPath = Path.Combine(_options.ReportPath, filename);
            var reportUri = "file://" + reportPath.Replace("\\", "/");
        }

        public void OnMutantsCreated(IReadOnlyProjectComponent reportComponent)
        {
            // This reporter does not currently report when mutants are created
        }

        public void OnMutantTested(IReadOnlyMutant result)
        {
            // This reporter does not currently report when mutants are tested
        }

        public void OnStartMutantTestRun(IEnumerable<IReadOnlyMutant> mutantsToBeTested)
        {
            // This reporter does not currently report when the mutation testrun starts
        }
    }
}
