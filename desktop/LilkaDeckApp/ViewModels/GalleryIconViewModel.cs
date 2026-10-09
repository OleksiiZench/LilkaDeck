using System;
using System.Threading.Tasks;
using System.Windows.Input;
using LilkaDeckApp.Mvvm;

namespace LilkaDeckApp.ViewModels;

/// <summary>One picture in the gallery.</summary>
public sealed class GalleryIconViewModel
{
    public GalleryIconViewModel(string filePath, Func<string, Task> onSelected, Action<Exception> onError)
    {
        FilePath = filePath;
        SelectCommand = new AsyncRelayCommand(() => onSelected(filePath), onError: onError);
    }
    
    public string FilePath { get; }
    
    public ICommand SelectCommand { get; }
}
