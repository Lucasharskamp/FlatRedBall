using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.OutputPlugin;
using L = Localization;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins;

[Export(typeof(PluginBase))]
public class OutputPrintPlugin : EmbeddedPlugin
{
    private OutputControl _outputControl; // This is the control we created

    public override void StartUp()
    {
        _outputControl = new OutputControl();
        var tab = base.CreateAndAddTab(_outputControl, L.Texts.Output, TabLocation.Bottom);

        this.OnOutputHandler += OnOutput;
        this.OnErrorOutputHandler += OnErrorOutput;
    }


    public void OnOutput(string output)
    {
        if (!string.IsNullOrWhiteSpace(output))
        {
            _outputControl.OnOutput(output);
        }
    }

    public void OnErrorOutput(string output)
    {
        if (!string.IsNullOrWhiteSpace(output))
        {
            _outputControl?.OnErrorOutput(output);
        }
    }
}