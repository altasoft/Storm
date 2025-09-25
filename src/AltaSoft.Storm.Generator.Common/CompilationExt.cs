using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

public static class CompilationExt
{
    #region Attributes

    /// <summary>
    /// Checks if the key-value pair matches the specified name and type.
    /// </summary>
    /// <param name="self">The key-value pair to check.</param>
    /// <param name="name">The name to compare.</param>
    /// <param name="type">The type to compare.</param>
    /// <returns>True if the key-value pair matches the specified name and type, false otherwise.</returns>
    public static bool Is(this KeyValuePair<string, TypedConstant> self, string name, string type)
    {
        return string.Equals(self.Key, name, System.StringComparison.Ordinal) && string.Equals(self.Value.Type?.ToString(), type, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Checks if the KeyValuePair's key matches the given name and the value's type matches the given SpecialType.
    /// </summary>
    /// <param name="self">The KeyValuePair to check.</param>
    /// <param name="name">The name to compare the key with.</param>
    /// <param name="specialType">The SpecialType to compare the value's type with.</param>
    /// <returns>True if the key matches the name and the value's type matches the SpecialType, otherwise false.</returns>
    public static bool Is(this KeyValuePair<string, TypedConstant> self, string name, SpecialType specialType)
    {
        return string.Equals(self.Key, name, System.StringComparison.Ordinal) && self.Value.Type?.SpecialType == specialType;
    }

    /// <summary>
    /// Extension method that checks if the given AttributeData is of the specified type.
    /// </summary>
    /// <returns>True if the AttributeData is of the specified type, false otherwise.</returns>
    public static bool IsOfType(this AttributeData self, int arity, string attributeName, string? attributeNamespace = Constants.StormAttributeNamespace)
    {
        var classType = self.AttributeClass;
        if (classType is null)
            return false;
        return classType.Arity == arity &&
                string.Equals(classType.Name, attributeName, System.StringComparison.Ordinal) &&
                string.Equals(classType.ContainingNamespace?.ToString(), attributeNamespace, System.StringComparison.Ordinal);
    }

    #endregion Attributes

    #region Property & Fields

    /// <summary>
    /// Retrieves the properties of the specified type symbol that are compatible with Storm framework.
    /// </summary>
    /// <param name="self">The type symbol.</param>
    /// <returns>An enumerable collection of property symbols.</returns>
    public static IEnumerable<IPropertySymbol> GetStormCompatibleProperties(this ITypeSymbol self)
    {
        return self.GetMembersOfType<IPropertySymbol>().Where(x
            => x is { IsStatic: false, IsWriteOnly: false, CanBeReferencedByName: true, DeclaredAccessibility: Accessibility.Public });
    }

    #endregion Property & Fields

    /// <summary>
    /// Retrieves members of a specified type from a given ITypeSymbol.
    /// </summary>
    /// <typeparam name="TMember">The type of members to retrieve.</typeparam>
    /// <param name="self">The ITypeSymbol to retrieve members from.</param>
    /// <returns>An IEnumerable of members of the specified type.</returns>
    public static IEnumerable<TMember> GetMembersOfType<TMember>(this ITypeSymbol? self) where TMember : ISymbol
    {
        return self?.GetMembers().OfType<TMember>() ?? [];
    }

    /// <summary>
    /// Reports a diagnostic with the specified ID, title, message, severity, location, and message parameters.
    /// </summary>
    /// <param name="self">The SourceProductionContext instance.</param>
    /// <param name="id">The ID of the diagnostic.</param>
    /// <param name="title">The title of the diagnostic.</param>
    /// <param name="message">The message of the diagnostic.</param>
    /// <param name="severity">The severity of the diagnostic.</param>
    /// <param name="location">The location of the diagnostic.</param>
    /// <param name="messageParams">The message parameters of the diagnostic.</param>
    public static void ReportDiagnostic(this SourceProductionContext self, string id, string title, string message, DiagnosticSeverity severity, Location? location, params object?[]? messageParams)
    {
        var descriptor = new DiagnosticDescriptor(id, title, message, "General", severity, true);

        self.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageParams));
    }

    /// <summary>
    /// Reports a diagnostic with the specified ID, title, message, severity, location, and message parameters.
    /// </summary>
    /// <param name="self">The SourceProductionContext instance.</param>
    /// <param name="id">The ID of the diagnostic.</param>
    /// <param name="title">The title of the diagnostic.</param>
    /// <param name="message">The message of the diagnostic.</param>
    /// <param name="severity">The severity of the diagnostic.</param>
    /// <param name="location">The location of the diagnostic.</param>
    /// <param name="messageParams">The message parameters of the diagnostic.</param>
    public static void ReportDiagnostic(this SourceProductionContext? self, string id, string title, string message, DiagnosticSeverity severity, Location? location, params object?[]? messageParams)
    {
        if (!self.HasValue)
            return;

        ReportDiagnostic(self.Value, id, title, message, severity, location, messageParams);
    }

    /// <summary>
    /// Reports a diagnostic error with the specified ID, title, message, location, and message parameters.
    /// </summary>
    /// <param name="self">The SourceProductionContext instance.</param>
    /// <param name="id">The ID of the diagnostic error.</param>
    /// <param name="title">The title of the diagnostic error.</param>
    /// <param name="message">The message of the diagnostic error.</param>
    /// <param name="location">The location of the diagnostic error.</param>
    /// <param name="messageParams">Optional message parameters for the diagnostic error.</param>
    public static void ReportDiagnosticError(this SourceProductionContext? self, string id, string title, string message, Location? location, params object?[]? messageParams)
    {
        self.ReportDiagnostic(id, title, message, DiagnosticSeverity.Error, location, messageParams);
    }

    /// <summary>
    /// Reports a diagnostic warning with the specified ID, title, message, location, and message parameters.
    /// </summary>
    /// <param name="self">The SourceProductionContext instance.</param>
    /// <param name="id">The ID of the diagnostic warning.</param>
    /// <param name="title">The title of the diagnostic warning.</param>
    /// <param name="message">The message of the diagnostic warning.</param>
    /// <param name="location">The location of the diagnostic warning.</param>
    /// <param name="messageParams">Optional message parameters for the diagnostic warning.</param>
    public static void ReportDiagnosticWarning(this SourceProductionContext? self, string id, string title, string message, Location? location, params object?[]? messageParams)
    {
        self.ReportDiagnostic(id, title, message, DiagnosticSeverity.Error, location, messageParams);
    }
}
