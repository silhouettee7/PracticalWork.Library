using Asp.Versioning;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.Controllers.Mappers.v1;
using PracticalWork.Library.Dtos;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Controllers.Api.v1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/library")]
public class LibraryController: Controller
{
    private readonly ILibraryService _libraryService;

    public LibraryController(ILibraryService libraryService)
    {
        _libraryService = libraryService;
    }
    
    /// <summary>
    /// Выдача книги
    /// </summary>
    [HttpPost]
    [Route("/borrow/{bookId:guid}/{readerId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType( 204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> BorrowBook(Guid bookId, Guid readerId)
    {
        await _libraryService.BorrowBook(bookId, readerId);
        return Created();
    }
    
    /// <summary>
    /// Возврат книги
    /// </summary>
    [HttpPost]
    [Route("/return/{bookId:guid}/{readerId:guid}")]
    [Produces("application/json")]
    [ProducesResponseType( 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ReturnBook(Guid bookId, Guid readerId)
    {
        await _libraryService.ReturnBook(bookId, readerId);
        return Ok();
    }
    
    /// <summary>
    /// Получение деталей книги
    /// </summary>
    [HttpGet]
    [Route("/books/{idOrTitle}/details")]
    [Produces("application/json")]
    [ProducesResponseType<BookDetailsResponse>( 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetBookDetails(string idOrTitle)
    {
        BookDetailsDto result;
        if (Guid.TryParse(idOrTitle, out var bookId))
        {
            result = await _libraryService.GetBookDetails(bookId);
        }
        else
        {
            result = await _libraryService.GetBookDetails(idOrTitle);
        }
        return Ok(result.Book.ToBookDetailsResponse(result.Id));
    }
    
    /// <summary>
    /// Получение неархивированных книг постранично
    /// </summary>
    [HttpPost]
    [Route("/books")]
    [Produces("application/json")]
    [ProducesResponseType<BookWithIssuanceCursorPaginationResponse>(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetBooksPage(CursorPaginationRequest request)
    {
        var result = await _libraryService
            .GetNonArchivedBooksPage(request);
        return Ok(result.ToBookWithIssuanceCursorPaginationResponse());
    }
}