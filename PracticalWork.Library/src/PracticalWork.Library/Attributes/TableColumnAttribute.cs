namespace PracticalWork.Library.Attributes;

/// <summary>
/// Атрибут для генерации отчета в csv
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TableColumnAttribute : Attribute
{
    public string Name { get; set; }
    public int Order { get; set; } = int.MaxValue;
    
    public TableColumnAttribute(string name)
    {
        Name = name;
    }

    public TableColumnAttribute(string name, int order): this(name)
    {
        Order = order;
    }
}