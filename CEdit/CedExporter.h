#pragma once

#ifdef _CEDIT
class CeOperation;
class CeMap;
#else
#include "CEditStubs.h"
#endif

class CedExporter
{
public:
	CedExporter();
	virtual ~CedExporter(void);
	void CreateExport(CeMap* cedFile);

	static void GetAllCoincidentLocations(const CeLocation* loc, CPtrArray& locs, FILE* log=0);

private:
	void FillGuidString(CString& s) const;
	void FillComputerName(CString& name) const;
	void AppendExportItems(const CTime& when, const CeOperation& op, IdFactory& idf, CPtrArray& exportItems);
	void GenerateExtraPoints(CeMap* cedFile, IdFactory& idf, CPtrArray& points);
	void CheckForExtraPoint(const CeLocation* loc, CMapPtrToPtr& locIndex, IdFactory& idf, CPtrArray& extraPoints);
	void RecordLocations(const CePoint& p, CMapPtrToPtr& locIndex);
	void Log(LPCTSTR msg);
	void Log(const CString& msg);
	void CleanObjectLists(CeMap* cedFile);
	void LoadValidData(CMapPtrToPtr& validData, CeMap* cedFile);

	FILE* LogFile;
};

