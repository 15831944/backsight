USE [Backsight]
GO

-- Create domain tables

CREATE TABLE [dbo].[DOMAIN_DLS_PARCEL_TYPES]
(
  [ShortValue]  [char](2)      NOT NULL CONSTRAINT [DF_DOMAIN_DLS_PARCEL_TYPES_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_DLS_PARCEL_TYPES] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_HALVES_of_DLS_QS_Polygons]
(
  [ShortValue]  [char](1)      NOT NULL CONSTRAINT [DF_DOMAIN_HALVES_of_DLS_QS_Polygons_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_HALVES_of_DLS_QS_Polygons] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Issuing_LTOs]
(
  [ShortValue]  [char](2)      NOT NULL CONSTRAINT [DF_DOMAIN_Issuing_LTOs_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Issuing_LTOs] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Meridian_Values]
(
  [ShortValue]  [char](1)      NOT NULL CONSTRAINT [DF_DOMAIN_Meridian_Values_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Meridian_Values] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Parcel_Types]
(
  [ShortValue]  [char](1)      NOT NULL CONSTRAINT [DF_DOMAIN_Parcel_Types_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Parcel_Types] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Parish_Lot_Types]
(
  [ShortValue]  [char](2)      NOT NULL CONSTRAINT [DF_DOMAIN_Parish_Lot_Types_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Parish_Lot_Types] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Parish_Values]
(
  [ShortValue]  [char](2)      NOT NULL CONSTRAINT [DF_DOMAIN_Parish_Values_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Parish_Values] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Part_of_Cadastral_Polygon]
(
  [ShortValue]  [char](2)      NOT NULL CONSTRAINT [DF_DOMAIN_Part_of_Cadastral_Polygon_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Part_of_Cadastral_Polygon] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Quarter_Sections]
(
  [ShortValue]  [char](2)      NOT NULL CONSTRAINT [DF_DOMAIN_Quarter_Sections_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Quarter_Sections] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_WaterBodyTypes]
(
  [ShortValue]  [char](1)      NOT NULL CONSTRAINT [DF_DOMAIN_WaterBodyTypes_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_WaterBodyTypes] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Plan_Types]
(
  [ShortValue]  [char](1)      NOT NULL CONSTRAINT [DF_DOMAIN_Plan_Types_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Plan_Types] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO

CREATE TABLE [dbo].[DOMAIN_Range_Values]
(
  [ShortValue]  [char](3)      NOT NULL CONSTRAINT [DF_DOMAIN_Range_Values_ShortValue]  DEFAULT (''),
  [LongValue]   [varchar](50)  NOT NULL,
  [Description] [varchar](100) NOT NULL,

  CONSTRAINT [PK_DOMAIN_Range_Values] PRIMARY KEY CLUSTERED ([ShortValue] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]

) ON [PRIMARY]
GO


-- Create data tables


CREATE TABLE [dbo].[CertificateofTitleParcelData]
(
  [PIN]                       [char](9)       NOT NULL,
  [Certificate_of_Title_Name] [varchar](12)   NULL,
  [Parcel_ID]                 [varchar](3)    NULL,

  CONSTRAINT [CertificateofTitleParcelData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[ContourElevationData]
(
	[PIN] 			[char](9) 		NOT NULL,
	[Elevation] 	[float] 		NOT NULL,
	
	CONSTRAINT [ContourElevationData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
	WITH (PAD_INDEX  = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[DLSParcelData]
(
  [PIN]                        [char](9)      NOT NULL,
  [PART]                       [char](2)      NOT NULL CONSTRAINT DF_DLSParcelData_PART DEFAULT '',
  [HALF]                       [char](1)      NOT NULL CONSTRAINT DF_DLSParcelData_HALF DEFAULT '',
  [DLS_PARCEL_TYPE]            [char](2)      NOT NULL CONSTRAINT DF_DLSParcelData_DLS_PARCEL_TYPE DEFAULT '',
  [DLS_LOT_num]                [varchar](3)   NULL,
  [DLS_LEGAL_SUB_DIVISION_num] [int]          NOT NULL CONSTRAINT DF_DLSParcelData_DLS_LEGAL_SUB_DIVISION_num DEFAULT 0,
  [QS_VALUE]                   [char](2)      NOT NULL,
  [SECTION_num]                [int]          NOT NULL CONSTRAINT DF_DLSParcelData_SECTION_num DEFAULT 0,
  [TOWNSHIP_num]               [int]          NOT NULL,
  [RANGE_VALUE]                [char](3)      NOT NULL,
  [MERIDIAN]                   [char](1)      NOT NULL,

  CONSTRAINT [DLSParcelData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[JudgesOrderParcelData]
(
  [PIN]              [char](9)       NOT NULL,
  [Judges_Order_ID]  [varchar](12)   NOT NULL,

  CONSTRAINT [JudgesOrderParcelData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[LegalInstrumentParcelData]
(
  [PIN]                 [char](9)      NOT NULL,
  [Legal_Instrument_ID] [varchar](11)  NOT NULL,

  CONSTRAINT [LegalInstrumentParcelData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[MappingAreaPolygonData]
(
	[PIN] 					[char](9) 		NOT NULL,
	[PropertyMappingName] 	[varchar](25) 	NOT NULL,
	
	CONSTRAINT [MappingAreaPolygonData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
	WITH (PAD_INDEX  = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[ParishLotParcelData]
(
  [PIN]           [char](9)      NOT NULL,
  [PART]          [char](2)      NOT NULL CONSTRAINT DF_ParishLotParcelData_PART DEFAULT '',
  [Par_Lot_ID]    [varchar](5)   NULL,
  [Par_Lot_Type]  [char](2)      NOT NULL,
  [Parish]        [char](2)      NOT NULL,

  CONSTRAINT [ParishLotParcelData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[PlanParcelData]
(
  [PIN]                  [char](9)      NOT NULL,
  [PART]                 [char](2)      NOT NULL CONSTRAINT DF_PlanParcelData_PART DEFAULT '',
  [Lot_ID]               [varchar](5)   NULL,
  [Block_ID]             [varchar](4)   NULL,
  [Plan_ID]              [varchar](7)   NOT NULL,
  [Parcel_Type]          [char](1)      NOT NULL CONSTRAINT DF_PlanParcelData_Parcel_Type DEFAULT '',
  [Plan_Type]            [char](1)      NOT NULL,
  [Original_Issuing_LTO] [char](2)      NOT NULL CONSTRAINT DF_PlanParcelData_Original_Issuing_LTO DEFAULT '',

  CONSTRAINT [PlanParcelData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[PropertyMapPolygonData]
(
  [PIN]  [char](9)      NOT NULL,
  [Name] [varchar](25)  NOT NULL,

  CONSTRAINT [aaaaaPropertyMapPolygonData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[PublicLaneData]
(
  [PIN]  [char](9)     NOT NULL,
  [NAME] [varchar](8)  NULL,

  CONSTRAINT [PublicLaneData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[PublicWalkData]
(
  [PIN]  [char](9)     NOT NULL,
  [NAME] [varchar](8)  NULL,

  CONSTRAINT [PublicWalkData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[StreetData]
(
  [PIN]         [char](9)      NOT NULL,
  [Street_Name] [varchar](40)  NOT NULL,

  CONSTRAINT [StreetData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

CREATE TABLE [dbo].[WaterBodyData]
(
  [PIN]       [char](9)       NOT NULL,
  [WB_Name]   [varchar](40)   NOT NULL,
  [WB_Type]   [char](1)       NOT NULL,

  CONSTRAINT [WaterBodyData_PK] PRIMARY KEY NONCLUSTERED ([PIN] ASC)
  WITH (PAD_INDEX = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
)
ON [PRIMARY]
GO

-- Define CHECK constraints

ALTER TABLE [dbo].[DLSParcelData] WITH CHECK ADD CONSTRAINT [CK_DLSParcelData_DLS_LEGAL_SUB_DIVISION_num]
CHECK ([DLS_LEGAL_SUB_DIVISION_num]>=0 AND [DLS_LEGAL_SUB_DIVISION_num]<=16)
GO

ALTER TABLE [dbo].[DLSParcelData] WITH CHECK ADD CONSTRAINT [CK_DLSParcelData_SECTION_num]
CHECK ([SECTION_num]>=0 AND [SECTION_num]<=36)
GO

ALTER TABLE [dbo].[DLSParcelData] WITH CHECK ADD CONSTRAINT [CK_DLSParcelData_TOWNSHIP_num]
CHECK ([TOWNSHIP_num]>=1 AND [TOWNSHIP_num]<=126)
GO


-- Define foreign key constraints for data tables that refer to domains


ALTER TABLE [dbo].[ParishLotParcelData]
ADD CONSTRAINT FK_ParishLotParcelData_DOMAIN_Part_of_Cadastral_Polygon FOREIGN KEY (PART)
REFERENCES dbo.DOMAIN_Part_of_Cadastral_Polygon (ShortValue)	
GO

ALTER TABLE [dbo].[DLSParcelData]
ADD CONSTRAINT FK_DLSParcelData_DOMAIN_Part_of_Cadastral_Polygon FOREIGN KEY (PART)
REFERENCES dbo.DOMAIN_Part_of_Cadastral_Polygon (ShortValue)	
GO

ALTER TABLE [dbo].[PlanParcelData]
ADD CONSTRAINT FK_PlanParcelData_DOMAIN_Part_of_Cadastral_Polygon FOREIGN KEY (PART)
REFERENCES dbo.DOMAIN_Part_of_Cadastral_Polygon (ShortValue)	
GO

ALTER TABLE [dbo].[PlanParcelData]
ADD CONSTRAINT FK_PlanParcelData_DOMAIN_Issuing_LTOs FOREIGN KEY (Original_Issuing_LTO)
REFERENCES dbo.DOMAIN_Issuing_LTOs (ShortValue)	
GO

ALTER TABLE [dbo].[DLSParcelData]
ADD CONSTRAINT FK_DLSParcelData_DOMAIN_HALVES_of_DLS_QS_Polygons FOREIGN KEY (HALF)
REFERENCES dbo.DOMAIN_HALVES_of_DLS_QS_Polygons (ShortValue)	
GO

ALTER TABLE [dbo].[DLSParcelData]
ADD CONSTRAINT FK_DLSParcelData_DOMAIN_DLS_PARCEL_TYPES FOREIGN KEY (DLS_PARCEL_TYPE)
REFERENCES dbo.DOMAIN_DLS_PARCEL_TYPES (ShortValue)	
GO

ALTER TABLE [dbo].[DLSParcelData]
ADD CONSTRAINT FK_DLSParcelData_DOMAIN_Quarter_Sections FOREIGN KEY (QS_Value)
REFERENCES dbo.DOMAIN_Quarter_Sections (ShortValue)	
GO

ALTER TABLE [dbo].[DLSParcelData]
ADD CONSTRAINT FK_DLSParcelData_DOMAIN_Range_Values FOREIGN KEY (Range_Value)
REFERENCES dbo.DOMAIN_Range_Values (ShortValue)	
GO

ALTER TABLE [dbo].[DLSParcelData]
ADD CONSTRAINT FK_DLSParcelData_DOMAIN_Meridian_Values FOREIGN KEY (Meridian)
REFERENCES dbo.DOMAIN_Meridian_Values (ShortValue)	
GO

ALTER TABLE [dbo].[ParishLotParcelData]
ADD CONSTRAINT FK_ParishLotParcelData_DOMAIN_Parish_Values FOREIGN KEY (Parish)
REFERENCES dbo.DOMAIN_Parish_Values (ShortValue)	
GO

ALTER TABLE [dbo].[ParishLotParcelData]
ADD CONSTRAINT FK_ParishLotParcelData_DOMAIN_Parish_Lot_Types FOREIGN KEY (Par_Lot_Type)
REFERENCES dbo.DOMAIN_Parish_Lot_Types (ShortValue)	
GO

ALTER TABLE [dbo].[PlanParcelData]
ADD CONSTRAINT FK_PlanParcelData_DOMAIN_Parcel_Types FOREIGN KEY (Parcel_Type)
REFERENCES dbo.DOMAIN_Parcel_Types (ShortValue)	
GO

ALTER TABLE [dbo].[PlanParcelData]
ADD CONSTRAINT FK_PlanParcelData_DOMAIN_Plan_Types FOREIGN KEY (Plan_Type)
REFERENCES dbo.DOMAIN_Plan_Types (ShortValue)	
GO

ALTER TABLE [dbo].[WaterBodyData]
ADD CONSTRAINT FK_WaterBodyData_DOMAIN_WaterBodyTypes FOREIGN KEY (WB_Type)
REFERENCES dbo.DOMAIN_WaterBodyTypes (ShortValue)	
GO
