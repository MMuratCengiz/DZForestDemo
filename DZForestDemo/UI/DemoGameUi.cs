using DenOfIz;
using NiziKit.Graphics;
using NiziKit.Inputs;
using NiziKit.UI;

namespace DZForestDemo.UI;

public class DemoGameUi
{
    private readonly ChatBox _chatBox = new();
    private string _currentChatText = "";

    public void Render()
    {
        using (NiziUi.Panel("ChatPanel")
                   .GrowHeight()
                   .GrowWidth(500, 750)
                   .Padding(15)
                   .Background(UiColor.Rgba(255, 255, 255, 0))
                   .AlignChildren(UiAlignX.Left, UiAlignY.Bottom)
                   .Open())
        {
            using (NiziUi.Column("ChatColumn")
                       .GrowWidth()
                       .GrowHeight(500, 600)
                       .Background(255, 255, 255, 35)
                       .Padding(10)
                       .Open())
            {
                using (NiziUi.Row("ChatBoxRow")
                           .GrowWidth()
                           .GrowHeight()
                           .Open())
                {
                    _chatBox.Render();
                }

                using (NiziUi.Row("Spacer").GrowWidth().Height(10).Open()) { }

                using (NiziUi.Row("Input").GrowWidth().Open())
                {
                    var input = NiziUi
                        .TextField("ChatInput", ref _currentChatText)
                        .GrowWidth();

                    input.Show();
                    if (input.SubmittedThisFrame)
                    {
                        _chatBox.AddMessage($"<black>{_currentChatText}</black>");
                        _currentChatText = "";
                    }
                }
            }
        }
    }
}
