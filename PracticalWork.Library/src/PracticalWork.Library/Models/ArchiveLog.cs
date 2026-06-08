using System.ComponentModel;
using PracticalWork.Library.Attributes;

namespace PracticalWork.Library.Models;

/// <summary>
/// Информация об архивировании книг
/// </summary>
public class ArchiveLog
{
    [TableColumn("Кол-во успешных",3)]
    public int SuccessCount { get; set; }
    [TableColumn("Общее кол-во",1)]
    public int TotalCount { get; set; }
    [TableColumn("Кол-во пропущенных",4)]
    public int WrongCount { get; set; }
    [TableColumn("Причины пропуска",5)]
    public string WrongReasons { get; set; }
    [TableColumn("Общее время",2)]
    public string TotalTime { get; set; }
}

