using AltaSoft.Storm.Generator.Common;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Helpers;

/// <summary>
/// Represents the context for the ORM generator.
/// </summary>
internal sealed class OrmGeneratorContext
{
    /// <summary>
    /// Initializes a new instance of the OrmGeneratorContext class with the specified SourceProductionContext, Compilation, and OrmGeneratorOptions.
    /// </summary>
    /// <param name="context">The SourceProductionContext to use.</param>
    /// <returns> A new instance of the OrmGeneratorContext class.</returns>
    public OrmGeneratorContext(SourceProductionContext context)
    {
        Context = context;
        Builder = new SourceCodeBuilder();
        Parser = new Parser(context);
    }

    /// <summary>
    /// Gets the SourceProductionContext object associated with the property.
    /// </summary>
    public SourceProductionContext Context { get; }

    /// <summary>
    /// Gets or sets the SourceCodeBuilder object used for building source code.
    /// </summary>
    public SourceCodeBuilder Builder { get; private set; }

    /// <summary>
    /// Gets or sets the Parser object used for parsing source code.
    /// </summary>
    public Parser Parser { get; private set; }

    /// <summary>
    /// Resets the Builder property by creating a new instance of SourceCodeBuilder.
    /// </summary>
    internal void ResetBuilder() => Builder = new SourceCodeBuilder();
}
