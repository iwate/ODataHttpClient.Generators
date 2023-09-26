using Microsoft.CodeAnalysis;

namespace ODataHttpClient.Generators;
public static class DiagnosticDescriptors
{
    const string Category = "ODataHttpClient.Generators";

    public static readonly DiagnosticDescriptor IllegalPropertyName = new(
        id: "OHCG001",
        title: "Illegal property name",
        messageFormat: "The property '{1}' does not exist in '{0}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}