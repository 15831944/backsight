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
using System.Data;

namespace Backsight.Data
{
    /// <summary>
    /// Delegate passed to <see cref="IDataServer.RunTransaction"/>
    /// </summary>
    public delegate void TransactionBody();

    /// <summary>
    /// Access to a database server.
    /// </summary>
    /// <remarks>An implementation is provided by the <see cref="DataServer"/> class</remarks>
    public interface IDataServer
    {
        /// <summary>
        /// The database connection string.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Checks whether it is possible to connect to the database.
        /// </summary>
        /// <returns>True if a database connection can be established.</returns>
        bool CanConnect();

        /// <summary>
        /// Executes a SELECT statement.
        /// </summary>
        /// <param name="sql">The select statement</param>
        /// <returns>The retrieved data</returns>
        DataTable ExecuteSelect(string sql);

        /// <summary>
        /// Executes a SELECT statement that returns a single int value (suitable for statements
        /// like <c>SELECT COUNT(*) FROM SomeTable</c>).
        /// </summary>
        /// <param name="sql">The select statement</param>
        /// <returns>The selected value (0 if the result was a database null).</returns>
        int ExecuteSelectInt(string sql);

        /// <summary>
        /// Executes an INSERT, UPDATE, or DELETE statement.
        /// </summary>
        /// <param name="sql">The SQL to execute</param>
        /// <returns>The number of rows affected by the statement</returns>
        int ExecuteNonQuery(string sql);

        /// <summary>
        /// Obtains a database connection..
        /// </summary>
        /// <returns>An object that may be used to access the database</returns>
        IConnection GetConnection();

        /// <summary>
        /// Runs a section of code as a multi-statement transaction.
        /// </summary>
        /// <param name="body">The code to execute within the transaction (may contain
        /// further calls to <c>RunTransaction</c>)</param>
        /// <remarks>
        /// Specifying this method as part of IConnectionManager obviously makes it more
        /// difficult to provide concrete implementations. It is here to try to ensure
        /// that transactional support is at least considered by implementors.
        /// </remarks>
        void RunTransaction(TransactionBody body);

        /// <summary>
        /// Returns a derived data adapter class with the Connection property set to the current database connection
        /// </summary>
        /// <typeparam name="T">The name of the derived data adapter class</typeparam>
        /// <returns>An instance of type T</returns>
        T CreateAdapter<T>() where T : new();

        /// <summary>
        /// Creates a new row for a database table
        /// </summary>
        /// <param name="tableName">The name of the table the new row will be added to.</param>
        /// <returns>A new row in the table (not yet inserted, with default data for all columns)</returns>
        DataRow CreateNewRow(string tableName);

        /// <summary>
        /// Saves a row in the database (adds if the <see cref="DataRow.RowState"/> has
        /// a value of <c>DataRowState.Detached</c>, otherwise attempts to update). This
        /// logic may well be defective in a situation where the row has already been added
        /// to a <c>DataTable</c>.
        /// </summary>
        /// <param name="row">The row to add. Usually a row that was created via a
        /// prior call to <see cref="CreateNewRow"/> (the important thing is that
        /// the table name must be defined as part of the rows associated <c>DataTable</c>)</param>
        /// <remarks>This makes use of the <see cref="SqlCommandBuilder"/> class to
        /// generate the relevant insert statement, which requires that the table involved
        /// must have a primary key (if not, you will get an exception)
        /// </remarks>
        /// <exception cref="ArgumentException">If the <see cref="DataTable"/> associated
        /// with the supplied row does not contain a defined table name.</exception>
        void SaveRow(DataRow row);
    }
}
