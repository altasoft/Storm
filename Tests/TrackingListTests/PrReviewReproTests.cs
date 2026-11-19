using FluentAssertions;

namespace AltaSoft.Storm.TrackingListTests;

/// <summary>
/// Reproductions of issues found while reviewing the bugfix/somebugs branch.
/// </summary>
public class PrReviewReproTests
{
    /// <summary>
    /// Finding #1: inserted items must be tracked by reference, not by value equality.
    /// An existing item that is value-equal to a newly added one must still be reported as updated.
    /// </summary>
    [Fact]
    public void Finding1_GetUpdatedItems_ExistingItemValueEqualToInsertedItem_ShouldBeReported()
    {
        var existing = new FakeTrackingItem(1);
        var list = new TrackingList<FakeTrackingItem>([existing], autoTrack: true);

        var added = new FakeTrackingItem(1); // value-equal (same Id), different instance
        list.Add(added);

        existing.MarkDirty();

        list.GetInsertedItems().Should().ContainSingle().Which.Should().BeSameAs(added);
        list.GetUpdatedItems().Should().ContainSingle().Which.Should().BeSameAs(existing);
    }
}
