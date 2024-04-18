﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ODataHttpClient.Generators;

[Generator(LanguageNames.CSharp)]
public class PickGenerator : IIncrementalGenerator
{
    public static string NAMESPACE = "ODataHttpClient.Generators";
    public static string TARGET_ATTR = "PickAttribute";
    public static string TARGET_ATTR_T = TARGET_ATTR + "`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource($"{NAMESPACE}.{TARGET_ATTR}.g.cs", $$"""
                // <auto-generated/>
                using System;
                using System.Collections.Generic;
                namespace {{NAMESPACE}}
                {
                    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                    public sealed class {{TARGET_ATTR}} : Attribute
                    {
                        public Type Type { get; }
                        public IEnumerable<string> Properties { get; }
                        public {{TARGET_ATTR}}(Type type, params string[] properties)
                        {
                            Type = type;
                            Properties = properties;
                        }
                    }
                }
                """);

            context.AddSource($"{NAMESPACE}.{TARGET_ATTR}OfT.g.cs", $$"""
                // <auto-generated/>
                using System;
                using System.Collections.Generic;
                namespace {{NAMESPACE}}
                {
                    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
                    public sealed class {{TARGET_ATTR}}<T> : Attribute
                    {
                        public Type Type { get; }
                        public IEnumerable<string> Properties { get; }
                        public {{TARGET_ATTR}}(params string[] properties)
                        {
                            Type = typeof(T);
                            Properties = properties;
                        }
                    }
                }
                """);
        });



        var source = context.SyntaxProvider.ForAttributeWithMetadataName($"{NAMESPACE}.{TARGET_ATTR}",
            static (node, token) => true,
            static (context, token) => context).Combine(context.CompilationProvider);

        var sourceOfT = context.SyntaxProvider.ForAttributeWithMetadataName($"{NAMESPACE}.{TARGET_ATTR_T}",
            static (node, token) => true,
            static (context, token) => context).Combine(context.CompilationProvider);

        context.RegisterSourceOutput(source, Emit);
        context.RegisterSourceOutput(sourceOfT, EmitOfT);
    }
    static void EmitOfT(SourceProductionContext context, (GeneratorAttributeSyntaxContext source, Compilation compilation) args)
    {
        var (source, compilation) = args;
        var attr = source.Attributes.First();
        var srcType = attr.AttributeClass.TypeArguments.First() as INamedTypeSymbol;
        EmitImpl(context, compilation, source, srcType);
    }
    static void Emit(SourceProductionContext context, (GeneratorAttributeSyntaxContext source, Compilation compilation) args)
    {
        var (source, compilation) = args;
        var attr = source.Attributes.First();
        var srcType = attr.ConstructorArguments.First().Value as INamedTypeSymbol;
        EmitImpl(context, compilation, source, srcType);
    }
    static void EmitImpl(SourceProductionContext context, Compilation compilation, GeneratorAttributeSyntaxContext source, INamedTypeSymbol srcType)
    {
        var srcTypeName = srcType.ToDisplayString();

        if (EntityGenerator.SourceCodeRegistry.TryGetValue(srcTypeName, out var injectionCode))
        {
            var options = ((CSharpCompilation)compilation).SyntaxTrees[0].Options;
            var syntaxTree = CSharpSyntaxTree.ParseText(injectionCode, (CSharpParseOptions)options);
            compilation = compilation.AddSyntaxTrees(syntaxTree);
            srcType = compilation.GetTypeByMetadataName(srcTypeName);
        }

        var typeSymbol = (INamedTypeSymbol)source.TargetSymbol;
        var typeNode = (TypeDeclarationSyntax)source.TargetNode;

        var attr = source.Attributes.First();
        var srcProperties = srcType.GetMembers()
            .Where(m => m.Kind == SymbolKind.Property && m.DeclaredAccessibility == Accessibility.Public)
            .Cast<IPropertySymbol>()
            .ToList();
        var srcPropertyNames = srcProperties.Select(p => p.Name).ToArray();
        var dstPropertyNames = attr.ConstructorArguments.SelectMany(arg => arg.Kind switch
        {
            TypedConstantKind.Primitive => new[] { arg.Value as string },
            TypedConstantKind.Array => arg.Values.Select(v => v.Value as string),
            _ => new string[0],
        }).Where(name => name != null).ToArray();
        var illegalNames = dstPropertyNames.Except(srcPropertyNames);

        if (illegalNames.Any())
        {
            foreach (var name in illegalNames)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.IllegalPropertyName,
                    typeNode.Identifier.GetLocation(),
                    srcTypeName,
                    name
                ));
            }
            return;
        }

        var dstManualProperties = typeSymbol.GetMembers()
            .Where(m => m.Kind == SymbolKind.Property && m.DeclaredAccessibility == Accessibility.Public)
            .Cast<IPropertySymbol>();

        static string DeclareProperty(IPropertySymbol symbol)
        {
            var st = symbol.IsStatic ? "static" : string.Empty;
            var typeName = symbol.Type.ToDisplayString();
            var propName = symbol.Name;
            return $$"""
                public {{st}} {{typeName}} {{propName}} { get; set; }
                """;
        }

        static string DeclareUsing(UsingDirectiveSyntax usingDirective)
        {
            if (usingDirective.Alias != null)
            {
                return $"using {usingDirective.Alias.Name} = {usingDirective.Name};";
            }

            return $"using {usingDirective.Name};";
        }

        string AssignManualProperty(IPropertySymbol dst)
        {
            var name = dst.Name;
            var src = srcProperties.FirstOrDefault(q => q.Name == dst.Name);
            if (src == null)
                return string.Empty;

            var srcIsEnumerable = src.Type.AllInterfaces.Any(i => i.ToDisplayString() == "System.Collections.IEnumerable");
            var dstIsEnumerable = dst.Type.AllInterfaces.Any(i => i.ToDisplayString() == "System.Collections.IEnumerable");
            if (srcIsEnumerable != dstIsEnumerable)
                return string.Empty;

            var srcType = src.Type;
            var dstType = dst.Type;

            if (srcIsEnumerable)
            {
                srcType = ((INamedTypeSymbol)src.Type).TypeArguments.FirstOrDefault();
                dstType = ((INamedTypeSymbol)dst.Type).TypeArguments.FirstOrDefault();
            }

            var expected = src.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var hasCreatedMethod = dstType.GetAttributes().Any(a =>
                a.AttributeClass.ContainingNamespace.ToDisplayString() == NAMESPACE
                && a.AttributeClass.Name == TARGET_ATTR);

            if (!hasCreatedMethod)
                return string.Empty;

            if (dst.IsVirtual)
            {
                if (srcIsEnumerable)
                {
                    return $$"""
                        if (src.{{name}} != null) this.{{name}} = src.{{name}}.Select({{dstType.Name}}.Create).ToList();
                        """;
                }
                else
                {
                    return $$"""
                        this.{{name}} = {{dstType.Name}}.Create(src.{{name}});
                        """;
                }
            }
            else
            {
                return $$"""
                    this.{{name}} = src.{{name}};
                    """;
            }
        }

        var usingDirectives = GetUsingDirectives(srcType).Select(DeclareUsing).Concat(new[] { "using System.Linq;" }).Distinct();
        var ns = !typeSymbol.ContainingNamespace.IsGlobalNamespace ? $"namespace {typeSymbol.ContainingNamespace};" : string.Empty;
        var code = $$"""
            // <auto-generated/>
            #nullable disable
            #pragma warning disable CS8600
            #pragma warning disable CS8601
            #pragma warning disable CS8602
            #pragma warning disable CS8603
            #pragma warning disable CS8604

            {{string.Join("\n", usingDirectives)}}

            {{ns}}

            public partial class {{typeSymbol.Name}}
            {
                {{string.Join("\n    ", srcProperties.Where(p => dstPropertyNames.Contains(p.Name)).Select(DeclareProperty))}}
            
                public void Assign({{srcTypeName}} src)
                {
                    {{string.Join("\n        ", dstPropertyNames.Select(n => $"this.{n} = src.{n};"))}}
                    {{string.Join("\n        ", dstManualProperties.Select(AssignManualProperty).Where(s => s != string.Empty))}}
                }

                public static {{typeSymbol.Name}} Create({{srcTypeName}} src)
                {
                    var obj = new {{typeSymbol.Name}}();
                    obj.Assign(src);
                    return obj;
                }
            }
            """;

        var fileName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace("<", "_")
            .Replace(">", "_");

        context.AddSource($"{fileName}.{NAMESPACE}.g.cs", code);
    }

    static IEnumerable<UsingDirectiveSyntax> GetUsingDirectives(INamedTypeSymbol typeSymbol)
    {
        var result = new List<UsingDirectiveSyntax>();

        foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
        {
            foreach (var parent in syntaxRef.GetSyntax().Ancestors(false))
            {
                if (parent is NamespaceDeclarationSyntax namespaceDeclarationSyntax)
                    result.AddRange(namespaceDeclarationSyntax.Usings);

                else if (parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclarationSyntax)
                    result.AddRange(fileScopedNamespaceDeclarationSyntax.Usings);

                else if (parent is CompilationUnitSyntax compilationUnitSyntax)
                    result.AddRange(compilationUnitSyntax.Usings);
            }
        }

        return result;
    }
}
