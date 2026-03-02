using Avalonia.Controls;

namespace DZForestDemo.AvaUi.Components;

public class CustomTabItem : TabItem
{
    protected override Type StyleKeyOverride => typeof(TabItem);
}
