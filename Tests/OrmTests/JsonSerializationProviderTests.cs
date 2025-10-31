using System.Text.Json;
using AltaSoft.Storm.Json;
using FluentAssertions;
using Xunit;

namespace AltaSoft.Storm.Tests;

public class JsonSerializationProviderTests
{
    [Fact]
    public void DefaultOptions_ShouldHavePropertyNameCaseInsensitive_SetToTrue()
    {
        // Arrange & Act
        var options = JsonSerializationProvider.DefaultSerializerOptions;

        // Assert
        options.PropertyNameCaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void FromJson_ShouldDeserialize_WithCaseInsensitivePropertyNames()
    {
        // Arrange
        var provider = new JsonSerializationProvider();
        var json = "{\"NaMe\":\"John\",\"AgE\":30}"; // Mixed case property names

        // Act
        var result = provider.FromJson(json, typeof(TestPerson));

        // Assert
        result.Should().NotBeNull();
        var person = result as TestPerson;
        person.Should().NotBeNull();
        person!.Name.Should().Be("John");
        person.Age.Should().Be(30);
    }

    [Fact]
    public void FromJson_ShouldDeserialize_WithUpperCasePropertyNames()
    {
        // Arrange
        var provider = new JsonSerializationProvider();
        var json = "{\"NAME\":\"Jane\",\"AGE\":25}"; // All uppercase property names

        // Act
        var result = provider.FromJson(json, typeof(TestPerson));

        // Assert
        result.Should().NotBeNull();
        var person = result as TestPerson;
        person.Should().NotBeNull();
        person!.Name.Should().Be("Jane");
        person.Age.Should().Be(25);
    }

    [Fact]
    public void FromJson_ShouldDeserialize_WithLowerCasePropertyNames()
    {
        // Arrange
        var provider = new JsonSerializationProvider();
        var json = "{\"name\":\"Bob\",\"age\":35}"; // All lowercase property names

        // Act
        var result = provider.FromJson(json, typeof(TestPerson));

        // Assert
        result.Should().NotBeNull();
        var person = result as TestPerson;
        person.Should().NotBeNull();
        person!.Name.Should().Be("Bob");
        person.Age.Should().Be(35);
    }

    private class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}

