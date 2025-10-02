using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace DotNetApp.Api.UnitTests;

public class CategoryConventionsTests
{
    private static readonly string[] AllowedMissingCategoryAttributes = new[] { "Skip" }; // If a test is skipped intentionally, still enforce category unless design changes.

    [Fact]
    [Trait("Category","Unit")]
    public void All_Facts_And_Theories_Have_Category_Trait()
    {
        var assembly = typeof(CategoryConventionsTests).Assembly;
        var testMethods = assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttributes().Any(a => a.GetType().Name is "FactAttribute" or "TheoryAttribute"))
            // Exclude this convention test itself to avoid self-reporting during partial edits
            .Where(m => m.DeclaringType != typeof(CategoryConventionsTests));

        var offenders = new List<string>();
        foreach (var m in testMethods)
        {
            bool MethodHasCategory() => m.CustomAttributes.Any(c =>
                c.AttributeType.Name == "TraitAttribute" &&
                c.ConstructorArguments.Count > 1 &&
                c.ConstructorArguments[0].ArgumentType == typeof(string) &&
                (string?)c.ConstructorArguments[0].Value == "Category");

            bool TypeHasCategory() => m.DeclaringType?.CustomAttributes.Any(c =>
                c.AttributeType.Name == "TraitAttribute" &&
                c.ConstructorArguments.Count > 1 &&
                c.ConstructorArguments[0].ArgumentType == typeof(string) &&
                (string?)c.ConstructorArguments[0].Value == "Category") == true;

            var hasCategory = MethodHasCategory() || TypeHasCategory();
            if (!hasCategory)
                offenders.Add($"{m.DeclaringType?.FullName}.{m.Name}");
        }

        if (offenders.Count > 0)
        {
            var msg = "Missing [Trait(\"Category\", ...)] on tests:\n" + string.Join('\n', offenders);
            throw new XunitException(msg);
        }
    }
}
