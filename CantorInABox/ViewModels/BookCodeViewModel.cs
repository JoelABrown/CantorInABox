namespace Mooseware.CantorInABox.ViewModels;

/// <summary>
/// View model for a prayer book code
/// </summary>
public class BookCodeViewModel
{
    /// <summary>
    /// The code which represents the prayer book
    /// </summary>
    public int Code { get; set; } = 0;
    /// <summary>
    /// The short descriptive name of the prayer book
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    public override string ToString()
    {
        return Name;
    }
    
    public BookCodeViewModel()
    {
        
    }
    
    public BookCodeViewModel(int code, string name)
    {
        Code = code;
        Name = name;
    }
}
