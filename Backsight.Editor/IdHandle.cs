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
using System.Windows.Forms;

using Backsight.Environment;

namespace Backsight.Editor
{
	/// <written by="Steve Stanton" on="17-DEC-1998" />
    /// <summary>An "ID handle" is a transient object used by software that needs to
    /// announce the possibility that it will want to assign a specific feature ID.
    /// 
    /// For example, when a dialog containing an ID field is initially
    /// presented to a user, the default ID should be shown. However, it is possible
    /// that the user could change that ID. The problem is that in order to show the
    /// default ID at all, the ID range object needs to create an empty <c>FeatureId</c>
    /// object. So if the user changes the ID (or simply cancels the dialog), we could
    /// easily end up with an unused feature ID. To avoid that, we COULD modify all the
    /// UI code, to ensure that it cleans up any IDs that it created ... but that would
    /// involve a lot of code, and could be implemented inconsistently.
    ///
    /// An ID handle gets around this problem by acting as a mechanism that the UI can
    /// use to reserve IDs without actually creating an empty <c>FeatureId</c> object.
    /// The actual creation of an ID is done by passing the ID handle into the Execute()
    /// function that is part of the <c>Operation</c> that actually creates the
    /// associated feature. This aim is to ensure that a feature ID can never be created
    /// unless the spatial feature already exists.
    /// </summary>
    /// <devnote>06-FEB-2007: The above is the original comment from 1998. What it doesn't
    /// say is why it's bad to have an unused feature ID. Perhaps it's something to do with
    /// the old persistence mechanism. If so, this class could well be irrelevant.</devnote>
    class IdHandle
    {
        #region Class data

        /// <summary>
        /// The ID packet that contains the ID. Will be null if the group is null.
        /// </summary>
        private IdPacket m_Packet;

        /// <summary>
        /// The entity type that the ID relates to.
        /// </summary>
        private IEntity m_Entity;

        /// <summary>
        /// The reserved ID (0 if not yet reserved).
        /// </summary>
        private uint m_Id;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IdHandle"/> class that doesn't
        /// refer to anything.
        /// </summary>
        public IdHandle()
        {
            m_Packet = null;
            m_Entity = null;
            m_Id = 0;
        }

        #endregion

        /// <summary>
        /// Has an ID been reserved?
        /// </summary>
        internal bool IsReserved
        {
            get { return (m_Id>0); }
        }

        /// <summary>
        /// A formatted string representing the key (if any) for this ID handle.
        /// </summary>
        public string FormattedKey
        {
            get
            {
                // If an ID has been reserved, format that as a string. Otherwise it's blank.

	            if (m_Id!=0 && m_Packet!=null)
                    return m_Packet.IdGroup.FormatId(m_Id);
                else
                    return String.Empty;
            }
        }

        /// <summary>
        /// Ensures that any reserved ID has been cleared (if the ID handle has actually been
        /// used to create a new <see cref="NativeId"/>, this does nothing).
        /// </summary>
        internal void DiscardReservedId()
        {
            if (m_Packet!=null && m_Id!=0)
                m_Packet.FreeReservedId(m_Id);
        }

        /// <summary>
        /// Reserves a feature ID. Any ID previously reserved by this ID handle will
        /// be released.
        /// </summary>
        /// <param name="ent">The entity type that the ID is required for.</param>
        /// <param name="id">The specific ID to reserve (0 if you want the next
        /// available ID).</param>
        /// <returns>True if the ID was successfully reserved.</returns>
        internal bool ReserveId(IEntity ent, uint id)
        {
            // Get the ID manager to define the results.

            IdManager idMan = CadastralMapModel.Current.IdManager;
            if (idMan==null)
            {
                this.Reset();
                return false;
            }

            if (idMan.ReserveId(this, ent, id))
            {
                m_Entity = ent;
                return true;
            }

            if (id!=0)
            {
                string errmsg = String.Format("Failed to reserve ID {0} for '{1}'", id, ent.Name);
                MessageBox.Show(errmsg);
            }

            return false;
        }

        /// <summary>
        /// Reserves a feature ID in a specific ID packet
        /// </summary>
        /// <devnote>This function is called by LoadIdCombo</devnote>
        /// <param name="packet">The ID packet containing the available ID.</param>
        /// <param name="ent">The entity type that the ID is for.</param>
        /// <param name="id">The available ID to reserve.</param>
        /// <returns>True if the ID was successfully reserved.</returns>
        internal bool ReserveId(IdPacket packet, IEntity ent, uint id)
        {
            // Ensure that any currently reserved ID is released.
            FreeId();

            // Just get the packet to do it.
            if (packet.ReserveId(id))
            {
                m_Packet = packet;
                m_Id = id;
                m_Entity = ent;
                return true;
            }

            this.Reset();
            return false;
        }

        /// <summary>
        /// Creates a feature ID from this ID handle. In order for this to work, a
        /// prior call to <c>IdHandle.ReserveId</c> is needed.
        /// </summary>
        /// <param name="feature">The feature that should get the created feature ID (currently
        /// without any defined ID)</param>
        /// <returns>The created feature ID (if any).</returns>
        internal FeatureId CreateId(Feature feature)
        {
            // Confirm that the feature does not already have an ID.
            if (feature.FeatureId!=null)
                throw new ApplicationException("IdHandle.CreateId - Feature already has an ID.");

            // Claim the reserved ID and cross reference to the feature
            FeatureId fid = CreateId();
            fid.Add(feature);
            return fid;
        }

        /// <summary>
        /// Creates a feature ID from this ID handle. In order for this to work, a
        /// prior call to <c>IdHandle.ReserveId</c> is needed.
        /// </summary>
        /// <returns>The created feature ID (null if an ID hasn't been reserved).</returns>
        internal FeatureId CreateId()
        {
            // The packet has to be known.
            if (m_Packet == null)
                return null;

            // Create a NativeId, clear the reserve status
            return m_Packet.CreateId(m_Id);
        }

        /// <summary>
        /// Releases any previously reserved ID.
        /// </summary>
        internal void FreeId()
        {
            if (m_Packet!=null)
                m_Packet.FreeReservedId(m_Id);

            m_Packet = null;
            m_Id = 0;
            m_Entity = null;
        }

        /// <summary>
        /// Defines a newly reserved ID.
        /// </summary>
        /// <param name="packet">The ID packet.</param>
        /// <param name="ent">The entity type for the ID.</param>
        /// <param name="id">The raw ID number.</param>
        /// <returns>True if the ID was valid.</returns>
        internal bool Define(IdPacket packet, IEntity ent, uint id)
        {
            // The ID has to be valid.
            if (id==0)
                return false;

            // Remember the supplied info.
            m_Packet = packet;
            m_Id = id;
            m_Entity = ent;

            return true;
        }

        /// <summary>
        /// Checks whether this ID handle is valid for a specific entity type.
        /// </summary>
        /// <param name="ent">The entity type to check.</param>
        /// <returns>True if this ID handle is suitable for the entity type.</returns>
        internal bool IsValidFor(IEntity ent)
        {
            if (ent==null)
                return false;

            IdManager idMan = CadastralMapModel.Current.IdManager;
            if (idMan==null)
                return true;

            // Try to find the ID group for the specified entity type.
            IIdGroup group = ent.IdGroup;

            // If we actually found an a group, it has to match the one that we already know about.
            // If we didn't find a group, this ID is valid only if it is undefined!

            if (group!=null && m_Packet!=null)
                return (group.Id == m_Packet.IdGroup.Id);
            else
                return (m_Id==0);
        }

        /// <summary>
        /// Resets everything in the class to null values.
        /// </summary>
        void Reset()
        {
            m_Packet = null;
            m_Entity = null;
            m_Id = 0;
        }

        /// <summary>
        /// The entity type for this ID handle. When setting, it is assumed that the entity
        /// type is consistent with the ID (as tested via a call to <c>IdHandle.IsValidFor</c>.
        /// </summary>
        internal IEntity Entity
        {
            get { return m_Entity; }
            set { m_Entity = value; }
        }
    }
}
