using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.Contracts.v1.Reader.Request;
using PracticalWork.Library.Contracts.v1.Reader.Response;
using PracticalWork.Library.Controllers.Mappers.v1;

namespace PracticalWork.Library.Controllers.Api.v1;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/readers")]
public class ReaderController: Controller
{
    private readonly IReaderService _readerService;

    public ReaderController(IReaderService readerService)
    {
        _readerService = readerService;
    }
    
    /// <summary>
    /// Создание карточки читателя
    /// </summary>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType<CreateReaderResponse>( 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateReader(CreateReaderRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _readerService.CreateReader(request.ToReader(),cancellationToken);
        return Ok(new CreateReaderResponse(result));
    }
    
    /// <summary>
    /// Продление карточки читателя
    /// </summary>
    [HttpPost]
    [Route("/{id:guid}/extend")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ExtendReader(Guid id, 
        ExtendReaderExpiryDateRequest request, CancellationToken cancellationToken)
    {
        await _readerService.ExtendExpiryDate(id, request.Date, cancellationToken);
        return Ok();
    }
    
    /// <summary>
    /// Закрытие карточки читателя
    /// </summary>
    [HttpPost]
    [Route("/{id:guid}/close")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CloseReader(Guid id, CancellationToken cancellationToken)
    {
        var (borrowBooksExist, borrowBooks) = 
            await _readerService.CloseReader(id, cancellationToken);
        if (!borrowBooksExist)
        {
            return Ok();
        }

        return BadRequest(borrowBooks
            .Select(b => b.ToBookResponse()));
    }
    
    /// <summary>
    /// Получение информации о взятых книгах карточки читатетеля
    /// </summary>
    [HttpGet]
    [Route("/{id:guid}/books")]
    [Produces("application/json")]
    [ProducesResponseType<List<BorrowedBookResponse>>(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetBorrowedBooks(Guid id, 
        CancellationToken cancellationToken)
    {
        var books = await _readerService.GetAllBorrowBooks(id, cancellationToken);
        return Ok(books);
    }
}