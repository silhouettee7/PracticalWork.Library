using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.Controllers.Mappers.v1;
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
    
    [HttpGet]
    [Route("/books/{idOrTitle}/details")]
    [Produces("application/json")]
    [ProducesResponseType<BookDetailsResponse>( 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetBookDetails(string idOrTitle)
    {
        (Guid bookId, Book book) result;
        if (Guid.TryParse(idOrTitle, out var bookId))
        {
            result = await _libraryService.GetBookDetails(bookId);
        }
        else
        {
            result = await _libraryService.GetBookDetails(idOrTitle);
        }
        return Ok(result.book.ToBookDetailsResponse(result.bookId));
    }
    
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