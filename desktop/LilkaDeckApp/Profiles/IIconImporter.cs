namespace LilkaDeckApp.Profiles;

/// <summary>Turns a picture the user chose into an icon ready to be sent to the device.</summary>
public interface IIconImporter
{
    /// <exception cref="IconImportException">The file cannot be used as an icon.</exception>
    ImportedIcon Import(string sourcePath);
}
