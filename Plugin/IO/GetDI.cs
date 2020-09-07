﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.IO
{
    /// <summary>
    /// Get the value of a specific digital input.
    /// </summary>
    public class GetDI : GH_Component
    {
        public GetDI() : base("Get Digital Input", "Get DI", "Get the value of a specific digital input.", AxisInfo.Plugin, AxisInfo.TabIO)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }
        #endregion

        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        #region Component Settings
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.DigitalIn;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("d3f8a44d-ea83-4740-8511-de8b4725dec5"); }

        }
        #endregion
    }
}