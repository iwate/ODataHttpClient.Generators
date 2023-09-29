using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ODataHttpClient.Generators.Tests;
using DTO = System.DateTimeOffset;

public class PickTest
{
    [Fact]
    public void Basic()
    {
        var props = typeof(ProductIdAndName).GetProperties();

        Assert.Equal(2, props.Length);
        Assert.Equal(nameof(Product.Id), props.ElementAt(0).Name);
        Assert.Equal(nameof(Product.Name), props.ElementAt(1).Name);
        Assert.Equal(typeof(int), props.ElementAt(0).PropertyType);
        Assert.Equal(typeof(string), props.ElementAt(1).PropertyType);
    }

    [Fact]
    public void NamespaceUsing()
    {
        var props = typeof(ProductIdAndSalesStart).GetProperties();

        Assert.Equal(2, props.Length);
        Assert.Equal(nameof(Product.Id), props.ElementAt(0).Name);
        Assert.Equal(nameof(Product.SalesStart), props.ElementAt(1).Name);
        Assert.Equal(typeof(int), props.ElementAt(0).PropertyType);
        Assert.Equal(typeof(DTO), props.ElementAt(1).PropertyType);
    }

    [Fact]
    public void GenericAttribute()
    {
        var props = typeof(ProductIdAndPrice).GetProperties();

        Assert.Equal(2, props.Length);
        Assert.Equal(nameof(Product.Id), props.ElementAt(0).Name);
        Assert.Equal(nameof(Product.UnitPrice), props.ElementAt(1).Name);
        Assert.Equal(typeof(int), props.ElementAt(0).PropertyType);
        Assert.Equal(typeof(decimal), props.ElementAt(1).PropertyType);
    }


    [Fact]
    public void ReferenceProjectType()
    {
        var props = typeof(Class1Prop23).GetProperties();

        Assert.Equal(2, props.Length);
        Assert.Equal("Prop2", props.ElementAt(0).Name);
        Assert.Equal("Prop3", props.ElementAt(1).Name);
    }

    [Fact]
    public void ReferencePackageType()
    {
        var props = typeof(Geo).GetProperties();

        Assert.Single(props);
        Assert.Equal("IsEmpty", props.ElementAt(0).Name);
    }

    [Fact]
    public void GeneratedEntityType()
    {
        var props = typeof(ProductSummary).GetProperties();

        Assert.Equal(2, props.Length);
        Assert.Equal("ID", props.ElementAt(0).Name);
        Assert.Equal("Name", props.ElementAt(1).Name);
    }

    [Fact]
    public void Assign()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Test",
            UnitPrice = 100m,
            SalesStart = DTO.Parse("2023-09-24T00:00:00Z")
        };

        var obj = new ProductIdAndName();
        obj.Assign(product);

        Assert.Equal(product.Id, obj.Id);
        Assert.Equal(product.Name, obj.Name);
    }

    [Fact]
    public void Create()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Test",
            UnitPrice = 100m,
            SalesStart = DTO.Parse("2023-09-24T00:00:00Z")
        };

        var obj = ProductIdAndName.Create(product);

        Assert.Equal(product.Id, obj.Id);
        Assert.Equal(product.Name, obj.Name);
    }

    [Fact]
    public void NestedAssign()
    {
        var product = new Product
        {
            Id = 1,
            Name = "Test",
            UnitPrice = 100m,
            SalesStart = DTO.Parse("2023-09-24T00:00:00Z"),
            Categories = new List<Category>
            {
                new Category { Id = 1, Name = "Category 1" },
                new Category { Id = 2, Name = "Category 2" },
                new Category { Id = 3, Name = "Category 3" },
            }
        };

        var obj = new ProductIdAndNameWithCategoryId();
        obj.Assign(product);

        Assert.Equal(product.Id, obj.Id);
        Assert.Equal(product.Name, obj.Name);
        Assert.Equal(product.Categories.ElementAt(0).Id, obj.Categories.ElementAt(0).Id);
        Assert.Equal(product.Categories.ElementAt(1).Id, obj.Categories.ElementAt(1).Id);
        Assert.Equal(product.Categories.ElementAt(2).Id, obj.Categories.ElementAt(2).Id);
    }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal UnitPrice { get; set; }
    public DTO SalesStart { get; set; }
    public virtual ICollection<Category> Categories { get; set; }
}

[Pick(typeof(Product), nameof(Product.Id), nameof(Product.Name))]
public partial class ProductIdAndName
{
}


[Pick(typeof(Product), nameof(Product.Id), nameof(Product.SalesStart))]
public partial class ProductIdAndSalesStart
{
}

[Pick<Product>(nameof(Product.Id), nameof(Product.UnitPrice))]
public partial class ProductIdAndPrice
{
}

[Pick<Product>(nameof(Product.Id), nameof(Product.Name))]
public partial class ProductIdAndNameWithCategoryId
{
    public virtual ICollection<CategoryId> Categories { get; set; }
}

[Pick<Category>(nameof(Category.Id))]
public partial class CategoryId
{
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public virtual ICollection<Product> Products { get; set; }
}

[Pick<ODataDemo.Product>(nameof(ODataDemo.Product.ID), "Name")]
public partial class ProductSummary { }

[Pick<ReferenceEntitites.Class1>(nameof(ReferenceEntitites.Class1.Prop2), "Prop3")]
public partial class Class1Prop23 { }

[Pick<Microsoft.Spatial.Geography>(nameof(Microsoft.Spatial.Geography.IsEmpty))]
public partial class Geo { }

[Pick(typeof(ODataDemo.Product), nameof(ODataDemo.Product.ID), "Name")]
public partial class ProductSummary_2 { }

[Pick(typeof(ReferenceEntitites.Class1), nameof(ReferenceEntitites.Class1.Prop2), "Prop3")]
public partial class Class1Prop23_2 { }

[Pick(typeof(Microsoft.Spatial.Geography), nameof(Microsoft.Spatial.Geography.IsEmpty))]
public partial class Geo_2 { }