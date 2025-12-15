using PracticalWork.Library.Models;

namespace PracticalWork.Library.Dtos;

public class BookDetailsDto
{
    public Guid Id { get; set; }
    public Book Book { get; set; }
}