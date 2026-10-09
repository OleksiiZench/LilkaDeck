using System.Threading.Tasks;

namespace LilkaDeckApp.Services;

/// <summary>Lets the user choose a picture on this computer.</summary>
public interface IFilePicker
{
    /// <summary>Returns the path of the chosen picture, or null when the user cancelled.</summary>
    Task<string?> PickImageAsync();
}
