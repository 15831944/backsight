/// <remarks>
/// Copyright 2008 - Steve Stanton. This file is part of Backsight
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
using System.IO;
using System.Xml.Serialization;

using Backsight.Forms;

namespace Backsight.Editor
{
    /// <written by="Steve Stanton" on="29-APR-2008" />
    /// <summary>
    /// Information about a mapping job that gets saved as a disk file (with the
    /// file type <c>.cedx</c>). The expectation is that the file type will be
    /// associated with the Cadastral Editor application so that the application
    /// can be launched with a double-click.
    /// </summary>
    [XmlRoot]
    public class JobFileInfo
    {
        #region Constants

        /// <summary>
        /// The file extension for job files is ".cedx"
        /// </summary>
        public const string TYPE = ".cedx";

        #endregion

        #region Class data

        /// <summary>
        /// Have changes been made to the values stored in this instance?
        /// Set to <c>true</c> on a call to <see cref="Set"/>. Set to <c>false</c>
        /// on a call to <see cref="WriteXML"/>.
        /// </summary>
        bool m_IsChanged;

        /// <summary>
        /// The database connection string
        /// </summary>
        string m_ConnectionString;

        /// <summary>
        /// The ID of the job that should be accessed (0 if not known)
        /// </summary>
        uint m_JobId;

        /// <summary>
        /// Information about the area that was last drawn.
        /// </summary>
        DrawInfo m_DrawInfo;

        /// <summary>
        /// Current display units
        /// </summary>
        DistanceUnitType m_DisplayUnit;

        /// <summary>
        /// Current data entry units
        /// </summary>
        DistanceUnitType m_EntryUnit;

        /// <summary>
        /// Should feature IDs be assigned automatically? (false if the user must specify).
        /// </summary>
        bool m_AutoNumber;

        /// <summary>
        /// Scale denominator at which labels (text) will start to be drawn.
        /// </summary>
        double m_ShowLabelScale;

        /// <summary>
        /// Scale denominator at which points will start to be drawn.
        /// </summary>
        double m_ShowPointScale;

        /// <summary>
        /// Height of point symbols, in meters on the ground.
        /// </summary>
        double m_PointHeight;

        /// <summary>
        /// Should intersection points be drawn? Relevant only if points
        /// are drawn at the current display scale (see the <see cref="ShowPointScale"/>
        /// property).
        /// </summary>
        bool m_AreIntersectionsDrawn;

        /// <summary>
        /// The nominal map scale, for use in converting the size of fonts.
        /// </summary>
        uint m_MapScale;

        /// <summary>
        /// The style for annotating lines with distances (and angles)
        /// </summary>
        LineAnnotationStyle m_Annotation;

        /// <summary>
        /// The ID of the default entity type for points (0 if undefined)
        /// </summary>
        int m_DefaultPointType;

        /// <summary>
        /// The ID of the default entity type for lines (0 if undefined)
        /// </summary>
        int m_DefaultLineType;

        /// <summary>
        /// The ID of the default entity type for polygon labels (0 if undefined)
        /// </summary>
        int m_DefaultPolygonType;

        /// <summary>
        /// The ID of the default entity type for text (0 if undefined)
        /// </summary>
        int m_DefaultTextType;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor (for serialization mechanism)
        /// </summary>
        public JobFileInfo()
        {
            m_ConnectionString = String.Empty;
            m_JobId = 0;
            m_DrawInfo = new DrawInfo(0.0, 0.0, 0.0);
            m_DisplayUnit = DistanceUnitType.AsEntered;
            m_EntryUnit = DistanceUnitType.Meters;
            m_AutoNumber = true;
            m_ShowLabelScale = 2000.0;
            m_ShowPointScale = 2000.0;
            m_PointHeight = 2.0;
            m_AreIntersectionsDrawn = false;
            m_MapScale = 2000;
            m_Annotation = new LineAnnotationStyle();
            m_IsChanged = false;
            m_DefaultPointType = 0;
            m_DefaultLineType = 0;
            m_DefaultPolygonType = 0;
            m_DefaultTextType = 0;
        }

        #endregion

        /// <summary>
        /// Method called whenever values of this class are changed. This just ensures
        /// that <see cref="m_IsChanged"/> gets set.
        /// </summary>
        /// <typeparam name="T">The type of value that's being changed</typeparam>
        /// <param name="value">The value to assign</param>
        /// <returns>The supplied value</returns>
        T Set<T>(T value)
        {
            m_IsChanged = true;
            return value;
        }

        /// <summary>
        /// Reads job information from an XML file.
        /// </summary>
        /// <param name="fileName">The file spec for the input data</param>
        /// <returns>The data read from the input file</returns>
        public static JobFileInfo CreateInstance(string fileName)
        {
            XmlSerializer xs = new XmlSerializer(typeof(JobFileInfo));
            using (TextReader reader = new StreamReader(fileName))
            {
                JobFileInfo result = (JobFileInfo)xs.Deserialize(reader);
                result.m_IsChanged = false;
                return result;
            }
        }

        /// <summary>
        /// Writes job information to an XML file.
        /// </summary>
        /// <param name="fileName">The output file (to create)</param>
        public void WriteXML(string fileName)
        {
            // Create the directory if it doesn't already exist
            string dir = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            XmlSerializer xs = new XmlSerializer(typeof(JobFileInfo));
            using (TextWriter writer = new StreamWriter(fileName))
            {
                xs.Serialize(writer, this);
                m_IsChanged = false;
            }
        }

        /// <summary>
        /// The database connection string
        /// </summary>
        [XmlElement]
        public string ConnectionString
        {
            get { return m_ConnectionString; }
            set { m_ConnectionString = Set<string>(value); }
        }

        /// <summary>
        /// The ID of the job that should be accessed (0 if not known)
        /// </summary>
        [XmlElement]
        public uint JobId
        {
            get { return m_JobId; }
            set { m_JobId = Set<uint>(value); }
        }

        /// <summary>
        /// Information about the area that was last drawn.
        /// </summary>
        [XmlElement]
        public DrawInfo LastDraw
        {
            get { return m_DrawInfo; }
            set { m_DrawInfo = Set<DrawInfo>(value); }
        }

        /// <summary>
        /// Current display units
        /// </summary>
        [XmlElement("DisplayUnit")]
        public DistanceUnitType DisplayUnitType
        {
            get { return m_DisplayUnit; }
            set { m_DisplayUnit = Set<DistanceUnitType>(value); }
        }

        /// <summary>
        /// Current data entry units
        /// </summary>
        [XmlElement("EntryUnit")]
        public DistanceUnitType EntryUnitType
        {
            get { return m_EntryUnit; }
            set { m_EntryUnit = Set<DistanceUnitType>(value); }
        }

        /// <summary>
        /// Should feature IDs be assigned automatically? (false if the user must specify).
        /// </summary>
        [XmlElement("AutoNumber")]
        public bool IsAutoNumber
        {
            get { return m_AutoNumber; }
            set { m_AutoNumber = Set<bool>(value); }
        }

        /// <summary>
        /// Scale denominator at which labels (text) will start to be drawn.
        /// </summary>
        [XmlElement("LabelScale")]
        public double ShowLabelScale
        {
            get { return m_ShowLabelScale; }
            set { m_ShowLabelScale = Set<double>(value); }
        }

        /// <summary>
        /// Scale denominator at which points will start to be drawn.
        /// </summary>
        [XmlElement("PointScale")]
        public double ShowPointScale
        {
            get { return m_ShowPointScale; }
            set { m_ShowPointScale = Set<double>(value); }
        }

        /// <summary>
        /// Height of point symbols, in meters on the ground.
        /// </summary>
        [XmlElement]
        public double PointHeight
        {
            get { return m_PointHeight; }
            set { m_PointHeight = Set<double>(value); }
        }

        /// <summary>
        /// Should intersection points be drawn? Relevant only if points
        /// are drawn at the current display scale (see the <see cref="ShowPointScale"/>
        /// property).
        /// </summary>
        [XmlElement("IntersectionsDrawn")]
        public bool AreIntersectionsDrawn
        {
            get { return m_AreIntersectionsDrawn; }
            set { m_AreIntersectionsDrawn = Set<bool>(value); }
        }

        /// <summary>
        /// The nominal map scale, for use in converting the size of fonts.
        /// </summary>
        [XmlElement]
        public uint NominalMapScale
        {
            get { return m_MapScale; }
            set { m_MapScale = Set<uint>(value); }
        }

        /// <summary>
        /// The style for annotating lines with distances (and angles)
        /// </summary>
        [XmlElement]
        public LineAnnotationStyle LineAnnotation
        {
            get { return m_Annotation; }
            set { m_Annotation = Set<LineAnnotationStyle>(value); }
        }

        /// <summary>
        /// The ID of the default entity type for points (0 if undefined)
        /// </summary>
        [XmlElement]
        public int DefaultPointType
        {
            get { return m_DefaultPointType; }
            set { m_DefaultPointType = Set<int>(value); }
        }

        /// <summary>
        /// The ID of the default entity type for lines (0 if undefined)
        /// </summary>
        [XmlElement]
        public int DefaultLineType
        {
            get { return m_DefaultLineType; }
            set { m_DefaultLineType = Set<int>(value); }
        }

        /// <summary>
        /// The ID of the default entity type for polygons (0 if undefined)
        /// </summary>
        [XmlElement]
        public int DefaultPolygonType
        {
            get { return m_DefaultPolygonType; }
            set { m_DefaultPolygonType = Set<int>(value); }
        }

        /// <summary>
        /// The ID of the default entity type for text (0 if undefined)
        /// </summary>
        [XmlElement]
        public int DefaultTextType
        {
            get { return m_DefaultTextType; }
            set { m_DefaultTextType = Set<int>(value); }
        }
    }
}
