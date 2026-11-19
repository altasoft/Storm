using AltaSoft.Storm.Interfaces;
using FluentAssertions;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

namespace AltaSoft.Storm.TrackingListTests;

/// <summary>
/// Minimal hand-rolled tracking object used to observe the calls made by <see cref="TrackingList{T}"/>.
/// </summary>
public sealed class FakeTrackingItem : ITrackingObject, IEquatable<FakeTrackingItem>
{
    private bool _tracking;
    private bool _dirty;

    public FakeTrackingItem(int id) => Id = id;

    public int Id { get; }

    public int StartChangeTrackingCalls { get; private set; }

    public List<bool> AcceptChangesCalls { get; } = [];

    public void MarkDirty()
    {
        if (_tracking)
            _dirty = true;
    }

    public bool IsChangeTrackingActive() => _tracking;

    public void StartChangeTracking()
    {
        StartChangeTrackingCalls++;
        _dirty = false;
        _tracking = true;
    }

    public void AcceptChanges(bool stopTracking = true)
    {
        AcceptChangesCalls.Add(stopTracking);
        _dirty = false;
        _tracking = !stopTracking;
    }

    public bool IsDirty() => _dirty;

    public IReadOnlySet<string> __GetChangedPropertyNames() => new HashSet<string>();

    public (string propertyName, IChangeTrackable? value)[] __TrackableMembers() => [];

    public bool Equals(FakeTrackingItem? other) => other is not null && other.Id == Id;

    public override bool Equals(object? obj) => Equals(obj as FakeTrackingItem);

    public override int GetHashCode() => Id;
}

public class TrackingListFixTests
{
    [Fact]
    public void AcceptChanges_WithStopTrackingFalse_KeepsChildrenTracking()
    {
        var item = new FakeTrackingItem(1);
        var list = new TrackingList<FakeTrackingItem>([item], autoTrack: true);
        item.IsChangeTrackingActive().Should().BeTrue();

        list.AcceptChanges(stopTracking: false);

        list.IsChangeTrackingActive().Should().BeTrue();
        item.AcceptChangesCalls.Last().Should().BeFalse();
        item.IsChangeTrackingActive().Should().BeTrue();

        // a subsequent change on the child is still detected
        item.MarkDirty();
        list.IsDirty().Should().BeTrue();
        list.GetUpdatedItems().Should().ContainSingle().Which.Should().BeSameAs(item);
    }

    [Fact]
    public void AcceptChanges_Default_StopsChildrenTracking()
    {
        var item = new FakeTrackingItem(1);
        var list = new TrackingList<FakeTrackingItem>([item], autoTrack: true);

        list.AcceptChanges();

        list.IsChangeTrackingActive().Should().BeFalse();
        item.AcceptChangesCalls.Last().Should().BeTrue();
        item.IsChangeTrackingActive().Should().BeFalse();
    }

    [Fact]
    public void Add_WhileListIsTracking_StartsTrackingOnTheNewItem()
    {
        var list = new TrackingList<FakeTrackingItem>(autoTrack: true);
        var item = new FakeTrackingItem(1);

        list.Add(item);

        item.IsChangeTrackingActive().Should().BeTrue();
        item.StartChangeTrackingCalls.Should().Be(1);
        list.GetInsertedItems().Should().ContainSingle();
    }

    [Fact]
    public void InsertAndIndexerSet_WhileListIsTracking_StartTrackingOnNewItems()
    {
        var list = new TrackingList<FakeTrackingItem>([new FakeTrackingItem(1)], autoTrack: true);
        var inserted = new FakeTrackingItem(2);
        var replaced = new FakeTrackingItem(3);

        list.Insert(0, inserted);
        list[1] = replaced;

        inserted.IsChangeTrackingActive().Should().BeTrue();
        replaced.IsChangeTrackingActive().Should().BeTrue();
    }

    [Fact]
    public void AddedThenModifiedItem_IsReportedOnlyAsInserted()
    {
        var existing = new FakeTrackingItem(1);
        var list = new TrackingList<FakeTrackingItem>([existing], autoTrack: true);
        var added = new FakeTrackingItem(2);

        list.Add(added);
        added.MarkDirty();
        existing.MarkDirty();

        added.IsDirty().Should().BeTrue();
        list.GetInsertedItems().Should().ContainSingle().Which.Should().BeSameAs(added);
        list.GetUpdatedItems().Should().ContainSingle().Which.Should().BeSameAs(existing);
    }

    [Fact]
    public void Add_ItemAlreadyTracking_IsNotRestarted()
    {
        var list = new TrackingList<FakeTrackingItem>(autoTrack: true);
        var item = new FakeTrackingItem(1);
        item.StartChangeTracking();
        item.MarkDirty();

        list.Add(item);

        item.StartChangeTrackingCalls.Should().Be(1);
        item.IsDirty().Should().BeTrue();
    }

    [Fact]
    public void IndexerSet_WhenOldElementIsNull_DoesNotThrow_AndTracksChange()
    {
        var list = new TrackingList<FakeTrackingItem?>([null], autoTrack: true);
        var item = new FakeTrackingItem(1);

        var act = () => list[0] = item;

        act.Should().NotThrow();
        list[0].Should().BeSameAs(item);
        list.GetInsertedItems().Should().Contain(item);
        list.GetDeletedItems().Should().Contain((FakeTrackingItem?)null);
    }

    [Fact]
    public void IndexerSet_NullOverNull_IsNoOp_AndNullOverValue_Works()
    {
        var item = new FakeTrackingItem(1);
        var list = new TrackingList<FakeTrackingItem?>([null, item], autoTrack: true);

        list[0] = null;
        list.IsDirty().Should().BeFalse();

        var act = () => list[1] = null;
        act.Should().NotThrow();
        list[1].Should().BeNull();
        list.GetDeletedItems().Should().Contain(item);
    }

    [Fact]
    public void IndexerSet_WhenOldStringIsNull_DoesNotThrow()
    {
        var list = new TrackingList<string?>([null], autoTrack: true);

        var act = () => list[0] = "x";

        act.Should().NotThrow();
        list.GetInsertedItems().Should().Contain("x");
    }
}
