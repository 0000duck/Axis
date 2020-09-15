﻿using Axis.Core;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Axis.Params
{
    public class ToolParam : GH_PersistentParam<Tool>
    {
        public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.

        public ToolParam()
          : base("Tool", "Tool", "Axis tool type.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabParam)
        { }

        public override Guid ComponentGuid => new Guid("E55644AF-9D59-486D-A698-637062C7734D");

        protected override Tool InstantiateT()
        {
            return Tool.Default;
        }

        protected override GH_GetterResult Prompt_Singular(ref Tool value)
        {
            Rhino.Input.Custom.GetPoint gpC = new Rhino.Input.Custom.GetPoint();
            gpC.SetCommandPrompt("Set default tool center point.");
            gpC.AcceptNothing(true);

            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default tool.");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = Tool.Default; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }

            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Plural(ref List<Tool> values)
        {
            return GH_GetterResult.cancel;
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            //Menu_AppendItem(menu, "Set the default value", SetDefaultHandler, SourceCount == 0);
            base.AppendAdditionalMenuItems(menu);
        }

        private void SetDefaultHandler(object sender, EventArgs e)
        {
            PersistentData.Clear();
            PersistentData.Append(Tool.Default, new GH_Path(0));
            ExpireSolution(true);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Icons.ToolParam;

    }
}