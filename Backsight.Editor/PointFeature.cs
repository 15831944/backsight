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
using System.Text;

using Backsight.Environment;
using Backsight.Geometry;

namespace Backsight.Editor
{
    /// <written by="Steve Stanton" on="13-JUL-1997" />
    /// <summary>
    /// A point feature (e.g. control point, any sort of computed point). A point feature must
    /// exist at both ends of every <see cref="LineFeature"/>.
    /// </summary>
    class PointFeature : Feature, IPoint, ITerminal, IPersistent
    {
        #region Class data

        /// <summary>
        /// The geometry for this point. Could conceivably be shared by more
        /// than one point (although it is expected that 99% of points will not
        /// be shared). This value could be null while in the process of deserializing
        /// data.
        /// </summary>
        Node m_Geom;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <c>PointFeature</c> with geometry that isn't shared
        /// with any other point.
        /// </summary>
        /// <param name="creator">The operation that created the feature (not null)</param>
        /// <param name="id">The internal ID of this feature within the project that created it.</param>
        /// <param name="e">The entity type for the feature (not null)</param>
        /// <param name="g">The geometry for the point (may be null)</param>
        internal PointFeature(Operation creator, InternalIdValue id, IEntity e, PointGeometry g)
            : base(creator, id, e, null)
        {
            if (g == null)
                m_Geom = null;
            else
                m_Geom = new Node(this, g);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointFeature"/> class (with geometry that
        /// isn't shared with any other point), and records it as part of the map model.
        /// </summary>
        /// <param name="f">Basic information about the feature.</param>
        /// <param name="g">The geometry for the point (may be null)</param>
        internal PointFeature(IFeature f, PointGeometry g)
            : base(f)
        {
            if (g == null)
                m_Geom = null;
            else
                m_Geom = new Node(this, g);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointFeature"/> class
        /// using the data read from persistent storage.
        /// </summary>
        /// <param name="editDeserializer">The mechanism for reading back content.</param>
        internal PointFeature(EditDeserializer editDeserializer)
            : base(editDeserializer)
        {
            long x, y;
            ReadData(editDeserializer, out x, out y);
            m_Geom = new Node(this, new PointGeometry(x, y));
        }

        #endregion

        public double X
        {
            get { return m_Geom.X; }
        }

        public double Y
        {
            get { return m_Geom.Y; }
        }

        public bool IsAt(IPosition p, double tol)
        {
            return Position.IsCoincident(this, p, tol);
        }

        public bool IsCoincident(PointFeature p)
        {
            return m_Geom.IsCoincident(p.Geometry);
        }

        public override SpatialType SpatialType
        {
            get { return SpatialType.Point; }
        }

        /// <summary>
        /// Try to find an attached circle that has a specific radius.
        ///	This method is intended for locations that appear at the center
        ///	of a circle.
        /// </summary>
        /// <param name="radius">The radius to search for (0 or less to
        /// search for ANY circle). A fixed tolerance of 0.001 meters on
        /// the ground is used.</param>
        /// <returns>The attached circle (null if no such circle).</returns>
        internal Circle GetCircle(double radius)
        {
            List<IFeatureDependent> deps = this.Dependents;
            if (deps == null)
                return null;

            foreach (IFeatureDependent d in deps)
            {
                if (d is Circle)
                {
                    Circle c = (d as Circle);
                    if (radius<Double.Epsilon)
                        return c;

                    if (Math.Abs(c.Radius - radius) < 0.001)
                        return c;
                }
            }

            return null;
        }

        /// <summary>
        /// The circles centered at this point
        /// </summary>
        /// <returns>The circles with a center that coincides with this point</returns>
        internal Circle[] GetCircles()
        {
            List<Circle> result = new List<Circle>();

            foreach (IFeatureDependent d in Dependents)
            {
                if (d is Circle)
                    result.Add((Circle)d);
            }

            return result.ToArray();
        }

        /// <summary>
        /// The geometry defining the position of this feature.
        /// </summary>
        public IPointGeometry Geometry // IPoint
        {
            get { return m_Geom; }
        }

        /// <summary>
        /// The geometry defining the position of this feature.
        /// </summary>
        internal PointGeometry PointGeometry
        {
            get { return m_Geom; }
        }

        /// <summary>
        /// Defines the position of this point as a new un-shared position.
        /// </summary>
        /// <param name="ctx">The context in which the assignment is being made. May be null, but do
        /// so with care - an editing context is vital when dealing with the propagation of updates.
        /// </param>
        /// <param name="value">The position to assign (not null).</param>
        internal void ApplyPointGeometry(EditingContext ctx, PointGeometry value)
        {
            if (value == null)
                throw new ArgumentNullException();

            if (ctx != null)
                ctx.RegisterChange(this);

            m_Geom = new Node(this, value);
        }

        /// <summary>
        /// The geometry defining the position of this feature.
        /// </summary>
        internal Node Node
        {
            get { return m_Geom; }
            //set { m_Geom = value; }
        }

        /// <summary>
        /// Defines the position of this point to refer to an existing node.
        /// </summary>
        /// <param name="value">The position to assign</param>
        internal void SetNode(Node value)
        {
            m_Geom = value;
        }

        public override void Render(ISpatialDisplay display, IDrawStyle style)
        {
            if (!IsTrimPoint())
                m_Geom.Render(display, style);
        }

        /// <summary>
        /// Does this point occur at the end of a trimmed dangling line?
        /// </summary>
        bool IsTrimPoint()
        {
            // The point has to be marked as trimmed
            if (!IsTrimmed)
                return false;

            // There has to be exactly one incident boundary
            IDivider[] divs = IncidentDividers();
            if (divs.Length!=1)
                return false;

            // The divider must be invisible
            return !divs[0].IsVisible;
        }

        public override IWindow Extent
        {
            get { return m_Geom.Extent; }
        }

        public override ILength Distance(IPosition point)
        {
            return m_Geom.Distance(point);
        }

        public bool IsCoincident(IPointGeometry p)
        {
            return m_Geom.IsCoincident(p);
        }

        public ILength Easting
        {
            get { return m_Geom.Easting; }
        }

        public ILength Northing
        {
            get { return m_Geom.Northing; }
        }

        public IDivider[] IncidentDividers()
        {
            List<IDivider> result = new List<IDivider>(4); // 4 is frequently the number in the result

            foreach (IFeatureDependent fd in Dependents)
            {
                if (fd is LineFeature)
                {
                    LineFeature line = (fd as LineFeature);
                    Topology t = line.Topology;

                    if (t!=null)
                    {
                        // Could have a situation where BOTH start and end of the line end
                        // at the same point (e.g. a multisegment that loops back on itself).

                        bool sMatch = line.StartPoint.IsCoincident(this);
                        bool eMatch = line.EndPoint.IsCoincident(this);

                        if (sMatch)
                        {
                            IDivider d = t.FirstDivider;
                            if (!d.IsOverlap)
                                result.Add(d);
                        }

                        if (eMatch)
                        {
                            // The result could conceivably contain the divider already (case where a multi-segment
                            // closes on itself)
                            IDivider d = t.LastDivider;
                            if (!d.IsOverlap && !result.Contains(d))
                                result.Add(d);
                        }

                        if (!(sMatch || eMatch))
                        {
                            // Cover a situation where this point is referenced to a line that
                            // passes through (intersection).
                            Debug.Assert(t is SectionTopologyList);
                            SectionTopologyList sections = (t as SectionTopologyList);
                            sections.AddIncidentDividers(result, this);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns formatted position of this point.
        /// </summary>
        /// <returns>Formatted position of this point.</returns>
        public override string ToString()
        {
            return String.Format("{0} {1}", InternalId, m_Geom.ToString());
        }

        /// <summary>
        /// Touches this feature for rollforward preview.
        /// </summary>
        /// <param name="afterOp">The edit causing the change (only edits that were performed after this
        /// edit are considered relevant).</param>
        /// <remarks>The <see cref="PointFeature"/> class overrides</remarks>
        internal override void Touch(Operation afterOp)
        {
            List<IFeatureDependent> deps = base.Dependents;
            if (deps != null)
            {
                foreach (IFeatureDependent fd in deps)
                {
                    // If it's an incident line, touch it. This covers things like
                    // NewLineOperation, which make use of existing points, but
                    // which do not reference the terminals to the op (perhaps it
                    // should).
                    LineFeature line = (fd as LineFeature);
                    if (line != null)
                        line.Touch(afterOp);
                }

                base.Touch(afterOp);
            }
        }

        /// <summary>
        /// Ensures this feature is clean after some sort of edit. If this point has been marked inactive,
        /// any incident topological sections will be merged.
        /// </summary>
        internal override void Clean()
        {
            if (IsInactive)
            {
                // If any topological lines pass THROUGH this point, ensure they have
                // been marked as moved (force recalculation of intersections).

                IDivider[] dividers = this.IncidentDividers();

                foreach (IDivider d in dividers)
                {
                    if (d is SectionDivider)
                    {
                        LineFeature line = d.Line;
                        if (!line.IsInactive && line.StartPoint != this && line.EndPoint != this)
                            line.ResetTopology();
                    }
                }
            }

            base.Clean();
        }

        /*
//	@mfunc	Create transient primitives (CeMiscText objects, and
//			optionally CeSegment objects) that correspond
//			to any angles associated with this point.
//
//			The caller is responsible for deleting the primitives
//			once done with them.
//
//	@parm	List of pointers to created primitives.
//	@parm	Should lines be produced too? NOT IMPLEMENTED.
//
//	@rdesc	The number of primitives that were created (if any).
//
//////////////////////////////////////////////////////////////////////

#include "CeMiscText.h"

UINT4 CePoint::CreateAngleText ( CPtrList& prims
							   , const LOGICAL wantLinesToo ) const {

	// Get the creating op to create any angle text for
	// this point.
	CeOperation* pop = GetpCreator();
	if ( pop ) pop->CreateAngleText(prims,wantLinesToo,this);

	// Get the location to do the same for any operations that
	// utilize this point as part of an angular observation.
	m_pLocation->CreateAngleText(prims,wantLinesToo,this);

	// Return the number of primitives in the list.
	return prims.GetCount();

} // end of CreateAngleText
         */

        /// <summary>
        /// Writes the content of this instance to a persistent storage area.
        /// </summary>
        /// <param name="editSerializer">The mechanism for storing content.</param>
        public override void WriteData(EditSerializer editSerializer)
        {
            base.WriteData(editSerializer);
            editSerializer.WritePointGeometry(DataField.X, DataField.Y, m_Geom);
        }

        /// <summary>
        /// Reads data that was previously written using <see cref="WriteData"/>
        /// </summary>
        /// <param name="editDeserializer">The mechanism for reading back content.</param>
        /// <param name="line">The line the point should appear on </param>
        /// <param name="positionRatio">The position ratio of the attached point.</param>
        /// <param name="point">The point that was created.</param>
        static void ReadData(EditDeserializer editDeserializer, out long x, out long y)
        {
            x = editDeserializer.ReadInt64(DataField.X);
            y = editDeserializer.ReadInt64(DataField.Y);
        }
    }
}
