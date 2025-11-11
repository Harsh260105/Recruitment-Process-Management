namespace RecruitmentSystem.Core.DTOs
{
    /// <summary>
    /// Represents a paginated result set for Core layer
    /// </summary>
    /// <typeparam name="T">The type of items in the result set</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// The items for the current page
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// Whether there is a next page available
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Whether there is a previous page available
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Factory method to create a PagedResult
        /// </summary>
        /// <param name="items">Items for current page</param>
        /// <param name="totalCount">Total count of all items</param>
        /// <param name="pageNumber">Current page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>New PagedResult instance</returns>
        public static PagedResult<T> Create(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            return new PagedResult<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}