#pragma once
#include<math.h>
#include<string.h>
#include<iostream>
#define _DllExport _declspec(dllexport)

#define UnityLog(acStr)  char acLogStr[512] = { 0 }; sprintf_s(acLogStr, "%s",acStr); Debug::Log(acLogStr,strlen(acStr));


extern "C"
{ 
	//C++ Call C#
	class Debug
	{
	public:
		static void (*Log)(char* message,int iSize);
	};




	// C# call C++
	void _DllExport InitCSharpDelegate(void (*Log)(char* message, int iSize));

	float _DllExport GetDistance(float x1, float y1, float x2, float y2);

	_DllExport int* fnwrapper_intarr();

	_DllExport int* add(int* message);

	_DllExport int* IFT(int* rawdata, int nrows, int ncols);

	_DllExport int* IFTopt(int* rawdata, int* riskdata, int nrows, int ncols);
	
	_DllExport int* GetImage(char x);

	_DllExport void ExportFile(int* imgdata, int nrows, int ncols, const char* filename);
}

