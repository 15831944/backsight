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

namespace Backsight.Index
{
    /// <written by="Steve Stanton" on="20-DEC-2006" />
    /// <summary>
    /// A covering rectangle where position is expressed to the nearest
    /// micron on the ground.
    /// </summary>  
    class Extent : IEquatable<Extent>
    {
        #region Class data

        /// <summary>
        /// The range of the extent in the east-west direction
        /// </summary>
        private RangeValue m_X;

        /// <summary>
        /// The range of the extent in the north-south direction
        /// </summary>
        private RangeValue m_Y;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Extent"/> class that refers
        /// to the complete 64-bit space.
        /// </summary>
        internal Extent()
            : this(UInt64.MinValue, UInt64.MinValue, UInt64.MaxValue, UInt64.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Extent"/> class.
        /// </summary>
        /// <param name="minx">The western edge, in microns</param>
        /// <param name="miny">The southern edge, in microns</param>
        /// <param name="maxx">The eastern edge, in microns</param>
        /// <param name="maxy">The northern edge, in microns</param>
        internal Extent(ulong minx, ulong miny, ulong maxx, ulong maxy)
        {
            m_X = new RangeValue(Dimension.X, minx, maxx);
            m_Y = new RangeValue(Dimension.Y, miny, maxy);
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Extent"/> class that
        /// has the same extent as a specific spatial object.
        /// </summary>
        /// <param name="so">The spatial object that will define the extent</param>
        internal Extent(ISpatialObject so)
            : this(so.Extent)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Extent"/> class that
        /// corresponds to a window (where the coverage is expressed using floating
        /// point values).
        /// </summary>
        /// <param name="extent">The covering rectangle (expressed using floating
        /// point values)</param>
        internal Extent(IWindow extent)
        {
            m_X = new RangeValue(Dimension.X,
                                 Length.ToMicrons(extent.Min.X),
                                 Length.ToMicrons(extent.Max.X));

            m_Y = new RangeValue(Dimension.Y,
                                 Length.ToMicrons(extent.Min.Y),
                                 Length.ToMicrons(extent.Max.Y));
        }

        #endregion

        /// <summary>
        /// Checks whether a pair of windows overlap
        /// </summary>
        /// <param name="w">The window to compare with this one</param>
        /// <returns>True if the windows overlap.
        /// False if they don't overlap (or only touch)</returns>
        internal bool IsOverlap(Extent w)
        {
            return (m_X.IsOverlap(w.m_X) && m_Y.IsOverlap(w.m_Y));
        }

        /// <summary>
        /// Checks whether this window overlaps a positional range
        /// </summary>
        /// <param name="r">The range to examine</param>
        /// <returns>True if range overlaps this window.
        /// False if there's no overlap (or range only touches)</returns>
        internal bool IsOverlap(RangeValue r)
        {
            return (m_X.IsOverlap(r) || m_Y.IsOverlap(r));
        }

        /// <summary>
        /// Checks whether this window overlaps a position
        /// </summary>
        /// <param name="p">The position to examine</param>
        /// <returns>True if the position overlaps this window (may be exactly
        /// coincident with the perimeter)</returns>
        internal bool IsOverlap(IPointGeometry p)
        {
            ulong val = SpatialIndex.ToUnsigned(p.Easting.Microns);
            if (val<m_X.Min || val>m_X.Max)
                return false;

            val = SpatialIndex.ToUnsigned(p.Northing.Microns);
            return (val>=m_Y.Min && val<=m_Y.Max);
        }

        /// <summary>
        /// Checks whether this window is entirely enclosed by another window.
        /// </summary>
        /// <param name="other">The window to compare with this one</param>
        /// <returns>True is this window is entirely enclosed</returns>
        internal bool IsEnclosedBy(Extent other)
        {
            return (other.m_X.Min <= this.m_X.Min
            &&      other.m_Y.Min <= this.m_Y.Min
            &&      other.m_X.Max >= this.m_X.Max
            &&      other.m_Y.Max >= this.m_Y.Max);
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="Extent"/>.
        /// </summary>
        /// <returns>A string that contains the min-max values, suitable for use in debug output</returns>
        public override string ToString()
        {
            return String.Format("X={0}, Y={1}", m_X.ToString(), m_Y.ToString());
        }

        /// <summary>
        /// The western edge, in microns
        /// </summary>
        internal ulong MinX
        {            
            get { return m_X.Min; }
        }

        /// <summary>
        /// The southern edge, in microns
        /// </summary>
        internal ulong MinY
        {
            get { return m_Y.Min; }
        }

        /// <summary>
        /// The eastern edge, in microns
        /// </summary>
        internal ulong MaxX
        {
            get { return m_X.Max; }
        }

        /// <summary>
        /// The northern edge, in microns
        /// </summary>
        internal ulong MaxY
        {
            get { return m_Y.Max; }
        }

        /// <summary>
        /// Is this window square?
        /// </summary>
        internal bool IsSquare
        {
            get { return (this.Width==this.Height); }
        }

        /// <summary>
        /// The width of the extent, in microns
        /// </summary>
        internal ulong Width
        {
            get { return m_X.Size; }
        }

        /// <summary>
        /// The height of the extent, in microns
        /// </summary>
        internal ulong Height
        {
            get { return m_Y.Size; }
        }

        /// <summary>
        /// Shifts this window to the east and/or north
        /// </summary>
        /// <param name="dx">The amount to add to the easting</param>
        /// <param name="dy">The amount to add to the northing</param>
        internal void Increase(ulong dx, ulong dy)
        {
            if (dx!=0)
                m_X.Increase(dx);

            if (dy!=0)
                m_Y.Increase(dy);

        }

        /// <summary>
        /// The outline of this extent, expressed as a closed polyline (arranged
        /// in a counter-clockwise order).
        /// </summary>
        internal IPosition[] Outline
        {
            get
            {
                double minx = Length.ToMeters((long)(m_X.Min ^ 0x8000000000000000));
                double miny = Length.ToMeters((long)(m_Y.Min ^ 0x8000000000000000));
                double maxx = Length.ToMeters((long)(m_X.Max ^ 0x8000000000000000));
                double maxy = Length.ToMeters((long)(m_Y.Max ^ 0x8000000000000000));

                IPosition[] result = new IPosition[5];
                result[0] = new Position(minx, miny);
                result[1] = new Position(maxx, miny);
                result[2] = new Position(maxx, maxy);
                result[3] = new Position(minx, maxy);
                result[4] = result[0];
                return result;
            }
        }

        #region IEquatable<Window> Members

        /// <summary>
        /// Checks whether this extent is exactly coincident with another one.
        /// </summary>
        /// <param name="that">The extent to compare with</param>
        /// <returns>True if the extents are spatially coincident</returns>
        public bool Equals(Extent that)
        {
            return (this.m_X.Min == that.m_X.Min
                &&  this.m_X.Max == that.m_X.Max
                &&  this.m_Y.Min == that.m_Y.Min
                &&  this.m_Y.Max == that.m_Y.Max);
        }

        #endregion
    }
}
