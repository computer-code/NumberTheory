using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Wolfram.NETLink.Internal.COM
{
	internal class COMDispatchHandler
	{
		internal object callDispatch(IKernelLink ml, object obj, string memberName, int callType, int[] argTypes, out OutParamRecord[] outParams)
		{
			outParams = null;
			bool isBeingCalledWithZeroArgs = argTypes.Length == 0;
			if (!COMUtilities.GetMemberInfo(obj, ref memberName, callType, isBeingCalledWithZeroArgs, out var paramTypes, out var paramFlags, out var endsWithParamArray))
			{
				string tag = ((callType != 1 && callType != 2) ? "nocommeth" : "nocomprop");
				throw new CallNETException(tag, "System.__ComObject", memberName);
			}
			bool flag = paramTypes != null;
			bool flag2 = endsWithParamArray;
			if (flag)
			{
				if (argTypes.Length > paramFlags.Length && !endsWithParamArray)
				{
					throw new CallNETException("methargc", "System.__ComObject", memberName);
				}
				int num = 0;
				for (int i = 0; i < Math.Max(paramFlags.Length, argTypes.Length); i++)
				{
					if (i < paramFlags.Length && (paramFlags[i] & PARAMFLAG.PARAMFLAG_FOPT) == 0)
					{
						num++;
					}
					if (i < argTypes.Length)
					{
						int num2 = argTypes[i];
						if (num2 == 13)
						{
							throw new CallNETException("methodargs", "System.__ComObject", memberName);
						}
						if (i < paramFlags.Length && (paramFlags[i] & PARAMFLAG.PARAMFLAG_FOUT) != 0)
						{
							flag2 = true;
						}
					}
				}
				if (argTypes.Length < num)
				{
					throw new CallNETException("methargc", "System.__ComObject", memberName);
				}
			}
			object[] array = new object[argTypes.Length];
			try
			{
				for (int j = 0; j < argTypes.Length; j++)
				{
					if (argTypes[j] == 6)
					{
						array[j] = Missing.Value;
						ml.GetSymbol();
					}
					else if (!flag || j >= paramFlags.Length)
					{
						array[j] = ml.GetObject();
					}
					else if ((paramFlags[j] & PARAMFLAG.PARAMFLAG_FOUT) != 0 && (paramFlags[j] & PARAMFLAG.PARAMFLAG_FIN) == 0)
					{
						Utils.discardNext(ml);
						array[j] = null;
					}
					else
					{
						array[j] = Utils.readArgAs(ml, argTypes[j], paramTypes[j]);
					}
				}
			}
			catch (Exception)
			{
				throw new CallNETException("methodargs", "System.__ComObject", memberName);
			}
			Type type = obj.GetType();
			object result = null;
			switch (callType)
			{
			case 1:
				result = type.InvokeMember(memberName, BindingFlags.GetProperty, null, obj, array);
				break;
			case 2:
				result = type.InvokeMember(memberName, BindingFlags.SetProperty, null, obj, array);
				break;
			case 5:
				if (flag2 && array.Length > 0)
				{
					ParameterModifier parameterModifier = new ParameterModifier(array.Length);
					for (int k = 0; k < array.Length; k++)
					{
						parameterModifier[k] = k >= paramFlags.Length || (paramFlags[k] & PARAMFLAG.PARAMFLAG_FOUT) != 0;
					}
					ParameterModifier[] modifiers = new ParameterModifier[1]
					{
						parameterModifier
					};
					result = type.InvokeMember(memberName, BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, obj, array, modifiers, null, null);
				}
				else
				{
					result = type.InvokeMember(memberName, BindingFlags.InvokeMethod | BindingFlags.GetProperty, null, obj, array);
				}
				break;
			case 3:
				result = type.InvokeMember(memberName, BindingFlags.GetProperty, null, obj, array);
				break;
			case 4:
				result = type.InvokeMember(memberName, BindingFlags.SetProperty, null, obj, array);
				break;
			}
			if (flag2)
			{
				for (int l = 0; l < array.Length; l++)
				{
					if (l >= paramFlags.Length || (paramFlags[l] & PARAMFLAG.PARAMFLAG_FOUT) != 0)
					{
						if (outParams == null)
						{
							outParams = new OutParamRecord[array.Length];
						}
						outParams[l] = new OutParamRecord(l, array[l]);
					}
				}
			}
			return result;
		}
	}
}
