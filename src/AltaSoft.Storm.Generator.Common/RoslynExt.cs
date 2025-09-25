using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// https://www.meziantou.net/working-with-types-in-a-roslyn-analyzer.htm

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Contains extension methods for working with Roslyn.
/// </summary>
public static class RoslynExt
{
    /// <summary>
    /// Determines whether the given type or any of its base types is a descendant of StormContext.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type or any of its base types is a descendant  of StormContext; otherwise, false.</returns>
    public static bool IsDescendantOfStormContext(this ITypeSymbol type)
    {
        var baseType = type.BaseType;
        while (baseType is not null)
        {
            if (baseType.GetFullName() == "AltaSoft.Storm.StormContext")
            {
                return true;
            }

            baseType = baseType.BaseType;
        }
        return false;
    }

    /// <summary>
    /// Determines if the given ITypeSymbol represents a DateTime or DateOnly type.
    /// </summary>
    /// <param name="self">The INamedTypeSymbol to check.</param>
    /// <returns>True if the INamedTypeSymbol represents a DateTime or DateOnly type, false otherwise.</returns>
    public static bool IsDateTimeOrDateOnly(this ITypeSymbol self) => self.SpecialType == SpecialType.System_DateTime || string.Equals(self.ToDisplayString(), "System.DateOnly", StringComparison.Ordinal);

    /// <summary>
    /// Determines if the given ITypeSymbol represents an array type.
    /// </summary>
    /// <param name="self">The ITypeSymbol to check.</param>
    /// <returns>True if the ITypeSymbol represents an array type, false otherwise.</returns>
    public static bool IsArray(this ITypeSymbol self)
    {
        return self.SpecialType == SpecialType.System_Array;
    }

    /// <summary>
    /// Retrieves the location of a named argument within an attribute.
    /// </summary>
    /// <param name="self">The AttributeData object.</param>
    /// <param name="namedArgument">The name of the named argument.</param>
    /// <returns>The location of the named argument, or null if not found.</returns>
    public static Location? GetNamedArgumentLocation(this AttributeData self, string namedArgument)
    {
        var syntaxReference = self.ApplicationSyntaxReference;

        var syntax = (AttributeSyntax?)syntaxReference?.GetSyntax();
        var argument = syntax?.ArgumentList?.Arguments.FirstOrDefault(x => string.Equals(x.NameEquals?.Name.Identifier.ValueText, namedArgument, StringComparison.Ordinal));

        return argument?.Expression.GetLocation() ?? argument?.GetLocation();
    }
    /// <summary>
    /// Gets the location of the generic argument from the attribute data.    
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static Location? GetGenericArgumentLocation(this AttributeData self)
    {
        var syntaxReference = self.ApplicationSyntaxReference?.GetSyntax();

        var attributeSyntax = syntaxReference as AttributeSyntax;
        if (attributeSyntax!.Name is GenericNameSyntax g)
        {
            return g.TypeArgumentList.GetLocation();
        }

        return syntaxReference?.GetLocation();

    }

    /// <summary>
    /// Gets Constructor argument location from the attribute data.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="index"></param>
    /// <returns></returns>

    public static Location? GetConstructorArgumentLocation(this AttributeData self, int index)
    {
        var syntaxReference = self.ApplicationSyntaxReference;

        var syntax = (AttributeSyntax?)syntaxReference?.GetSyntax();
        if (syntax?.ArgumentList?.Arguments.Count > index)
            return syntax.ArgumentList.Arguments[index].GetLocation();
        return syntax?.GetLocation();
    }

    /// <summary>
    /// Checks if the given INamedTypeSymbol has a parameterless constructor.
    /// </summary>
    /// <param name="typeSymbol">The ITypeSymbol to check.</param>
    /// <returns>True if the INamedTypeSymbol has a parameterless constructor, false otherwise.</returns>
    public static bool HasParameterlessConstructor(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
            return false;

        foreach (var constructor in namedTypeSymbol.Constructors)
        {
            if (constructor.Parameters.IsEmpty && !constructor.IsImplicitlyDeclared)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Retrieves the attributes of a class declaration syntax.
    /// </summary>
    /// <param name="self">The class declaration syntax.</param>
    /// <returns>A list of attribute syntax.</returns>
    public static List<AttributeSyntax> GetAttributes(this TypeDeclarationSyntax self)
    {
        return self.AttributeLists.SelectMany(a => a.Attributes).ToList();
    }

    public static string GetAccessibility(this TypeDeclarationSyntax self)
    {
        var modifiers = self.Modifiers;

        if (modifiers.Any(SyntaxKind.PublicKeyword))
            return "public";
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
            return "private";
        if (modifiers.Any(SyntaxKind.ProtectedKeyword) && modifiers.Any(SyntaxKind.InternalKeyword))
            return "protected internal";
        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
            return "protected";
        if (modifiers.Any(SyntaxKind.InternalKeyword))
            return "internal";
        if (modifiers.Any(SyntaxKind.FileKeyword))
            return "file"; // C# 11 feature
#if NET9_0_OR_GREATER
        if (modifiers.Any(SyntaxKind.PrivateProtectedKeyword))
            return "private protected";
#endif
        return "internal"; // Default accessibility for top-level classes
    }

    /// <summary>
    /// Extension method to get the namespace of a TypeDeclarationSyntax.
    /// </summary>
    /// <param name="self">The TypeDeclarationSyntax instance.</param>
    /// <returns>The namespace of the TypeDeclarationSyntax, or null if it does not have a parent NamespaceDeclarationSyntax.</returns>
    public static string? GetNamespace(this TypeDeclarationSyntax self)
    {
        return self.Parent is not NamespaceDeclarationSyntax ns ? null : ns.Name.ToString();
    }

    /// <summary>
    /// Extension method to retrieve the CompilationUnitSyntax node that contains the given SyntaxNode.
    /// </summary>
    /// <param name="syntaxNode">The SyntaxNode to find the CompilationUnitSyntax node for.</param>
    /// <returns>The CompilationUnitSyntax node that contains the given SyntaxNode, or throws an exception if not found.</returns>
    public static CompilationUnitSyntax GetCompilationUnit(this SyntaxNode syntaxNode)
    {
        return syntaxNode.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault() ?? throw new Exception("Cannot find compilation unit");
    }

    /// <summary>
    /// Retrieves a list of using directives from the given CompilationUnitSyntax.
    /// </summary>
    /// <param name="root">The CompilationUnitSyntax to retrieve the using directives from.</param>
    /// <returns>A list of strings representing the using directives.</returns>
    public static List<string> GetUsings(this CompilationUnitSyntax root)
    {
        return root.ChildNodes()
            .OfType<UsingDirectiveSyntax>()
            .Select(n => n.Name?.ToString() ?? string.Empty)
            .ToList();
    }
}
