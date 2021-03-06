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


namespace Backsight.Editor.Observations
{
	/// <written by="Steve Stanton" on="23-OCT-1997" />
    /// <summary>
    /// A distance observation.
    /// </summary>
    class Distance : Observation, ILength, IEquatable<Distance>
    {
        #region Static

        /// <summary>
        /// Attempts to parse the supplied string.
        /// </summary>
        /// <param name="s">The string to parse</param>
        /// <param name="d">The result of the parse attempt (null if the string cannot
        /// be parsed as a distance)</param>
        /// <returns>True if <paramref name="d"/> was successfully defined</returns>
        internal static bool TryParse(string s, out Distance d)
        {
            Distance t = new Distance(s);
            d = (t.IsDefined ? t : null);
            return t.IsDefined;
        }

        #endregion

        #region Class data

        /// <summary>
        /// The way the distance was originally specified by the user.
        /// </summary>
        DistanceUnit m_EnteredUnit;

        /// <summary>
        /// Observed distance, in meters on the ground.
        /// </summary>
        double m_ObservedMetric;

        /// <summary>
        /// Is the distance fixed? (if so, the distance cannot be adjusted in any way).
        /// </summary>
        bool m_IsFixed;

        /// <summary>
        /// Should any annotation of this distance be drawn on non-standard side of an
        /// associated line feature?
        /// </summary>
        internal bool IsAnnotationFlipped { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Distance"/> class
        /// with a distance of zero (and undefined units).
        /// </summary>
        internal Distance()
        {
            m_EnteredUnit = null;
            m_ObservedMetric = 0.0;
            m_IsFixed = false;
            IsAnnotationFlipped = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Distance"/> class
        /// using the data read from persistent storage.
        /// </summary>
        /// <param name="editDeserializer">The mechanism for reading back content.</param>
        internal Distance(EditDeserializer editDeserializer)
        {
            double distance = editDeserializer.ReadDouble(DataField.Value);
            DistanceUnitType unitType = (DistanceUnitType)editDeserializer.ReadByte(DataField.Unit);
            m_EnteredUnit = EditingController.GetUnits(unitType);
            m_ObservedMetric = m_EnteredUnit.ToMetric(distance);

            if (editDeserializer.IsNextField(DataField.Fixed))
                m_IsFixed = editDeserializer.ReadBool(DataField.Fixed);
            else
                m_IsFixed = false;

            if (editDeserializer.IsNextField(DataField.Flipped))
                IsAnnotationFlipped = editDeserializer.ReadBool(DataField.Flipped);
            else
                IsAnnotationFlipped = false;
        }

        /// <summary>
        /// Creates a distance (regarded as non-fixed)
        /// </summary>
        /// <param name="distance">The entered distance value</param>
        /// <param name="unit">The units for the entered distance.</param>
        internal Distance(double distance, DistanceUnit unit)
        {
            m_ObservedMetric = unit.ToMetric(distance);
            m_EnteredUnit = unit;
            m_IsFixed = false;
            IsAnnotationFlipped = false;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy">The distance to copy.</param>
        internal Distance(Distance copy)
        {
            m_ObservedMetric = copy.m_ObservedMetric;
            m_EnteredUnit = copy.m_EnteredUnit;
            m_IsFixed = copy.m_IsFixed;
            IsAnnotationFlipped = copy.IsAnnotationFlipped;
        }

        /// <summary>
        /// Constructor that accepts a string. Use the <c>IsDefined</c> property to check
        /// whether the string was parsed ok. Also see <see cref="TryParse"/>.
        /// </summary>
        /// <param name="s">The string to parse. It should look like a floating
        ///	point number, but may have a units abbreviation stuck on the end (like that
        ///	produced by <c>Distance.Format</c>).</param>
        ///	<param name="defaultEntryUnit">The default units</param>
        internal Distance(string str, DistanceUnit defaultEntryUnit)
        {
            // Initialize with undefined values.
            m_ObservedMetric = 0.0;
            m_EnteredUnit = null;
            m_IsFixed = false;
            IsAnnotationFlipped = false;

            // Ignore any trailing white space. Return if it's ALL white space.
            string s = str.Trim();
            if (s.Length == 0)
                return;

            // Split the string into a numeric & abbreviation part
            string num, abbr;
            ParseDistanceString(str, out num, out abbr);

            // Try to convert the entered string to a float.
            double dval;
            if (!Double.TryParse(num, out dval))
                return;

            // If the abbreviation corresponds to some form a data entry, save the
            // entered distance in those. Otherwise save in the supplied data entry units.
            if (abbr.Length > 0)
            {
                EditingController ec = EditingController.Current;
                m_EnteredUnit = ec.GetUnit(abbr);
                if (m_EnteredUnit != null)
                    m_ObservedMetric = m_EnteredUnit.ToMetric(dval);
            }
            else
            {
                m_ObservedMetric = defaultEntryUnit.ToMetric(dval);
                m_EnteredUnit = defaultEntryUnit;
            }
        }

        /// <summary>
        /// Constructor that accepts a string. Use the <c>IsDefined</c> property to check
        /// whether the string was parsed ok. Also see <see cref="TryParse"/>.
        /// </summary>
        /// <param name="s">The string to parse. It should look like a floating
        ///	point number, but may have a units abbreviation stuck on the end (like that
        ///	produced by a call to <see cref="Format"/>).</param>
        [Obsolete("Use the version that accepts the default entry unit")]
        internal Distance(string str)
            : this(str, EditingController.Current.EntryUnit)
        {
        }

        #endregion

        /// <summary>
        /// Breaks a distance string into two parts (the numeric part, and optional abbreviation).
        /// </summary>
        /// <param name="s">The string to parse</param>
        /// <param name="num">The numeric part (the supplied string in a situation where
        /// none of the characters are letters)</param>
        /// <param name="abbr">The abbreviation (empty if an abbreviation is not present)</param>
        void ParseDistanceString(string s, out string num, out string abbr)
        {
            // Working back from the end of the string, look for the first
            // character that isn't a letter (the numeric bit runs to there).

            for (int index=s.Length-1; index>=0; index--)
            {
                if (!Char.IsLetter(s[index]))
                {
                    num = s.Substring(0, index+1);
                    abbr = (index==s.Length-1 ? String.Empty : s.Substring(index+1));
                    return;
                }
            }

            num = s;
            abbr = String.Empty;
        }

        /// <summary>
        /// Is the distance fixed? (if so, the distance cannot be adjusted in any way).
        /// </summary>
        internal bool IsFixed
        {
            get { return m_IsFixed; }
        }

        /// <summary>
        /// Marks this as a fixed distance (not to be adjusted).
        /// </summary>
        internal void SetFixed()
        {
            m_IsFixed = true;
        }

        /// <summary>
        /// Has this distance been defined properly? (meaning the value
        /// for <c>EntryUnit</c> is not null).
        /// </summary>
        internal bool IsDefined
        {
            get { return (m_EnteredUnit!=null); }
        }

        /// <summary>
        /// The way the distance was originally specified by the user.
        /// </summary>
        internal DistanceUnit EntryUnit
        {
            get { return m_EnteredUnit; }
        }

        /// <summary>
        /// Observed distance, in meters on the ground.
        /// </summary>
        public double Meters
        {
            get { return m_ObservedMetric; }
        }

        /// <summary>
        /// Observed distance, in microns on the ground.
        /// </summary>
        public long Microns
        {
            get { return Backsight.Length.ToMicrons(m_ObservedMetric); }
        }

        /// <summary>
        /// Formats this distance in a specific unit of measurement.
        /// </summary>
        /// <param name="unit">The desired unit of measurement.</param>
        /// <param name="appendAbbrev">True if units abbreviation should be appended (default was TRUE)</param>
        /// <returns></returns>
        internal string Format(DistanceUnit unit, bool appendAbbrev)
        {
            return unit.Format(m_ObservedMetric, appendAbbrev);
        }

        /// <summary>
        /// Formats this distance in units that correspond to the original data entry unit
        /// (with units abbreviation appended).
        /// </summary>
        /// <returns>The formatted distance</returns>
        internal string Format()
        {
            return Format(true);
        }

        /// <summary>
        /// Formats this distance in units that correspond to the original data entry unit.
        /// </summary>
        /// <param name="appendAbbrev">True if units abbreviation should be appended.</param>
        /// <returns></returns>
        internal string Format(bool appendAbbrev)
        {
            return (m_EnteredUnit==null ?
                String.Empty : m_EnteredUnit.Format(m_ObservedMetric, appendAbbrev));
        }

        /// <summary>
        /// Returns this distance in a specific type of unit.
        /// </summary>
        /// <param name="unit">The desired unit of measurement.</param>
        /// <returns></returns>
        double GetDistance(DistanceUnit unit)
        {
            return unit.FromMetric(m_ObservedMetric);
        }

        /// <summary>
        /// Returns the distance in the way it was originally entered. 
        /// </summary>
        /// <returns></returns>
        double GetDistance()
        {
            return (m_EnteredUnit==null ? 0.0 : m_EnteredUnit.FromMetric(m_ObservedMetric));
        }

        /// <summary>
        /// Ensures that the stored distance is greater than 0. 
        /// </summary>
        /// <returns>True if the sign of the distance was changed.</returns>
        internal bool SetPositive()
        {
            if (m_ObservedMetric < 0.0)
            {
                m_ObservedMetric = -m_ObservedMetric;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Ensures that the stored distance is less than 0. 
        /// </summary>
        /// <returns>True if the sign of the distance was changed.</returns>
        internal bool SetNegative()
        {
            if (m_ObservedMetric > 0.0)
            {
                m_ObservedMetric = -m_ObservedMetric;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the equivalent distance on the mapping plane. 
        /// </summary>
        /// <param name="from">The position the distance is measured from.</param>
        /// <param name="bearing">The bearing for the distance, in radians.</param>
        /// <param name="sys">The mapping system</param>
        /// <returns>The distance on the mapping plane.</returns>
        internal double GetPlanarMetric(IPosition from, double bearing, ISpatialSystem sys)
        {
        	// Return zero if this distance is undefined.
	        if (!this.IsDefined)
                return 0.0;

            // Calculate approximation for the terminal position (treating
	        // this distance as a planar distance).
            IPosition to = Geom.Polar(from, bearing, m_ObservedMetric);

            // Use the approximate location to determine line scale factor
            double sfac = sys.GetLineScaleFactor(from,to);
        	return (m_ObservedMetric * sfac);
        }

        /// <summary>
        /// Retruns the equivalent distance on the mapping plane. 
        /// </summary>
        /// <param name="from">The position the distance is measured from.</param>
        /// <param name="to">The approximate end position.</param>
        /// <param name="sys">The mapping system</param>
        /// <returns>The distance on the mapping plane.</returns>
        internal double GetPlanarMetric(IPosition from, IPosition to, ISpatialSystem sys)
        {
	        if (!this.IsDefined)
                return 0.0;

            double sfac = sys.GetLineScaleFactor(from,to);
        	return (m_ObservedMetric * sfac);
        }

        /// <summary>
        /// Checks whether this observation makes reference to a specific feature.
        /// </summary>
        /// <param name="feature">The feature to check for.</param>
        /// <returns>False (always)</returns>
        internal override bool HasReference(Feature feat)
        {
            return false;
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
            // Nothing to do
        }

        /// <summary>
        /// Relational equality test
        /// </summary>
        /// <param name="that">The distance to compare with.</param>
        /// <returns>True if the distance values are the same, and the distances are
        /// either both fixed or both floating</returns>
        /// <seealso cref="IsIdentical"/>
        public bool Equals(Distance that) // IEquatable<Distance>
        {
            return (this.IsFixed == that.IsFixed &&
                    Math.Abs(this.Meters - that.Meters) < Constants.TINY);
        }

        /// <summary>
        /// Is this <see cref="Distance"/> identical to the supplied one (the distance involved (including
        /// it's units), and attributes to say the distance is fixed, and annotation flipped).
        /// </summary>
        /// <param name="that">The distance to compare with</param>
        /// <returns>True if the supplied distance is identical</returns>
        /// <remarks>This should really be done by <see cref="Equals"/>, but I'm not confident that
        /// it's usage is consistent with a more thorough equality test.</remarks>
        internal bool IsIdentical(Distance that)
        {
            return (this.m_IsFixed == that.m_IsFixed &&
                    this.m_EnteredUnit == that.m_EnteredUnit &&
                    this.IsAnnotationFlipped == that.IsAnnotationFlipped &&
                    Math.Abs(this.m_ObservedMetric - that.m_ObservedMetric) < Constants.TINY);
        }

        /// <summary>
        /// The observed distance value (in data entry units)
        /// </summary>
        internal double ObservedValue
        {
            get { return GetDistance(m_EnteredUnit); }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>The result of a call to <see cref="Format"/> (with units abbreviation appended).</returns>
        public override string ToString()
        {
            return Format(true);
        }

        /// <summary>
        /// Writes the content of this instance to a persistent storage area.
        /// </summary>
        /// <param name="editSerializer">The mechanism for storing content.</param>
        public override void WriteData(EditSerializer editSerializer)
        {
            editSerializer.WriteDouble(DataField.Value, ObservedValue);
            editSerializer.WriteByte(DataField.Unit, (byte)m_EnteredUnit.UnitType);

            if (m_IsFixed)
                editSerializer.WriteBool(DataField.Fixed, true);

            if (IsAnnotationFlipped)
                editSerializer.WriteBool(DataField.Flipped, true);
        }

        /// <summary>
        /// Toggles the <see cref="IsAnnotationFlipped"/> property.
        /// </summary>
        internal void ToggleIsFlipped()
        {
            IsAnnotationFlipped = !IsAnnotationFlipped;
        }
    }
}
