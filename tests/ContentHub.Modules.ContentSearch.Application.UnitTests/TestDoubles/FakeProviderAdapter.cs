using System.Runtime.CompilerServices;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.UnitTests.TestDoubles;

internal sealed class FakeProviderAdapter : IProviderAdapter
{
    private readonly IReadOnlyList<FetchedContent> _items;
    private readonly Exception? _throw;

    public FakeProviderAdapter(ProviderFormat format, IReadOnlyList<FetchedContent> items, Exception? throwOnFetch = null)
    {
        Format = format;
        _items = items;
        _throw = throwOnFetch;
    }

    public ProviderFormat Format { get; }

    public async IAsyncEnumerable<FetchedContent> FetchAsync(
        Provider provider,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        if (_throw is not null && provider is not null)
        {
            throw _throw;
        }

        foreach (var item in _items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}
