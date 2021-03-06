// <remarks>
// Copyright 2007 - Steve Stanton. This file is part of Backsight
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
using System.Collections.Generic;
using System.Diagnostics;

using Backsight.Geometry;

namespace Backsight.Editor
{
    /// <written by="Steve Stanton" on="21-AUG-2007" />
    /// <summary>
    /// The exterior edge of an island polygon (sometimes refered to as a "phantom polygon").
    /// </summary>
    class Island : Ring
    {
        #region Class data

        /// <summary>
        /// The enclosing polygon (if known). A null value either means the enclosing polygon
        /// still has to be determined, or the island really has no enclosing polygon. In the
        /// latter case, the <c>RingFlag.Floating</c> flag bit will be set.
        /// </summary>
        Polygon m_Container;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <c>Island</c> polygon.
        /// </summary>
        /// <param name="rm">The metrics for this island</param>
        /// <param name="edge">The boundaries that define the exterior edge of the island,
        /// arranged in a (counter?)-clockwise cycle.</param>
        internal Island(RingMetrics rm, List<Face> edge)
            : base(rm, edge)
        {
            Debug.Assert(rm.SignedArea <= 0.0);
            m_Container = null;
        }

        #endregion

        /// <summary>
        /// Is this island floating in space (without any enclosing polygon)?
        /// </summary>
        internal bool IsFloating
        {
            get { return IsFlagSet(RingFlag.Floating); }
            set { SetFlag(RingFlag.Floating, value); }
        }

        /// <summary>
        /// Finds the enclosing polygon for this island. If the island does have an
        /// enclosing polygon, the polygon is modified to refer to it, and the enclosing
        /// area is reduced to take account of the island. It is also possible that the
        /// island may not have ANY enclosing polygons, in which case the island will be
        /// marked as "floating".
        /// </summary>
        /// <returns>True if built ok.</returns>
        internal void SetContainer()
        {
            if (m_Container!=null)
                return;

            if (IsFloating)
                return;

            // Attempt to locate an enclosing polygon.
            ISpatialIndex index = CadastralMapModel.Current.Index;
            Polygon enc = new FindIslandContainerQuery(index, this).Result;

            // If there aren't any, just mark this polygon as floating. Otherwise
            // ensure the container knows about this island.
            if (enc==null)
                IsFloating = true;
            else
                enc.ClaimIsland(this);
        }

        /// <summary>
        /// The area of this island, in square meters on the mapping projection. The stored area
        /// of an island is less than zero. This override negates the value so you should get back
        /// an area greater than zero.
        /// </summary>
        public override double Area
        {
            get { return -base.Area; }
        }

        /// <summary>
        /// The enclosing polygon (if known). A null value either means the enclosing polygon
        /// still has to be determined, or the island really has no enclosing polygon (use the
        /// <c>IsFloating</c> property to determine what null signifies).
        /// </summary>
        /// <remarks>Setting the container to a not-null value will clear the <c>IsFloating</c> property.
        /// However, setting the container to null does not clear the <c>IsFloating</c> property.
        /// </remarks>
        internal Polygon Container
        {
            get { return m_Container; }
            set
            {
                m_Container = value;
                if (value!=null)
                    IsFloating = false;
            }
        }

        /// <summary>
        /// Draws the outline (the exterior edge) of this island.
        /// </summary>
        /// <param name="display">The display to draw to</param>
        /// <param name="style">The drawing style</param>
        public override void Render(ISpatialDisplay display, IDrawStyle style)
        {
            RenderOutline(display, style);
        }

        public override string ToString()
        {
            if (m_Container==null)
                return base.ToString() + "->?";
            else
                return base.ToString() + "->" + m_Container.ToString();
        }

        /// <summary>
        /// Ensures this island is clean after some sort of edit. If this island has been marked
        /// for deletion, the enclosing polygon will be told to release this island.
        /// Then <see cref="Ring.Clean"/> will be called.
        /// </summary>
        internal override void Clean()
        {
            // If this polygon has been marked for deletion, notify any enclosing polygon.
            if (IsDeleted)
            {
                if (m_Container!=null)
                    m_Container.Release(this);
            }

            base.Clean();
        }

        /// <summary>
        /// The user-perceived polygon associated with this ring is the polygon that
        /// encloses this island, as returned by the <see cref="Container"/> property.
        /// </summary>
        internal override Polygon RealPolygon
        {
            get { return this.Container; }
        }
    }
}
