using Backdash.Data;

namespace Backdash.Tests.Specs.Unit.Data;

public class ObjectPoolTests
{
    [Fact]
    public void ShouldRentAndReturnSameObject()
    {
        var sut = ObjectPool.Create<TestObj>();

        var value = sut.Rent();
        sut.Count.Should().Be(0);

        sut.Return(value);
        sut.Count.Should().Be(1);

        var value2 = sut.Rent();
        sut.Count.Should().Be(0);

        value2.Should().BeSameAs(value);
    }

    [Fact]
    public void ShouldRentAndReturnSettingNullRefObject()
    {
        var sut = ObjectPool.Create<TestObj>();
        TestObj? value = sut.Rent();
        value.Should().NotBeNull();
        sut.Count.Should().Be(0);

        sut.Return(ref value);
        sut.Count.Should().Be(1);
        value.Should().BeNull();
    }

    [Fact]
    public void ShouldNotReturnSameObjectTwice()
    {
        var sut = ObjectPool.Create<TestObj>();
        var value = sut.Rent();
        sut.Count.Should().Be(0);

        sut.Return(value);
        sut.Return(value);
        sut.Count.Should().Be(1);
    }

    [Fact]
    public void ShouldCallCreateAndReturnFunction()
    {
        ObjectPool<TestObj> sut = new(
            () => new()
            {
                Value = 1,
                Returned = false,
            },
            obj =>
            {
                obj.Value = 0;
                obj.Returned = true;
            }
        );

        var value = sut.Rent();

        value.Should().BeEquivalentTo(new
        {
            Value = 1,
            Returned = false,
        });

        value.Value = 2;
        sut.Return(value);

        value.Should().BeEquivalentTo(new
        {
            Value = 0,
            Returned = true,
        });
    }

    [Fact]
    public void ShouldAddMoreThanCapacity()
    {
        const int capacity = 5;
        var sut = ObjectPool.Create<TestObj>(capacity);
        FillPool(sut, capacity * 2);
        sut.Count.Should().Be(capacity);
    }

    [Fact]
    public void ShouldClear()
    {
        var sut = ObjectPool.Create<TestObj>();
        FillPool(sut, 5);

        sut.Count.Should().Be(5);
        sut.Clear();
        sut.Count.Should().Be(0);
    }

    [Fact]
    public void ShouldDisposeItems()
    {
        var sut = ObjectPool.Create<TestObjDisposable>();
        TestObjDisposable[] rents =
        [
            sut.Rent(),
            sut.Rent(),
            sut.Rent(),
        ];

        foreach (var r in rents)
            sut.Return(r);

        sut.Count.Should().Be(rents.Length);
        sut.Dispose();
        rents.Should().AllSatisfy(r => r.Disposed.Should().BeTrue());
    }

    [Fact]
    public void ShouldReturnAndClearList()
    {
        const int cap = 10;
        var sut = ObjectPool.Create<TestObj>();
        List<TestObj> buffer = new(cap);
        sut.Rent(buffer, cap);
        buffer.Count.Should().Be(cap);
        sut.Count.Should().Be(0);

        sut.ReturnAll(buffer).Should().BeTrue();
        buffer.Count.Should().Be(0);
        sut.Count.Should().Be(cap);
    }

    [Fact]
    public void ShouldReturnRepeatedListItems()
    {
        const int cap = 10;
        var sut = ObjectPool.Create<TestObj>();
        List<TestObj> buffer = new(cap);
        sut.Rent(buffer, cap);
        sut.Count.Should().Be(0);

        var copy = buffer.ToList();
        Random.Shared.Shuffle(copy);
        buffer.AddRange(copy);
        buffer.Distinct().Count().Should().Be(cap);

        sut.ReturnAll(buffer).Should().BeTrue();
        buffer.Count.Should().Be(0);
        sut.Count.Should().Be(cap);
    }

    [Fact]
    public void ShouldReturnAndKeepNonReturnedList()
    {
        const int cap = 5;
        var sut = ObjectPool.Create<TestObj>(cap);
        List<TestObj> buffer = new(cap + 1);
        sut.Rent(buffer, buffer.Capacity);
        sut.Count.Should().Be(0);

        sut.ReturnAll(buffer).Should().BeFalse();
        buffer.Count.Should().Be(1);
        sut.Count.Should().Be(cap);
    }

    static void FillPool(ObjectPool<TestObj> sut, int count) =>
        Enumerable.Repeat<object?>(null, count)
            .Select(_ => sut.Rent())
            .ToList()
            .ForEach(x => sut.Return(x));

    sealed class TestObj
    {
        static int index;
        public int Value { get; set; } = Interlocked.Increment(ref index);
        public bool Returned { get; set; }

        public override string ToString() => $"{(Returned ? "###" : "")}Obj[{Value}]";
    }

    sealed class TestObjDisposable : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }
}
