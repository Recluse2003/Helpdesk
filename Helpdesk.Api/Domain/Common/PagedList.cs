namespace Helpdesk.Api.Domain.Common
{
    /// <summary>
    /// Represents a paginated collection of items together with information about the current page.
    /// </summary>
    /// <typeparam name="T">The type of items contained in the collection.</typeparam>
    public record PagedList<T>
    {
        /// <summary>
        /// Gets the items belonging to the current page.
        /// </summary>
        public IReadOnlyCollection<T> Items { get; }

        /// <summary>
        /// Gets the current page number, starting at 1.
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Gets the maximum number of items requested per page.
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Gets the total number of items across all pages.
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Gets the total number of pages required to contain all items.
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>
        /// Gets a value indicating whether another page exists after the current page.
        /// </summary>
        public bool HasNextPage => PageNumber * PageSize < TotalCount;

        /// <summary>
        /// Gets a value indicating whether a page exists before the current page.
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Initialises a new instance of the <see cref="PagedList{T}"/> class.
        /// </summary>
        /// <param name="items">The items belonging to the current page.</param>
        /// <param name="pageNumber">The current page number, starting at 1.</param>
        /// <param name="pageSize">The maximum number of items per page.</param>
        /// <param name="totalCount">The total number of items across all pages.</param>
        public PagedList(IReadOnlyCollection<T> items, int pageNumber, int pageSize, int totalCount)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
    }
}
