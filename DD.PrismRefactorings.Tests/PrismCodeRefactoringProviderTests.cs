// <copyright file="PrismCodeRefactoringProviderTests.cs" company="Daniel Dreibrodt">
// Copyright (c) Daniel Dreibrodt. All rights reserved.
// </copyright>

namespace DD.PrismRefactorings.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DD.PrismRefactorings.Tests.Properties;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.Text;
    using NUnit.Framework;

    [TestFixture]
    public class PrismCodeRefactoringProviderTests
    {
        private static TestCaseData[] refactorings =
        {
            new TestCaseData(Resources.SampleViewModel, Resources.SampleViewModelRefactored).SetName("ApplyRefactoring(NoCtor)"),
            new TestCaseData(Resources.SampleViewModelWithFields, Resources.SampleViewModelWithFieldsRefactored).SetName("ApplyRefactoring(NoCtorAndFields)"),
            new TestCaseData(Resources.SampleViewModelWithCtor, Resources.SampleViewModelWithCtorRefactored).SetName("ApplyRefactoring(Ctor)"),
            new TestCaseData(Resources.SampleViewModelWithCtorAndFields, Resources.SampleViewModelWithCtorAndFieldsRefactored).SetName("ApplyRefactoring(CtorAndFields)"),
            new TestCaseData(Resources.NoClassDeclaration, Resources.NoClassDeclarationRefactored).SetName("ApplyRefactoring(NoClass)"),
        };

        [Test]
        public async Task ComputeRefactorings()
        {
            var code = Resources.SampleViewModel;
            var doc = CreateDocument(code, "SampleViewModel.cs");
            var span = new TextSpan(code.IndexOf("public string Name") + "public string ".Length, "Name".Length);
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(doc, span, actions.Add, CancellationToken.None);
            var provider = new PrismCodeRefactoringProvider();

            await provider.ComputeRefactoringsAsync(context);

            Assert.That(actions, Has.Count.EqualTo(1));
        }

        [TestCaseSource(nameof(refactorings))]
        public async Task ApplyRefactoring(string code, string refactoredCode)
        {
            var doc = CreateDocument(code, "SampleViewModel.cs");
            var workspace = doc.Project.Solution.Workspace;
            var span = new TextSpan(code.IndexOf("public string Name") + "public string ".Length, "Name".Length);
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(doc, span, actions.Add, CancellationToken.None);
            var provider = new PrismCodeRefactoringProvider();
            await provider.ComputeRefactoringsAsync(context);
            var action = actions.First();

            var operations = await action.GetOperationsAsync(CancellationToken.None);
            operations[0].Apply(workspace, CancellationToken.None);

            var updatedCode = await workspace.CurrentSolution.GetDocument(doc.Id).GetTextAsync();
            Assert.That(updatedCode.ToString(), Is.EqualTo(refactoredCode));
        }

        protected static Document CreateDocument(string code, string fileName)
        {
            var projectId = ProjectId.CreateNewId(debugName: "TestProject");
            var documentId = DocumentId.CreateNewId(projectId, debugName: fileName);

            // find these assemblies in the running process
            string[] simpleNames = { "mscorlib", "System.Core", "System" };

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => simpleNames.Contains(a.GetName().Name, StringComparer.OrdinalIgnoreCase))
                .Select(a => MetadataReference.CreateFromFile(a.Location));

            return new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddMetadataReferences(projectId, references)
                .AddDocument(documentId, fileName, code)
                .GetDocument(documentId);
        }
    }
}
