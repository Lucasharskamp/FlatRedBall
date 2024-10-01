using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace GlueFormsCore.FormHelpers
{
    [DebuggerDisplay("{Text}")]
    public class GeneralToolStripMenuItem
    {
        public GeneralToolStripMenuItem() { }

        public GeneralToolStripMenuItem(string text) { Text = text; }

        public string Text { get; internal set; }
        public EventHandler Click { get; set; }
        public string ShortcutKeyDisplayString { get; internal set; }

        public System.Windows.Controls.Image Image { get; internal set; }

        public List<GeneralToolStripMenuItem> DropDownItems { get; } = new();

        public ToolStripMenuItem ToTsmi()
        {
            var tsmi = new ToolStripMenuItem(Text);
            tsmi.Click += Click;
            tsmi.ShortcutKeyDisplayString = ShortcutKeyDisplayString;

            foreach(var dropdownItem in DropDownItems)
            {
                tsmi.DropDownItems.Add(dropdownItem.ToTsmi());
            }

            return tsmi;
        }
    }

    public static class GeneralToolStripMenuItemExtensions
    {
        public static GeneralToolStripMenuItem Add(this List<GeneralToolStripMenuItem> items, string text, EventHandler click)
        {
            var item = new GeneralToolStripMenuItem();
            item.Text = text;
            item.Click += click;
            items.Add(item);
            return item;
        }
    }
}
