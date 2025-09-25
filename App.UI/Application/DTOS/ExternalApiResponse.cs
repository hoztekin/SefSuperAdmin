namespace App.UI.Application.DTOS
{
    public class ExternalApiResponse<T>
    {
        public PaginationInfo Pagination { get; set; }
        public DataContainer<T> Data { get; set; }
        public string ResponseType { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class PaginationInfo
    {
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalResults { get; set; }
        public string Sort { get; set; }
        public string SortDir { get; set; }
    }

    public class DataContainer<T>
    {
        public List<T> List { get; set; }
    }
}
