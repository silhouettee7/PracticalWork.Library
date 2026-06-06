using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using PracticalWork.Library.Abstractions.Storage;
using PracticalWork.Library.Data.PostgreSql.Entities;
using PracticalWork.Library.Data.PostgreSql.Extensions;
using PracticalWork.Library.Enums;
using PracticalWork.Library.Models;

namespace PracticalWork.Library.Data.PostgreSql.Repositories;

public class ReaderRepository: IReaderRepository
{
    private readonly AppDbContext _appDbContext;
    public ReaderRepository(AppDbContext context)
    {
        _appDbContext = context;
    }
    public async Task<Guid> CreateReader(Reader reader, CancellationToken cancellationToken)
    {
        ReaderEntity readerEntity = new()
        {
            FullName = reader.FullName,
            PhoneNumber = reader.PhoneNumber,
            ExpiryDate = reader.ExpiryDate,
            IsActive = reader.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _appDbContext.Readers.Add(readerEntity);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        
        return readerEntity.Id;
    }

    public async Task<bool> IsExistReader(string phone, CancellationToken cancellationToken)
    {
        return await _appDbContext.Readers.AnyAsync(reader => reader.PhoneNumber == phone, cancellationToken: cancellationToken);
    }

    public async Task<Reader> GetReader(Guid id, CancellationToken cancellationToken)
    {
        var reader = await _appDbContext.Readers
            .SingleOrDefaultAsync(reader => reader.Id == id, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException($"Карточка:{id} не найдена");
        return new Reader
        {
            FullName = reader.FullName,
            ExpiryDate = reader.ExpiryDate,
            IsActive = reader.IsActive,
            PhoneNumber = reader.PhoneNumber,
        };
    }

    public async Task UpdateReader(Guid id, Reader reader, CancellationToken cancellationToken)
    {
        var readerEntity = await _appDbContext.Readers.SingleOrDefaultAsync(r => r.Id == id, cancellationToken: cancellationToken) 
                           ?? throw new EntityNotFoundException($"Карточка:{id} не нашлась");
        readerEntity.FullName = reader.FullName;
        readerEntity.PhoneNumber = reader.PhoneNumber;
        readerEntity.ExpiryDate = reader.ExpiryDate;
        readerEntity.IsActive = reader.IsActive;
        _appDbContext.Readers.Update(readerEntity);
        await _appDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Reader> GetReaderWithBorrowBooks(Guid id, CancellationToken cancellationToken)
    {
        var readerEntity = await _appDbContext.Readers
            .Include(r => r.BorrowedRecords
                .Where(b => b.Status == BookIssueStatus.Issued))
            .ThenInclude(b => b.Book)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException($"Карточка:{id} не нашлась");
        var reader = new Reader
        {
            FullName = readerEntity.FullName,
            PhoneNumber = readerEntity.PhoneNumber,
            ExpiryDate = readerEntity.ExpiryDate,
            BorrowBooks = readerEntity.BorrowedRecords
                .Select(b => b.Book.ToBook())
                .ToList()
        };
        return reader;
    }

    public async Task<(bool isActive, IReadOnlyList<BorrowedBook> books)> GetReadersBorrowBooks(Guid id,
        CancellationToken cancellationToken)
    {
        var readerEntity = await _appDbContext.Readers
            .Include(r => r.BorrowedRecords
                .Where(b => b.Status == BookIssueStatus.Issued))
            .ThenInclude(b => b.Book)
            .Select(r => new { r.Id, r.IsActive, r.BorrowedRecords })
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken: cancellationToken)
            ?? throw new EntityNotFoundException($"Карточка:{id} не нашлась");
        
        return (readerEntity.IsActive,readerEntity.BorrowedRecords
            .Select(b => new BorrowedBook 
            {
                Title = b.Book.Title,
                Authors = b.Book.Authors,
                Category = b.Book.Category,
                Description = b.Book.Description,
                Year = b.Book.Year,
                DueDate = b.DueDate,
                Status = b.Status,
                BorrowDate = b.BorrowDate,
                ReturnDate = b.ReturnDate ?? default
            })
            .ToList());
    }

    public async Task<int> GetNewReadersCount(DateTime startDate, DateTime endDate, 
        CancellationToken cancellationToken)
    {
        return await _appDbContext.Readers
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt < endDate)
            .CountAsync(cancellationToken: cancellationToken);
    }
}