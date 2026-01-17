using NiziKit.Core;

namespace NiziKit.Components;

public interface IComponent
{
    GameObject? Owner { get; set; }
}
