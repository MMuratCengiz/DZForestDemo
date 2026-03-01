using Avalonia.Controls;
using Avalonia.Input;
using DenOfIz;
using DZForestDemo.AvaUi.Views;
using NiziKit.Graphics.Resources;
using NiziKit.Inputs;
using NiziKit.Skia;

namespace DZForestDemo.AvaUi;

public class DemoUi
{
    private readonly NiziAvalonia _ui;
    private readonly ChatWindow _chatBox = new();

    public DemoUi()
    {
        _ui = new NiziAvalonia
        {
            Content = _chatBox
        };

        _chatBox.ChatBox.KeyDown += (sender, args) =>
        {
            if (args.Key == Key.Enter)
            {
                _chatBox.Messages.Children.Add(new TextBlock { Text = _chatBox.ChatBox.Text });
                _chatBox.ChatBox.Text = "";
            }
        };
    }

    public Texture? Texture => _ui.Texture;

    public void OnEvent(ref Event ev)
    {
        _ui.OnEvent(ref ev);
    }

    public void Update(float deltaTime)
    {

        _ui.Update(deltaTime);
    }
}
