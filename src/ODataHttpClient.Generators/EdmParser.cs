using System;
using System.Linq;
using System.Collections.Generic;
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
                    string type = null;
                    if (EdmType.StartsWith(PREFIX_COLLECTION))
                    {
                        var edmType = EdmType.Substring(PREFIX_COLLECTION.Length, EdmType.Length - PREFIX_COLLECTION.Length - 1);
                        type = TransformToClrType(edmType);
                        type = $"System.Collections.Generic.ICollection<{type}>";
                    }
                    else
                    {
                        type = TransformToClrType(EdmType);
                    }

                    if (Nullable)
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

            private static string TransformToClrType(string edmType)
            {
                if (!edmType.StartsWith("Edm."))
                    return edmType;

                switch (edmType)
                {
                    case "Edm.Binary": return "byte[]";
                    case "Edm.Boolean": return "bool";
                    case "Edm.Byte": return "byte";
                    case "Edm.Date": return "System.DateOnly";
                    case "Edm.DateTimeOffset": return "System.DateTimeOffset";
                    case "Edm.Decimal": return "decimal";
                    case "Edm.Double": return "double";
                    case "Edm.Duration": return "System.TimeSpan";
                    case "Edm.Guid": return "System.Guid";
                    case "Edm.Int16": return "short";
                    case "Edm.Int32": return "int";
                    case "Edm.Int64": return "long";
                    case "Edm.SByte": return "sbyte";
                    case "Edm.Single": return "float";
                    case "Edm.Stream": return "System.IO.Stream";
                    case "Edm.String": return "string";
                    case "Edm.TimeOfDay": return "System.TimeSpan";
                    case "Edm.Geography": return "Microsoft.Spatial.Geography";
                    case "Edm.GeographyPoint": return "Microsoft.Spatial.GeographyPoint";
                    case "Edm.GeographyLineString": return "Microsoft.Spatial.GeographyLineString";
                    case "Edm.GeographyPolygon": return "Microsoft.Spatial.GeographyPolygon";
                    case "Edm.GeographyMultiPoint": return "Microsoft.Spatial.GeographyMultiPoint";
                    case "Edm.GeographyMultiLineString": return "Microsoft.Spatial.GeographyMultiLineString";
                    case "Edm.GeographyMultiPolygon": return "Microsoft.Spatial.GeographyMultiPolygon";
                    case "Edm.GeographyCollection": return "Microsoft.Spatial.GeographyCollection";
                    case "Edm.Geometry": return "Microsoft.Spatial.Geometry";
                    case "Edm.GeometryPoint": return "Microsoft.Spatial.GeometryPoint";
                    case "Edm.GeometryLineString": return "Microsoft.Spatial.GeometryLineString";
                    case "Edm.GeometryPolygon": return "Microsoft.Spatial.GeometryPolygon";
                    case "Edm.GeometryMultiPoint": return "Microsoft.Spatial.GeometryMultiPoint";
                    case "Edm.GeometryMultiLineString": return "Microsoft.Spatial.GeometryMultiLineString";
                    case "Edm.GeometryMultiPolygon": return "Microsoft.Spatial.GeometryMultiPolygon";
                    case "Edm.GeometryCollection": return "Microsoft.Spatial.GeometryCollection";
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
