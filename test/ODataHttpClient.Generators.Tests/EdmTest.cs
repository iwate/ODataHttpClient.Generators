using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace ODataHttpClient.Generators.Tests
{
    public class EdmTest
    {
        [Fact]
        public void ParseXml()
        {
            var xml = File.ReadAllText("data/ODataDemo.metadata.xml");
            var ns = EdmParser.ParseEdmXml(xml);

            Assert.NotNull(ns);
            Assert.Single(ns);
            Assert.Equal("ODataDemo", ns.First().Name);
            Assert.Equal(11, ns.First().Classes.Count);

            var addr = ns.First().Classes.ElementAt(0);

            Assert.Equal("Address", addr.Name);
            Assert.Null(addr.BaseType);
            Assert.Equal(5, addr.Properties.Count);

            Assert.Equal("Street", addr.Properties.ElementAt(0).Name);
            Assert.Equal("string?", addr.Properties.ElementAt(0).ClrType);
            Assert.True(addr.Properties.ElementAt(0).Nullable);

            Assert.Equal("City", addr.Properties.ElementAt(1).Name);
            Assert.Equal("string?", addr.Properties.ElementAt(1).ClrType);
            Assert.True(addr.Properties.ElementAt(1).Nullable);

            Assert.Equal("State", addr.Properties.ElementAt(2).Name);
            Assert.Equal("string?", addr.Properties.ElementAt(2).ClrType);
            Assert.True(addr.Properties.ElementAt(2).Nullable);

            Assert.Equal("ZipCode", addr.Properties.ElementAt(3).Name);
            Assert.Equal("string?", addr.Properties.ElementAt(3).ClrType);
            Assert.True(addr.Properties.ElementAt(3).Nullable);

            Assert.Equal("Country", addr.Properties.ElementAt(4).Name);
            Assert.Equal("string?", addr.Properties.ElementAt(4).ClrType);
            Assert.True(addr.Properties.ElementAt(4).Nullable);

            var supplier = ns.First().Classes.ElementAt(5);

            Assert.Equal("Supplier", supplier.Name);
            Assert.Null(supplier.BaseType);
            Assert.Equal(6, supplier.Properties.Count);

            Assert.Equal("ID", supplier.Properties.ElementAt(0).Name);
            Assert.Equal("int", supplier.Properties.ElementAt(0).ClrType);
            Assert.False(supplier.Properties.ElementAt(0).Nullable);

            Assert.Equal("Name", supplier.Properties.ElementAt(1).Name);
            Assert.Equal("string?", supplier.Properties.ElementAt(1).ClrType);
            Assert.True(supplier.Properties.ElementAt(1).Nullable);

            Assert.Equal("Address", supplier.Properties.ElementAt(2).Name);
            Assert.Equal("ODataDemo.Address?", supplier.Properties.ElementAt(2).ClrType);
            Assert.True(supplier.Properties.ElementAt(2).Nullable);

            Assert.Equal("Location", supplier.Properties.ElementAt(3).Name);
            Assert.Equal("Microsoft.Spatial.GeographyPoint?", supplier.Properties.ElementAt(3).ClrType);
            Assert.True(supplier.Properties.ElementAt(3).Nullable);

            Assert.Equal("Concurrency", supplier.Properties.ElementAt(4).Name);
            Assert.Equal("int", supplier.Properties.ElementAt(4).ClrType);
            Assert.False(supplier.Properties.ElementAt(4).Nullable);

            Assert.Equal("Products", supplier.Properties.ElementAt(5).Name);
            Assert.Equal("System.Collections.Generic.ICollection<ODataDemo.Product>?", supplier.Properties.ElementAt(5).ClrType);
            Assert.True(supplier.Properties.ElementAt(5).Nullable);
        }

        [Fact]
        public void Generate()
        {
            Assert.NotNull(find("ODataDemo.Product"));
            Assert.NotNull(find("ODataDemo.FeaturedProduct"));
            Assert.NotNull(find("ODataDemo.ProductDetail"));
            Assert.NotNull(find("ODataDemo.Category"));
            Assert.NotNull(find("ODataDemo.Supplier"));
            Assert.NotNull(find("ODataDemo.Address"));
            Assert.NotNull(find("ODataDemo.Person"));
            Assert.NotNull(find("ODataDemo.Customer"));
            Assert.NotNull(find("ODataDemo.Employee"));
            Assert.NotNull(find("ODataDemo.PersonDetail"));
            Assert.NotNull(find("ODataDemo.Advertisement"));
        }

        [Fact]
        public void Inherit()
        {
            var product = find("ODataDemo.Product");
            var featuredProduct = find("ODataDemo.FeaturedProduct");
            Assert.NotNull(product);
            Assert.NotNull(featuredProduct);
            Assert.Equal(featuredProduct.BaseType, product);
        }

        Type find(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(name);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
