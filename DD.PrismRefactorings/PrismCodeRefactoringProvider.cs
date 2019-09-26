// <copyright file="PrismCodeRefactoringProvider.cs" company="Daniel Dreibrodt">
// Copyright (c) Daniel Dreibrodt. All rights reserved.
// </copyright>

namespace DD.PrismRefactorings
{
    using System.Composition;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeRefactorings;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Rename;

    /// <summary>
    /// Provides refactorings related to the Prism framework.
    /// </summary>
    /// <seealso cref="Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoringProvider" />
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PrismCodeRefactoringProvider))]
    [Shared]
    public sealed class PrismCodeRefactoringProvider : CodeRefactoringProvider
    {
        /// <inheritdoc/>
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var selectedNode = root.FindNode(context.Span);

            var propertyDeclaration = selectedNode.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (propertyDeclaration != null)
            {
                var accessors = propertyDeclaration.AccessorList?.Accessors;
                var getter = accessors?.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
                var setter = accessors?.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);
                if (getter != null && setter != null && IsAutoAccessor(getter) && IsAutoAccessor(setter))
                {
                    var action = CodeAction.Create("Convert to Prism property", c => this.CreatePrismProperty(context.Document, propertyDeclaration, c));
                    context.RegisterRefactoring(action);
                }
            }
        }

        private static bool IsAutoAccessor(AccessorDeclarationSyntax accessor)
        {
            return accessor.Body == null && accessor.ExpressionBody == null;
        }

        private static AccessorDeclarationSyntax CreateGetter(string backingFieldName)
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithKeyword(CreateTokenWithTrailingSpace(SyntaxKind.GetKeyword))
                                .WithExpressionBody(
                                    SyntaxFactory.ArrowExpressionClause(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.ThisExpression(),
                                            SyntaxFactory.IdentifierName(backingFieldName)))
                                    .WithArrowToken(CreateTokenWithTrailingSpace(SyntaxKind.EqualsGreaterThanToken)))
                                .WithSemicolonToken(CreateTokenWithTrailingSpace(SyntaxKind.SemicolonToken));
        }

        private static AccessorDeclarationSyntax CreateSetter(string backingFieldName)
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                .WithKeyword(CreateTokenWithTrailingSpace(SyntaxKind.SetKeyword))
                                .WithExpressionBody(
                                    SyntaxFactory.ArrowExpressionClause(
                                        SyntaxFactory.InvocationExpression(
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.ThisExpression(),
                                                SyntaxFactory.IdentifierName("SetProperty")))
                                        .WithArgumentList(
                                            SyntaxFactory.ArgumentList(
                                                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                                                    new SyntaxNodeOrToken[]
                                                    {
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                SyntaxFactory.ThisExpression(),
                                                                SyntaxFactory.IdentifierName(backingFieldName)))
                                                        .WithRefKindKeyword(CreateTokenWithTrailingSpace(SyntaxKind.RefKeyword)),
                                                        CreateTokenWithTrailingSpace(SyntaxKind.CommaToken),
                                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value")),
                                                    }))))
                                    .WithArrowToken(CreateTokenWithTrailingSpace(SyntaxKind.EqualsGreaterThanToken)))
                                .WithSemicolonToken(CreateTokenWithTrailingSpace(SyntaxKind.SemicolonToken));
        }

        private static SyntaxToken CreateTokenWithTrailingSpace(SyntaxKind tokenKind)
        {
            return SyntaxFactory.Token(SyntaxFactory.TriviaList(), tokenKind, SyntaxFactory.TriviaList(SyntaxFactory.Space));
        }

        private async Task<Document> CreatePrismProperty(Document document, PropertyDeclarationSyntax propertyDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var classDeclaration = propertyDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var fields = classDeclaration?.DescendantNodes().OfType<FieldDeclarationSyntax>();

            var backingFieldName = propertyDeclaration.Identifier.ValueText;
            if (char.IsUpper(backingFieldName, 0))
            {
                backingFieldName = char.ToLower(backingFieldName[0]) + backingFieldName.Substring(1);
            }
            else
            {
                backingFieldName = "_" + backingFieldName;
            }

            if (fields != null)
            {
                int i = 1;
                while (fields.Any(f => f.Declaration.Variables.Any(v => v.Identifier.ValueText == backingFieldName)))
                {
                    i++;
                    backingFieldName = backingFieldName + i.ToString("D");
                }
            }

            var indentation = propertyDeclaration.GetLeadingTrivia().LastOrDefault(t => t.Kind() == SyntaxKind.WhitespaceTrivia);

            var newFieldDeclaration =
                SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(
                            propertyDeclaration.Type.WithTrailingTrivia(SyntaxFactory.Space),
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(backingFieldName)))))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxFactory.TriviaList(indentation), SyntaxKind.PrivateKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space))));

            var newPropertyDeclaration = propertyDeclaration
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { CreateGetter(backingFieldName), CreateSetter(backingFieldName) })));

            var newRoot = root.ReplaceNode(propertyDeclaration, newPropertyDeclaration);
            classDeclaration = classDeclaration != null ? newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.ValueText == classDeclaration.Identifier.ValueText) : null;

            if (classDeclaration != null)
            {
                var ctor = classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
                if (ctor != null)
                {
                    newRoot = newRoot.InsertNodesBefore(ctor, new[] { newFieldDeclaration });
                }
                else
                {
                    var lastFieldDeclaration = classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>().LastOrDefault();
                    if (lastFieldDeclaration != null)
                    {
                        newRoot = newRoot.InsertNodesAfter(lastFieldDeclaration, new[] { newFieldDeclaration });
                    }
                    else
                    {
                        var newMembers = classDeclaration.Members.Insert(0, newFieldDeclaration);
                        newRoot = newRoot.ReplaceNode(classDeclaration, classDeclaration.WithMembers(newMembers));
                    }
                }
            }
            else
            {
                newRoot = newRoot.InsertNodesBefore(newRoot.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault(), new[] { newFieldDeclaration });
            }

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
