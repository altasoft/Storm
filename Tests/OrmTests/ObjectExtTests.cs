using System;
using AltaSoft.Storm.Extensions;
using FluentAssertions;
using Xunit;

namespace AltaSoft.Storm.Tests;

public class ObjectExtTests
{
    [Fact]
    public void GetDbValue_ShouldReturnDefault_WhenDbValueIsNull()
    {
        // Arrange
        object? dbValue = null;

        // Act
        var result = dbValue.GetDbValue<int>();

        // Assert
        result.Should().Be(default);
    }

    [Fact]
    public void GetDbValue_ShouldReturnDefault_WhenDbValueIsDBNull()
    {
        // Arrange
        object dbValue = DBNull.Value;

        // Act
        var result = dbValue.GetDbValue<int>();

        // Assert
        result.Should().Be(default);
    }

    [Fact]
    public void GetDbValue_ShouldReturnValue_WhenDbValueIsNotNull()
    {
        // Arrange
        object dbValue = 42;

        // Act
        var result = dbValue.GetDbValue<int>();

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void GetDbValue_ShouldThrowInvalidCastException_WhenDbValueCannotBeCast()
    {
        // Arrange
        object dbValue = "not an int";

        // Act
        Action act = () => dbValue.GetDbValue<int>();

        // Assert
        act.Should().Throw<InvalidCastException>();
    }
}
