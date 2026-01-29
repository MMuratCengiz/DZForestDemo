using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class ImportPanel : UserControl
{
    public ImportPanel()
    {
        InitializeComponent();

        var queueBorder = this.FindControl<Border>("ImportQueueBorder");
        if (queueBorder != null)
        {
            queueBorder.AddHandler(DragDrop.DragOverEvent, OnQueueDragOver);
            queueBorder.AddHandler(DragDrop.DropEvent, OnQueueDrop);
        }
    }

    private void OnQueueDragOver(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void OnQueueDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not ImportViewModel vm)
        {
            return;
        }

        if (!e.Data.Contains(DataFormats.Files))
        {
            return;
        }

        var files = e.Data.GetFiles();
        if (files == null)
        {
            return;
        }

        var paths = new List<string>();
        foreach (var item in files)
        {
            if (item.TryGetLocalPath() is { } path)
            {
                paths.Add(path);
            }
        }

        if (paths.Count > 0)
        {
            vm.AddFilesToQueue(paths);
        }

        e.Handled = true;
    }
}
