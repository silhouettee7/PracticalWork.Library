using Asp.Versioning;
using Contracts.Mappers;
using Microsoft.AspNetCore.Mvc;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.Controllers.Filters;
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
    public async Task<IActionResult> CreateBook(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var result = await _bookService.CreateBook(request.ToBook(), cancellationToken);
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
    public async Task<IActionResult> UpdateBook(Guid id, UpdateBookRequest request, 
        CancellationToken cancellationToken)
    {
        await _bookService.UpdateBook(id, request.ToBook(), cancellationToken);
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
    public async Task<IActionResult> ArchiveBook(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookService.ArchiveBook(id, cancellationToken);
        return Ok(result.ToArchiveBookResponse());
    }
    
    /// <summary>
    /// Добавление деталей к книге
    /// </summary>
    [HttpPost]
    [Route("/details")]
    [ServiceFilter<FileImageValidationFilter>]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> AddBookDetails([FromForm] AddBookDetailsRequest request,
        CancellationToken cancellationToken)
    {
        await using var stream = request.CoverImage.OpenReadStream();
        var contentType = request.CoverImage.ContentType;
        await _bookService.AddBookDetails(request.Id, request.Description, 
            stream, contentType, cancellationToken);
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
    public async Task<IActionResult> GetBooksPage(BookCursorPaginationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _bookService
            .GetBooksPage(request.ToCursorPaginationRequest(), 
                request.Status is null ? null: (BookStatus)request.Status, 
                request.Category is null ? null: (BookCategory)request.Category, 
                request.Author, cancellationToken);
        return Ok(result.ToBookCursorPaginationResponse());
    }
}