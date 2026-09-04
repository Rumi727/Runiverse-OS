#nullable enable

using System.Collections;
using Error = RuniOS.CodeAnalysis.Generators.TypeSyntaxSerializer.SerializeError;
using ErrorResult = RuniOS.CodeAnalysis.Generators.TypeSyntaxSerializer.SerializeErrorResult;
using Results = RuniOS.CodeAnalysis.Generators.TypeSyntaxSerializer.SerializeErrorResults;

namespace RuniOS.CodeAnalysis.Tests;

[Trait("Category", "Contract")]
public sealed class SerializeErrorResultsTests
{
    [Fact]
    public void Default_value_is_empty_in_every_enumeration_form()
    {
        Results result = default;
        Assert.True(result.isSuccess);
        Assert.Equal(0, result.count);
        Assert.Equal(0, ((IReadOnlyCollection<ErrorResult>)result).Count);
        Assert.Empty(result);
        Assert.Empty(((IEnumerable)result).Cast<ErrorResult>());
        var enumerator = result.GetEnumerator();
        Assert.False(enumerator.MoveNext());
        Assert.Empty(default(Results) | default(Results));
    }

    [Theory]
    [InlineData(Error.invalidIdentifier)]
    [InlineData(Error.unsupportedArrayType)]
    [InlineData(Error.unsupportedFunctionPointer)]
    [InlineData(Error.unrepresentableType)]
    public void Single_error_retains_null_problematic_object_and_indexed_value(Error kind)
    {
        Results result = new(kind, null);
        Assert.False(result.isSuccess);
        Assert.Equal(1, result.count);
        Assert.Equal(new ErrorResult(kind, null), result[0]);
        Assert.Equal(result[0], Assert.Single(result));
        Assert.Equal(result[0], Assert.Single(((IEnumerable)result).Cast<ErrorResult>()));
    }

    [Fact]
    public void Combination_is_associative_ordered_and_does_not_mutate_operands()
    {
        object firstObject = new();
        Results first = new(Error.invalidIdentifier, firstObject);
        Results second = new(Error.unsupportedArrayType, "second");
        Results third = new(Error.unsupportedFunctionPointer, "third");
        Results left = (first | second) | third;
        Results right = first | (second | third);
        Assert.Equal(3, left.count);
        Assert.Equal(left.ToArray(), right.ToArray());
        Assert.Equal(new[] { first[0], second[0], third[0] }, left.ToArray());
        Assert.Equal(first.ToArray(), (first | default(Results)).ToArray());
        Assert.Equal(first.ToArray(), (default(Results) | first).ToArray());
        Assert.Same(firstObject, left[0].problematicObject);
        Assert.Equal(1, first.count);
        Assert.Equal(1, second.count);
        Assert.Equal(1, third.count);
    }

    [Fact]
    public void Repeated_errors_are_not_deduplicated()
    {
        Results result = new(Error.invalidIdentifier, "same");
        Results combined = result | result;
        Assert.Equal(2, combined.count);
        Assert.Equal(combined[0], combined[1]);
    }
}
