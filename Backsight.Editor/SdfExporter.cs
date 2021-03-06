﻿using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

using OSGeo.FDO;
using OSGeo.FDO.ClientServices;
using OSGeo.FDO.Commands;
using OSGeo.FDO.Commands.DataStore;
using OSGeo.FDO.Connections;
using OSGeo.FDO.Commands.SpatialContext;
using OSGeo.FDO.Geometry;
using OSGeo.FDO.Schema;
using OSGeo.FDO.Commands.Schema;
using OSGeo.FDO.Commands.Feature;
using OSGeo.FDO.Expression;

namespace Backsight.Editor
{
    /// <summary>
    /// Experimental export to Autodesk SDF format.
    /// </summary>
    class SdfExporter
    {
        #region Class data

        readonly FgfGeometryFactory m_Factory;
        IConnection m_Connection;
        int m_NumOk;
        int m_NumFail;

        #endregion

        #region Constructor

        internal SdfExporter()
        {
            m_Factory = new FgfGeometryFactory();
        }

        #endregion

        internal void Export(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            /*
            IProviderRegistry pr = FeatureAccessManager.GetProviderRegistry();
            ProviderCollection pc = pr.GetProviders();
            MessageBox.Show("Number of providers=" + pc.Count);
            */

            try
            {
                IConnectionManager cm = FeatureAccessManager.GetConnectionManager();
                m_Connection = cm.CreateConnection("SDFProvider.dll");
                //m_Connection = cm.CreateConnection("OSGeo.SDF.3.6");
                //m_Connection = cm.CreateConnection("OSGeo.SQLite.3.6");
                RunExport(fileName);
                MessageBox.Show(String.Format("nok={0}  nFail={1}", m_NumOk, m_NumFail));
            }

            finally
            {
                if (m_Connection != null)
                {
                    m_Connection.Dispose();
                    m_Connection = null;
                }
            }
        }

        void RunExport(string fileName)
        {
            CadastralMapModel mapModel = CadastralMapModel.Current;

            using (ICreateDataStore cmd = m_Connection.CreateCommand(CommandType.CommandType_CreateDataStore) as ICreateDataStore)
            {
                try
                {
                    cmd.DataStoreProperties.SetProperty("File", fileName);
                    cmd.Execute();
                }
                catch (OSGeo.FDO.Common.Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // The connection after the created is ConnectionState_Closed, so open it!
            m_Connection.ConnectionInfo.ConnectionProperties.SetProperty("File", fileName);
            m_Connection.Open();

            // Define coordinate system
            using (ICreateSpatialContext cmd = m_Connection.CreateCommand(CommandType.CommandType_CreateSpatialContext) as ICreateSpatialContext)
            {
                ISpatialSystem ss = mapModel.SpatialSystem;
                cmd.CoordinateSystem = ss.Name; // CSMap key name
                cmd.ExtentType = SpatialContextExtentType.SpatialContextExtentType_Static;
                IWindow mapExtent = mapModel.Extent;
                IDirectPosition minxy = m_Factory.CreatePositionXY(mapExtent.Min.X, mapExtent.Min.Y);
                IDirectPosition maxxy = m_Factory.CreatePositionXY(mapExtent.Max.X, mapExtent.Max.Y);
                IEnvelope extent = m_Factory.CreateEnvelope(minxy, maxxy);
                IGeometry gx = m_Factory.CreateGeometry(extent);
                cmd.Extent = m_Factory.GetFgf(gx);
                cmd.XYTolerance = 0.000001; // resolution?
                cmd.CoordinateSystemWkt = EditingController.Current.GetCoordinateSystemText();
                cmd.Execute();
            }

            // Define feature schema
            FeatureSchema fs = new FeatureSchema("Steve", "This is a test");

            FeatureClass fc = new FeatureClass("FC", "Test feature class");
            fs.Classes.Add(fc);
            GeometricPropertyDefinition gp = new GeometricPropertyDefinition("Geometry", "Polygon property");

            // When you stick more than one geometric type into the output, you can't
            // convert to SHP (not with FDO Toolbox anyway).
            //gp.GeometryTypes = (int)GeometricType.GeometricType_Surface;
            gp.GeometryTypes = (int)GeometricType.GeometricType_All;
            fc.Properties.Add(gp);
            fc.GeometryProperty = gp;

            // c.f. FdoToolbox ExpressUtility
            DataPropertyDefinition dp = new DataPropertyDefinition("ID", "Test ID");
            dp.DataType = DataType.DataType_Int32;
            dp.Nullable = false;
            dp.ReadOnly = true;
            dp.IsAutoGenerated = true;
            fc.Properties.Add(dp);

            // Feature class requires an identity column for the insert
            fc.IdentityProperties.Add(dp);

            using (IApplySchema cmd = m_Connection.CreateCommand(CommandType.CommandType_ApplySchema) as IApplySchema)
            {
                cmd.FeatureSchema = fs;
                cmd.Execute();
            }

            mapModel.Index.QueryWindow(null, SpatialType.Polygon | SpatialType.Point, ExportFeature);

            m_Connection.Flush();
            m_Connection.Close();
        }

        /// <summary>
        /// Something that processes an item in the index (for use with implementations
        /// of the <c>ISpatialIndex.QueryWindow</c> method).
        /// </summary>
        /// <param name="item">An object associated with the spatial index</param>
        /// <returns>True if the query should be continued. False if the query should be
        /// terminated (e.g. a result may have been obtained).</returns>
        public bool ExportFeature(ISpatialObject item)
        {
            // Islands only get exported in the context of their enclosing polygon
            if (item is Island)
                return true;

            if (item.SpatialType == SpatialType.Point)
                ExportPoint((PointFeature)item);
            else if (item.SpatialType == SpatialType.Polygon)
                //ExportCurvePolygon((Polygon)item);
                ExportPolygon((Polygon)item);
            else
                throw new NotSupportedException("Unexpected spatial type: " + item.SpatialType);

            return true;
        }

        void ExportCurvePolygon(Polygon pol)
        {
            // Get the exterior ring
            IRing xr = GetRing(pol);

            // Pick up any islands
            RingCollection irs = new RingCollection();
            if (pol.HasAnyIslands)
            {
                foreach (Island i in pol.Islands)
                {
                    IRing ir = GetRing(i);
                    irs.Add(ir);
                }
            }

            IGeometry g = m_Factory.CreateCurvePolygon(xr, irs);
            ExportGeometry(g);
        }

        void ExportGeometry(IGeometry g)
        {
            // c.f. FdoToolbox bulk copy? - FdoBatchedOutputOperation
            using (IInsert cmd = m_Connection.CreateCommand(CommandType.CommandType_Insert) as IInsert)
            {
                cmd.SetFeatureClassName("FC");

                PropertyValueCollection pvc = cmd.PropertyValues;

                //PropertyValue pvId = new PropertyValue();
                //pvId.SetName("ID");
                //pvId.Value = new Int32Value(id);
                //pvc.Add(pvId);

                GeometryValue gv = new GeometryValue(m_Factory.GetFgf(g));
                PropertyValue pvGeom = new PropertyValue();
                pvGeom.SetName("Geometry");
                pvGeom.Value = gv;
                pvc.Add(pvGeom);

                try
                {
                    IFeatureReader reader = cmd.Execute();
                    reader.Dispose();
                    m_NumOk++;
                }

                catch
                {
                    m_NumFail++;
                }
            }
        }

        IRing GetRing(Ring r)
        {
            CurveSegmentCollection csc = new CurveSegmentCollection();

            foreach (IDivider d in r.Edge)
            {
                LineGeometry line = d.LineGeometry;
                ICurveSegmentAbstract cseg = null;

                if (line is SectionGeometry)
                {
                    SectionGeometry section = (line as SectionGeometry);
                    line = section.Make();
                }

                if (line is MultiSegmentGeometry)
                {
                    MultiSegmentGeometry mseg = (line as MultiSegmentGeometry);
                    DirectPositionCollection dpc = GetDirectPositions(mseg.Data);
                    cseg = m_Factory.CreateLineStringSegment(dpc);
                }
                else if (line is SegmentGeometry)
                {
                    SegmentGeometry segment = (line as SegmentGeometry);
                    DirectPositionCollection dpc = GetDirectPositions(new IPosition[] { segment.Start, segment.End });
                    cseg = m_Factory.CreateLineStringSegment(dpc);

                }
                else if (line is ArcGeometry)
                {
                    ArcGeometry arc = (line as ArcGeometry);
                    IPosition mp = arc.GetMidPosition();
                    IDirectPosition bc = m_Factory.CreatePositionXY(arc.BC.X, arc.BC.Y);
                    IDirectPosition mid = m_Factory.CreatePositionXY(mp.X, mp.Y);
                    IDirectPosition ec = m_Factory.CreatePositionXY(arc.EC.X, arc.EC.Y);
                    cseg = m_Factory.CreateCircularArcSegment(bc, mid, ec);
                }
                else
                {
                    throw new NotSupportedException("Unknown line type: "+line.GetType().Name);
                }

                csc.Add(cseg);
            }

            return m_Factory.CreateRing(csc);
        }


        void ExportPolygon(Polygon pol)
        {
            // Get the exterior ring
            ILinearRing xr = GetLinearRing(pol);

            // Pick up any islands
            LinearRingCollection irs = null;
            if (pol.HasAnyIslands)
            {
                irs = new LinearRingCollection();

                foreach (Island i in pol.Islands)
                {
                    ILinearRing ir = GetLinearRing(i);
                    irs.Add(ir);
                }
            }

            IGeometry g = m_Factory.CreatePolygon(xr, irs);
            ExportGeometry(g);
        }

        ILinearRing GetLinearRing(Ring r)
        {
            // Output an approximation for any circular arcs
            ILength curvetol = new Length(0.1);
            IPosition[] outline = r.GetOutline(curvetol);
            DirectPositionCollection dpc = GetDirectPositions(outline);
            return m_Factory.CreateLinearRing(dpc);
        }

        ILineString GetLineString(IPosition[] positions)
        {
            DirectPositionCollection dpc = GetDirectPositions(positions);
            return m_Factory.CreateLineString(dpc);
        }

        DirectPositionCollection GetDirectPositions(IPosition[] positions)
        {
            DirectPositionCollection dpc = new DirectPositionCollection();
            foreach (IPosition p in positions)
            {
                IDirectPosition xy = m_Factory.CreatePositionXY(p.X, p.Y);
                dpc.Add(xy);
            }

            return dpc;
        }

        void ExportPoint(PointFeature p)
        {
            IDirectPosition dp = new DirectPositionImpl(p.X, p.Y);
            IGeometry g = m_Factory.CreatePoint(dp);
            ExportGeometry(g);
        }
    }
}
