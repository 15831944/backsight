﻿// <remarks>
// Copyright 2012 - Steve Stanton. This file is part of Backsight
//
// Backsight is free software; you can redistribute it and/or modify it under the terms
// of the GNU Lesser General Public License as published by the Free Software Foundation;
// either version 3 of the License, or (at your option) any later version.
//
// Backsight is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
// </remarks>

using System;
using System.Windows.Forms;

namespace Backsight.Editor.Forms
{
    /// <summary>
    /// Dialog for selecting a specific spatial feature (by entering it's internal ID), for use
    /// in debugging.
    /// </summary>
    public partial class FindByInternalIdForm : Form
    {
        public FindByInternalIdForm()
        {
            InitializeComponent();
        }

        private void findButton_Click(object sender, EventArgs e)
        {
            try
            {
                CadastralMapModel mapModel = CadastralMapModel.Current;
                string s = idTextBox.Text;
                uint idValue = UInt32.Parse(s);
                Feature f = mapModel.Find<Feature>(new InternalIdValue(idValue));
                if (f == null)
                {
                    MessageBox.Show("Cannot find feature with ID=" + idValue);
                    return;
                }

                EditingController.Current.Select(f);
                Position p = null;

                if (f is PointFeature)
                    p = new Position((f as PointFeature).PointGeometry);
                else if (f is LineFeature)
                    p = new Position((f as LineFeature).StartPoint);
                else if (f is TextFeature)
                    p = new Position((f as TextFeature).Position);

                if (p == null)
                {
                    MessageBox.Show("Cannot determine position for selected feature");
                    return;
                }

                ISpatialDisplay d = EditingController.Current.ActiveDisplay;

                if (d.MapScale > 2000.0)
                    d.MapScale = 2000.0;

                d.Center = p;

                this.DialogResult = DialogResult.Cancel;
                Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
