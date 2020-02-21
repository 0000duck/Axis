﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using Rhino.Geometry;

using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;

using System.Windows.Forms;
using Eto.Forms;
using Eto.Drawing;

using Auth0.OidcClient;
using static Axis.Properties.Settings;

namespace Axis.Core
{
    public class Engine : GH_Component
    {
        // Sticky variables for the options.
        bool m_Logout = false;
        public List<string> log = new List<string>();
        public bool loggedIn = false;
        public bool forceLogout = false;
        public bool clear = false;
        public Auth0Client client = null;

        // Set up our client to handle the login.
        Auth0ClientOptions clientOptions = new Auth0ClientOptions
        {
            Domain = "axisarch.eu.auth0.com",
            ClientId = "bDiJKd5tM8eqHsTX01ovqyFvOSBnC4mE",
            Browser = new WebBrowserBrowser("Authenticating...", 400, 640)
        };

        Dictionary<string, string> extra = new Dictionary<string, string>()
        {
            {"response_type", "code"}
        };

        public Engine() : base("Axis", "Axis", "Manage the Axis application.", AxisInfo.Plugin, AxisInfo.TabCore)
        {
        }

        /// <summary>
        /// Create the component attributes.
        /// </summary>
        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "Log", "Log", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (clear)
            {
                ClearToken();
                log.Add("Cleared token at " + System.DateTime.Now.ToShortDateString());
            }

            // Initiate the client
            client = new Auth0Client(clientOptions);
            clientOptions.PostLogoutRedirectUri = clientOptions.RedirectUri;

            // Handle the logout.
            if ((loggedIn) || forceLogout)
            {
                client.LogoutAsync();
                loggedIn = false;
                this.Message = "Logged Out";
                log.Add("Logged out of Axis at " + System.DateTime.Now.ToShortDateString());
            }

            forceLogout = false;
            Axis.Properties.Settings.Default.LoggedIn = loggedIn;

            DA.SetDataList(0, log);
        }

        public void ClearToken()
        {
            Default.LastLoggedIn = new DateTime(2000, 1, 1);
            Default.LoggedIn = false;
            Default.Token = null;
        }

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem fLogout = Menu_AppendItem(menu, "Force Logout", logout_Click);
            fLogout.ToolTipText = "Forceably logout of the Axis domain.";
            ToolStripMenuItem clear = Menu_AppendItem(menu, "Clear Token", clear_Click);
            clear.ToolTipText = "Clear authentification token from the PC.";
        }

        private void logout_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Logout");
            forceLogout = true;
            ExpireSolution(true);
        }

        private void clear_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Clear");
            clear = true;
            ExpireSolution(true);
        }

        public class CustomAttributes : GH_ComponentAttributes
        {
            public CustomAttributes(Engine owner) : base(owner) { }

            #region Custom layout logic
            private System.Drawing.RectangleF PathBounds { get; set; }
            private System.Drawing.RectangleF SettingsBounds { get; set; }

            protected override void Layout()
            {
                base.Layout();

                // We'll extend the basic layout by adding three regions to the bottom of this component,
                PathBounds = new System.Drawing.RectangleF(Bounds.X, Bounds.Bottom, Bounds.Width, 20);
                SettingsBounds = new System.Drawing.RectangleF(Bounds.X, Bounds.Bottom + 20, Bounds.Width, 20);
                Bounds = new System.Drawing.RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 40);
            }
            #endregion

            #region Custom Mouse handling
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    Engine comp = Owner as Engine;

                    if (PathBounds.Contains(e.CanvasLocation))
                    {
                        comp.RecordUndoEvent("Login");
                        comp.log.Add("Clicked button to log in.");
                        
                        // This stuff is happening in the wrong place - needs to be refactored and called. (Static method error).
                        // Handle the login.
                        if (!comp.loggedIn)
                        {
                            comp.client.LoginAsync(comp.extra).ContinueWith(t =>
                            {
                                if (!t.Result.IsError)
                                {
                                    Default.Token = t.Result.AccessToken;
                                    comp.log.Clear();
                                    comp.log.Add("Logged in to Axis at " + DateTime.Now.ToShortTimeString());
                                    DateTime validTo = DateTime.Now.AddDays(2);
                                    comp.log.Add("Login valid to: " + validTo.ToLongDateString() + ", " + validTo.ToShortTimeString());
                                    comp.Message = "OK";
                                    comp.loggedIn = true;
                                }
                                else
                                {
                                    Debug.WriteLine("Error logging in: " + t.Result.Error);
                                    comp.log.Add(t.Result.ToString());
                                    comp.log.Add("Error logging in: " + t.Result.Error);
                                    comp.loggedIn = false;
                                }
                                t.Dispose();
                            });

                            // Update our login time.
                            Default.LastLoggedIn = DateTime.Now;

                            if (comp.loggedIn) comp.Message = "Logged In";
                            else comp.Message = "Error";
                        }
                        comp.ExpireSolution(true);

                        /*
                        Eto.Forms.Form dialog = new Eto.Forms.Form();
                        dialog.Size = new Eto.Drawing.Size(300, 300);

                        StackLayout buttonStack = new StackLayout();

                        Eto.Forms.Button b0 = new Eto.Forms.Button();
                        buttonStack.Items.Add(b0);

                        // Set the main content and options.
                        dialog.Content = buttonStack;

                        dialog.BackgroundColor = Eto.Drawing.Colors.SlateGray;
                        dialog.Maximizable = false;
                        dialog.Minimizable = false;
                        dialog.Topmost = true;

                        dialog.Show();
                        */

                        return GH_ObjectResponse.Handled;
                    }

                    if (SettingsBounds.Contains(e.CanvasLocation))
                    {
                        comp.RecordUndoEvent("SetSettings");
                        comp.log.Add("Clicked button to open settings dialog.");
                        comp.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
            #endregion

            #region Custom Render logic
            protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
            {
                switch (channel)
                {
                    case GH_CanvasChannel.Objects:
                        // We need to draw everything outselves.
                        base.RenderComponentCapsule(canvas, graphics, true, true, false, true, true, true);

                        Engine comp = Owner as Engine;

                        GH_Capsule buttonPath = GH_Capsule.CreateCapsule(PathBounds, GH_Palette.White);
                        buttonPath.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                        buttonPath.Dispose();

                        GH_Capsule buttonSettings = GH_Capsule.CreateCapsule(SettingsBounds, GH_Palette.White);
                        buttonSettings.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                        buttonSettings.Dispose();

                        graphics.DrawString("Login", GH_FontServer.Small, System.Drawing.Brushes.Black, PathBounds, GH_TextRenderingConstants.CenterCenter);
                        graphics.DrawString("Settings", GH_FontServer.Small, System.Drawing.Brushes.Black, SettingsBounds, GH_TextRenderingConstants.CenterCenter);

                        break;
                    default:
                        base.Render(canvas, graphics, channel);
                        break;
                }
            }
            #endregion
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("54b2cc2c-688d-4972-a234-2c9976d0a9f8"); }
        }
    }
}