using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Core.Common.Mutants;
using Stryker.Core.Common.ProjectComponents;

namespace Stryker.Playground.Domain;

public class ProjectComponentBuilder
{
    public static IProjectComponent BuildProjectComponent(SyntaxTree originalSourceTree, IEnumerable<Mutant> mutants)
    {
        var projectFolder = new CsharpFolderComposite()
        {
            FullPath = "/playground",
            RelativePath = "",
        };

        var sourceFile = new CsharpFileLeaf()
        {
            Mutants = mutants,
            SourceCode = originalSourceTree.GetRoot().ToFullString(),
            RelativePath = "Program.cs",
            FullPath = projectFolder + "/Program.cs",
            SyntaxTree = originalSourceTree,
        };
        
        projectFolder.Add(sourceFile);

        return projectFolder;
    }
}