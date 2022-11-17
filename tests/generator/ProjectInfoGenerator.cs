using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nuke.Generator.Shims;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Nuke.Generator
{
    [Generator]
    public class BuilderSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var embededFiles = context.AdditionalFiles
                    // something is screwy with MSBuild, it doesn't consistently pass the extra metadata that would allow us to filter only additional files we're interested in
                    // https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md
                    
                // .Where(x => 
                //     context.AnalyzerConfigOptions.GetOptions(x).TryGetValue("build_metadata.AdditionalFiles.GenerateContentAsConstant", out var shouldGenerateStr) 
                //             && bool.TryParse(shouldGenerateStr, out var shouldGenerate) 
                //             && shouldGenerate
                //     )
                .Select(x => new { Info = new FileInfo(x.Path), Content = x.GetText().ToString()})
                .ToList();

            var constants = embededFiles.Select(file => FieldDeclaration(
                    VariableDeclaration(
                            PredefinedType(
                                Token(SyntaxKind.StringKeyword)))
                        .WithVariables(
                            SingletonSeparatedList<VariableDeclaratorSyntax>(
                                VariableDeclarator(
                                        Identifier(file.Info.Name.Replace(".","_")))
                                    .WithInitializer(
                                        EqualsValueClause(
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(file.Content)))))))
                .WithModifiers(
                    TokenList(
                        new[]
                        {
                            Token(SyntaxKind.InternalKeyword),
                            Token(SyntaxKind.ConstKeyword)
                        })))
                .Cast<MemberDeclarationSyntax>()
                .ToArray();
            
            var projectsClassGeneratedSource = CompilationUnit()
                
                .AddMembers(
                    ClassDeclaration("EmbeddedResources")
                        .AddModifiers(Token(SyntaxKind.InternalKeyword))
                        .AddMembers(constants))
                .NormalizeWhitespace()
                .ToFullString();


            context.AddSource("EmbeddedResources.gen.cs", projectsClassGeneratedSource);
        }


    }
}