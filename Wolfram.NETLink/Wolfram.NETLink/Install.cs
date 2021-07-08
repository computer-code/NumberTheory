namespace Wolfram.NETLink
{
	public class Install
	{
		internal const int CALL = 1;

		internal const int LOADTYPE1 = 2;

		internal const int LOADTYPE2 = 3;

		internal const int LOADEXISTINGTYPE = 4;

		internal const int LOADASSEMBLY = 5;

		internal const int LOADASSEMBLYFROMDIR = 6;

		internal const int GETASSEMBLYOBJ = 7;

		internal const int GETTYPEOBJ = 8;

		internal const int RELEASEOBJECT = 9;

		internal const int MAKEOBJECT = 10;

		internal const int CREATEDELEGATE = 11;

		internal const int VAL = 12;

		internal const int REFLECTTYPE = 13;

		internal const int REFLECTASM = 14;

		internal const int SETCOMPLEX = 15;

		internal const int SAMEQ = 16;

		internal const int INSTANCEOF = 17;

		internal const int CAST = 18;

		internal const int PEEKTYPES = 20;

		internal const int PEEKOBJECTS = 21;

		internal const int PEEKASSEMBLIES = 22;

		internal const int MODAL = 31;

		internal const int SHOW = 32;

		internal const int SHAREKERNEL = 33;

		internal const int ALLOWUICOMPS = 34;

		internal const int UILINK = 35;

		internal const int ISCOMPROP = 40;

		internal const int CREATECOM = 41;

		internal const int GETACTIVECOM = 42;

		internal const int RELEASECOM = 43;

		internal const int LOADTYPELIBRARY = 44;

		internal const int DEFINEDELEGATE = 50;

		internal const int DLGTYPENAME = 51;

		internal const int ADDHANDLER = 52;

		internal const int REMOVEHANDLER = 53;

		internal const int CREATEDLL1 = 60;

		internal const int CREATEDLL2 = 61;

		internal const int GETEXCEPTION = 70;

		internal const int CONNECTTOFE = 80;

		internal const int DISCONNECTTOFE = 81;

		internal const int NOOP = 90;

		internal const int NOOP2 = 91;

		internal const int CALLTYPE_CTOR = 0;

		internal const int CALLTYPE_FIELD_OR_SIMPLE_PROP_GET = 1;

		internal const int CALLTYPE_FIELD_OR_SIMPLE_PROP_SET = 2;

		internal const int CALLTYPE_PARAM_PROP_GET = 3;

		internal const int CALLTYPE_PARAM_PROP_SET = 4;

		internal const int CALLTYPE_METHOD = 5;

		internal const string MMA_CREATEINSTANCEDEFS = "NETLink`Package`createInstanceDefs";

		internal const string MMA_OBJECTSYMBOLPREFIX = "NETLink`Objects`NETObject$";

		internal const string MMA_LOADTYPE = "NETLink`Package`loadTypeFromNET";

		internal const string MMA_HANDLEEXCEPTION = "NETLink`Package`handleException";

		internal const string MMA_PREPAREFORMANUALRETURN = "NETLink`Package`prepareForManualReturn";

		internal const string MMA_MANUALEXCEPTION = "NETLink`Package`manualException";

		internal const string MMA_SPECIALEXCEPTION = "NETLink`Package`specialException";

		internal const string MMA_OUTPARAM = "NETLink`Package`outParam";

		internal const string MMA_CALLBACKWRAPPER = "NETLink`Package`delegateCallbackWrapper";

		internal const string MMA_NETMETHODCALLBACKWRAPPER = "NETLink`Package`methodCallbackWrapper";

		internal const string MMA_UIPACKET = "NETLink`Package`UIPacket";

		internal const string MMA_YIELDNORETURN = "NETLink`Package`nYieldNoReturn";

		internal const string NO_CTOR = "noctor";

		internal const string METHOD_NOT_FOUND = "nomethod";

		internal const string FIELD_NOT_FOUND = "nofield";

		internal const string NO_INDEXER = "noindexer";

		internal const string PROP_NOT_FOUND = "noprop";

		internal const string COM_METHOD_NOT_FOUND = "nocommeth";

		internal const string COM_PROP_NOT_FOUND = "nocomprop";

		internal const string CTOR_ARG_COUNT = "ctorargc";

		internal const string METHOD_ARG_COUNT = "methargc";

		internal const string INDEXER_ARG_COUNT = "indxrargc";

		internal const string PARAM_PROP_ARG_COUNT = "parampropargc";

		internal const string CTOR_BAD_ARGS = "ctorargs";

		internal const string METHOD_BAD_ARGS = "methodargs";

		internal const string FIELD_BAD_ARGS = "fieldtype";

		internal const string PROP_BAD_ARGS = "proptype";

		internal const string INDEXER_BAD_ARGS = "indxrargs";

		internal const string PARAM_PROP_BAD_ARGS = "parampropargs";

		internal const string FIELD_CANNOT_BE_SET = "fieldnoset";

		internal const string PROP_CANNOT_BE_READ = "propnoget";

		internal const string PROP_CANNOT_BE_SET = "propnoset";

		internal const string INDEXER_CANNOT_BE_READ = "indxrnoget";

		internal const string INDEXER_CANNOT_BE_SET = "indxrnoset";

		internal const string IS_EVENT = "event";

		internal const string BAD_CAST = "cast";

		internal const string BAD_OBJECT_REFERENCE = "badobj";

		internal const string BAD_TYPE = "badtype";

		internal const int ARGTYPE_INTEGER = 1;

		internal const int ARGTYPE_REAL = 2;

		internal const int ARGTYPE_STRING = 3;

		internal const int ARGTYPE_BOOLEAN = 4;

		internal const int ARGTYPE_NULL = 5;

		internal const int ARGTYPE_MISSING = 6;

		internal const int ARGTYPE_OBJECTREF = 7;

		internal const int ARGTYPE_VECTOR = 8;

		internal const int ARGTYPE_MATRIX = 9;

		internal const int ARGTYPE_TENSOR3 = 10;

		internal const int ARGTYPE_LIST = 11;

		internal const int ARGTYPE_COMPLEX = 12;

		internal const int ARGTYPE_OTHER = 13;

		private static string[] argTypePatterns = new string[13]
		{
			"_Integer",
			"_Real",
			"_String",
			"True | False",
			"Null",
			"Default",
			"_?NETObjectQ",
			"_?VectorQ",
			"_?MatrixQ",
			"x_ /; TensorRank[x] === 3",
			"_List",
			"_Complex",
			"_"
		};

		public static bool install(IMathLink ml)
		{
			try
			{
				ml.Connect();
				ml.Put("Begin[\"NETLink`Package`\"]");
				definePattern(ml, "nCall[typeName_String, obj_Symbol, callType_Integer, isByVal_, memberName_String, argCount_Integer, typesAndArgs___]", "{typeName, obj, callType, isByVal, memberName, argCount, typesAndArgs}", 1);
				definePattern(ml, "nLoadType1[type_String, assemblyName_]", "{type, assemblyName}", 2);
				definePattern(ml, "nLoadType2[type_String, assemblyObj_]", "{type, assemblyObj}", 3);
				definePattern(ml, "nLoadExistingType[typeObject_?NETObjectQ]", "{typeObject}", 4);
				definePattern(ml, "nLoadAssembly[assemblyNameOrPath_String, suppressErrors_]", "{assemblyNameOrPath, suppressErrors}", 5);
				definePattern(ml, "nLoadAssemblyFromDir[assemblyName_String, dir_String]", "{assemblyName, dir}", 6);
				definePattern(ml, "nGetAssemblyObject[asmName_String]", "{asmName}", 7);
				definePattern(ml, "nGetTypeObject[aqTypeName_String]", "{aqTypeName}", 8);
				definePattern(ml, "nReleaseObject[instances:{__Symbol}]", "{instances}", 9);
				definePattern(ml, "nMakeObject[typeName_String, argType_Integer, val_]", "{typeName, argType, val}", 10);
				definePattern(ml, "nVal[obj_?NETObjectQ]", "{obj}", 12);
				definePattern(ml, "nReflectType[typeName_String]", "{typeName}", 13);
				definePattern(ml, "nReflectAsm[asmName_String]", "{asmName}", 14);
				definePattern(ml, "nSetComplex[typeName_String]", "{typeName}", 15);
				definePattern(ml, "nSameQ[obj1_?NETObjectQ, obj2_?NETObjectQ]", "{obj1, obj2}", 16);
				definePattern(ml, "nInstanceOf[obj_?NETObjectQ, aqTypeName_String]", "{obj, aqTypeName}", 17);
				definePattern(ml, "nCast[obj_?NETObjectQ, aqTypeName_String]", "{obj, aqTypeName}", 18);
				definePattern(ml, "nPeekTypes[]", "{}", 20);
				definePattern(ml, "nPeekObjects[]", "{}", 21);
				definePattern(ml, "nPeekAssemblies[]", "{}", 22);
				definePattern(ml, "nCreateDelegate[typeName_String, mFunc_String, sendTheseArgs_Integer, callsUnshare:(True | False), wrapInNETBlock:(True | False)]", "{typeName, mFunc, sendTheseArgs, callsUnshare, wrapInNETBlock}", 11);
				definePattern(ml, "nDefineDelegate[name_String, retTypeName_String, paramTypeNames_List]", "{name, retTypeName, paramTypeNames}", 50);
				definePattern(ml, "nDlgTypeName[eventObject_?NETObjectQ, aqTypeName_String, evtName_String]", "{eventObject, aqTypeName, evtName}", 51);
				definePattern(ml, "nAddHandler[eventObject_?NETObjectQ, aqTypeName_String, evtName_String, delegate_?NETObjectQ]", "{eventObject, aqTypeName, evtName, delegate}", 52);
				definePattern(ml, "nRemoveHandler[eventObject_?NETObjectQ, aqTypeName_String, evtName_String, delegate_?NETObjectQ]", "{eventObject, aqTypeName, evtName, delegate}", 53);
				definePattern(ml, "nCreateDLL1[funcName_String, dllName_String, callConv_String, retTypeName_String, argTypeNames_, areOutParams_, strFormat_String]", "{funcName, dllName, callConv, retTypeName, argTypeNames, areOutParams, strFormat}", 60);
				definePattern(ml, "nCreateDLL2[decl_String, refAsms_, lang_String]", "{decl, refAsms, lang}", 61);
				definePattern(ml, "nModal[modal:(True | False), formToActivate_?NETObjectQ]", "{modal, formToActivate}", 31);
				definePattern(ml, "nShow[formToActivate_?NETObjectQ]", "{formToActivate}", 32);
				definePattern(ml, "nShareKernel[sharing:(True | False)]", "{sharing}", 33);
				definePattern(ml, "nAllowUIComputations[allow:(True | False)]", "{allow}", 34);
				definePattern(ml, "nUILink[name_String, prot_String]", "{name, prot}", 35);
				definePattern(ml, "nIsCOMProp[obj_?NETObjectQ, memberName_String]", "{obj, memberName}", 40);
				definePattern(ml, "nCreateCOM[clsIDOrProgID_String]", "{clsIDOrProgID}", 41);
				definePattern(ml, "nGetActiveCOM[clsIDOrProgID_String]", "{clsIDOrProgID}", 42);
				definePattern(ml, "nReleaseCOM[obj_?NETObjectQ]", "{obj}", 43);
				definePattern(ml, "nLoadTypeLibrary[tlbPath_String, safeArrayAsArray_, assemblyFile_String]", "{tlbPath, safeArrayAsArray, assemblyFile}", 44);
				definePattern(ml, "nGetException[]", "{}", 70);
				definePattern(ml, "nConnectToFEServer[linkName_String]", "{linkName}", 80);
				definePattern(ml, "nDisconnectToFEServer[]", "{}", 81);
				definePattern(ml, "noop[]", "{}", 90);
				definePattern(ml, "noop2[argc_Integer, args___]", "{argc, args}", 91);
				ml.PutFunction("MapThread", 2);
				ml.PutFunction("Function", 1);
				ml.PutFunction("Set", 2);
				ml.PutFunction("argTypeToInteger", 1);
				ml.PutFunction("Slot", 1);
				ml.Put(1);
				ml.PutFunction("Slot", 1);
				ml.Put(2);
				ml.PutFunction("List", 2);
				ml.PutFunction("Map", 2);
				ml.PutSymbol("ToExpression");
				ml.Put(argTypePatterns);
				ml.PutFunction("List", 13);
				ml.Put(1);
				ml.Put(2);
				ml.Put(3);
				ml.Put(4);
				ml.Put(5);
				ml.Put(6);
				ml.Put(7);
				ml.Put(8);
				ml.Put(9);
				ml.Put(10);
				ml.Put(11);
				ml.Put(12);
				ml.Put(13);
				ml.Put("End[]");
				ml.PutSymbol("End");
				ml.Flush();
				return true;
			}
			catch (MathLinkException)
			{
				return false;
			}
		}

		private static void definePattern(IMathLink ml, string patt, string args, int index)
		{
			ml.PutFunction("NETLink`Package`netlinkDefineExternal", 3);
			ml.Put(patt);
			ml.Put(args);
			ml.Put(index);
		}
	}
}
