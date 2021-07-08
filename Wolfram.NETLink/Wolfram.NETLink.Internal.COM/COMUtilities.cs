using System;
using System.Runtime.InteropServices;

namespace Wolfram.NETLink.Internal.COM
{
	internal class COMUtilities
	{
		internal class FuncIndexHolder
		{
			internal int funcIndexForCALLTYPE_METHOD = -1;

			internal int funcIndexForCALLTYPE_FIELD_OR_SIMPLE_PROP_GET = -1;

			internal int funcIndexForCALLTYPE_FIELD_OR_ANY_PROP_SET = -1;
		}

		private static Guid IID_NULL = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

		[DllImport("kernel32.dll")]
		private static extern int GetUserDefaultLCID();

		[DllImport("ole32.dll", CharSet = CharSet.Unicode)]
		private static extern int ProgIDFromCLSID(ref Guid clsid, out string progID);

		internal static bool IsCOMProp(object obj, string memberName)
		{
			if (Utils.IsMono)
			{
				return false;
			}
			string str = memberName;
			object comObjectData = Marshal.GetComObjectData(obj, "NETLinkIsCOMProp" + str);
			if (comObjectData != null)
			{
				return (bool)comObjectData;
			}
			UCOMIDispatch iDispatch;
			UCOMITypeInfo iTypeInfo;
			try
			{
				iDispatch = GetIDispatch(obj);
				iTypeInfo = GetITypeInfo(obj, iDispatch);
			}
			catch (Exception)
			{
				Marshal.SetComObjectData(obj, "NETLinkIsCOMProp" + str, false);
				return false;
			}
			int memberDispID = GetMemberDispID(obj, iDispatch, ref memberName);
			if (memberDispID == -1)
			{
				Marshal.SetComObjectData(obj, "NETLinkIsCOMProp" + str, false);
				return false;
			}
			bool flag = false;
			iTypeInfo.GetTypeAttr(out var ppTypeAttr);
			int cFuncs = ((TYPEATTR)Marshal.PtrToStructure(ppTypeAttr, typeof(TYPEATTR))).cFuncs;
			iTypeInfo.ReleaseTypeAttr(ppTypeAttr);
			for (int i = 0; i < cFuncs; i++)
			{
				iTypeInfo.GetFuncDesc(i, out var ppFuncDesc);
				FUNCDESC fUNCDESC = (FUNCDESC)Marshal.PtrToStructure(ppFuncDesc, typeof(FUNCDESC));
				int memid = fUNCDESC.memid;
				INVOKEKIND invkind = fUNCDESC.invkind;
				int cParams = fUNCDESC.cParams;
				iTypeInfo.ReleaseFuncDesc(ppFuncDesc);
				if (memid == memberDispID)
				{
					flag = invkind == INVOKEKIND.INVOKE_PROPERTYGET && cParams == 0;
					break;
				}
			}
			Marshal.SetComObjectData(obj, "NETLinkIsCOMProp" + str, flag);
			return flag;
		}

		internal static bool GetMemberInfo(object obj, ref string memberName, int callType, bool isBeingCalledWithZeroArgs, out Type[] paramTypes, out PARAMFLAG[] paramFlags, out bool endsWithParamArray)
		{
			paramTypes = null;
			paramFlags = null;
			endsWithParamArray = false;
			GetUserDefaultLCID();
			try
			{
				UCOMIDispatch iDispatch = GetIDispatch(obj);
				int memberDispID = GetMemberDispID(obj, iDispatch, ref memberName);
				if (memberDispID == -1)
				{
					return false;
				}
				if (isBeingCalledWithZeroArgs)
				{
					return true;
				}
				UCOMITypeInfo iTypeInfo = GetITypeInfo(obj, iDispatch);
				FuncIndexHolder funcIndexHolder = (FuncIndexHolder)Marshal.GetComObjectData(obj, "NETLinkFuncIndex" + memberName);
				if (funcIndexHolder == null)
				{
					funcIndexHolder = new FuncIndexHolder();
					Marshal.SetComObjectData(obj, "NETLinkFuncIndex" + memberName, funcIndexHolder);
				}
				int num = -1;
				switch (callType)
				{
				case 1:
					if (funcIndexHolder.funcIndexForCALLTYPE_FIELD_OR_SIMPLE_PROP_GET != -1)
					{
						num = funcIndexHolder.funcIndexForCALLTYPE_FIELD_OR_SIMPLE_PROP_GET;
					}
					break;
				case 2:
				case 4:
					if (funcIndexHolder.funcIndexForCALLTYPE_FIELD_OR_ANY_PROP_SET != -1)
					{
						num = funcIndexHolder.funcIndexForCALLTYPE_FIELD_OR_ANY_PROP_SET;
					}
					break;
				case 3:
				case 5:
					if (funcIndexHolder.funcIndexForCALLTYPE_METHOD != -1)
					{
						num = funcIndexHolder.funcIndexForCALLTYPE_METHOD;
					}
					break;
				}
				IntPtr ppFuncDesc;
				if (num == -1)
				{
					iTypeInfo.GetTypeAttr(out var ppTypeAttr);
					int cFuncs = ((TYPEATTR)Marshal.PtrToStructure(ppTypeAttr, typeof(TYPEATTR))).cFuncs;
					iTypeInfo.ReleaseTypeAttr(ppTypeAttr);
					bool flag = false;
					for (int i = 0; i < cFuncs; i++)
					{
						iTypeInfo.GetFuncDesc(i, out ppFuncDesc);
						FUNCDESC fUNCDESC = (FUNCDESC)Marshal.PtrToStructure(ppFuncDesc, typeof(FUNCDESC));
						int memid = fUNCDESC.memid;
						INVOKEKIND invkind = fUNCDESC.invkind;
						iTypeInfo.ReleaseFuncDesc(ppFuncDesc);
						if (memid == memberDispID)
						{
							flag = true;
							if (callType == 1 && invkind == INVOKEKIND.INVOKE_PROPERTYGET)
							{
								funcIndexHolder.funcIndexForCALLTYPE_FIELD_OR_SIMPLE_PROP_GET = i;
								num = i;
								break;
							}
							if ((callType == 2 || callType == 4) && (invkind == INVOKEKIND.INVOKE_PROPERTYPUT || invkind == INVOKEKIND.INVOKE_PROPERTYPUTREF))
							{
								funcIndexHolder.funcIndexForCALLTYPE_FIELD_OR_ANY_PROP_SET = i;
								num = i;
								break;
							}
							if ((callType == 5 && (invkind == INVOKEKIND.INVOKE_FUNC || invkind == INVOKEKIND.INVOKE_PROPERTYGET)) || (callType == 3 && invkind == INVOKEKIND.INVOKE_PROPERTYGET))
							{
								funcIndexHolder.funcIndexForCALLTYPE_METHOD = i;
								num = i;
								break;
							}
						}
					}
					if (num == -1)
					{
						return !flag;
					}
				}
				iTypeInfo.GetFuncDesc(num, out ppFuncDesc);
				try
				{
					FUNCDESC fUNCDESC2 = (FUNCDESC)Marshal.PtrToStructure(ppFuncDesc, typeof(FUNCDESC));
					int num2 = fUNCDESC2.cParams;
					endsWithParamArray = fUNCDESC2.cParamsOpt == -1;
					if (endsWithParamArray)
					{
						num2--;
					}
					paramTypes = new Type[num2];
					paramFlags = new PARAMFLAG[num2];
					IntPtr lprgelemdescParam = fUNCDESC2.lprgelemdescParam;
					for (int j = 0; j < num2; j++)
					{
						ELEMDESC eLEMDESC = (ELEMDESC)Marshal.PtrToStructure((IntPtr)(lprgelemdescParam.ToInt64() + j * Marshal.SizeOf(typeof(ELEMDESC))), typeof(ELEMDESC));
						TYPEDESC tdesc = eLEMDESC.tdesc;
						paramFlags[j] = eLEMDESC.desc.paramdesc.wParamFlags;
						VarEnum vt = (VarEnum)tdesc.vt;
						bool flag2 = vt == VarEnum.VT_SAFEARRAY;
						bool flag3 = vt == VarEnum.VT_PTR;
						bool isOut = (paramFlags[j] & PARAMFLAG.PARAMFLAG_FOUT) != 0;
						if (flag2 || flag3)
						{
							IntPtr lpValue = tdesc.lpValue;
							TYPEDESC tYPEDESC = (TYPEDESC)Marshal.PtrToStructure(lpValue, typeof(TYPEDESC));
							vt = (VarEnum)tYPEDESC.vt;
							if (vt == VarEnum.VT_SAFEARRAY)
							{
								flag2 = true;
								lpValue = tYPEDESC.lpValue;
								vt = (VarEnum)((TYPEDESC)Marshal.PtrToStructure(lpValue, typeof(TYPEDESC))).vt;
							}
						}
						paramTypes[j] = managedTypeForVariantType(vt, flag2, flag3, isOut);
					}
				}
				finally
				{
					iTypeInfo.ReleaseFuncDesc(ppFuncDesc);
				}
				return true;
			}
			catch (Exception)
			{
				return true;
			}
		}

		internal static string GetDefaultCOMInterfaceName(object obj)
		{
			try
			{
				string strName = (string)Marshal.GetComObjectData(obj, "NETLinkCOMInterface");
				if (strName == null)
				{
					UCOMIDispatch iDispatch = GetIDispatch(obj);
					UCOMITypeInfo iTypeInfo = GetITypeInfo(obj, iDispatch);
					iTypeInfo.GetDocumentation(-1, out strName, out var strDocString, out var dwHelpContext, out var strHelpFile);
					iTypeInfo.GetContainingTypeLib(out var ppTLB, out var _);
					ppTLB.GetDocumentation(-1, out var strName2, out strDocString, out dwHelpContext, out strHelpFile);
					strName = strName2 + "." + strName;
					Marshal.SetComObjectData(obj, "NETLinkCOMInterface", strName);
					Marshal.ReleaseComObject(ppTLB);
				}
				return strName;
			}
			catch (Exception)
			{
				return null;
			}
		}

		internal static object Cast(object obj, Type t)
		{
			if (t.IsInterface)
			{
				IntPtr iUnknownForObject = Marshal.GetIUnknownForObject(obj);
				object typedObjectForIUnknown = Marshal.GetTypedObjectForIUnknown(iUnknownForObject, t);
				Marshal.Release(iUnknownForObject);
				return new COMObjectWrapper(typedObjectForIUnknown, t);
			}
			return Marshal.CreateWrapperOfType(obj, t);
		}

		internal static object createCOMObject(string clsIDOrProgID)
		{
			Type type = ((clsIDOrProgID.IndexOf("-") == -1) ? Type.GetTypeFromProgID(clsIDOrProgID, throwOnError: true) : Type.GetTypeFromCLSID(new Guid(clsIDOrProgID), throwOnError: true));
			return Activator.CreateInstance(type);
		}

		internal static object getActiveCOMObject(string clsIDOrProgID)
		{
			if (clsIDOrProgID.IndexOf("-") != -1)
			{
				Guid clsid = new Guid(clsIDOrProgID);
				ProgIDFromCLSID(ref clsid, out var progID);
				clsIDOrProgID = progID;
			}
			object activeObject = Marshal.GetActiveObject(clsIDOrProgID);
			Type typeFromProgID = Type.GetTypeFromProgID(clsIDOrProgID, throwOnError: false);
			if (typeFromProgID == null)
			{
				return activeObject;
			}
			return Marshal.CreateWrapperOfType(activeObject, typeFromProgID);
		}

		internal static int releaseCOMObject(object obj)
		{
			int num = Marshal.ReleaseComObject(obj);
			if (num == 0)
			{
				UCOMITypeInfo uCOMITypeInfo = (UCOMITypeInfo)Marshal.GetComObjectData(obj, "NETLinkITypeInfo");
				if (uCOMITypeInfo != null)
				{
					Marshal.ReleaseComObject(uCOMITypeInfo);
				}
				UCOMIDispatch uCOMIDispatch = (UCOMIDispatch)Marshal.GetComObjectData(obj, "NETLinkIDispatch");
				if (uCOMIDispatch != null)
				{
					Marshal.ReleaseComObject(uCOMIDispatch);
				}
			}
			return num;
		}

		private static UCOMIDispatch GetIDispatch(object obj)
		{
			return (UCOMIDispatch)obj;
		}

		private static UCOMITypeInfo GetITypeInfo(object obj, UCOMIDispatch iDisp)
		{
			UCOMITypeInfo typeInfo = (UCOMITypeInfo)Marshal.GetComObjectData(obj, "NETLinkITypeInfo");
			if (typeInfo == null)
			{
				iDisp.GetTypeInfo(0, GetUserDefaultLCID(), out typeInfo);
				Marshal.SetComObjectData(obj, "NETLinkITypeInfo", typeInfo);
			}
			return typeInfo;
		}

		private static int GetMemberDispID(object obj, UCOMIDispatch iDisp, ref string memberName)
		{
			object comObjectData = Marshal.GetComObjectData(obj, "NETLinkDispID" + memberName);
			if (comObjectData != null)
			{
				memberName = (string)Marshal.GetComObjectData(obj, "NETLinkModifiedName" + memberName);
				return (int)comObjectData;
			}
			int userDefaultLCID = GetUserDefaultLCID();
			string[] array = memberName.Split('U');
			int num = (int)Math.Pow(2.0, array.Length - 1);
			for (int i = 0; i < num; i++)
			{
				string text = array[0];
				for (int j = 1; j < array.Length; j++)
				{
					text = text + ((((i >> j - 1) & 1) == 0) ? "U" : "_") + array[j];
				}
				int rgDispId;
				try
				{
					iDisp.GetIDsOfNames(ref IID_NULL, new string[1]
					{
						text
					}, 1, userDefaultLCID, out rgDispId);
				}
				catch (Exception)
				{
					continue;
				}
				Marshal.SetComObjectData(obj, "NETLinkDispID" + memberName, rgDispId);
				Marshal.SetComObjectData(obj, "NETLinkModifiedName" + memberName, text);
				memberName = text;
				return rgDispId;
			}
			return -1;
		}

		private static Type managedTypeForVariantType(VarEnum vt, bool isArray, bool isPtr, bool isOut)
		{
			if (isArray)
			{
				switch (vt)
				{
				case VarEnum.VT_I1:
					return typeof(sbyte[]);
				case VarEnum.VT_I2:
					return typeof(short[]);
				case VarEnum.VT_I4:
				case VarEnum.VT_INT:
				case VarEnum.VT_HRESULT:
					return typeof(int[]);
				case VarEnum.VT_I8:
					return typeof(long[]);
				case VarEnum.VT_UI1:
					return typeof(byte[]);
				case VarEnum.VT_UI2:
					return typeof(ushort[]);
				case VarEnum.VT_UI4:
				case VarEnum.VT_UINT:
					return typeof(uint[]);
				case VarEnum.VT_UI8:
					return typeof(ulong[]);
				case VarEnum.VT_DECIMAL:
					return typeof(decimal[]);
				case VarEnum.VT_R4:
					return typeof(float[]);
				case VarEnum.VT_R8:
					return typeof(double[]);
				case VarEnum.VT_BOOL:
					return typeof(bool[]);
				case VarEnum.VT_BSTR:
				case VarEnum.VT_LPSTR:
				case VarEnum.VT_LPWSTR:
					return typeof(string[]);
				default:
					return typeof(object[]);
				}
			}
			switch (vt)
			{
			case VarEnum.VT_I1:
				return typeof(sbyte);
			case VarEnum.VT_I2:
				return typeof(short);
			case VarEnum.VT_UI1:
				return typeof(byte);
			case VarEnum.VT_UI2:
				return typeof(ushort);
			case VarEnum.VT_I4:
			case VarEnum.VT_INT:
			case VarEnum.VT_HRESULT:
				return typeof(int);
			case VarEnum.VT_I8:
				return typeof(long);
			case VarEnum.VT_UI4:
			case VarEnum.VT_UINT:
				return typeof(uint);
			case VarEnum.VT_UI8:
				return typeof(ulong);
			case VarEnum.VT_DECIMAL:
				return typeof(decimal);
			case VarEnum.VT_R4:
				return typeof(float);
			case VarEnum.VT_R8:
				return typeof(double);
			case VarEnum.VT_BOOL:
				return typeof(bool);
			case VarEnum.VT_BSTR:
			case VarEnum.VT_LPSTR:
			case VarEnum.VT_LPWSTR:
				return typeof(string);
			default:
				return typeof(object);
			}
		}
	}
}
