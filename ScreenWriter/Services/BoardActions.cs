using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using ScreenWriter.Models;
using ScreenWriter.Windows;
using Clipboard   = System.Windows.Clipboard;
using MessageBox  = System.Windows.MessageBox;
using Point       = System.Windows.Point;

namespace ScreenWriter.Services;

/// <summary>The board "File" commands: dialogs, prompts and error messages around the overlay.</summary>
public sealed class BoardActions(OverlayWindow overlay, ToolbarWindow toolbar, HotkeyService hotkeys)
{
    private const string FolderName = "Screen Writer";

    private int     _modalDepth;
    private string? _pendingOpen;

    private static LocalizationService Loc => LocalizationService.Instance;

    public void Execute(BoardCommand command)
    {
        if (_modalDepth > 0 || !overlay.IsBoard) return;

        switch (command)
        {
            case BoardCommand.New:         NewBoard();     break;
            case BoardCommand.SaveImage:   SaveImage();    break;
            case BoardCommand.CopyImage:   CopyImage();    break;
            case BoardCommand.Save:        Save();         break;
            case BoardCommand.SaveAs:      SaveAs();       break;
            case BoardCommand.Open:        OpenWithDialog(); break;
            case BoardCommand.InsertImage: InsertImageWithDialog(); break;
            case BoardCommand.Paste:       Paste();        break;
        }
    }

    // ── Image output ──────────────────────────────────────────────────────────
    private void SaveImage()
    {
        var path = Modal(() =>
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter           = $"PNG (*.png)|*.png",
                DefaultExt       = ".png",
                FileName         = $"Screen Writer {DateTime.Now:yyyy-MM-dd HH-mm-ss}.png",
                InitialDirectory = StartFolder(SettingsService.Instance.LastImageFolder,
                                               Environment.SpecialFolder.MyPictures),
            };
            return dialog.ShowDialog(overlay) == true ? dialog.FileName : null;
        });
        if (path is null) return;

        try
        {
            File.WriteAllBytes(path, BoardImage.Encode(overlay.RenderBoard(), new PngBitmapEncoder()));
            SettingsService.Instance.SetLastImageFolder(Path.GetDirectoryName(path)!);
            toolbar.FlashStatus(Loc.Get("Str_Saved"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Str_FileSaveFailed");
        }
    }

    private void CopyImage()
    {
        var bitmap = overlay.RenderBoard();
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetImage(bitmap);
                toolbar.FlashStatus(Loc.Get("Str_Copied"));
                return;
            }
            catch (COMException) { Thread.Sleep(60); }   // another app is holding the clipboard
        }
        ShowError("Str_ClipboardBusy");
    }

    // ── Board files ───────────────────────────────────────────────────────────
    private void NewBoard()
    {
        if (ConfirmSaveIfNeeded(overlay.Mode)) overlay.NewBoard();
    }

    private bool Save()
    {
        var path = overlay.FilePath;
        return path is null ? SaveAs() : WriteBoard(path);
    }

    private bool SaveAs()
    {
        var path = Modal(() =>
        {
            var name = overlay.FilePath is { } current
                ? Path.GetFileName(current)
                : $"{Loc.Get(overlay.Mode == BoardMode.White ? "Str_BoardWhite" : "Str_BoardBlack")} {DateTime.Now:yyyy-MM-dd HH-mm}{BoardFile.Extension}";

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter           = BoardFilter(),
                DefaultExt       = BoardFile.Extension,
                FileName         = name,
                InitialDirectory = StartFolder(SettingsService.Instance.LastBoardFolder,
                                               Environment.SpecialFolder.MyDocuments),
            };
            return dialog.ShowDialog(overlay) == true ? dialog.FileName : null;
        });
        return path is not null && WriteBoard(path);
    }

    private bool WriteBoard(string path)
    {
        try
        {
            overlay.SaveBoard(path);
            SettingsService.Instance.SetLastBoardFolder(Path.GetDirectoryName(path)!);
            toolbar.FlashStatus(Loc.Get("Str_Saved"));
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError("Str_FileSaveFailed");
            return false;
        }
    }

    private void OpenWithDialog()
    {
        var path = Modal(() =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter           = BoardFilter(),
                InitialDirectory = StartFolder(SettingsService.Instance.LastBoardFolder,
                                               Environment.SpecialFolder.MyDocuments),
            };
            return dialog.ShowDialog(overlay) == true ? dialog.FileName : null;
        });
        if (path is not null) OpenPath(path);
    }

    /// <summary>Opens a board from any source: dialog, drop, or a double-clicked file.</summary>
    public void OpenPath(string path)
    {
        if (_modalDepth > 0) { _pendingOpen = path; return; }

        if (!BoardFile.IsBoardFile(path) || !File.Exists(path))
        {
            ShowError("Str_FileInvalid");
            return;
        }

        LoadedBoard loaded;
        try
        {
            // Read first: a bad file must not disturb the current board or ask to save it.
            loaded = BoardFile.Load(path);
        }
        catch (BoardFileException ex)
        {
            ShowError(ex.Key);
            return;
        }

        if (!ConfirmSaveIfNeeded(loaded.Mode)) return;

        overlay.ApplyBoard(loaded, path);
        SettingsService.Instance.SetLastBoardFolder(Path.GetDirectoryName(Path.GetFullPath(path))!);
    }

    // ── Inserting images ──────────────────────────────────────────────────────
    private void InsertImageWithDialog()
    {
        var path = Modal(() =>
        {
            var patterns = string.Join(";", BoardImage.SupportedExtensions.Select(e => "*" + e));
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter           = $"{Loc.Get("Str_ImageFilter")} ({patterns})|{patterns}",
                InitialDirectory = StartFolder(SettingsService.Instance.LastImageFolder,
                                               Environment.SpecialFolder.MyPictures),
            };
            return dialog.ShowDialog(overlay) == true ? dialog.FileName : null;
        });
        if (path is not null) InsertFile(path, null);
    }

    private void InsertFile(string path, Point? center)
    {
        try
        {
            overlay.InsertImage(BoardImage.FromFile(path), center);
        }
        catch (Exception ex) when (IsImageFailure(ex))
        {
            ShowError("Str_ImageInvalid");
        }
    }

    private void Paste()
    {
        try
        {
            if (Clipboard.ContainsData("PNG") && Clipboard.GetData("PNG") is MemoryStream png)
                overlay.InsertImage(BoardImage.FromBytes(png.ToArray()));
            else if (Clipboard.ContainsFileDropList()
                     && Clipboard.GetFileDropList().Cast<string>().FirstOrDefault(BoardImage.IsSupportedFile) is { } file)
                InsertFile(file, null);
            else if (Clipboard.ContainsImage() && Clipboard.GetImage() is { } bitmap)
                overlay.InsertImage(BoardImage.FromBitmap(bitmap));
            else
                toolbar.FlashStatus(Loc.Get("Str_NoImageInClipboard"));
        }
        catch (COMException) { ShowError("Str_ClipboardBusy"); }
        catch (Exception ex) when (IsImageFailure(ex)) { ShowError("Str_ImageInvalid"); }
    }

    /// <summary>A file dropped on a board: an image is inserted, a board file is opened.</summary>
    public void HandleDrop(string[] paths, Point position)
    {
        if (_modalDepth > 0 || !overlay.IsBoard) return;

        var path = paths[0];
        if (BoardFile.IsBoardFile(path))             OpenPath(path);
        else if (BoardImage.IsSupportedFile(path))   InsertFile(path, position);
        else                                         ShowError("Str_ImageInvalid");
    }

    // ── Unsaved changes ───────────────────────────────────────────────────────
    public bool HasUnsavedBoards => overlay.NeedsSave(BoardMode.White) || overlay.NeedsSave(BoardMode.Black);

    /// <summary>Asks about each unsaved board. False means the user cancelled.</summary>
    public bool ConfirmSaveAll()
        => ConfirmSaveIfNeeded(BoardMode.White) && ConfirmSaveIfNeeded(BoardMode.Black);

    private bool ConfirmSaveIfNeeded(BoardMode mode)
    {
        if (!overlay.NeedsSave(mode)) return true;

        // Show the board the question is about.
        overlay.SetBoardMode(mode);
        var result = Modal(() => MessageBox.Show(overlay,
            Loc.Get(mode == BoardMode.White ? "Str_BoardUnsavedWhite" : "Str_BoardUnsavedBlack"),
            Loc.Get("Str_SaveChangesTitle"),
            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Yes));

        return result switch
        {
            MessageBoxResult.Yes => Save(),
            MessageBoxResult.No  => true,
            _                    => false,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    // While a dialog is open the global shortcuts are released (Ctrl+V must reach its
    // file-name box) and the toolbar cannot start another command.
    private T Modal<T>(Func<T> show)
    {
        overlay.FinishEditing();
        toolbar.CloseMenus();
        if (_modalDepth++ == 0)
        {
            hotkeys.Suspend();
            toolbar.IsEnabled = false;
        }
        try
        {
            return show();
        }
        finally
        {
            if (--_modalDepth == 0)
            {
                toolbar.IsEnabled = true;
                toolbar.BringToTopmost();   // the dialog raised the board above it
                hotkeys.Resume();

                if (_pendingOpen is { } pending)
                {
                    _pendingOpen = null;
                    overlay.Dispatcher.BeginInvoke(() => OpenPath(pending));
                }
            }
        }
    }

    private void ShowError(string key)
        => Modal(() => MessageBox.Show(overlay, Loc.Get(key), "Screen Writer",
            MessageBoxButton.OK, MessageBoxImage.Warning));

    private static string BoardFilter()
        => $"{Loc.Get("Str_BoardFileType")} (*{BoardFile.Extension})|*{BoardFile.Extension}";

    private static bool IsImageFailure(Exception ex)
        => ex is IOException or UnauthorizedAccessException or NotSupportedException
              or ArgumentException or InvalidOperationException or COMException
              or FileFormatException or OverflowException;

    private static string StartFolder(string? remembered, Environment.SpecialFolder root)
    {
        if (!string.IsNullOrEmpty(remembered) && Directory.Exists(remembered)) return remembered;

        var folder = Path.Combine(Environment.GetFolderPath(root), FolderName);
        try { Directory.CreateDirectory(folder); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Environment.GetFolderPath(root);
        }
        return folder;
    }
}
