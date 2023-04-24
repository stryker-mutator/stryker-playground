using System;
using System.Collections.Generic;
using System.Linq;

namespace Stryker.Core.Common.ProjectComponents.TestProjects
{
    public sealed class TestProject : IEquatable<TestProject>
    {
        public IEnumerable<TestFile> TestFiles { get; init; } = new List<TestFile>();

        public bool Equals(TestProject? other) => other != null && other.TestFiles.SequenceEqual(TestFiles);

        public override bool Equals(object? obj) => obj is TestProject project && Equals(project);

        // Stryker disable once bitwise: Bitwise mutation does not change functional usage of GetHashCode
        public override int GetHashCode() => TestFiles.GetHashCode();
    }
}
