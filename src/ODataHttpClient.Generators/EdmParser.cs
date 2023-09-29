using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ODataHttpClient.Generators
{
    internal class EdmParser
    {
        internal static IEnumerable<Namespace> ParseEdmXml(string xml)
        {
            var result = new List<Namespace>();
            var doc = XDocument.Parse(xml);

            foreach (var schema in doc.Descendants(xmlns + "Schema"))
            {
                result.Add(new Namespace(schema));
            }

            return result;
        }

        private static XNamespace xmlns = "http://docs.oasis-open.org/odata/ns/edm";
        internal class Property
        {
            public string EdmType { get; set; }
            public string Name { get; set; }
            public bool Nullable { get; set; }

            private const string PREFIX_COLLECTION = "Collection(";
            public string ClrType
            {
                get
                {
                    string type;
                    bool primitiveOrStruct;
                    if (EdmType.StartsWith(PREFIX_COLLECTION))
                    {
                        var edmType = EdmType.Substring(PREFIX_COLLECTION.Length, EdmType.Length - PREFIX_COLLECTION.Length - 1);
                        (type, primitiveOrStruct) = TransformToClrType(edmType);
                        type = $"System.Collections.Generic.ICollection<{type}>";
                    }
                    else
                    {
                        (type, primitiveOrStruct) = TransformToClrType(EdmType);
                    }

                    if (Nullable && primitiveOrStruct)
                        return type + "?";

                    return type;
                }
            }

            public Property() { }
            public Property(XElement el)
            {
                Name = el.Attribute("Name").Value;
                EdmType = el.Attribute("Type").Value;
                Nullable = el.Attribute("Nullable")?.Value != "false";
            }

            private static (string type, bool primitiveOrStruct) TransformToClrType(string edmType)
            {
                if (!edmType.StartsWith("Edm."))
                    return (edmType, false);

                switch (edmType)
                {
                    case "Edm.Binary": return ("byte[]", false);
                    case "Edm.Boolean": return ("bool", true);
                    case "Edm.Byte": return ("byte", true);
                    case "Edm.Date": return ("System.DateOnly", true);
                    case "Edm.DateTimeOffset": return ("System.DateTimeOffset", true);
                    case "Edm.Decimal": return ("decimal", true);
                    case "Edm.Double": return ("double", true);
                    case "Edm.Duration": return ("System.TimeSpan", true);
                    case "Edm.Guid": return ("System.Guid", true);
                    case "Edm.Int16": return ("short", true);
                    case "Edm.Int32": return ("int", true);
                    case "Edm.Int64": return ("long", true);
                    case "Edm.SByte": return ("sbyte", true);
                    case "Edm.Single": return ("float", true);
                    case "Edm.Stream": return ("System.IO.Stream", false);
                    case "Edm.String": return ("string", false);
                    case "Edm.TimeOfDay": return ("System.TimeSpan", true);
                    case "Edm.Geography": return ("Microsoft.Spatial.Geography", false);
                    case "Edm.GeographyPoint": return ("Microsoft.Spatial.GeographyPoint", false);
                    case "Edm.GeographyLineString": return ("Microsoft.Spatial.GeographyLineString", false);
                    case "Edm.GeographyPolygon": return ("Microsoft.Spatial.GeographyPolygon", false);
                    case "Edm.GeographyMultiPoint": return ("Microsoft.Spatial.GeographyMultiPoint", false);
                    case "Edm.GeographyMultiLineString": return ("Microsoft.Spatial.GeographyMultiLineString", false);
                    case "Edm.GeographyMultiPolygon": return ("Microsoft.Spatial.GeographyMultiPolygon", false);
                    case "Edm.GeographyCollection": return ("Microsoft.Spatial.GeographyCollection", false);
                    case "Edm.Geometry": return ("Microsoft.Spatial.Geometry", false);
                    case "Edm.GeometryPoint": return ("Microsoft.Spatial.GeometryPoint", false);
                    case "Edm.GeometryLineString": return ("Microsoft.Spatial.GeometryLineString", false);
                    case "Edm.GeometryPolygon": return ("Microsoft.Spatial.GeometryPolygon", false);
                    case "Edm.GeometryMultiPoint": return ("Microsoft.Spatial.GeometryMultiPoint", false);
                    case "Edm.GeometryMultiLineString": return ("Microsoft.Spatial.GeometryMultiLineString", false);
                    case "Edm.GeometryMultiPolygon": return ("Microsoft.Spatial.GeometryMultiPolygon", false);
                    case "Edm.GeometryCollection": return ("Microsoft.Spatial.GeometryCollection", false);
                    default:
                        throw new NotSupportedException(edmType);
                }
            }
        }

        internal class Class
        {
            public string Name { get; set; }
            public string BaseType { get; set; }
            public ICollection<Property> Properties { get; } = new List<Property>();

            public Class() { }
            public Class(XElement el)
            {
                Name = el.Attribute("Name").Value;
                BaseType = el.Attribute("BaseType")?.Value;
                Properties = el.Descendants(xmlns + "Property")
                            .Concat(el.Descendants(xmlns + "NavigationProperty"))
                            .Select(type => new Property(type)).ToList();
            }
        }

        internal class Namespace
        {
            public string Name { get; set; }
            public ICollection<Class> Classes { get; }

            public Namespace() { }
            public Namespace(XElement el)
            {
                Name = el.Attribute("Namespace").Value;
                Classes = el.Descendants(xmlns + "ComplexType")
                            .Concat(el.Descendants(xmlns + "EntityType"))
                            .Select(type => new Class(type)).ToList();
            }
        }
    }
}
