using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.Controllers.Mappers.v1;
using PracticalWork.Library.Enums;

namespace PracticalWork.Library.Controllers.Api.v1;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/books")]
public class BooksController : Controller
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    /// <summary> Создание новой книги</summary>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType<CreateBookResponse>( 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateBook(CreateBookRequest request)
    {
        var result = await _bookService.CreateBook(request.ToBook());

        return Ok(new CreateBookResponse(result));
    }
    
    /// <summary>
    /// Обновление книги
    /// </summary>
    [HttpPut]
    [Route("/{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateBook(Guid id, UpdateBookRequest request)
    {
        await _bookService.UpdateBook(id, request.ToBook());
        return Ok();
    }

    /// <summary>
    /// Архивирование книги
    /// </summary>
    [HttpPost]
    [Route("/{id:guid}/archive")]
    [Produces("application/json")]
    [ProducesResponseType<ArchiveBookResponse>(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ArchiveBook(Guid id)
    {
        var result = await _bookService.ArchiveBook(id);
        return Ok(result.ToArchiveBookResponse());
    }
    
    /// <summary>
    /// Добавление деталей к книге
    /// </summary>
    [HttpPost]
    [Route("/details")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> AddBookDetails([FromForm] AddBookDetailsRequest request)
    {
        await using var stream = request.CoverImage.OpenReadStream();
        var contentType = request.CoverImage.ContentType;
        await _bookService.AddBookDetails(request.Id, request.Description, stream, contentType);
        return Ok();
    }
    
    /// <summary>
    /// Получение книг постранично
    /// </summary>
    [HttpPost]
    [Route("/page")]
    [Produces("application/json")]
    [ProducesResponseType<BookCursorPaginationResponse>(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetBooksPage(BookCursorPaginationRequest request)
    {
        var result = await _bookService
            .GetBooksPage(request.ToCursorPaginationRequest(), 
                request.Status is null ? null: (BookStatus)request.Status, 
                request.Category is null ? null: (BookCategory)request.Category, 
                request.Author);
        return Ok(result.ToBookCursorPaginationResponse());
    }
}