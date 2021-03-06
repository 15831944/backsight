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
using System.Data.SqlClient;

namespace Backsight.Data
{
    /// <summary>
    /// Something that provides access to an <see cref="SqlConnection"/>
    /// </summary>
    /// <remarks>See comments in the <see cref="ConnectionWrapper"/> class</remarks>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// The database connection
        /// </summary>
        SqlConnection Value { get; }

        /// <summary>
        /// The data server that created the connection (not null).
        /// </summary>
        IDataServer DataServer { get; }
    }
}
