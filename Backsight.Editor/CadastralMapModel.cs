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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

using Backsight.Data;
using Backsight.Editor.Database;
using Backsight.Editor.Operations;
using Backsight.Editor.Properties;
using Backsight.Environment;
using Backsight.Geometry;
using Backsight.Index;

namespace Backsight.Editor
{
    class CadastralMapModel : ISpatialModel, ISpatialData, ILoader
    {
        #region Static Methods

        /// <summary>
        /// The current map model. When the application is running, this should be
        /// not null. It will be null at design time.
        /// </summary>
        internal static CadastralMapModel Current
        {
            get
            {
                EditingController cec = EditingController.Current;
                return (cec==null ? null : (cec.MapModel as CadastralMapModel));
            }
        }

        #endregion

        #region Class data

        /// <summary>
        /// Spatial index for the data in this model. This will be null while the model
        /// is being deserialized from the database.
        /// </summary>
        EditingIndex m_Index;

        /// <summary>
        /// Default rotation angle for text (in radians).
        /// </summary>
        double m_Rotation;

        /// <summary>
        /// The coordinate system.
        /// </summary>
        readonly CoordinateSystem m_CoordSystem;

        /// <summary>
        /// Window of all data in the map.
        /// </summary>
        Window m_Window;

        /// <summary>
        /// Editing sessions.
        /// </summary>
        readonly List<Session> m_Sessions;

        /// <summary>
        /// Management of user-specified IDs
        /// </summary>
        readonly IdManager m_IdManager;

        /// <summary>
        /// The session that we are currently appending to. Defined on a call to
        /// <see cref="AppendWorkingSession"/>
        /// </summary>
        Session m_WorkingSession;

        /// <summary>
        /// Spatial features that have been loaded (including features that may
        /// have been deactivated).
        /// </summary>
        readonly Dictionary<InternalIdValue, Feature> m_Features;

        /// <summary>
        /// Index of feature IDs that are based on the Backsight numbering strategy (as opposed
        /// to user-perceived IDs that originate from some foreign source). The key
        /// is the raw ID, the value the created ID object.
        /// </summary>
        readonly Dictionary<uint, NativeId> m_NativeIds;

        /// <summary>
        /// Index of all foreign IDs (typically IDs that get defined via import from some
        /// alien data source).
        /// </summary>
        readonly Dictionary<string, ForeignId> m_ForeignIds;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new empty model
        /// </summary>
        internal CadastralMapModel()
        {
            m_Rotation = 0.0;
            m_CoordSystem = new CoordinateSystem();
            m_Window = new Window();
            m_Sessions = new List<Session>();
            m_Index = null; // new EditingIndex();
            m_IdManager = new IdManager();
            m_Features = new Dictionary<InternalIdValue, Feature>(1000);
            m_NativeIds = new Dictionary<uint, NativeId>(1000);
            m_ForeignIds = new Dictionary<string, ForeignId>(1000);
        }

        #endregion

        public override string ToString()
        {
            string name = Name;
            return (String.IsNullOrEmpty(name) ? "(Untitled)" : name);
        }

        /// <summary>
        /// The user-name of the model (including the path).
        /// Blank if the model is undefined, or it's got a temporary system-generated name.
        /// </summary>
        public string Name
        {
            get { return EditingController.Current.JobFile.Name; }
        }

        /// <summary>
        /// The spatial index for this model (in a form that's suitable for typical queries).
        /// </summary>
        /// <remarks>The result may be null, since some older software may rely on
        /// a null result to determine the course of events. Where appropriate, use the
        /// <see cref="EditingIndex"/> property instead.</remarks>
        internal ISpatialIndex Index
        {
            get
            {
                /*
                // The index may be null if the model has just been deserialized
                if (m_Index==null)
                    m_Index = new EditingIndex();
                */
                return m_Index;
            }
        }

        /// <summary>
        /// The spatial index for this model (in a form that's suitable for editing).
        /// If it doesn't already exist, it will be created.
        /// </summary>
        internal EditingIndex EditingIndex
        {
            get
            {
                // The index may be null if the model has just been deserialized
                //if (m_Index==null)
                //    m_Index = new EditingIndex();

                return m_Index;
            }
        }

        /// <summary>
        /// Default rotation angle for text (in radians).
        /// </summary>
        internal double DefaultTextRotation
        {
            get { return m_Rotation; }
            set { m_Rotation = value; }
        }

        /// <summary>
        /// The editing sessions that define this model
        /// </summary>
        internal Session[] Sessions
        {
            get { return m_Sessions.ToArray(); }
        }

        #region ISpatialModel Members

        public bool IsEmpty
        {
            get { return m_Window.IsEmpty; }
        }

        public IWindow Extent
        {
            get { return m_Window; }
        }

        public ISpatialObject QueryClosest(IPosition p, ILength radius, SpatialType types)
        {
            return m_Index.QueryClosest(p, radius, types);
        }

        #endregion

        #region ISpatialData Members

        public void Render(ISpatialDisplay display, IDrawStyle style)
        {
            // Do nothing if the index hasn't been created yet
            if (m_Index==null)
                return;

            // Default to draw all defined feature types
            SpatialType types = SpatialType.Feature;

            // Suppress text if the display scale is too small
            EditingController ec = EditingController.Current;
            JobFileInfo jfi = ec.JobFile.Data;
            if (display.MapScale > jfi.ShowLabelScale)
                types ^= SpatialType.Text;

            // Suppress points if the display scale is too small
            if (display.MapScale > ec.JobFile.Data.ShowPointScale)
                types ^= SpatialType.Point;

            new DrawQuery(m_Index, display, style, types);

            // Draw intersections if necessary
            if (jfi.AreIntersectionsDrawn && (types & SpatialType.Point)!=0)
                (m_Index as EditingIndex).DrawIntersections(display);

            //(m_Index as SpatialIndex).Draw(display); // for testing
        }

        #endregion

        internal uint MakeBackup()
        {
            return 0;
            /*
            IDataController dc = DataRoot.Controller;
            if (dc==null)
                return 0;

            // If it's a new file that has NEVER been saved, it'll
            // have a temporary file name. So in that case, just
            // get the user to do a regular Save-As.
            if ( !m_IsSaved ) {
	            OnFileSaveAs();
	            return 0;
            }

            // Get the highest operation sequence number from the map.
            UINT4 maxop = m_pMap->GetMaxOp();

            // Get the file spec for the backup file.
            CString spec;
            spec.Format("%s%010d",GetMapSpec(),maxop);

            // Return if we've already made the backup.
            CFileStatus fstatus;
            if ( CFile::GetStatus((LPCTSTR)spec,fstatus) ) return 0;

            // Display a message to say what's happening.
            CString msg;
            msg.Format("Saving backup %s",spec);
            CdMessage* pMess = new CdMessage(msg);
            pMess->Create(CdMessage::IDD);

            // Copy the PSE database.
            m_osdb->copy((LPCTSTR)spec,0664,0);

            // Sleep for 1 second so the user can see the message
            // when dealing with small files.
            Sleep(1000);
            delete pMess;

            // If it got copied ok, return the sequence number.
            if ( CFile::GetStatus((LPCTSTR)spec,fstatus) ) return maxop;

            // Tell the user something screwed up.
            msg.Format("Failed to create backup file %s",spec);
            AfxMessageBox(msg);
            return 0;
             */
        }

        internal void Close()
        {
            Session s = Session.WorkingSession;
            if (s!=null)
            {
                if (s.OperationCount==0)
                    s.Delete();
                else
                    s.UpdateEndTime();

                m_Sessions.Clear();
                Session.ClearCurrentSession();
            }
        }

        /// <summary>
        /// The sequence number of the last edit in the last session (0 if no edits have
        /// been performed in the last session)
        /// </summary>
        internal uint LastOpSequence
        {
            get
            {
                Session s = LastSession;
                if (s==null)
                    return 0;

                Operation o = s.LastOperation;
                if (o==null)
                    return 0;

                return o.EditSequence;
            }
        }

        /// <summary>
        /// The last editing session in this model (null if this is a freshly created model,
        /// and data is still being loaded)
        /// </summary>
        internal Session LastSession
        {
            get
            {
                int numSession = m_Sessions.Count;
                return (numSession==0 ? null : m_Sessions[numSession-1]);
            }
        }

        public ISpatialSystem SpatialSystem
        {
            get { return m_CoordSystem; }
        }

        internal ICoordinateSystem CoordinateSystem
        {
            get { return m_CoordSystem; }
        }

        /// <summary>
        /// Remembers that rollforward is about to begin.
        /// </summary>
        /// <devnote>This should probably move to EditingController</devnote>
        internal void StartRollforward()
        {
            // @devnote There was a potential memory leak here.
            // If CeMap::Rollforward did not eventually complete for
            // any reason, memory for CeLabelShift objects was not
            // deleted. DO NOT do it here, because the memory may have
            // been allocated in a prior session. I tried to fix this
            // via CeMap::BeforeHook, but that causes a recursive fault.
            // As it stands now, FinishRollforward is supposed to also
            // remove (and delete) any leftover label-shifts. So the
            // removal that happens here is only expected to cover
            // those cases where old files have spurious pointers.

            //m_LabelShifts.RemoveAll();
        }

        /// <summary>
        /// Creates a new point feature as part of this model.
        /// The caller is responsible for assigning any ID that the new point should have.
        /// </summary>
        /// <param name="p">The position for the point (not null). If it's an instance of
        /// IPointGeometry, it will be re-used as the geometry for the new feature (otherwise
        /// a new instance of PointGeometry will be created).
        /// </param>
        /// <param name="e">The entity type for the point (not null)</param>
        /// <param name="creator">The operation creating the point</param>
        /// <returns>The created point feature.</returns>
        internal PointFeature AddPoint(IPosition p, IEntity e, Operation creator)
        {
            PointGeometry g = PointGeometry.Create(p);
            uint ss = Session.ReserveNextItem();
            PointFeature f = new DirectPointFeature(creator, ss, e, g);
            //m_Window.Union(p);
            //m_Index.Add(f);
            return f;
        }

        /// <summary>
        /// Ensures a point feature exists at a specific location in this map model.
        /// The caller is responsible for assigning any ID that the new point should have.
        /// </summary>
        /// <param name="p">The position where a point feature is required</param>
        /// <param name="creator">The operation that should be recorded as the creator
        /// of any newly created point</param>
        /// <returns>The point feature at the specified position (may be a new point)</returns>
        internal PointFeature EnsurePointExists(IPosition p, Operation creator)
        {
            if (p is PointFeature)
                return (p as PointFeature);

            EditingIndex index = EditingIndex;
            ILength tol = new Length(Constants.XYRES);
            PointFeature pf = (index.QueryClosest(p, tol, SpatialType.Point) as PointFeature);
            //PointFeature pf = (index.QueryClosest(p, Length.Zero, SpatialType.Point) as PointFeature);
            if (pf==null)
                pf = AddPoint(p, DefaultPointType, creator);

            return pf;
        }

        /// <summary>
        /// Creates a new line feature that connects two points.
        /// </summary>
        /// <param name="from">The starting point for the new line</param>
        /// <param name="to">The end point for the new line</param>
        /// <param name="e">The entity type for the line (not null)</param>
        /// <param name="creator">The operation creating the line</param>
        /// <returns>The created line feature.</returns>
        internal SegmentLineFeature AddLine(PointFeature from, PointFeature to, IEntity e, Operation creator)
        {
            SegmentLineFeature f = new SegmentLineFeature(creator, 0, e, from, to);
            //m_Window.Union(f.Extent);
            //m_Index.Add(f);

            // Ensure polygons in the vicinity have been marked for rebuild
            f.MarkPolygons();

            return f;
        }

        /// <summary>
        /// Includes features created by an editing operation as part of the editing
        /// index. Also ensures the overal map extent has been expanded (if necessary)
        /// to include the extent of the features.
        /// </summary>
        /// <param name="fa">The features to add to the index</param>
        internal void AddToIndex(Feature[] fa)
        {
            EditingIndex index = this.EditingIndex;

            foreach (Feature f in fa)
            {
                // Ignore if the feature has no extent (this should apply only during 
                // deserialization of TextFeature instances that are associated with
                // RowTextGeometry). It would be nice to do all indexing after all
                // edits have been deserialized. However, I believe that some of the
                // deserialization logic expects previous edits to be indexed.

                // ...on second thoughts, this is just too messy. If deserialization
                // logic does really require access to a spatial index, the relevant
                // edits should be modified to avoid the dependency

                //IWindow x = f.Extent;
                //if (x != null)
                //{
                //    f.AddToIndex(index);
                //    m_Window.Union(f.Extent);
                //}
                //else
                //{
                //    Debug.Assert(f is TextFeature);
                //    TextFeature tf = (TextFeature)f;
                //    Debug.Assert(tf.TextGeometry is RowTextGeometry);
                //}

                if (f.AddToIndex(index))
                    m_Window.Union(f.Extent);
            }

            // The extent of circles don't get included in the map extent, because
            // they're regarded only as construction lines (that should be invisible
            // to the user).

            List<Circle> createdCircles = GetCreatedCircles(fa);
            foreach (Circle c in createdCircles)
                c.AddToIndex(index);
        }

        /// <summary>
        /// Ensures that any IDs associated with an array of features have been indexed
        /// as part of this model.
        /// </summary>
        /// <param name="fa">The features that need to be indexed</param>
        internal void AddFeatureIds(Feature[] fa)
        {
            foreach (Feature f in fa)
            {
                FeatureId fid = f.FeatureId;

                if (fid != null)
                {
                    if (fid is ForeignId)
                    {
                        ForeignId foreignId = (fid as ForeignId);
                        ForeignId existingId = FindForeignId(foreignId.FormattedKey);
                        if (existingId == null)
                            AddForeignId(foreignId);
                        else if (!object.ReferenceEquals(foreignId, existingId))
                            throw new Exception("More than one foreign ID object for: "+foreignId.FormattedKey);
                    }
                    else
                    {
                        NativeId nativeId = (fid as NativeId);
                        NativeId existingId = FindNativeId(nativeId.RawId);
                        if (existingId == null)
                            m_NativeIds.Add(nativeId.RawId, nativeId);
                        else if (!object.ReferenceEquals(nativeId, existingId))
                            throw new Exception("More than one native ID object for: "+nativeId.RawId);
                    }
                }
            }
        }

        /// <summary>
        /// The circles associated with an array of features
        /// </summary>
        /// <param name="fa">The features of interest</param>
        /// <returns>The circles (if any) that are associated with the supplied features</returns>
        static List<Circle> GetCreatedCircles(Feature[] fa)
        {
            if (fa.Length==0)
                return new List<Circle>();

            // The following will be overkill in most cases, but not for things like
            // bulk data imports...

            // The circles found so far will be noted in an index that's keyed by the
            // internal ID of the point at the center of the circle.
            Dictionary<InternalIdValue, List<Circle>> dic = new Dictionary<InternalIdValue, List<Circle>>();

            List<Circle> result = new List<Circle>(100);

            foreach (Feature f in fa)
            {
                if (f is ArcFeature)
                {
                    Circle c = (f as ArcFeature).Circle;

                    if (c.Creator == f.Creator)
                    {
                        InternalIdValue centerPointId = c.CenterPoint.InternalId;
                        bool addToResult = false;
                        List<Circle> circles;

                        if (dic.TryGetValue(centerPointId, out circles))
                        {
                            if (circles.IndexOf(c)<0)
                            {
                                circles.Add(c);
                                addToResult = true;
                            }
                        }
                        else
                        {
                            circles = new List<Circle>(1);
                            circles.Add(c);
                            dic.Add(centerPointId, circles);
                            addToResult = true;
                        }

                        if (addToResult)
                            result.Add(c);
                    }
                }
            }

            result.TrimExcess();
            return result;
        }

        /// <summary>
        /// Cleans up after each edit
        /// </summary>
        internal void CleanEdit()
        {
            // Do usual cleaning stuff.
            CleanupQuery cq = new CleanupQuery(this);

            // Ensure that all moved stuff has been intersected
            // (and that any trimmed lines have been adjusted appropriately).
            //System.Windows.Forms.MessageBox.Show("starting intersect");
            //Stopwatch sw = Stopwatch.StartNew();
            //if (cq.Moves.Count > 0)
            //{
                Intersect(cq.Moves);
            //    new CleanupQuery(this);
            //}
            //sw.Stop();
            //System.Windows.Forms.MessageBox.Show(sw.Elapsed.ToString());

            //System.Windows.Forms.MessageBox.Show("building topology");
            //sw.Reset();
            //sw.Start();
            BuildPolygons();
            //sw.Stop();
            //System.Windows.Forms.MessageBox.Show(sw.Elapsed.ToString());
        }

        /// <summary>
        /// Builds polygon topology for everything in the active layer of the map.
        /// </summary>
        /// <devnote>
        /// Should really return status to indicate whether an early return has been
        /// made due to unclean topology, or define current topological status as part
        /// of the model.
        /// </devnote><devnote>
        /// If unclean topology is detected, it may be desirable to continue processing
        /// of any other layers which exist in the map. If we are holding topological
        /// status (as alluded to above), we will need to have separate status indicators
        /// for each layer.
        /// </devnote>
        void BuildPolygons()
        {
            new PolygonBuilder(this).Build();
        }

        /// <summary>
        /// Intersects all features that have moved during rollforward.
        /// </summary>
        /// <param name="moves">The moved features.</param>
        void Intersect(List<Feature> moves)
        {
            // Return if nothing has moved
            if (moves.Count==0)
                return;

            Trace.Write("Intersecting "+moves.Count+" lines");
            List<LineFeature> trims = new List<LineFeature>();
            int nMove = 0;

            foreach(Feature f in moves)
            {
                f.IsMoved = false;
                LineFeature line = (f as LineFeature);
                if (line==null)
                    continue;

                nMove++;
                line.Split(trims);

                // If the line needs to be trimmed, add it to our list.
                // We'll do it at the end.

                /*
                    // Note that IsTrimmed checks that both FFL_TRIM and
                    // FFL_SYSTOPOL are set, which is kind of what you'd
                    // expect after calling CeArc::Split (assuming that
                    // the line intersects something). However, when intersects
                    // are detected, they ignore stuff that is still marked as
                    // moved. So we might not create any arc sections until
                    // later on. By calling WasTrimmed() as well, we ensure
                    // that the moved arc really does make it into the list
                    // (WasTrimmed checks that FFL_TRIM is set and that
                    // FFL_SYSTOPOL is clear). In the event that the line
                    // no longer intersects anything, that will be trapped
                    // when we call CeSplit::Trim below. Phew!

                    if (line.IsTrimmed || line.WasTrimmed)
                        trims.Append(line);
                 */
            }

            // If we moved anything, ensure that any newly marked polygons 
            // have been cleaned, and the spatial index is up to date.
            if (nMove > 0)
            {
                // If we hit any lines that need to be trimmed, do them now.
                // Any trimmed portions will be marked inactive.
                foreach (LineFeature trimLine in trims)
                {
                    /*
                    Split split = trimLine.Split;
                    if (split != null)
                        split.Trim(null);
                     */
                }

                // This is a little bit of overkill
                CleanupQuery cq = new CleanupQuery(this);
            }

            /*
	        // Clear out the list.
            moves.Clear();

	        // If we moved anything, ensure that any newly marked polygons 
	        // have been cleaned, and the space is up to date. We don't need
	        // to re-clean topological arcs, since splitting does not de-activate
            // them.

	        // Oh yes it does! When line A is cut by line B, it produces arc
	        // sections A1,A2,B1,B2. If A1 is also cut by line C, A1 will be
	        // marked for deletion and replaced with A3,A4 (and C1,C2 will get
	        // created).

	        // If you don't clean this up, the line primitive for A1 will still
	        // be marked active (still in spatial index). At the moment, BuildPolygons
	        // will happily try to build topology for this, leading to an assertion as
	        // it tries to form network topology. Although we COULD check there, and
	        // leave the deleted stuff kicking around till the next CleanEdit. who
	        // knows what other weird stuff could happen. So clean arcs too!

	        // If this is really an issue (speed wise), we COULD avoid the call to
	        // CleanArcs if system-defined arc sections were processed above, but none
	        // of them created new arc sections.

	        if (nMove>0)
            {
		        // If we hit any arcs that need to be trimmed, do them now.
		        // Any trimmed portions will be marked inactive.
                foreach(LineFeature trimLine in trims)
                {
                    Split split = trimLine.Split;
                    if (split!=null)
                        split.Trim(null);
                }

                List<ISpatialObject> nomoves = new List<ISpatialObject>(); // shouldn't get any MORE moves
		        CleanArcs(nomoves);
                Debug.Assert(nomoves.Count==0);
		        CleanPolygons();

		        // No need to clean text (well, I can't think of any).
		        m_Index.Clean(m_UpdateWindow, false);
            }
             */
        }

        internal IEntity GetDefaultEntity(SpatialType t)
        {
            if (t == SpatialType.Point)
                return DefaultPointType;
            else if (t == SpatialType.Line)
                return DefaultLineType;
            else if (t == SpatialType.Polygon)
                return DefaultPolygonType;
            else if (t == SpatialType.Text)
                return DefaultTextType;
            else
                throw new NotImplementedException("GetDefaultEntity");
        }

        /// <summary>
        /// Remembers the default entity type that should be assigned to features with
        /// a specific geometric type.
        /// </summary>
        /// <param name="t">The geometric type</param>
        /// <param name="e">The default entity type for any new features that
        /// are created with the specified geometric type (null for the blank type)</param>
        internal void SetDefaultEntity(SpatialType t, IEntity e)
        {
            if (e == null)
                SetDefaultEntity(t, 0);
            else
                SetDefaultEntity(t, e.Id);
        }

        /// <summary>
        /// Remembers the default entity type that should be assigned to features with
        /// a specific geometric type.
        /// </summary>
        /// <param name="t">The geometric type</param>
        /// <param name="entityId">The internal ID of the default entity type for any new features that
        /// are created with the specified geometric type</param>
        internal void SetDefaultEntity(SpatialType t, int entityId)
        {
            if (t == SpatialType.Point)
                EditingController.Current.JobFile.Data.DefaultPointType = entityId;
            else if (t == SpatialType.Line)
                EditingController.Current.JobFile.Data.DefaultLineType = entityId;
            else if (t == SpatialType.Polygon)
                EditingController.Current.JobFile.Data.DefaultPolygonType = entityId;
            else if (t == SpatialType.Text)
                EditingController.Current.JobFile.Data.DefaultTextType = entityId;
            else
                throw new NotImplementedException("SetDefaultEntityType");
        }

        internal IEntity DefaultPointType
        {
            get
            {
                int entityId = EditingController.Current.JobFile.Data.DefaultPointType;
                return EnvironmentContainer.FindEntityById(entityId);
            }
        }

        internal IEntity DefaultLineType
        {
            get
            {
                int entityId = EditingController.Current.JobFile.Data.DefaultLineType;
                return EnvironmentContainer.FindEntityById(entityId);
            }
        }

        internal IEntity DefaultPolygonType
        {
            get
            {
                int entityId = EditingController.Current.JobFile.Data.DefaultPolygonType;
                return EnvironmentContainer.FindEntityById(entityId);
            }
        }

        internal IEntity DefaultTextType
        {
            get
            {
                int entityId = EditingController.Current.JobFile.Data.DefaultTextType;
                return EnvironmentContainer.FindEntityById(entityId);
            }
        }

        /// <summary>
        /// Finds all circles that pass near a specific position.
        /// </summary>
        /// <param name="p">The search position.</param>
        /// <param name="tol">The search tolerance (expected to be quite small).</param>
        /// <returns>The circles close to the search position (may be empty list)</returns>
        internal List<Circle> FindCircles(IPosition p, ILength tol)
        {
            return new FindCirclesQuery(m_Index, p, tol).Result;
        }

        /// <summary>
        /// Adds a circle to the model. If a circle with the specified radius is
        /// already attached to the point at the center of the circle, you get back the
        /// existing circle. If there is no such circle, one will be created, and the
        /// center point will be modified to refer to it.
        /// </summary>
        /// <param name="c">The point at the center.</param>
        /// <param name="radius">The radius (on the ground), in meters</param>
        /// <returns></returns>
        [Obsolete("Calling this during deserialization (before geomtry is defined) is bad")]
        internal Circle AddCircle(PointFeature c, double radius)
        {
            // Try to match the center point with an existing circle with
            // the specified radius.
	        Circle circle = c.GetCircle(radius);
	        if (circle!=null)
                return circle;

            // Create a new circle.
            circle = new Circle(c, radius);

            // Refer the center point to the circle.
            circle.AddReferences();
            return circle;
        }


        /// <summary>
        /// Adds a circular arc that corresponds to a complete circle.
        /// </summary>
        /// <param name="center">The point at the center of the circle.</param>
        /// <param name="radius">The radius of the circle (on the ground), in meters</param>
        /// <param name="start">The point (if any) that should be regarded as the
        /// "start" of the circle. If null, an arbitrary start point at the top of
        /// the circle will be used.</param>
        /// <param name="creator">The operation creating the arc</param>
        /// <returns>The newly created line</returns>
        internal ArcFeature AddCompleteCircularArc(PointFeature center, double radius, PointFeature start,
                                                    Operation creator)
        {
            // Do we already have a suitable circle object? If not, create one.
            Circle circle = AddCircle(center, radius);

            // Create an arc that runs all the way round the circle.
            // If an explicit start point has been supplied, that location
            // will be treated as the BC (and EC) of the new curve. Otherwise
            // we'll define an arbitrary location at the top of the circle.

            // The arc is regarded as a construction line (with blank entity type)
            IEntity e = EnvironmentContainer.FindBlankEntity();

            PointFeature bc = start;
            if (bc==null)
            {
                IPosition p = new Position(center.X, center.Y+radius);
                bc = AddPoint(p, e, creator);
            }

            // Create a clockwise arc.
            LineFeature curve = AddCircularArc(circle, bc, bc, true, e, creator);

            // The line is NEVER topological.
            //pArc->SetTopology(FALSE);

            // Get the window of the circle, and ensure the map's window
	        // is big enough to enclose it. The only other place this is
	        // done is in CeMap::AddLocation. However, since a circle may
	        // extend beyond any defined location, we need to do it here
	        // too.
            //CeWindow win(*pCurve);
            //m_Window.Expand(win);

            return (ArcFeature)curve;
        }

        /// <summary>
        /// Add a circular arc to the map
        /// </summary>
        /// <param name="circle">The circle on which the arc lies. This will be modified to refer
        /// to the newly created arc.</param>
        /// <param name="start">The point at the start of arc. This will be modified to refer
        /// to the newly created arc.</param>
        /// <param name="end">The point at the end of arc. This will be modified to refer
        /// to the newly created arc.</param>
        /// <param name="clockwise">True if the arc is clockwise.</param>
        /// <param name="lineEnt">The entity type for the new line.</param>
        /// <param name="creator">The editing operation creating the arc</param>
        /// <returns>The newly created line</returns>
        internal LineFeature AddCircularArc(Circle circle, PointFeature start, PointFeature end,
            bool clockwise, IEntity lineEnt, Operation creator)
        {
            ArcFeature result = new ArcFeature(creator, 0, lineEnt, circle, start, end, clockwise);
            //m_Window.Union(result.Extent);
            //m_Index.Add(result);

            // Ensure polygons in the vicinity have been marked for rebuild
            result.MarkPolygons();

            return result;
        }

        /*
        /// <summary>
        /// Rolls back the last operation known to this map. Does not save the map to disk.
        /// </summary>
        /// <param name="cursess">Specify true if the rollback should be restricted
        /// to operations performed during the current session. False if rollback can span
        /// sessions.</param>
        /// <returns>The code identifying the op that was rolled back. Zero if nothing
        /// was rolled back.</returns>
        internal uint Rollback(bool cursess)
        {
            // Go through each session (starting at the last). As soon
            // as we hit a session that can rollback something, we're done.
            int status = 0;

            if (cursess)
            {
                status = Session.WorkingSession.Rollback();
            }
            else
            {
                for (int i=m_Sessions.Count-1; i>=0 && status==0; i--)
                {
                    Session s = m_Sessions[i];
                    status = s.Rollback();
                }
            }

            // Return >0 only if we successfully rolled back; 0 if there was nothing to rollback,
            // or some error occurred during rollback.
            if (status>0)
            {
                CleanEdit();
                return (uint)status;
            }

            return 0;
        }
        */

        /// <summary>
        /// Rolls back the last operation known to this map.
        /// </summary>
        /// <param name="s">The session to rollback (all subsequent sessions must be empty)</param>
        /// <returns>True if rollback succeeded</returns>
        internal bool Rollback(Session s)
        {
            if (s.Revision!=0)
                return false;

            int editSequence = s.Rollback();
            if (editSequence <= 0)
                return false;

            CleanEdit();

            // Remove the edit from the database
            using (IConnection ic = ConnectionFactory.Create())
            {
                string sql = String.Format("DELETE FROM [ced].[Edits] WHERE [SessionId]={0} AND [EditSequence]={1}",
                                s.Id, editSequence);
                SqlCommand cmd = new SqlCommand(sql, ic.Value);
                cmd.ExecuteNonQuery();
            }

            // If the session is now empty, and it's not the working session, remove it from the model
            if (s.OperationCount==0 && !Object.ReferenceEquals(s, Session.WorkingSession))
                m_Sessions.Remove(s);

            return true;
        }

        /// <summary>
        /// Obtains a terminal at the specified position.
        /// </summary>
        /// <param name="p">The position of interest</param>
        /// <returns>The corresponding terminal (created if a terminal doesn't already exist
        /// at the position of interest)</returns>
        internal ITerminal GetTerminal(IPosition p)
        {
            // Check whether we have an existing point feature or intersection
            PointGeometry pg = PointGeometry.Create(p);
            ITerminal t = m_Index.FindTerminal(pg);
            if (t!=null)
                return t;

            // Create an intersection and return that
            Intersection x = new Intersection(pg);
            m_Index.AddIntersection(x);
            return x;
        }

        /// <summary>
        /// Is an editing operation currently being saved? This implementation currently
        /// returns false (always), because the notion of a commit-in-progress does not seem
        /// relevant in the new implementation of the Cadastral Editor (though this may need
        /// to be considered again).
        /// </summary>
        internal bool IsCommittingEdit
        {
            get { return false; }
        }

        /// <summary>
        /// Adds a new miscellaneous text label. 
        /// </summary>
        /// <param name="creator">The editing operation creating the text</param>
        /// <param name="s">The text string.</param>
        /// <param name="ent">The entity type for the string.</param>
        /// <param name="vtx">The position of the top-left corner of the first character of the text.</param>
        /// <param name="height">The height of the text, in meters on the ground.</param>
        /// <param name="width">The width of the text, in meters on the ground.</param>
        /// <param name="rotation">The clockwise rotation of the text, in radians from the horizontal.</param>
        /// <returns>The newly created text</returns>
        internal TextFeature AddMiscText(Operation creator, string s, IEntity ent, IPosition vtx, double height,
                                            double width, double rotation)
        {
            // Create the "geometry"
            PointGeometry topLeft = PointGeometry.Create(vtx);
            MiscTextGeometry text = new MiscTextGeometry(s, topLeft, ent.Font, height, width, (float)rotation);

            // If an entity type has not been given, get the default entity type for text.
            IEntity newEnt = ent;
            if (newEnt==null)
                newEnt = DefaultTextType;

            if (newEnt==null)
                throw new Exception("CadastralMapModel.AddMiscText - Unspecified entity type.");

            // Do standard stuff for adding a label.
            return new MiscTextFeature(creator, 0, newEnt, text);
        }

        /// <summary>
        /// Adds a label that is based on a reserved ID.
        /// </summary>
        /// <param name="creator">The editing operation creating the text</param>
        /// <param name="polygonId">The reserved ID and entity type for the the label.</param>
        /// <param name="vtx">The reference position of the label (the position of the top-left corner of the first character
        /// of the text).</param>
        /// <param name="height">The height of the text, in meters on the ground.</param>
        /// <param name="width">The width of the text, in meters on the ground.</param>
        /// <param name="rotation">The clockwise rotation of the text, in radians from the horizontal.</param>
        /// <returns>The newly created text</returns>
        internal TextFeature AddKeyLabel(Operation creator, IdHandle polygonId, IPosition vtx,
                                            double height, double width, double rotation)
        {
            // Exit with error if the key is not reserved.
            if (!polygonId.IsReserved)
                throw new ArgumentException("CadastralMapMode.AddKeyLabel - ID is undefined.");

            // Add the label.
            IEntity ent = polygonId.Entity;
            PointGeometry p = PointGeometry.Create(vtx);
            KeyTextGeometry text = new KeyTextGeometry(p, ent.Font, height, width, (float)rotation);
            TextFeature label = new KeyTextFeature(creator, 0, ent, text);

            // Define the label's ID
      		polygonId.CreateId(label);
            text.Label = label;

            return label;
        }

        /// <summary>
        /// Adds a label that is based on a key text
        /// </summary>
        /// <param name="creator">The editing operation creating the text</param>
        /// <param name="ent">The entity type for the label.</param>
        /// <param name="vtx">The reference position of the label</param>
        /// <param name="height">The height of the text, in meters on the ground.</param>
        /// <param name="width">The width of the text, in meters on the ground.</param>
        /// <param name="rotation">The clockwise rotation of the text, in radians from the horizontal.</param>
        /// <returns>The newly created text</returns>
        internal TextFeature AddKeyLabel(Operation creator, IEntity ent, IPosition vtx,
                                            double height, double width, double rotation)
        {
            // Create a key-text primitive.
            PointGeometry pos = PointGeometry.Create(vtx);
            KeyTextGeometry text = new KeyTextGeometry(pos, ent.Font, height, width, (float)rotation);

            // Do standard stuff for adding a label
            TextFeature result = new KeyTextFeature(creator, 0, ent, text);
            text.Label = result;
            return result;
        }

        /// <summary>
        /// Adds a label that is based on a row. This version covers cases where the
        /// label is replacing a label on a base layer (see the <see cref="ReplaceTextOperation"/>).
        /// </summary>
        /// <param name="creator">The editing operation creating the text</param>
        /// <param name="ent">The entity type for the label.</param>
        /// <param name="vtx">The reference position of the label</param>
        /// <param name="id">The user-perceived ID for the label</param>
        /// <param name="row">The row to attach to the label.</param>
        /// <param name="atemplate">The template for the row text.</param>
        /// <param name="height">The height of the text, in meters on the ground.</param>
        /// <param name="width">The width of the text, in meters on the ground.</param>
        /// <param name="rotation">The clockwise rotation of the text, in radians from the horizontal.</param>
        /// <returns>The newly created text</returns>
        internal TextFeature AddRowLabel(Operation creator, IEntity ent, IPosition vtx, FeatureId id,
                                            DataRow row, ITemplate atemplate,
                                            double height, double width, double rotation)
        {
            // Add the label with null geometry for now (chicken and egg -- need Feature in order
            // to create the Row object that's needed for the RowTextGeometry)
            TextFeature label = new RowTextFeature(creator, 0, ent, null);

            // Define the label's ID and attach the row to it
            Row r = new Row(id, atemplate.Schema, row);

            // Attach the geometry
            PointGeometry p = PointGeometry.Create(vtx);
            RowTextGeometry text = new RowTextGeometry(r, atemplate, p, ent.Font, height, width, (float)rotation);
            label.TextGeometry = text;

            return label;
        }

        /// <summary>
        /// Adds a label that is based on a row.
        /// </summary>
        /// <param name="creator">The editing operation creating the text</param>
        /// <param name="polygonId">The ID and entity type to assign to the label.</param>
        /// <param name="vtx">The reference position of the label.</param>
        /// <param name="row">The row to attach to the label.</param>
        /// <param name="atemplate">The template for the row text.</param>
        /// <param name="height">The height of the text, in meters on the ground.</param>
        /// <param name="width">The width of the text, in meters on the ground.</param>
        /// <param name="rotation">The clockwise rotation of the text, in radians from the horizontal.</param>
        /// <returns>The newly created text</returns>
        internal TextFeature AddRowLabel(Operation creator, IdHandle polygonId, IPosition vtx, DataRow row,
            ITemplate atemplate, double height, double width, double rotation)
        {
            // Exit with error if the key is not reserved.
            if (!polygonId.IsReserved)
                throw new ArgumentException();

            // Add the label with null geometry for now (chicken and egg -- need Feature in order
            // to create the Row object that's needed for the RowTextGeometry)
            IEntity ent = polygonId.Entity;
            TextFeature label = new RowTextFeature(creator, 0, ent, null);

            // Define the label's ID and attach the row to it
            FeatureId id = polygonId.CreateId(label);
            Row r = new Row(id, atemplate.Schema, row);

            // Attach the geometry
            PointGeometry p = PointGeometry.Create(vtx);
            RowTextGeometry text = new RowTextGeometry(r, atemplate, p, ent.Font, height, width, (float)rotation);
            label.TextGeometry = text;

            return label;
        }

        /// <summary>
        /// Loads this model from the database
        /// </summary>
        /// <param name="job">The job to load</param>
        /// <param name="user">The user who is doing the load</param>
        internal void Load(Job job, User user)
        {
            m_Sessions.Clear();
            SessionData.Load(this, job, user);

            // Intersect topological lines that aren't marked for deletion
            Trace.Write("Intersecting lines");

            // First mark all lines as "moved" (they get ignored by the intersect
            // finder until they have been intersected themselves)
            MarkAllLinesMoved();

            m_Index.QueryWindow(null, SpatialType.Line, delegate (ISpatialObject item)
            {
                LineFeature line = (LineFeature)item;
                line.IsMoved = false;
                line.Split(null);
                return true;
            });

            // Now build the topology for the map
            BuildPolygons();

            // Initialize ID handling. This associates ID allocations with their
            // corresponding ID group.
            m_IdManager.Load(this, job, user);

            // Now go through the sessions to notify the ID manager about IDs
            // that have been used
            foreach (Session s in m_Sessions)
            {
                if (s.Job.JobId == job.JobId && s.User.UserId == user.UserId)
                    s.LoadUsedIds(m_IdManager);
            }
        }

        /// <summary>
        /// Marks all spatially indexed lines as "moved". Lines marked in this way
        /// will be ignored by intersect detection software until they themselves
        /// have been intersected against the map.
        /// </summary>
        void MarkAllLinesMoved()
        {
            m_Index.QueryWindow(null, SpatialType.Line, delegate(ISpatialObject item)
            {
                LineFeature line = (LineFeature)item;
                line.IsMoved = true;
                return true;
            });
        }

        /// <summary>
        /// Initializes the number of elements in this model's session list
        /// </summary>
        /// <param name="numSession">The size of the session list.</param>
        internal void SetSessionCapacity(int numSession)
        {
            m_Sessions.Capacity = numSession;
        }

        /// <summary>
        /// Remembers a session as part of this model
        /// </summary>
        /// <param name="s"></param>
        internal void AddSession(Session s)
        {
            m_Sessions.Add(s);
        }

        /// <summary>
        /// Creates a new editing session and appends to this model. This is called by
        /// the editing controller to represent the latest session (as opposed to a historical
        /// session that is created during initial data loading).
        /// </summary>
        /// <returns>The created session</returns>
        internal Session AppendWorkingSession(Job job, User user)
        {
            SessionData data = SessionData.Insert(job.JobId, user.UserId);

            // Create the session (and remember as part of this model)
            Session s = Session.CreateCurrentSession(this, data, user, job);
            m_WorkingSession = s;
            return s;
        }

        /// <summary>
        /// The object that manages assignment of user-specified IDs
        /// </summary>
        internal IdManager IdManager
        {
            get { return m_IdManager; }
        }

        /// <summary>
        /// The session that we are currently appending to. Defined on a call to
        /// <see cref="AppendWorkingSession"/>.
        /// </summary>
        /// <remarks>As a convenience, you can also access this via the
        /// static <see cref="Session.WorkingSession"/> property</remarks>
        internal Session WorkingSession
        {
            get { return m_WorkingSession; }
        }

        /// <summary>
        /// Scans the editing sessions that make up this model, counting up the number of
        /// edits that have not been published.
        /// </summary>
        /// <returns>The number of unpublished edits</returns>
        internal uint GetUnpublishedEditCount()
        {
            uint total = 0;

            for (int i=m_Sessions.Count-1; i>=0; i--)
            {
                Session s = m_Sessions[i];
                if (s.Revision!=0)
                    break;

                total += (uint)s.OperationCount;
            }

            return total;
        }

        /// <summary>
        /// Marks unpublished sessions with the specified revision number.
        /// </summary>
        /// <param name="revision">The revision number of a publication</param>
        internal void SetPublished(uint revision)
        {
            for (int i=m_Sessions.Count-1; i>=0; i--)
            {
                Session s = m_Sessions[i];
                if (s.Revision!=0)
                    break;

                s.Revision = revision;
            }
        }

        /// <summary>
        /// Creates the spatial index
        /// </summary>
        internal void CreateIndex()
        {
            m_Index = new EditingIndex();

            foreach (Session s in m_Sessions)
                s.AddToIndex();
        }

        /// <summary>
        /// Records a new feature loaded as part of this model. This indexes the
        /// feature by it's internal ID.
        /// </summary>
        /// <param name="f">The feature that has been loaded (if null, nothing gets done)</param>
        internal void AddFeature(Feature f)
        {
            if (f != null)
            {
                try { m_Features.Add(f.InternalId, f); }
                catch { throw new Exception("Failed to index feature " + f.DataId); }
            }
        }

        /// <summary>
        /// Attempts to locate an editing operation based on its internal ID.
        /// </summary>
        /// <param name="s">The formatted version of an internal ID (as produced
        /// by a prior call to <see cref="InternalIdValue.Format"/>)</param>
        /// <returns>The corresponding operation (null if not found)</returns>
        internal Operation FindOperation(string s)
        {
            InternalIdValue id = new InternalIdValue(s);
            return FindOperation(id);
        }

        /// <summary>
        /// Attempts to locate an editing operation based on its internal ID.
        /// </summary>
        /// <param name="id">The ID to look for</param>
        /// <returns>The corresponding operation (null if not found)</returns>
        internal Operation FindOperation(InternalIdValue id)
        {
            Session s = FindSession(id.SessionId);

            if (s == null)
                return null;
            else
                return s.FindOperation(id.ItemSequence);
        }

        /// <summary>
        /// Attempts to locate an editing session within this model
        /// </summary>
        /// <param name="sessionId">The sequence number of the session to look for</param>
        /// <returns>The corresponding session (null if not found)</returns>
        internal Session FindSession(uint sessionId)
        {
            return m_Sessions.Find(delegate(Session s) { return s.Id==sessionId; });
        }

        /// <summary>
        /// Attempts to locate a spatial feature based on its internal ID.
        /// This is an indexed lookup.
        /// </summary>
        /// <param name="s">The formatted version of an internal ID (as produced
        /// by a prior call to <see cref="InternalIdValue.Format"/>)</param>
        /// <returns>The corresponding feature (null if not found)</returns>
        public T Find<T>(string s) where T : Feature
        {
            InternalIdValue id = new InternalIdValue(s);
            return Find<T>(id);
        }

        /// <summary>
        /// Attempts to locate a spatial feature based on its internal ID.
        /// This is an indexed lookup.
        /// </summary>
        /// <param name="id">The ID to look for</param>
        /// <returns>The corresponding feature (null if not found)</returns>
        internal T Find<T>(InternalIdValue id) where T : Feature
        {
            Feature f;
            if (m_Features.TryGetValue(id, out f))
                return (f as T);
            else
                return null;
        }

        /// <summary>
        /// Tries to obtains a native ID for a feature.
        /// </summary>
        /// <param name="rawId">The raw ID to look for</param>
        /// <returns>The ID that corresponds to the supplied key. Null if there is no matching ID - make
        /// a call to <see cref="AddNativeId"/> to register a new ID.</returns>
        internal NativeId FindNativeId(uint rawId)
        {
            NativeId result;

            if (m_NativeIds.TryGetValue(rawId, out result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Creates a new native ID and remembers it as part of this model. This will fail
        /// if a native key with the same key has already been added - first make a call
        /// to <see cref="FindNativeId"/> to see if the add is needed.
        /// </summary>
        /// <param name="rawId">The raw key for the new ID</param>
        /// <returns>The corresponding ID (not null)</returns>
        /// <exception cref="ArgumentException">If an ID group that encloses the specified
        /// raw key cannot be found</exception>
        internal NativeId AddNativeId(uint rawId)
        {
            IdGroup group = m_IdManager.FindGroupByRawId(rawId);
            Debug.Assert(group != null);
            NativeId result = new NativeId(group, rawId);
            m_NativeIds.Add(rawId, result);
            return result;
        }

        /// <summary>
        /// Tries to obtains a foreign ID for a feature.
        /// </summary>
        /// <param name="key">The formatted key to look for</param>
        /// <returns>The ID that corresponds to the supplied key. Null if there is no matching ID - make
        /// a call to <see cref="AddForeignId"/> to register a new ID.</returns>
        internal ForeignId FindForeignId(string key)
        {
            ForeignId result;

            if (m_ForeignIds.TryGetValue(key, out result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Creates a new foreign ID and remembers it as part of this model. This will fail
        /// if a foreign key with the same key has already been added - first make a call
        /// to <see cref="FindForeignId"/> to see if the add is needed.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The new ID object</returns>
        internal ForeignId AddForeignId(string key)
        {
            ForeignId result = new ForeignId(key);
            m_ForeignIds.Add(key, result);
            return result;
        }

        /// <summary>
        /// Remembers a foreign ID as part of this model.
        /// </summary>
        /// <param name="fid">The ID to index as part of this model</param>
        internal void AddForeignId(ForeignId fid)
        {
            m_ForeignIds.Add(fid.FormattedKey, fid);
        }

        /// <summary>
        /// Obtains all loaded feature IDs
        /// </summary>
        /// <returns>The native and foreign IDs that have been loaded (may be an empty array)</returns>
        internal FeatureId[] GetFeatureIds()
        {
            int numNative = m_NativeIds.Count;
            int numForeign = m_ForeignIds.Count;
            List<FeatureId> result = new List<FeatureId>(numNative + numForeign);

            foreach (NativeId nid in m_NativeIds.Values)
                result.Add(nid);

            foreach (ForeignId fid in m_ForeignIds.Values)
                result.Add(fid);

            return result.ToArray();
        }

        /// <summary>
        /// Writes a snapshot of the geometry associated with this model. The files created in
        /// this way are expected to have a short lifetime (there is no provision for
        /// evolving the content). They get created to represent snapshots of the
        /// model while the user experiments with updates. This is meant to make it
        /// easy to undo changes that the user decides to discard.
        /// </summary>
        /// <param name="fileName">The name of the file to create</param>
        internal void WriteGeometry(string fileName)
        {
            using (Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, this);
            }
        }

        /// <summary>
        /// Obtains the edits that are dependent on features created by a specific edit.
        /// This is used by dialogs that provide rollforward previews. It returns 
        /// the subsequent operations that could be impacted as a result of the change.
        /// </summary>
        /// <param name="op">The operation in which the change will occur</param>
        /// <returns>The dependent edits (starting with the supplied editing operation)</returns>
        internal Operation[] Touch(Operation op)
        {
            // If the edit didn't create any spatial data, it can't impact any
            // other edits.
            if (op.FeatureCount == 0)
                return new Operation[] { op };

            // Locate the session containing the edit
            int sessionIndex = m_Sessions.FindIndex(delegate(Session s)
            {
                return object.ReferenceEquals(s, op.Session);
            });

            if (sessionIndex < 0)
                throw new Exception("Cannot locate editing session for edit");

            // Process the session containing the edit
            List<Operation> result = new List<Operation>();
            m_Sessions[sessionIndex].Touch(result, op);

            // Process all subsequent sessions
            for (int i=sessionIndex+1; i<m_Sessions.Count; i++)
                m_Sessions[i].Touch(result, null);

            // Express the edits we found as an array
            return result.ToArray();
        }
    }
}
