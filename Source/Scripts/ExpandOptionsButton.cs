using Godot;

public partial class ExpandOptionsButton : Button {

  private VBoxContainer OptionsContainerNode { get; set; }
	private PanelContainer MenuPanel { get; set; }
  private bool State { get; set; } = true;

  public override void _Ready() {
    OptionsContainerNode = GetNode<VBoxContainer>("%OptionsContainer");
		MenuPanel = GetNode<PanelContainer>("%MenuPanel");
  }

  public void ToggleState(){
		State = !State;
		OptionsContainerNode.Visible = !OptionsContainerNode.Visible;
		Text = State ? "-" : "+";
		MenuPanel.ResetSize();
	}
}
