using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Wolfram.NETLink.Internal.COM
{
	internal class COMTypeLibraryLoader
	{
		internal enum REGKIND
		{
			REGKIND_DEFAULT,
			REGKIND_REGISTER,
			REGKIND_NONE
		}

		internal class ImporterNotiferSink : ITypeLibImporterNotifySink
		{
			private string asmDir;

			private bool safeArrayAsArray;

			internal ImporterNotiferSink(bool safeArrayAsArray, string asmDir)
			{
				this.safeArrayAsArray = safeArrayAsArray;
				this.asmDir = asmDir;
			}

			public void ReportEvent(ImporterEventKind eventKind, int eventCode, string eventMsg)
			{
			}

			public Assembly ResolveRef(object typeLib)
			{
				bool foundPIAInstead;
				return generateAssemblyFromTypeLib((UCOMITypeLib)typeLib, safeArrayAsArray, asmDir, out foundPIAInstead);
			}
		}

		[DllImport("oleaut32.dll", CharSet = CharSet.Unicode)]
		private static extern void LoadTypeLibEx(string strTypeLibName, REGKIND regKind, out UCOMITypeLib TypeLib);

		internal static Assembly loadTypeLibrary(string pathToTypeLib, bool safeArrayAsArray, string assemFilePath, out bool foundPIAInstead)
		{
			LoadTypeLibEx(pathToTypeLib, REGKIND.REGKIND_NONE, out var TypeLib);
			if (TypeLib == null)
			{
				throw new ArgumentException("Could not find the specified file, or it did not have type library information in it.");
			}
			return generateAssemblyFromTypeLib(TypeLib, safeArrayAsArray, assemFilePath, out foundPIAInstead);
		}

		internal static Assembly generateAssemblyFromTypeLib(UCOMITypeLib typeLib, bool safeArrayAsArray, string asmFilePath, out bool foundPIAInstead)
		{
			foundPIAInstead = false;
			string typeLibName = Marshal.GetTypeLibName(typeLib);
			string text;
			string text2;
			if (asmFilePath == "")
			{
				text = "interop." + typeLibName + ".dll";
				text2 = "";
			}
			else
			{
				text2 = Path.GetDirectoryName(asmFilePath);
				if (text2 == null)
				{
					string pathRoot = Path.GetPathRoot(asmFilePath);
					text2 = ((pathRoot == null) ? "" : pathRoot);
				}
				else
				{
					text2 += Path.DirectorySeparatorChar;
				}
				text = Path.GetFileName(asmFilePath);
				if (text == "")
				{
					text = "interop." + typeLibName + ".dll";
				}
			}
			string asmFileName = text2 + text;
			ImporterNotiferSink notifySink = new ImporterNotiferSink(safeArrayAsArray, text2);
			TypeLibConverter typeLibConverter = new TypeLibConverter();
			typeLib.GetLibAttr(out var ppTLibAttr);
			TYPELIBATTR tYPELIBATTR = (TYPELIBATTR)Marshal.PtrToStructure(ppTLibAttr, typeof(TYPELIBATTR));
			string asmName;
			string asmCodeBase;
			bool primaryInteropAssembly = typeLibConverter.GetPrimaryInteropAssembly(tYPELIBATTR.guid, tYPELIBATTR.wMajorVerNum, tYPELIBATTR.wMinorVerNum, tYPELIBATTR.lcid, out asmName, out asmCodeBase);
			typeLib.ReleaseTLibAttr(ppTLibAttr);
			if (primaryInteropAssembly)
			{
				Assembly assembly = Assembly.LoadWithPartialName(asmName);
				if (assembly != null)
				{
					foundPIAInstead = true;
					return assembly;
				}
			}
			TypeLoader.isBuildingDynamicAssembly = true;
			try
			{
				AssemblyBuilder assemblyBuilder = typeLibConverter.ConvertTypeLibToAssembly(typeLib, asmFileName, safeArrayAsArray ? TypeLibImporterFlags.SafeArrayAsSystemArray : TypeLibImporterFlags.None, notifySink, null, null, typeLibName, null);
				assemblyBuilder.GetTypes();
				if (asmFilePath != "")
				{
					assemblyBuilder.Save(text);
				}
				return assemblyBuilder;
			}
			finally
			{
				TypeLoader.isBuildingDynamicAssembly = false;
			}
		}
	}
}
