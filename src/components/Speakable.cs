using Godot;

namespace Components;

public partial class Speakable : Area2D
{
  [Export]
  private string? _text;

  public void Speak()
  {
    if (_text == null)
    {
      return;
    }

    // Show text box with _text;
    GD.Print(_text);
  }
}
