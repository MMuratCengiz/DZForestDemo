using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

namespace NiziKit.Editor.Views;

public partial class InlineContextMenu : UserControl
{
    public InlineContextMenu()
    {
        InitializeComponent();
    }

    private static double GetIconSize(string key, double fallback = 16)
    {
        if (Avalonia.Application.Current?.TryFindResource(key, out var resource) == true && resource is double size)
            return size;
        return fallback;
    }

    public void Show(Point position, IEnumerable<InlineMenuItem> items)
    {
        // Ensure we're on the UI thread
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Show(position, items));
            return;
        }

        MenuItems.Children.Clear();

        foreach (var item in items)
        {
            if (item.IsSeparator)
            {
                MenuItems.Children.Add(new Border
                {
                    Height = 1,
                    Margin = new Thickness(8, 4),
                    Background = Avalonia.Application.Current?.FindResource("DividerStrokeColorDefaultBrush") as IBrush
                });
            }
            else if (item.Children?.Any() == true)
            {
                // Submenu - create expandable section
                var expander = new Expander
                {
                    Header = CreateMenuItemContent(item),
                    Padding = new Thickness(0),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                var subPanel = new StackPanel { Spacing = 0, Margin = new Thickness(16, 0, 0, 0) };
                foreach (var child in item.Children)
                {
                    if (child.IsSeparator)
                    {
                        subPanel.Children.Add(new Border
                        {
                            Height = 1,
                            Margin = new Thickness(8, 4),
                            Background = Avalonia.Application.Current?.FindResource("DividerStrokeColorDefaultBrush") as IBrush
                        });
                    }
                    else
                    {
                        subPanel.Children.Add(CreateMenuButton(child));
                    }
                }

                expander.Content = subPanel;
                MenuItems.Children.Add(expander);
            }
            else
            {
                MenuItems.Children.Add(CreateMenuButton(item));
            }
        }

        // Position the menu
        Margin = new Thickness(position.X, position.Y, 0, 0);
        IsVisible = true;
    }

    public void Hide()
    {
        // Ensure we're on the UI thread
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(Hide);
            return;
        }

        IsVisible = false;
    }

    private Button CreateMenuButton(InlineMenuItem item)
    {
        var button = new Button
        {
            Content = CreateMenuItemContent(item),
            Command = item.Command,
            CommandParameter = item.CommandParameter,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 8),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };

        button.Click += (s, e) => Hide();

        return button;
    }

    private static Control CreateMenuItemContent(InlineMenuItem item)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12
        };

        if (item.Icon != null)
        {
            panel.Children.Add(new SymbolIcon
            {
                Symbol = item.Icon.Value,
                FontSize = GetIconSize("IconSizeBase")
            });
        }

        panel.Children.Add(new TextBlock
        {
            Text = item.Header,
            VerticalAlignment = VerticalAlignment.Center
        });

        return panel;
    }
}

public class InlineMenuItem
{
    public string Header { get; set; } = "";
    public Symbol? Icon { get; set; }
    public ICommand? Command { get; set; }
    public object? CommandParameter { get; set; }
    public bool IsSeparator { get; set; }
    public List<InlineMenuItem>? Children { get; set; }

    public static InlineMenuItem Separator() => new() { IsSeparator = true };
}
