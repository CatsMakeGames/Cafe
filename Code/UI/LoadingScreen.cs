using Godot;
using System;

/**<Summary>Script for loading screen that provides easy access to values</Summary>*/
public class LoadingScreen : Node2D
{
    private ProgressBar _progressBar;
    private Label _textLabel;

    public string Text
    {
        set => _textLabel.Text = value;
        get => _textLabel.Text;
    }
    /**<summary>Value of the progress bar</summary>*/
    public double Progress
    {
        set => _progressBar.Value = value;
        get => _progressBar.Value;
    }
    public double MaxProgress { get => _progressBar.MaxValue; }
    public override void _Ready()
    {
        base._Ready();
        _progressBar = GetNode<ProgressBar>("ColorRect/ProgressBar");
        _textLabel = GetNode<Label>("ColorRect/Label");
    }
}
