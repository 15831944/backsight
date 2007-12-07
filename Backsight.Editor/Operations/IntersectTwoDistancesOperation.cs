/// <remarks>
/// Copyright 2007 - Steve Stanton. This file is part of Backsight
///
/// Backsight is free software; you can redistribute it and/or modify it under the terms
/// of the GNU Lesser General Public License as published by the Free Software Foundation;
/// either version 3 of the License, or (at your option) any later version.
///
/// Backsight is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
/// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
/// See the GNU Lesser General Public License for more details.
///
/// You should have received a copy of the GNU Lesser General Public License
/// along with this program. If not, see <http://www.gnu.org/licenses/>.
/// </remarks>

using System;
using System.Collections.Generic;

using Backsight.Environment;
using Backsight.Geometry;

namespace Backsight.Editor.Operations
{
    /// <written by="Steve Stanton" on="10-JAN-1998" was="CeIntersectDist" />
    /// <summary>
    /// Create point (and optional lines) based on 2 distance observations.
    /// </summary>
    class IntersectTwoDistancesOperation : IntersectOperation
    {
        #region Class data

        /// <summary>
        /// First observed distance  (either a <see cref="Distance"/>, or an <see cref="OffsetPoint"/>).
        /// </summary>
        Observation m_Distance1;

        /// <summary>
        /// The point the 1st distance was measured from.
        /// </summary>
        PointFeature m_From1;

        /// <summary>
        /// Second observed distance  (either a <see cref="Distance"/>, or an <see cref="OffsetPoint"/>).
        /// </summary>
        Observation m_Distance2;

        /// <summary>
        /// The point the 2nd distance was measured from.
        /// </summary>
        PointFeature m_From2;

        /// <summary>
        /// True if it was the default intersection (the one with the lowest bearing
        /// with respect to <see cref="m_From1"/> and <see cref="m_From2"/>).
        /// </summary>
        bool m_Default;

        // Creations ...

        /// <summary>
        /// The created intersection point (if any).
        /// </summary>
        PointFeature m_To;

        /// <summary>
        /// The first created line (if any)
        /// </summary>
        LineFeature m_Line1; // was m_pArc1

        /// <summary>
        /// The second created line (if any)
        /// </summary>
        LineFeature m_Line2; // was m_pArc2

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <c>IntersectTwoDistancesOperation</c> with everything set to null.
        /// </summary>
        internal IntersectTwoDistancesOperation()
        {
            m_Distance1 = null;
            m_From1 = null;
            m_Distance2 = null;
            m_From2 = null;
            m_Default = true;

            m_To = null;
            m_Line1 = null;
            m_Line2 = null;
        }

        #endregion

        /// <summary>
        /// The first observed distance (either a <see cref="Distance"/>, or an <see cref="OffsetPoint"/>).
        /// </summary>
        internal Observation Distance1
        {
            get { return m_Distance1; }
        }

        /// <summary>
        /// The second observed distance (either a <see cref="Distance"/>, or an <see cref="OffsetPoint"/>).
        /// </summary>
        internal Observation Distance2
        {
            get { return m_Distance2; }
        }

        /// <summary>
        /// The point the 1st distance was measured from.
        /// </summary>
        internal PointFeature Distance1FromPoint // was GetpFrom1
        {
            get { return m_From1; }
        }

        /// <summary>
        /// The point the 2nd distance was measured from.
        /// </summary>
        internal PointFeature Distance2FromPoint // was GetpFrom2
        {
            get { return m_From2; }
        }

        /// <summary>
        /// The first line created (if any).
        /// </summary>
        internal LineFeature CreatedLine1 // was GetpArc1
        {
            get { return m_Line1; }
        }

        /// <summary>
        /// The second line created (if any).
        /// </summary>
        internal LineFeature CreatedLine2 // was GetpArc2
        {
            get { return m_Line2; }
        }

        /// <summary>
        /// The point feature at the intersection created by this edit.
        /// </summary>
        internal override PointFeature IntersectionPoint
        {
            get { return m_To; }
        }

        /// <summary>
        /// Was the intersection created at it's default position?
        /// </summary>
        internal override bool IsDefault
        {
            get { return m_Default; }
        }

        /// <summary>
        /// A point feature that is close to the intersection (for use when relocating
        /// the intersection as part of rollforward processing). This implementation
        /// returns null.
        /// </summary>
        internal override PointFeature ClosePoint
        {
            get { return null; }
        }

        /// <summary>
        /// A user-perceived title for this operation.
        /// </summary>
        public override string Name
        {
            get { return "Distance-distance intersection"; }
        }

        /// <summary>
        /// Finds the observed length of a line that was created by this operation.
        /// </summary>
        /// <param name="line">The line to find</param>
        /// <returns>The observed length of the line (null if this operation doesn't
        /// reference the specified line)</returns>
        internal override Distance GetDistance(LineFeature line)
        {
            // If the distance-line is the one we're after, AND it was
            // defined as a distance (as opposed to an offset point),
            // return a reference to it.
            if (Object.ReferenceEquals(line, m_Line1))
                return (m_Distance1 as Distance);

            if (Object.ReferenceEquals(line, m_Line2))
                return (m_Distance2 as Distance);

            return null;
        }

        /// <summary>
        /// The features created by this editing operation.
        /// </summary>
        internal override Feature[] Features
        {
            get
            {
                List<Feature> result = new List<Feature>(3);

                if (m_To!=null)
                    result.Add(m_To);

                if (m_Line1!=null)
                    result.Add(m_Line1);

                if (m_Line2!=null)
                    result.Add(m_Line2);

                return result.ToArray();
            }
        }

        /// <summary>
        /// The unique identifier for this edit.
        /// </summary>
        internal override EditingActionId EditId
        {
            get { return EditingActionId.DistIntersect; }
        }

        /// <summary>
        /// Adds references to existing features referenced by this operation (including features
        /// that are indirectly referenced by observation classes).
        /// </summary>
        public override void AddReferences()
        {
            m_From1.AddOp(this);
            m_From2.AddOp(this);

            m_Distance1.AddReferences(this);
            m_Distance2.AddReferences(this);
        }

        /// <summary>
        /// Rollback this operation (occurs when a user undoes the last edit).
        /// </summary>
        /// <returns>True if operation was rolled back ok</returns>
        internal override bool Undo()
        {
            base.OnRollback();

            // Get rid of the observations.
            m_Distance1.OnRollback(this);
            m_Distance2.OnRollback(this);

            // Cut direct refs made by this operation.
            if (m_From1!=null)
                m_From1.CutOp(this);

            if (m_From2!=null)
                m_From2.CutOp(this);

            // Undo the intersect point and any connecting lines
            Rollback(m_To);
            Rollback(m_Line1);
            Rollback(m_Line2);

            return true;
        }

        /// <summary>
        /// Rollforward this edit in response to some sort of update.
        /// </summary>
        /// <returns>True if operation has been re-executed successfully</returns>
        internal override bool Rollforward()
        {
            // Return if this operation has not been marked as changed.
            if (!IsChanged)
                return base.OnRollforward();

            // Re-calculate the position of the point of intersection.
            IPosition xsect = Calculate(m_Distance1, m_From1, m_Distance2, m_From2, m_Default);

            if (xsect==null)
                throw new RollforwardException(this, "Cannot re-calculate intersection point.");

            // Update the intersection point to the new position.
            m_To.Move(xsect);

            // Rollforward the base class.
            return base.OnRollforward();
        }

        /// <summary>
        /// Executes this operation. 
        /// </summary>
        /// <param name="dist1">1st distance observation.</param>
        /// <param name="from1">The point the 1st distance was observed from.</param>
        /// <param name="dist2">2nd distance observation.</param>
        /// <param name="from2">The point the 2nd distance was observed from.</param>
        /// <param name="isdefault">True if the default intersection is required (the one that has the
        /// lowest bearing with respect to the 2 from points). False for the other one (if any).</param>
        /// <param name="pointId">The ID and entity type for the intersect point</param>
        /// <param name="ent1">The entity type for 1st line (null for no line)</param>
        /// <param name="ent2">The entity type for 2nd line (null for no line)</param>
        internal void Execute(Observation dist1, PointFeature from1, Observation dist2, PointFeature from2,
                                bool isdefault, IdHandle pointId, IEntity ent1, IEntity ent2)
        {
            // Calculate the position of the point of intersection.
            IPosition xsect = Calculate(dist1, from1, dist2, from2, isdefault);
            if (xsect==null)
                throw new Exception("Cannot calculate intersection point");

            // Add the intersection point
            m_To = AddIntersection(xsect, pointId);

            // Remember the from points.
            m_From1 = from1;
            m_From2 = from2;

            // Save option about whether we want default intersection or not.
            m_Default = isdefault;

            // If we have a defined entity types for lines, add them too.
            CadastralMapModel map = MapModel;

            if (ent1!=null)
                m_Line1 = map.AddLine(m_From1, m_To, ent1, this);

            if (ent2!=null)
                m_Line2 = map.AddLine(m_From2, m_To, ent2, this);

            // Peform standard completion steps
            Complete();
        }

        /// <summary>
        /// Calculates the intersection point.
        /// </summary>
        /// <param name="dist1">1st distance observation.</param>
        /// <param name="from1">The point the 1st distance was observed from.</param>
        /// <param name="dist2">2nd distance observation.</param>
        /// <param name="from2">The point the 2nd distance was observed from.</param>
        /// <param name="usedefault">True if the default intersection is required (the one that has the
        /// lowest bearing with respect to the 2 from points). False for the other one (if any).</param>
        /// <returns>The position of the intersection (null if it cannot be calculated).</returns>
        IPosition Calculate(Observation dist1, PointFeature from1, Observation dist2, PointFeature from2, bool usedefault)
        {
        	// Call the static function that is also used by the dialog.
            IPosition xsect, x1, x2;
            if (Calculate(dist1, from1, dist2, from2, usedefault, out xsect, out x1, out x2))
                return xsect;
            else
                return null;
        }

        /// <summary>
        /// Calculates intersection points.
        /// </summary>
        /// <param name="dist1">1st distance observation.</param>
        /// <param name="from1">The point the 1st distance was observed from.</param>
        /// <param name="dist2">2nd distance observation.</param>
        /// <param name="from2">The point the 2nd distance was observed from.</param>
        /// <param name="usedefault">True if the default intersection is required (the one that has the
        /// lowest bearing with respect to the 2 from points). False for the other one (if any).</param>
        /// <param name="xsect">The position of the intersection (if any).</param>
        /// <param name="xsect1">The 1st choice intersection (if any).</param>
        /// <param name="xsect2">The 2nd choice intersection (if any).</param>
        /// <returns>True if intersections were calculated. False if the distance circles
        /// don't intersect.</returns>
        /// <returns></returns>
        internal static bool Calculate(Observation dist1, PointFeature from1, Observation dist2, PointFeature from2, bool usedefault,
                                        out IPosition xsect, out IPosition xsect1, out IPosition xsect2)
        {
            // Initialize intersection positions.
            xsect = xsect1 = xsect2 = null;

            // Get the 2 distances.
            ILength d1 = dist1.GetDistance(from1);
            ILength d2 = dist2.GetDistance(from2);
            if (d1.Meters < Constants.TINY || d2.Meters < Constants.TINY)
                return false;

            // Form circles with radii that match the observed distances.
            ICircleGeometry circle1 = new CircleGeometry(from1, d1);
            ICircleGeometry circle2 = new CircleGeometry(from2, d2);

            // See if there is actually an intersection between the two circles.
            IPosition x1, x2;
            uint nx = IntersectionHelper.Intersect(circle1, circle2, out x1, out x2);
            if (nx==0)
                return false;

            // If we have 2 intersections, and we need the non-default one, pick up the 2nd
            // intersection. If only 1 intersection, use that, regardless of the setting for
            // the "use default" flag.

            if (nx==2 && !usedefault)
                xsect = x2;
            else
                xsect = x1;

            // Return if both distances are offset points.
            OffsetPoint offset1 = (dist1 as OffsetPoint);
            OffsetPoint offset2 = (dist2 as OffsetPoint);

            if (offset1!=null && offset2!=null)
            {
                xsect1 = x1;
                xsect2 = x2;
                return true;
            }

            // Reduce observed distances to the mapping plane.
            ICoordinateSystem sys = CadastralMapModel.Current.CoordinateSystem;

            if (offset1==null)
                d1 = new Length(d1.Meters * sys.GetLineScaleFactor(from1, xsect1));

            if (offset2!=null)
                d2 = new Length(d2.Meters * sys.GetLineScaleFactor(from2, xsect1));

            // And calculate the exact intersection (like above)...
            // Form circles with radii that match the observed distances.
            ICircleGeometry circle1p = new CircleGeometry(from1, d1);
            ICircleGeometry circle2p = new CircleGeometry(from2, d2);

            // See if there is still an intersection between the two circles.
            nx = IntersectionHelper.Intersect(circle1p, circle2p, out x1, out x2);
            if (nx==0)
                return false;

            // If we have 2 intersections, and we need the non-default one, pick up the 2nd
            // intersection. If only 1 intersection, use that, regardless of the setting for
            // the "use default" flag.

            if (nx==2 && !usedefault)
                xsect = x2;
            else
                xsect = x1;

	        xsect1 = x1;
	        xsect2 = x2;

	        return true;
        }

        /// <summary>
        /// Checks whether this operation makes reference to a specific feature.
        /// </summary>
        /// <param name="feat">The feature to check for.</param>
        /// <returns>True if this edit depends on (contains a reference to) the supplied feature</returns>
        bool HasReference(Feature feat)
        {
            if (Object.ReferenceEquals(m_From1, feat) ||
                Object.ReferenceEquals(m_From2, feat))
                return true;

            if (m_Distance1.HasReference(feat))
                return true;

            if (m_Distance2.HasReference(feat))
                return true;

            return false;
        }

        /// <summary>
        /// Updates this operation. 
        /// </summary>
        /// <param name="dist1">1st distance observation.</param>
        /// <param name="from1">The point the 1st distance was observed from.</param>
        /// <param name="dist2">2nd distance observation.</param>
        /// <param name="from2">The point the 2nd distance was observed from.</param>
        /// <param name="isdefault">True if the default intersection is required (the one that has the
        /// lowest bearing with respect to the 2 from points). False for the other one (if any).</param>
        /// <param name="ent1">The entity type for 1st line (null for no line)</param>
        /// <param name="ent2">The entity type for 2nd line (null for no line)</param>
        /// <returns></returns>
        bool Correct(Observation dist1, PointFeature from1, Observation dist2, PointFeature from2,
                        bool isdefault, IEntity ent1, IEntity ent2)
        {
            if ((ent1==null && m_Line1!=null) || (ent2==null && m_Line2!=null))
                throw new Exception("You cannot delete lines via update. Use Line Delete.");

            // Calculate the position of the point of intersection.
            IPosition xsect = Calculate(dist1, from1, dist2, from2, isdefault);
            if (xsect==null)
                return false;

            // If the from points have changed, cut references to this
            // operation from the old points, and change it so the
            // operation is referenced from the new points.
            if (!Object.ReferenceEquals(m_From1, from1))
            {
                m_From1.CutOp(this);
                m_From1 = from1;
                m_From1.AddOp(this);
            }

            if (!Object.ReferenceEquals(m_From2, from2))
            {
                m_From2.CutOp(this);
                m_From2 = from2;
                m_From2.AddOp(this);
            }

            // If either old observation refers to an offset point, cut the
            // reference that the point has to this op. If nothing has
            // changed, the reference will be re-inserted when the
            // observation is re-saved below.
            CutOffsetRef(m_Distance1);
            CutOffsetRef(m_Distance2);

            // Get rid of the previously defined observations, and replace
            // with the new ones (we can't necessarily change the old ones
            // because we may have changed the type of observation).

            m_Distance1.OnRollback(this);
            m_Distance2.OnRollback(this);

            m_Distance1 = dist1;
            m_Distance1.AddReferences(this);

            m_Distance2 = dist2;
            m_Distance2.AddReferences(this);

            // Save option about whether we want default intersection or not.
            m_Default = isdefault;

            // If we have defined entity types for lines, and we did not
            // have a line before, add a new line now.

            if (ent1!=null)
            {
                if (m_Line1==null)
                    m_Line1 = MapModel.AddLine(m_From1, m_To, ent1, this); // m_To hasn't moved yet!
                else if (m_Line1.EntityType.Id != ent1.Id)
                    throw new NotImplementedException("IntersectTwoDistancesOperation.Correct");
                    //m_Line1.EntityType = ent1;
            }

            if (ent2!=null)
            {
                if (m_Line2==null)
                    m_Line2 = MapModel.AddLine(m_From2, m_To, ent2, this); // m_To hasn't moved yet!
                else if (m_Line2.EntityType.Id != ent2.Id)
                    throw new NotImplementedException("IntersectTwoDistancesOperation.Correct");
                    //m_Line2.EntityType = ent2;
            }

            return true;
        }

        /// <summary>
        /// Cuts any reference made to this operation for an observed distance that was
        /// specified using an <see cref="OffsetPoint"/> object.
        /// </summary>
        /// <param name="dist">The distance observation. If this is not an <c>OffsetPoint</c>
        /// object, this function does nothing.</param>
        void CutOffsetRef(Observation dist)
        {
            // Return if the distance observation is not for an offset point
            OffsetPoint off = (dist as OffsetPoint);
            if (off==null)
                return;

            // Get the offset point and cut the reference it has to this operation.
            PointFeature point = off.Point;
            if (point!=null)
                point.CutOp(this);
        }
    }
}