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
    public async Task<Guid> CreateReaderAsync(Reader reader)
    {
        ReaderEntity readerEntity = new ReaderEntity
        {
            FullName = reader.FullName,
            PhoneNumber = reader.PhoneNumber,
            ExpiryDate = reader.ExpiryDate,
            IsActive = reader.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _appDbContext.Readers.Add(readerEntity);
        await _appDbContext.SaveChangesAsync();
        
        return readerEntity.Id;
    }

    public async Task<bool> IsExistReaderAsync(string phone)
    {
        return await _appDbContext.Readers.AnyAsync(reader => reader.PhoneNumber == phone);
    }

    public async Task<Reader> GetReaderAsync(Guid id)
    {
        var reader = await _appDbContext.Readers
            .SingleOrDefaultAsync(reader => reader.Id == id);
        return new Reader
        {
            FullName = reader.FullName,
            ExpiryDate = reader.ExpiryDate,
            IsActive = reader.IsActive,
            PhoneNumber = reader.PhoneNumber,
        };
    }

    public async Task UpdateReaderAsync(Guid id, Reader reader)
    {
        var readerEntity = await _appDbContext.Readers.FindAsync(id) ?? throw new Exception("Карточка не нашлась");
        readerEntity.FullName = reader.FullName;
        readerEntity.PhoneNumber = reader.PhoneNumber;
        readerEntity.ExpiryDate = reader.ExpiryDate;
        readerEntity.IsActive = reader.IsActive;
        _appDbContext.Readers.Update(readerEntity);
        await _appDbContext.SaveChangesAsync();
    }

    public async Task<Reader> GetReaderWithBorrowBooksAsync(Guid id)
    {
        var readerEntity = await _appDbContext.Readers
            .Include(r => r.BorrowedRecords
                .Where(b => b.Status == BookIssueStatus.Issued))
            .ThenInclude(b => b.Book)
            .SingleOrDefaultAsync(r => r.Id == id);
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

    public async Task<IReadOnlyList<BorrowedBook>> GetReadersBorrowBooksAsync(Guid id)
    {
        var readerEntity = await _appDbContext.Readers
            .Include(r => r.BorrowedRecords)
            .ThenInclude(b => b.Book)
            .Select(r => new { r.Id, r.BorrowedRecords })
            .SingleOrDefaultAsync(r => r.Id == id);
        
        return readerEntity.BorrowedRecords
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
                ReturnDate = b.ReturnDate
            })
            .ToList();
    }
}