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
using System.Runtime.Serialization;

using Backsight.Environment;

namespace Backsight.Editor
{
    /// <summary>
    /// Fronts an instance of some object that implements <c>IEntity</c>.
    /// </summary>
    [Serializable]
    class EntityFacade : EnvironmentItemFacade<IEntity>, IEntity
    {
        protected EntityFacade(IEntity data)
            : base(data)
        {
        }

        public override string ToString()
        {
            return Name;
        }

        public string Name
        {
            get { return (this.Data==null ? String.Empty : this.Data.Name); }
        }

        public bool IsPointValid
        {
            get { return (this.Data==null ? false : this.Data.IsPointValid); }
        }

        public bool IsLineValid
        {
            get { return (this.Data==null ? false : this.Data.IsLineValid); }
        }

        public bool IsPolygonValid
        {
            get { return (this.Data==null ? false : this.Data.IsPolygonValid); }
        }

        public bool IsPolygonBoundaryValid
        {
            get { return (this.Data==null ? false : this.Data.IsPolygonBoundaryValid); }
        }

        public bool IsTextValid
        {
            get { return (this.Data==null ? false : this.Data.IsTextValid); }
        }

        public bool IsValid(SpatialType t)
        {
            return (this.Data==null ? false : this.Data.IsValid(t));
        }

        /// <summary>
        /// The table(s) that are usually associated with this entity type.
        /// </summary>
        public ITable[] DefaultTables
        {
            get { return (this.Data == null ? null : this.Data.DefaultTables); }
        }

        public IIdGroup IdGroup
        {
            get { return (this.Data==null ? null : this.Data.IdGroup); }
        }

        public ILayer Layer
        {
            get { return (this.Data==null ? null : this.Data.Layer); }
        }

        public IFont Font
        {
            get { return (this.Data == null ? null : this.Data.Font); }
        }

        [OnDeserialized]
        void GetEnvironmentData(StreamingContext context)
        {
            this.Data = EnvironmentContainer.FindEntityById(this.Id);
        }
    }
}
