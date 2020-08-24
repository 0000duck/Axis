﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//using Rhino.Geometry;

using Axis.Params;
using Axis.Core;
using GH_IO.Serialization;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Axis.Params
{
    public class Param_Manipulator : GH_PersistentParam<Manipulator>
    {
        //public override GH_Exposure Exposure => GH_Exposure.hidden; // <--- Make it hidden when it is working.
        public Param_Manipulator()
          : base("Axis Robot", "Axis Robot", "This parampeter will store Axis Robots and their data.", Axis.AxisInfo.Plugin, Axis.AxisInfo.TabCore)
        { }

        public override Guid ComponentGuid => new Guid("17C49BD4-7A54-4471-961A-B5E0E971F7F4");

        protected override Manipulator InstantiateT()
        {
            return Manipulator.Default;
        }
        protected override GH_GetterResult Prompt_Singular(ref Manipulator value)
        {
            Rhino.Input.Custom.GetOption go = new Rhino.Input.Custom.GetOption();
            go.SetCommandPrompt("Set default Robot");
            go.AcceptNothing(true);
            go.AddOption("True");

            switch (go.Get())
            {
                case Rhino.Input.GetResult.Option:
                    if (go.Option().EnglishName == "True") { value = Manipulator.Default; }
                    return GH_GetterResult.success;

                case Rhino.Input.GetResult.Nothing:
                    return GH_GetterResult.accept;

                default:
                    return GH_GetterResult.cancel;
            }

            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<Manipulator> values)
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
            PersistentData.Append(Manipulator.Default, new GH_Path(0));
            //PersistentData.Append(Axis.Core.Tool.Default, new GH_Path(0));
            ExpireSolution(true);
        }
        public override bool Write(GH_IWriter writer)
        {
            PersistentData.Write(writer);
            return base.Write(writer);
        }
        public override bool Read(GH_IReader reader)
        {
            //SetPersistentData(Manipulator.Default, new GH_Path(0));
            SetPersistentData(Manipulator.Default);
            return base.Read(reader);
        }
    }
}
