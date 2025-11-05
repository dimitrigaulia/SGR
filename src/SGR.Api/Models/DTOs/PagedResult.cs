namespace SGR.Api.Models.DTOs;

public class PagedResult<T>
{
    public required IEnumerable<T> Items { get; set; }
    public required int Total { get; set; }
}

