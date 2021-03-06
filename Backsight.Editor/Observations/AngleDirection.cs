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


namespace Backsight.Editor.Observations
{
	/// <written by="Steve Stanton" on="06-NOV-1997" />
    /// <summary>
    /// An "angle" is an angle taken from a point, with respect to a backsight
    /// that provides the reference orientation.
    /// </summary>
    class AngleDirection : Direction, IFeatureRef
    {
        #region Class data

        /// <summary>
        /// The angle in radians. A negated value indicates an anticlockwise angle.
        /// </summary>
        RadianValue m_Observation;

        /// <summary>
        /// The backsight point.
        /// </summary>
        PointFeature m_Backsight;

        /// <summary>
        /// The occupied station.
        /// </summary>
        PointFeature m_From;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AngleDirection"/> class.
        /// </summary>
        internal AngleDirection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AngleDirection"/> class
        /// using the data read from persistent storage.
        /// </summary>
        /// <param name="editDeserializer">The mechanism for reading back content.</param>
        internal AngleDirection(EditDeserializer editDeserializer)
            : base(editDeserializer)
        {
            m_Backsight = editDeserializer.ReadFeatureRef<PointFeature>(this, DataField.Backsight);
            m_From = editDeserializer.ReadFeatureRef<PointFeature>(this, DataField.From);
            m_Observation = editDeserializer.ReadRadians(DataField.Value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AngleDirection"/> class.
        /// </summary>
        /// <param name="backsight">The backsight point.</param>
        /// <param name="occupied">The occupied station.</param>
        /// <param name="observation">The angle to an observed point, measured with respect
        /// to the reference orientation defined by the backsight. Positive values indicate
        /// a clockwise rotation & negated values for counter-clockwise.</param>
        internal AngleDirection(PointFeature backsight, PointFeature occupied, IAngle observation)
        {
            m_Backsight = backsight;
            m_From = occupied;
            m_Observation = new RadianValue(observation.Radians);
        }

        #endregion

        internal override double ObservationInRadians
        {
            get { return m_Observation.Radians; }
        }

        internal void SetObservationInRadians(double value)
        {
            m_Observation = new RadianValue(value);
        }

        /// <summary>
        /// The angle as a bearing
        /// </summary>
        internal override IAngle Bearing
        {
            get
            {
                // Get the bearing to the backsight
                double bb = Geom.BearingInRadians(m_From, m_Backsight);

                // Add on the observed angle, and restrict to [0,2*PI]
                double a = bb + m_Observation.Value;
                return new RadianValue(Direction.Normalize(a));
            }
        }

        internal override PointFeature From
        {
            get { return m_From; }
            set { m_From = value; }
        }

        internal PointFeature Backsight
        {
            get { return m_Backsight; }
            set { m_Backsight = value; }
        }

        /// <summary>
        /// Obtains the features that are referenced by this operation (including features
        /// that are indirectly referenced by observation classes).
        /// </summary>
        /// <returns>The referenced features (never null, but may be an empty array).</returns>
        internal override Feature[] GetReferences()
        {
            List<Feature> result = new List<Feature>(base.GetReferences());

            if (m_From != null)
                result.Add(m_From);

            if (m_Backsight != null)
                result.Add(m_Backsight);

            return result.ToArray();
        }

        /// <summary>
        /// Cuts references to an operation that are made by the features this
        /// direction refers to.
        /// </summary>
        /// <param name="op">The operation that should no longer be referred to.</param>
        internal override void CutReferences(Operation op)
        {
            if (m_From!=null)
                m_From.CutOp(op);

            if (m_Backsight!=null)
                m_Backsight.CutOp(op);
        }

        /// <summary>
        /// Relational equality test. 
        /// </summary>
        /// <param name="that">The angle to compare with</param>
        /// <returns>True if the supplied angle has the same from-point & backsight,
        /// and the same observed value.</returns>
        public bool Equals(AngleDirection that)
        {
            if (that==null)
                return false;

            return (Object.ReferenceEquals(this.m_From, that.m_From) &&
                    Object.ReferenceEquals(this.m_Backsight, that.m_Backsight) &&
                    Math.Abs(this.m_Observation.Value - that.m_Observation.Value) < Constants.TINY);
        }

        /// <summary>
        /// Performs actions when the operation that uses this observation is marked
        /// for deletion as part of its rollback function. This cuts any reference from any
        /// previously existing feature that was cross-referenced to the operation (see
        /// calls made to AddOp).
        /// </summary>
        /// <param name="op">The operation that makes use of this observation.</param>
        internal override void OnRollback(Operation op)
        {
            CutReferences(op);
            base.OnRollback(op);
        }

        /// <summary>
        /// Checks whether this observation makes reference to a specific feature.
        /// </summary>
        /// <param name="feature">The feature to check for.</param>
        /// <returns>True if this direction refers to the feature</returns>
        internal override bool HasReference(Feature feature)
        {
            if (Object.ReferenceEquals(m_From, feature) ||
                Object.ReferenceEquals(m_Backsight, feature))
                return true;

            return base.HasReference(feature);
        }

        /// <summary>
        /// Writes the content of this instance to a persistent storage area.
        /// </summary>
        /// <param name="editSerializer">The mechanism for storing content.</param>
        public override void WriteData(EditSerializer editSerializer)
        {
            base.WriteData(editSerializer);

            editSerializer.WriteFeatureRef<PointFeature>(DataField.Backsight, m_Backsight);
            editSerializer.WriteFeatureRef<PointFeature>(DataField.From, m_From);
            editSerializer.WriteRadians(DataField.Value, m_Observation);
        }

        /// <summary>
        /// Ensures that a persistent field has been associated with a spatial feature.
        /// </summary>
        /// <param name="field">A tag associated with the item</param>
        /// <param name="feature">The feature to assign to the field (not null).</param>
        /// <returns>
        /// True if a matching field was processed. False if the field is not known to this
        /// class (may be known to another class in the type hierarchy).
        /// </returns>
        public bool ApplyFeatureRef(DataField field, Feature feature)
        {
            switch (field)
            {
                case DataField.Backsight:
                    m_Backsight = (PointFeature)feature;
                    return true;

                case DataField.From:
                    m_From = (PointFeature)feature;
                    return true;

            }

            return false;
        }
    }
}
