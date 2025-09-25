using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.Interfaces;
using FluentAssertions;
using Microsoft.Data.SqlClient;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

namespace AltaSoft.Storm.TrackingListTests;

public record TestEntity(int X, string Y) : IEntityComparer<TestEntity>, IDataBindable
{
    /// <inheritdoc />
    public bool KeyEquals(TestEntity other) => X == other.X;

    public uint? __GetLoadingFlags() => null;

    /// <inheritdoc />
    public (StormColumnDef column, object? value)[] __GetColumnValues()
    {
        var colX = new StormColumnDef(nameof(X), null, "X",
            StormColumnFlags.CanSelect | StormColumnFlags.CanInsert | StormColumnFlags.CanUpdate,
            UnifiedDbType.DateTime, 0, 0, 0, SaveAs.Default, 0, false, null, null, X.GetType(), null);

        var colY = new StormColumnDef(nameof(Y), null, "Y",
            StormColumnFlags.CanSelect | StormColumnFlags.CanInsert | StormColumnFlags.CanUpdate,
            UnifiedDbType.DateTime, 0, 0, 0, SaveAs.Default, 0, true, null, null, Y.GetType(), null);

        return [
            (colX, X),
            (colY, Y)
        ];
    }

    public void __AddDetailRow(StormColumnDef column, object row)
    {
    }

    public void __SetAutoIncValue(SqlDataReader dr, int idx)
    {
    }

    public void __SetAutoIncValue(SqlDataReader dr)
    {
    }
}

public class EntityTests
{
    [Fact]
    public void Add_ShouldTrackInsertedItem()
    {
        // Arrange
        var trackingList = new EntityTrackingList<TestEntity>();
        var item = new TestEntity(1, "Test string");

        // Act
        trackingList.StartChangeTracking();
        trackingList.Add(item);

        // Assert
        trackingList.Should().Contain(item);
        trackingList.GetInsertedItems().Should().Contain(item);
        trackingList.GetDeletedItems().Should().BeEmpty();
        trackingList.GetUpdatedItems().Should().BeEmpty();
    }

    [Fact]
    public void Remove_ShouldTrackDeletedItem()
    {
        // Arrange
        var trackingList = new EntityTrackingList<TestEntity>();
        var item = new TestEntity(1, "Test string");
        trackingList.Add(item);

        // Act
        trackingList.StartChangeTracking();
        trackingList.Remove(item);

        // Assert
        trackingList.Should().NotContain(item);
        trackingList.GetDeletedItems().Should().Contain(item);
        trackingList.GetInsertedItems().Should().BeEmpty();
        trackingList.GetUpdatedItems().Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldTrackAllItemsAsDeleted()
    {
        // Arrange
        var trackingList = new EntityTrackingList<TestEntity>();
        var item1 = new TestEntity(1, "Test string");
        var item2 = new TestEntity(2, "Test string 2");
        trackingList.Add(item1);
        trackingList.Add(item2);

        // Act
        trackingList.StartChangeTracking();
        trackingList.Clear();

        // Assert
        trackingList.Should().NotContain(item1);
        trackingList.Should().NotContain(item2);
        trackingList.GetDeletedItems().Should().Contain(item1);
        trackingList.GetDeletedItems().Should().Contain(item2);
        trackingList.GetInsertedItems().Should().BeEmpty();
        trackingList.GetUpdatedItems().Should().BeEmpty();
    }

    [Fact]
    public void ReplaceAtIndexWithDifferentKey_ShouldTrackDeletedAndInsertedItem()
    {
        // Arrange
        var trackingList = new EntityTrackingList<TestEntity>();
        var originalItem = new TestEntity(1, "Original");
        var newItem = new TestEntity(2, "New item");
        trackingList.Add(originalItem);

        // Act
        trackingList.StartChangeTracking();
        trackingList[0] = newItem;

        // Assert
        trackingList.Should().Contain(newItem);
        trackingList.Should().NotContain(originalItem);
        trackingList.GetInsertedItems().Should().Contain(newItem);
        trackingList.GetDeletedItems().Should().Contain(originalItem);
        trackingList.GetUpdatedItems().Should().BeEmpty();
    }

    [Fact]
    public void AddUpdateDeleteAdd_ShouldTrackCorrectly()
    {
        // Arrange
        var trackingList = new EntityTrackingList<TestEntity>();
        var item1 = new TestEntity(1, "Test string");
        var item2 = new TestEntity(2, "Test string 2");

        // Act
        // Add item1
        trackingList.StartChangeTracking();
        trackingList.Add(item1);

        // Replace item1 with item2 at index 0
        trackingList[0] = item2;

        // Remove item2
        trackingList.Remove(item2);

        // Add item2 again
        trackingList.Add(item2);

        // Assert
        trackingList.Should().Contain(item2);
        trackingList.Should().NotContain(item1);
        trackingList.GetDeletedItems().Should().BeEmpty(); // item2 was removed
        trackingList.GetInsertedItems().Should().Contain(item2); // item2 was added again
        trackingList.GetUpdatedItems().Should().BeEmpty();
    }

    #region MyRegion

    [Fact]
    public void ReplaceAtIndexWithSameKey_ShouldTrackUpdatedItem()
    {
        // Arrange
        var trackingList = new EntityTrackingList<TestEntity>();
        var originalItem = new TestEntity(1, "Original");
        var newItem = new TestEntity(1, "New item");
        trackingList.Add(originalItem);

        // Act
        trackingList.StartChangeTracking();
        trackingList[0] = newItem;

        // Assert
        trackingList.Should().Contain(newItem);
        trackingList.Should().NotContain(originalItem);
        trackingList.GetInsertedItems().Should().BeEmpty();
        trackingList.GetDeletedItems().Should().BeEmpty();
        trackingList.GetUpdatedItems().Should().Contain(newItem);
    }

    [Fact]
    public void AddUpdate_ShouldTrackCorrectly()
    {
        // Arrange
        var trackingList = new EntityTrackingList<TestEntity>();
        var item1 = new TestEntity(1, "Test string");
        var item2 = new TestEntity(1, "Test string 2");

        // Act
        // Add item1
        trackingList.StartChangeTracking();
        trackingList.Add(item1);

        // Replace item1 with item2 at index 0
        trackingList[0] = item2;

        // Assert
        trackingList.Should().Contain(item2);
        trackingList.Should().NotContain(item1);
        trackingList.GetDeletedItems().Should().BeEmpty();
        trackingList.GetInsertedItems().Should().Contain(item2);
        trackingList.GetUpdatedItems().Should().BeEmpty();
    }

    [Fact]
    public void UpdateDelete_ShouldTrackCorrectly()
    {
        // Arrange
        var trackingList = new EntityTrackingList<TestEntity>();
        var item1 = new TestEntity(1, "Test string");
        var item2 = new TestEntity(1, "Test string 2");
        // Add item1
        trackingList.Add(item1);

        // Act
        trackingList.StartChangeTracking();

        // Replace item1 with item2 at index 0
        trackingList[0] = item2;

        // Remove item2
        trackingList.Remove(item2);

        // Assert
        trackingList.Should().BeEmpty();
        trackingList.GetDeletedItems().Should().BeEmpty();
        trackingList.GetInsertedItems().Should().BeEmpty();
        trackingList.GetUpdatedItems().Should().BeEmpty();
    }

    #endregion MyRegion
}
