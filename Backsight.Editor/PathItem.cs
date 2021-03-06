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

using Backsight.Editor.Observations;


namespace Backsight.Editor
{
    /// <written by="Steve Stanton" on="21-JAN-1998" was="CePathItem" />
    /// <summary>
    /// An item in a path description. This is a helper class used by
    /// the <see cref="PathForm"/> dialog.
    /// </summary>
    class PathItem
    {
        #region Static

        /// <summary>
        /// Obtains the highest leg number in the supplied item array.
        /// </summary>
        /// <param name="items">Array of path items.</param>
        /// <returns>The maximum value for the leg number in the supplied items.</returns>
        internal static int GetMaxLegNumber(PathItem[] items)
        {
            // Each path item contains a leg number, arranged sequentially.
            int nleg=0;

            foreach (PathItem item in items)
                nleg = Math.Max(nleg, item.LegNumber);

            return nleg;
        }

        #endregion

        #region Class data

        /// <summary>
        /// The type of item
        /// </summary>
        PathItemType m_Item;

        /// <summary>
        /// Associated value (if any). The meaning of the value
        /// depends on the type of item.
        /// </summary>
        double m_Value;

        /// <summary>
        /// The type of distance unit in effect for the item (defined
        /// for all items, even if the value is undefined).
        /// </summary>
        DistanceUnit m_Unit;

        /// <summary>
        /// Leg sequence number (defined values start at 1). Circular
        /// legs have a negated leg number.
        /// </summary>
        int m_Leg;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor creates a null item.
        /// </summary>
        internal PathItem()
        {
            m_Item = PathItemType.Null;
            m_Unit = null;
            m_Value = 0.0;
            m_Leg = 0;
        }

        /// <summary>
        /// Creates a new <c>PathItem</c>
        /// </summary>
        /// <param name="itemType">The type of item involved</param>
        /// <param name="unit">The distance unit (was default=null)</param>
        /// <param name="value">The item value (was default=0.0)</param>
        internal PathItem(PathItemType itemType, DistanceUnit unit, double value)
        {
            m_Item = itemType;
            m_Unit = unit;
            m_Value = value;
            m_Leg = 0;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="copy">The item to copy from</param>
        internal PathItem(PathItem copy)
        {
            m_Item = copy.m_Item;
            m_Unit = copy.m_Unit;
            m_Value = copy.m_Value;
            m_Leg = copy.m_Leg;
        }

        #endregion

        internal int LegNumber // was GetLeg
        {
            get { return Math.Abs(m_Leg); }
            set { m_Leg = value; }
        }

        internal PathItemType ItemType // was GetType
        {
            get { return m_Item; }
            set { m_Item = value; }
        }

        internal double Value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        internal bool IsDistance
        {
            get
            {
                return (m_Unit != null &&
                       (m_Item == PathItemType.Distance || m_Item == PathItemType.Radius));
            }
        }

        /// <summary>
        /// Defines a distance object based on this path item.
        /// </summary>
        /// <returns>The distance corresponding to this item (null if it's not
        /// a distance item)</returns>
        internal Distance GetDistance()
        {
            if (IsDistance)
                return new Distance(m_Value, m_Unit);
            else
                return null;
        }

        /// <summary>
        /// The type of distance unit in effect for the item (should be defined
        /// for all items, even if the value is undefined).
        /// </summary>
        internal DistanceUnit Units
        {
            get { return m_Unit; }
        }
    }
}
