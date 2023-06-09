﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BindableSourceGenerator
{
    [Generator]
    public class BindableSourceGenerator : IIncrementalGenerator
    {
        // TODO Generate Bindables from properties instead of the opposite,
        // TODO - As that would allow (the more common use-case of) setting attributes on the property.
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var syntaxTargets = context.SyntaxProvider.CreateSyntaxProvider(
                (n, _) => IsValidTarget(n),
                (ctx, _) => new SyntaxTarget(ctx.Node as ClassDeclarationSyntax, ctx.SemanticModel)
            ).Where(syntaxTarget => syntaxTarget.BindableTargets.Count > 0);

            context.RegisterSourceOutput(syntaxTargets, (spc, syntaxTarget) =>
            {
                var safeFilePath = syntaxTarget.FilePath.Replace("/", "_").Replace("\\", "_");
                var hintName = $"{safeFilePath.GetHashCode():X}_{syntaxTarget.HintName}.BindablesDecl";

                // TODO use syntax factory

                var bindablePropertiesBuilder = new StringBuilder();
                foreach (var bindableTarget in syntaxTarget.BindableTargets)
                {
                    bindablePropertiesBuilder.AppendLine($@"
    {bindableTarget.Visibility} {bindableTarget.TypeName} {bindableTarget.Name}
    {{
        get => {bindableTarget.Name}Bindable.Value;
        set => {bindableTarget.Name}Bindable.Value = value;
    }}"
                    );
                }

                spc.AddSource($"{hintName}", $@"
public partial class {syntaxTarget.Name}
{{
    {bindablePropertiesBuilder}
}}"
                );
            });
        }

        class BindableTarget
        {
            public string Name;
            public string TypeName;
            public string Visibility;
        }

        class SyntaxTarget
        {
            public string Name { get; private set; }
            public string HintName { get; private set; }
            public string FilePath { get; private set; }

            public List<BindableTarget> BindableTargets { get; private set; } = new List<BindableTarget>();

            public SyntaxTarget(ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classSyntax);

                FilePath = classSyntax.SyntaxTree.FilePath;
                Name = symbol.Name;
                HintName = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat).Replace('<', '{').Replace('>', '}');

                var fields = classSyntax.DescendantNodes().OfType<FieldDeclarationSyntax>();
                foreach (var fieldSyntax in fields)
                {
                    var field = (IFieldSymbol)semanticModel.GetDeclaredSymbol(fieldSyntax.Declaration.Variables.First());
                    if (field.Name.EndsWith("Bindable"))
                    {
                        if (field.Type.Name != "Bindable")
                        {
                            throw new System.Exception("Bindable fields need to have a Bindable type.");
                        }

                        var propertyName = field.Name.Replace("Bindable", string.Empty);
                        var fieldType = field.Type as INamedTypeSymbol;
                        var targetType = fieldType.TypeArguments[0];

                        BindableTargets.Add(new BindableTarget()
                        {
                            Name = propertyName,
                            TypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            Visibility = field.DeclaredAccessibility == Accessibility.Public ? "public" : "protected",
                        });
                    }
                }
            }
        }

        // static bool ContainsMemberWithName(INamedTypeSymbol symbol, string name)
        //     => symbol.GetMembers(name).Length > 0;

        static bool IsValidTarget(SyntaxNode syntaxNode)
            => syntaxNode is ClassDeclarationSyntax classSyntax
            && !classSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);
    }
}
