using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace NiziKit.Editor;

public class EditorMainView : UserControl
{
    public EditorMainView()
    {
        Background = new SolidColorBrush(Color.FromArgb(200, 30, 30, 30));

        Content = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            ColumnDefinitions = new ColumnDefinitions("200,*,300"),
            Children =
            {
                // Top menu bar
                CreateMenuBar(),

                // Left panel - Scene hierarchy
                CreateLeftPanel(),

                // Center - Viewport placeholder
                CreateCenterPanel(),

                // Right panel - Inspector
                CreateRightPanel(),

                // Bottom - Status bar
                CreateStatusBar()
            }
        };
    }

    private static Control CreateMenuBar()
    {
        var menu = new Menu
        {
            Items =
            {
                new MenuItem { Header = "_File", Items = { new MenuItem { Header = "_New Scene" }, new MenuItem { Header = "_Open..." }, new MenuItem { Header = "_Save" }, new MenuItem { Header = "E_xit" } } },
                new MenuItem { Header = "_Edit", Items = { new MenuItem { Header = "_Undo" }, new MenuItem { Header = "_Redo" }, new MenuItem { Header = "_Copy" }, new MenuItem { Header = "_Paste" } } },
                new MenuItem { Header = "_View", Items = { new MenuItem { Header = "_Scene" }, new MenuItem { Header = "_Game" }, new MenuItem { Header = "_Inspector" } } },
                new MenuItem { Header = "_Help", Items = { new MenuItem { Header = "_About" } } }
            }
        };

        Grid.SetRow(menu, 0);
        Grid.SetColumnSpan(menu, 3);
        return menu;
    }

    private static Control CreateLeftPanel()
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = new StackPanel
            {
                Margin = new Thickness(8),
                Children =
                {
                    new TextBlock
                    {
                        Text = "Scene Hierarchy",
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.White,
                        Margin = new Thickness(0, 0, 0, 8)
                    },
                    new TreeView
                    {
                        Items =
                        {
                            new TreeViewItem
                            {
                                Header = "Root",
                                IsExpanded = true,
                                Items =
                                {
                                    new TreeViewItem { Header = "Main Camera" },
                                    new TreeViewItem { Header = "Directional Light" },
                                    new TreeViewItem
                                    {
                                        Header = "Environment",
                                        Items =
                                        {
                                            new TreeViewItem { Header = "Ground" },
                                            new TreeViewItem { Header = "Sky" }
                                        }
                                    },
                                    new TreeViewItem
                                    {
                                        Header = "Characters",
                                        Items =
                                        {
                                            new TreeViewItem { Header = "Player" },
                                            new TreeViewItem { Header = "NPC_01" }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        Grid.SetRow(panel, 1);
        Grid.SetColumn(panel, 0);
        return panel;
    }

    private static Control CreateCenterPanel()
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 25, 25, 25)),
            Child = new Grid
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Viewport\n(DenOfIz Scene renders here)",
                        Foreground = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextAlignment = TextAlignment.Center
                    }
                }
            }
        };

        Grid.SetRow(panel, 1);
        Grid.SetColumn(panel, 1);
        return panel;
    }

    private static Control CreateRightPanel()
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Margin = new Thickness(8),
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Inspector",
                            FontWeight = FontWeight.Bold,
                            Foreground = Brushes.White,
                            Margin = new Thickness(0, 0, 0, 8)
                        },
                        CreatePropertyGroup("Transform", new[]
                        {
                            ("Position", "0, 0, 0"),
                            ("Rotation", "0, 0, 0"),
                            ("Scale", "1, 1, 1")
                        }),
                        CreatePropertyGroup("Mesh Renderer", new[]
                        {
                            ("Mesh", "Cube"),
                            ("Material", "Default-Material")
                        }),
                        new Button
                        {
                            Content = "Add Component",
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            Margin = new Thickness(0, 16, 0, 0)
                        }
                    }
                }
            }
        };

        Grid.SetRow(panel, 1);
        Grid.SetColumn(panel, 2);
        return panel;
    }

    private static Control CreatePropertyGroup(string title, (string name, string value)[] properties)
    {
        var panel = new Expander
        {
            Header = title,
            IsExpanded = true,
            Margin = new Thickness(0, 0, 0, 8),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 4, 0, 0)
            }
        };

        var content = (StackPanel)panel.Content;
        foreach (var (name, value) in properties)
        {
            content.Children.Add(new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("80,*"),
                Margin = new Thickness(0, 2, 0, 2),
                Children =
                {
                    new TextBlock
                    {
                        Text = name,
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBox
                    {
                        Text = value,
                        [Grid.ColumnProperty] = 1
                    }
                }
            });
        }

        return panel;
    }

    private static Control CreateStatusBar()
    {
        var panel = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(255, 45, 45, 45)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(8, 4),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children =
                {
                    new TextBlock
                    {
                        Text = "Ready",
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = "FPS: 60",
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        VerticalAlignment = VerticalAlignment.Center,
                        [Grid.ColumnProperty] = 1
                    }
                }
            }
        };

        Grid.SetRow(panel, 2);
        Grid.SetColumnSpan(panel, 3);
        return panel;
    }
}
