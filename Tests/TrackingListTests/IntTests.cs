using FluentAssertions;

namespace AltaSoft.Storm.TrackingListTests;

public class IntTests
{
    [Fact]
    public void Add_ShouldTrackInsertedItem()
    {
        // Arrange
        var trackingList = new TrackingList<int>();
        var item = Random.Shared.Next(1000);

        // Act
        trackingList.StartChangeTracking();
        trackingList.Add(item);

        // Assert
        trackingList.Should().Contain(item);
        trackingList.GetInsertedItems().Should().Contain(item);
        trackingList.GetDeletedItems().Should().BeEmpty();
    }

    [Fact]
    public void Remove_ShouldTrackDeletedItem()
    {
        // Arrange
        var trackingList = new TrackingList<int>();
        var item = Random.Shared.Next(1000);
        trackingList.Add(item);

        // Act
        trackingList.StartChangeTracking();
        trackingList.Remove(item);

        // Assert
        trackingList.Should().NotContain(item);
        trackingList.GetDeletedItems().Should().Contain(item);
        trackingList.GetInsertedItems().Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldTrackAllItemsAsDeleted()
    {
        // Arrange
        var trackingList = new TrackingList<int>();
        var item1 = Random.Shared.Next(1000);
        var item2 = item1 + Random.Shared.Next(1000) + 1;
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
    }

    [Fact]
    public void ReplaceAtIndex_ShouldTrackDeletedAndInsertedItem()
    {
        // Arrange
        var trackingList = new TrackingList<int>();
        var originalItem = Random.Shared.Next(1000);
        var newItem = originalItem + Random.Shared.Next(1000) + 1;
        trackingList.Add(originalItem);

        // Act
        trackingList.StartChangeTracking();
        trackingList[0] = newItem;

        // Assert
        trackingList.Should().Contain(newItem);
        trackingList.Should().NotContain(originalItem);

        trackingList.GetInsertedItems().Should().Contain(newItem);
        trackingList.GetDeletedItems().Should().Contain(originalItem);
    }

    [Fact]
    public void AddUpdateDeleteAdd_ShouldTrackCorrectly()
    {
        // Arrange
        var trackingList = new TrackingList<int>();
        var item1 = Random.Shared.Next(1000);
        var item2 = item1 + Random.Shared.Next(1000) + 1;

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
    }
}
