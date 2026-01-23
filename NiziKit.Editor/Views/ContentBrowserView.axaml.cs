using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views;

public partial class ContentBrowserView : UserControl
{
    private ContentItem? _rightClickedItem;

    public ContentBrowserView()
    {
        InitializeComponent();

        // Hide context menu when clicking elsewhere
        PointerPressed += OnViewPointerPressed;
    }

    private void OnViewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && InlineMenu.IsVisible)
        {
            InlineMenu.Hide();
        }
    }

    private void OnTabPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is ContentTab tab && DataContext is ContentBrowserViewModel vm)
        {
            vm.SelectedTab = tab;
        }
    }

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ContentBrowserViewModel vm && vm.SelectedItem != null)
        {
            vm.HandleItemDoubleClickCommand.Execute(vm.SelectedItem);
        }
    }

    private void OnContentGridPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            _rightClickedItem = null;
            ShowGridInlineMenu(point.Position);
            e.Handled = true;
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            InlineMenu.Hide();
        }
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed && sender is Border border && border.DataContext is ContentItem item)
        {
            _rightClickedItem = item;

            // Select the item
            if (DataContext is ContentBrowserViewModel vm)
            {
                vm.SelectedItem = item;
            }

            ShowItemInlineMenu(point.Position, item);
            e.Handled = true;
        }
    }

    private void ShowGridInlineMenu(Point position)
    {
        if (DataContext is not ContentBrowserViewModel vm)
        {
            return;
        }

        var items = new List<InlineMenuItem>
        {
            new()
            {
                Header = "Create Folder",
                Icon = Symbol.Folder,
                Command = vm.CreateFolderCommand
            },
            InlineMenuItem.Separator(),
            new()
            {
                Header = "Create Material",
                Icon = Symbol.Globe,
                Command = vm.CreateMaterialCommand
            },
            new()
            {
                Header = "Create Shader",
                Icon = Symbol.Code,
                Command = vm.CreateShaderCommand
            },
            InlineMenuItem.Separator(),
            new()
            {
                Header = "Create Asset Pack",
                Icon = Symbol.AllApps,
                Command = vm.CreatePackCommand
            },
            InlineMenuItem.Separator(),
            new()
            {
                Header = "Refresh",
                Icon = Symbol.Refresh,
                Command = vm.RefreshCurrentTabCommand
            }
        };

        InlineMenu.Show(position, items);
    }

    private void ShowItemInlineMenu(Point position, ContentItem item)
    {
        if (DataContext is not ContentBrowserViewModel vm)
        {
            return;
        }

        var items = new List<InlineMenuItem>
        {
            new()
            {
                Header = "Open",
                Icon = Symbol.Open,
                Command = vm.HandleItemDoubleClickCommand,
                CommandParameter = item
            }
        };

        if (vm.SelectedPack != null && !item.IsDirectory && item.Type != NiziKit.Editor.Services.AssetFileType.Pack)
        {
            items.Add(InlineMenuItem.Separator());
            items.Add(new InlineMenuItem
            {
                Header = $"Add To '{vm.SelectedPack.PackName}'",
                Icon = Symbol.Add,
                Command = vm.AddToSelectedPackCommand
            });
        }

        items.Add(InlineMenuItem.Separator());
        items.Add(new InlineMenuItem
        {
            Header = "Delete",
            Icon = Symbol.Delete,
            Command = vm.DeleteSelectedCommand
        });
        items.Add(InlineMenuItem.Separator());
        items.Add(new InlineMenuItem
        {
            Header = "Show in Explorer",
            Icon = Symbol.OpenFolder,
            Command = vm.ShowInExplorerCommand
        });

        InlineMenu.Show(position, items);
    }

    private void OnPackTreeItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed && sender is Border border && border.DataContext is FolderTreeNode node)
        {
            if (node.IsPack)
            {
                ShowPackContextMenu(point.Position, node);
                e.Handled = true;
            }
        }
    }

    private void ShowPackContextMenu(Point position, FolderTreeNode packNode)
    {
        if (DataContext is not ContentBrowserViewModel vm)
        {
            return;
        }

        var items = new List<InlineMenuItem>();
        var isInScene = vm.ScenePackTree.Any(p => p.PackName == packNode.PackName);
        if (isInScene)
        {
            items.Add(new InlineMenuItem
            {
                Header = "Remove from Scene",
                Icon = Symbol.Remove,
                Command = vm.RemovePackFromSceneCommand,
                CommandParameter = packNode
            });
        }
        else
        {
            items.Add(new InlineMenuItem
            {
                Header = "Add to Scene",
                Icon = Symbol.Add,
                Command = vm.AddPackToSceneCommand,
                CommandParameter = packNode
            });
        }

        items.Add(InlineMenuItem.Separator());
        items.Add(new InlineMenuItem
        {
            Header = "Rename",
            Icon = Symbol.Rename,
            Command = vm.RenamePackCommand,
            CommandParameter = packNode
        });
        items.Add(InlineMenuItem.Separator());
        items.Add(new InlineMenuItem
        {
            Header = "Show in Explorer",
            Icon = Symbol.OpenFolder,
            Command = vm.ShowPackInExplorerCommand,
            CommandParameter = packNode
        });

        InlineMenu.Show(position, items);
    }
}
