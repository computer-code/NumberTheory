using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Wolfram.NETLink.Internal.COM;

namespace Wolfram.NETLink.Internal
{
	internal class ObjectHandler
	{
		private class TypeRecord
		{
			private Type t;

			private string typeName;

			private string fullNameForMma;

			private ConstructorInfo[] cis;

			private FieldInfo[] fis;

			private PropertyInfo[] pis;

			private MethodInfo[] mis;

			private EventInfo[] eis;

			private PropertyInfo[] indxrs;

			private string[] fNames;

			private string[] pNames;

			private string[] mNames;

			private string[] eNames;

			private string iName;

			internal Type type => t;

			internal string name => typeName;

			internal string fullName => fullNameForMma;

			internal Array staticFieldNames
			{
				get
				{
					ArrayList arrayList = new ArrayList();
					FieldInfo[] array = fis;
					foreach (FieldInfo fieldInfo in array)
					{
						if (fieldInfo.IsStatic)
						{
							arrayList.Add(fieldInfo.Name);
						}
					}
					return arrayList.ToArray();
				}
			}

			internal Array staticPropertyNames
			{
				get
				{
					ArrayList arrayList = new ArrayList();
					PropertyInfo[] array = pis;
					foreach (PropertyInfo propertyInfo in array)
					{
						MethodInfo[] accessors = propertyInfo.GetAccessors();
						MethodInfo[] array2 = accessors;
						foreach (MethodInfo methodInfo in array2)
						{
							if (methodInfo.IsStatic)
							{
								arrayList.Add(propertyInfo.Name);
								break;
							}
						}
					}
					return arrayList.ToArray();
				}
			}

			internal Array staticMethodNames
			{
				get
				{
					ArrayList arrayList = new ArrayList();
					MethodInfo[] array = mis;
					foreach (MethodInfo methodInfo in array)
					{
						if (methodInfo.IsStatic)
						{
							arrayList.Add(methodInfo.Name);
						}
					}
					return arrayList.ToArray();
				}
			}

			internal Array staticEventNames
			{
				get
				{
					ArrayList arrayList = new ArrayList();
					EventInfo[] array = eis;
					foreach (EventInfo eventInfo in array)
					{
						MethodInfo addMethod = eventInfo.GetAddMethod();
						if (addMethod.IsStatic)
						{
							arrayList.Add(eventInfo.Name);
						}
					}
					return arrayList.ToArray();
				}
			}

			internal Array nonPrimitiveFieldOrSimplePropNames
			{
				get
				{
					ArrayList arrayList = new ArrayList();
					PropertyInfo[] array = pis;
					foreach (PropertyInfo propertyInfo in array)
					{
						Type propertyType = propertyInfo.PropertyType;
						if (Utils.IsTrulyPrimitive(propertyType) || propertyType == typeof(string) || propertyType == typeof(Expr) || propertyType == typeof(Array) || propertyType.IsArray || !propertyInfo.CanRead || propertyInfo.GetIndexParameters().Length > 0)
						{
							continue;
						}
						MethodInfo[] accessors = propertyInfo.GetAccessors();
						MethodInfo[] array2 = accessors;
						foreach (MethodInfo methodInfo in array2)
						{
							if (!methodInfo.IsStatic)
							{
								arrayList.Add(propertyInfo.Name);
								break;
							}
						}
					}
					FieldInfo[] array3 = fis;
					foreach (FieldInfo fieldInfo in array3)
					{
						Type fieldType = fieldInfo.FieldType;
						if (!Utils.IsTrulyPrimitive(fieldType) && fieldType != typeof(string) && fieldType != typeof(Expr) && fieldType != typeof(Array) && !fieldType.IsArray && !fieldInfo.IsStatic)
						{
							arrayList.Add(fieldInfo.Name);
						}
					}
					return arrayList.ToArray();
				}
			}

			internal Array isStaticPropertyParameterized
			{
				get
				{
					ArrayList arrayList = new ArrayList();
					PropertyInfo[] array = pis;
					foreach (PropertyInfo propertyInfo in array)
					{
						bool flag = false;
						MethodInfo[] accessors = propertyInfo.GetAccessors();
						foreach (MethodInfo methodInfo in accessors)
						{
							if (methodInfo.IsStatic)
							{
								flag = true;
								break;
							}
						}
						if (flag)
						{
							arrayList.Add(propertyInfo.GetIndexParameters().Length > 0);
						}
					}
					return arrayList.ToArray();
				}
			}

			internal string[] fieldNames => fNames;

			internal string[] propertyNames => pNames;

			internal string[] methodNames => mNames;

			internal string[] eventNames => eNames;

			internal string indexerName => iName;

			internal ConstructorInfo[] constructors => cis;

			internal FieldInfo[] fields => fis;

			internal PropertyInfo[] properties => pis;

			internal MethodInfo[] methods => mis;

			internal EventInfo[] events => eis;

			internal MemberInfo[] indexers => indxrs;

			internal TypeRecord(Type t)
			{
				this.t = t;
				typeName = t.Name;
				fullNameForMma = fullTypeNameForMathematica(t);
				cis = t.GetConstructors();
				fis = t.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				pis = t.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				mis = t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				eis = t.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
				fis = (FieldInfo[])cullOutHiddenBySigMembers(fis).ToArray(typeof(FieldInfo));
				pis = (PropertyInfo[])cullOutHiddenBySigMembers(pis).ToArray(typeof(PropertyInfo));
				mis = (MethodInfo[])cullOutHiddenBySigMembers(mis).ToArray(typeof(MethodInfo));
				eis = (EventInfo[])cullOutHiddenBySigMembers(eis).ToArray(typeof(EventInfo));
				if (t.IsInterface)
				{
					MethodInfo[] methods = typeof(object).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
					MethodInfo[] array = new MethodInfo[mis.Length + methods.Length];
					mis.CopyTo(array, 0);
					methods.CopyTo(array, mis.Length);
					mis = array;
				}
				fNames = new string[fis.Length];
				for (int i = 0; i < fis.Length; i++)
				{
					fNames[i] = fis[i].Name;
				}
				pNames = new string[pis.Length];
				for (int j = 0; j < pis.Length; j++)
				{
					pNames[j] = pis[j].Name;
				}
				mNames = new string[mis.Length];
				for (int k = 0; k < mis.Length; k++)
				{
					mNames[k] = mis[k].Name;
				}
				eNames = new string[eis.Length];
				for (int l = 0; l < eis.Length; l++)
				{
					eNames[l] = eis[l].Name;
				}
				ArrayList arrayList = new ArrayList();
				MemberInfo[] defaultMembers = t.GetDefaultMembers();
				foreach (MemberInfo memberInfo in defaultMembers)
				{
					if (memberInfo.MemberType == MemberTypes.Property)
					{
						arrayList.Add(memberInfo);
						iName = memberInfo.Name;
					}
				}
				indxrs = (PropertyInfo[])arrayList.ToArray(typeof(PropertyInfo));
			}

			private ArrayList cullOutHiddenBySigMembers(MemberInfo[] mis)
			{
				bool flag = false;
				bool flag2 = false;
				ParameterInfo[] array = new ParameterInfo[0];
				ArrayList arrayList = new ArrayList();
				if (mis.Length == 0)
				{
					return arrayList;
				}
				switch (mis[0].MemberType)
				{
				case MemberTypes.Property:
					flag = true;
					break;
				case MemberTypes.Method:
					flag2 = true;
					break;
				}
				foreach (MemberInfo memberInfo in mis)
				{
					if (memberInfo == null)
					{
						continue;
					}
					string name = memberInfo.Name;
					ParameterInfo[] array2 = (flag ? ((PropertyInfo)memberInfo).GetIndexParameters() : ((!flag2) ? array : ((MethodInfo)memberInfo).GetParameters()));
					for (int j = 0; j < mis.Length; j++)
					{
						MemberInfo memberInfo2 = mis[j];
						if (memberInfo2 == null)
						{
							continue;
						}
						bool flag3 = true;
						if (!(memberInfo2.Name == name))
						{
							continue;
						}
						ParameterInfo[] array3 = (flag ? ((PropertyInfo)memberInfo2).GetIndexParameters() : ((!flag2) ? array : ((MethodInfo)memberInfo2).GetParameters()));
						if (array3.Length != array2.Length)
						{
							break;
						}
						for (int k = 0; k < array3.Length; k++)
						{
							if (array2[k] != array3[k])
							{
								flag3 = false;
								break;
							}
						}
						if (flag3 && memberInfo.DeclaringType.IsSubclassOf(memberInfo2.DeclaringType))
						{
							mis[j] = null;
						}
					}
				}
				foreach (MemberInfo memberInfo3 in mis)
				{
					if (memberInfo3 != null)
					{
						arrayList.Add(memberInfo3);
					}
				}
				return arrayList;
			}
		}

		private class MethodRec
		{
			internal MethodBase m;

			internal ParameterInfo[] pia;

			internal MethodRec(MethodBase m, ParameterInfo[] pia)
			{
				this.m = m;
				this.pia = pia;
			}
		}

		private class PointerArgumentManager
		{
			private bool[] wasAlreadyArray;

			private Array[] valueHolderArrays;

			private GCHandle[] gcHandles;

			internal PointerArgumentManager(int argc)
			{
				gcHandles = new GCHandle[argc];
				valueHolderArrays = new Array[argc];
				wasAlreadyArray = new bool[argc];
			}

			internal IntPtr add(int indexInArgArray, object arg, ParameterInfo pi)
			{
				Type type = arg.GetType();
				Type parameterType = pi.ParameterType;
				Type elementType = parameterType.GetElementType();
				Array array2;
				if (type.IsArray)
				{
					wasAlreadyArray[indexInArgArray] = true;
					Type elementType2 = type.GetElementType();
					Array array = (Array)arg;
					if (elementType2 != elementType)
					{
						int length = array.GetLength(0);
						array2 = Array.CreateInstance(elementType, length);
						for (int i = 0; i < length; i++)
						{
							array2.SetValue(Convert.ChangeType(array.GetValue(i), elementType), i);
						}
					}
					else
					{
						array2 = (Array)arg;
					}
				}
				else
				{
					array2 = Array.CreateInstance(elementType, 1);
					array2.SetValue(Convert.ChangeType(arg, elementType), 0);
				}
				valueHolderArrays[indexInArgArray] = array2;
				ref GCHandle reference = ref gcHandles[indexInArgArray];
				reference = GCHandle.Alloc(array2, GCHandleType.Pinned);
				return Marshal.UnsafeAddrOfPinnedArrayElement(array2, 0);
			}

			internal bool wasPointerArg(int index)
			{
				return valueHolderArrays[index] != null;
			}

			internal object getValue(int index)
			{
				Array array = valueHolderArrays[index];
				if (!wasAlreadyArray[index])
				{
					return array.GetValue(0);
				}
				return array;
			}

			internal void release()
			{
				for (int i = 0; i < gcHandles.Length; i++)
				{
					if (wasPointerArg(i))
					{
						gcHandles[i].Free();
					}
				}
			}
		}

		private class InstanceCollection
		{
			internal class InstanceCollectionEnumerator
			{
				private Hashtable bucketTable;

				private IEnumerator topLevelEnumerator;

				private IEnumerator withinBucketEnumerator;

				public object Current
				{
					get
					{
						uint num = (uint)((DictionaryEntry)topLevelEnumerator.Current).Key;
						uint num2 = (uint)((DictionaryEntry)withinBucketEnumerator.Current).Key;
						ulong key = ((ulong)num << 24) | num2;
						Bucket.BucketRec bucketRec = (Bucket.BucketRec)((DictionaryEntry)withinBucketEnumerator.Current).Value;
						_ = bucketRec.obj;
						return mmaSymbolFromKey(key, bucketRec.aliases[0]);
					}
				}

				internal InstanceCollectionEnumerator(Hashtable bucketTable)
				{
					this.bucketTable = bucketTable;
				}

				public bool MoveNext()
				{
					if (topLevelEnumerator == null)
					{
						topLevelEnumerator = bucketTable.GetEnumerator();
						if (!topLevelEnumerator.MoveNext())
						{
							return false;
						}
					}
					if (withinBucketEnumerator == null)
					{
						withinBucketEnumerator = ((Bucket)((DictionaryEntry)topLevelEnumerator.Current).Value).GetEnumerator();
					}
					if (withinBucketEnumerator.MoveNext())
					{
						return true;
					}
					if (topLevelEnumerator.MoveNext())
					{
						withinBucketEnumerator = ((Bucket)((DictionaryEntry)topLevelEnumerator.Current).Value).GetEnumerator();
						return withinBucketEnumerator.MoveNext();
					}
					return false;
				}
			}

			private Hashtable table;

			internal InstanceCollection()
			{
				table = new Hashtable(541);
			}

			internal ulong keyOf(object obj)
			{
				uint hashCode = (uint)getHashCode(obj);
				Bucket bucket = (Bucket)table[hashCode];
				if (bucket == null)
				{
					return 0uL;
				}
				uint num = bucket.keyOf(obj);
				if (num != 0)
				{
					return ((ulong)hashCode << 24) | num;
				}
				return 0uL;
			}

			internal object get(ulong key)
			{
				if (key == 0)
				{
					return null;
				}
				uint num = (uint)(key >> 24);
				Bucket bucket = (Bucket)table[num];
				uint withinBucketKey = (uint)(key & 0xFFFFFF);
				return bucket.get(withinBucketKey);
			}

			internal ulong put(object obj, string alias)
			{
				uint hashCode = (uint)getHashCode(obj);
				Bucket bucket = (Bucket)table[hashCode];
				if (bucket == null)
				{
					bucket = new Bucket();
					table.Add(hashCode, bucket);
				}
				uint num = bucket.put(obj, alias);
				return ((ulong)hashCode << 24) | num;
			}

			internal bool addAlias(ulong key, string alias)
			{
				uint num = (uint)(key >> 24);
				Bucket bucket = (Bucket)table[num];
				uint withinBucketKey = (uint)(key & 0xFFFFFF);
				return bucket.addAlias(withinBucketKey, alias);
			}

			internal void remove(ulong key)
			{
				uint num = (uint)(key >> 24);
				Bucket bucket = (Bucket)table[num];
				if (bucket != null)
				{
					if (bucket.size() == 1)
					{
						table.Remove(num);
						return;
					}
					uint withinBucketKey = (uint)(key & 0xFFFFFF);
					bucket.remove(withinBucketKey);
				}
			}

			internal int size()
			{
				ICollection values = table.Values;
				int num = 0;
				foreach (Bucket item in values)
				{
					num += item.size();
				}
				return num;
			}

			public InstanceCollectionEnumerator GetEnumerator()
			{
				return new InstanceCollectionEnumerator(table);
			}

			private static int getHashCode(object obj)
			{
				if (!(obj is Expr))
				{
					return obj.GetHashCode();
				}
				return ((Expr)obj).inheritedHashCode();
			}
		}

		private class Bucket
		{
			internal class BucketRec
			{
				private object o;

				internal uint key;

				internal bool compareValuesOnly;

				internal string[] aliases;

				internal object obj
				{
					get
					{
						if (compareValuesOnly)
						{
							Type type = o.GetType();
							if (Utils.IsTrulyPrimitive(type))
							{
								return Convert.ChangeType(o, type);
							}
							return Enum.ToObject(type, o);
						}
						return o;
					}
				}

				internal BucketRec(object obj, uint key, string alias)
				{
					this.key = key;
					aliases = new string[1]
					{
						alias
					};
					Type type = obj.GetType();
					if (Utils.IsTrulyPrimitive(type))
					{
						compareValuesOnly = true;
						o = Convert.ChangeType(obj, type);
					}
					else if (type.IsEnum)
					{
						compareValuesOnly = true;
						o = Enum.ToObject(type, obj);
					}
					else
					{
						o = obj;
					}
				}

				internal bool addAlias(string alias)
				{
					string[] array = aliases;
					foreach (string a in array)
					{
						if (a == alias)
						{
							return false;
						}
					}
					string[] array2 = new string[aliases.Length + 1];
					aliases.CopyTo(array2, 0);
					array2[array2.Length - 1] = alias;
					aliases = array2;
					return true;
				}
			}

			private const uint largestKey = 16777215u;

			private Hashtable table;

			private uint nextKey;

			internal Bucket()
			{
				table = new Hashtable(17, 1f);
				nextKey = 1u;
			}

			internal uint put(object obj, string alias)
			{
				uint num = nextKey++;
				if (nextKey > 16777215)
				{
					nextKey = 1u;
				}
				table.Add(num, new BucketRec(obj, num, alias));
				return num;
			}

			internal bool addAlias(uint withinBucketKey, string alias)
			{
				BucketRec bucketRec = (BucketRec)table[withinBucketKey];
				return bucketRec.addAlias(alias);
			}

			internal object get(uint withinBucketKey)
			{
				return ((BucketRec)table[withinBucketKey])?.obj;
			}

			internal void remove(uint withinBucketKey)
			{
				table.Remove(withinBucketKey);
			}

			internal uint keyOf(object obj)
			{
				ICollection values = table.Values;
				foreach (BucketRec item in values)
				{
					object obj2 = item.obj;
					if (object.ReferenceEquals(obj2, obj) || (item.compareValuesOnly && obj.Equals(obj2)))
					{
						return item.key;
					}
				}
				return 0u;
			}

			internal int size()
			{
				return table.Count;
			}

			internal IEnumerator GetEnumerator()
			{
				return table.GetEnumerator();
			}
		}

		private const int OK = 0;

		private const int NAME_NOT_FOUND = 1;

		private const int ARG_COUNT_INCORRECT = 2;

		private const int ARGS_DONT_MATCH = 3;

		private const int NOT = 0;

		private const int ASSIGNABLE = 1;

		private const int EXACTLY = 2;

		private InstanceCollection instanceCollection = new InstanceCollection();

		private Hashtable typesTable = new Hashtable();

		private COMDispatchHandler comDispatchHandler = new COMDispatchHandler();

		private object canOnlyMatchOutParam = new object();

		private ArrayList matchingMethods = new ArrayList(32);

		private ArrayList methodRecs = new ArrayList(32);

		private static int objectSymbolPrefixLength = "NETLink`Objects`NETObject$".Length;

		internal void loadType(IKernelLink ml, Type t)
		{
			string assemblyQualifiedName = t.AssemblyQualifiedName;
			if (typesTable.ContainsKey(assemblyQualifiedName))
			{
				ml.PutFunction("List", 2);
				ml.Put(fullTypeNameForMathematica(t));
				ml.Put(assemblyQualifiedName);
				return;
			}
			TypeRecord typeRecord = new TypeRecord(t);
			ml.PutFunction("List", 9);
			ml.Put(fullTypeNameForMathematica(t));
			ml.Put(assemblyQualifiedName);
			ml.Put((t.Namespace == null) ? "" : t.Namespace);
			ml.Put(typeRecord.staticFieldNames);
			ml.PutFunction("Thread", 1);
			ml.PutFunction("List", 2);
			ml.Put(typeRecord.staticPropertyNames);
			ml.Put(typeRecord.isStaticPropertyParameterized);
			ml.Put(typeRecord.staticMethodNames);
			ml.Put(typeRecord.staticEventNames);
			ml.Put(typeRecord.nonPrimitiveFieldOrSimplePropNames);
			ml.Put(typeRecord.indexers.Length > 0);
			typesTable.Add(assemblyQualifiedName, typeRecord);
		}

		internal object call(IKernelLink ml, string assemblyQualifiedName, string objSymbol, int callType, string mmaMemberName, int[] argTypes, out OutParamRecord[] outParams)
		{
			TypeRecord typeRecord = (TypeRecord)typesTable[assemblyQualifiedName];
			if (typeRecord == null)
			{
				throw new CallNETException("badtype", assemblyQualifiedName, null);
			}
			object obj = lookupObject(objSymbol);
			if (obj == null && objSymbol != "Null")
			{
				throw new CallNETException("badobj", null, null);
			}
			int num = mmaMemberName.LastIndexOf('`');
			string memberName = ((num != -1) ? mmaMemberName.Substring(num + 1) : mmaMemberName);
			if (callType == 1 || callType == 2)
			{
				outParams = null;
				return callFieldOrSimpleProperty(ml, callType, typeRecord, obj, memberName, argTypes);
			}
			return callMethod(ml, callType, typeRecord, obj, memberName, argTypes, out outParams);
		}

		internal void releaseInstance(string[] objectSyms)
		{
			foreach (string sym in objectSyms)
			{
				ulong key = keyFromMmaSymbol(sym);
				instanceCollection.remove(key);
			}
		}

		internal unsafe void putReference(IKernelLink ml, object obj, Type upCastCls)
		{
			bool flag = upCastCls != null;
			Type type = (flag ? upCastCls : obj.GetType());
			if (flag && !upCastCls.IsInstanceOfType(obj))
			{
				throw new InvalidCastException();
			}
			bool flag2 = !Utils.IsMono && type.IsCOMObject && type.Name == "__ComObject";
			bool flag3 = obj is COMObjectWrapper;
			string text = null;
			if (flag3)
			{
				COMObjectWrapper cOMObjectWrapper = (COMObjectWrapper)obj;
				obj = cOMObjectWrapper.wrappedObject;
				if (cOMObjectWrapper.type != null)
				{
					Marshal.SetComObjectData(obj, "NETLinkInterface", cOMObjectWrapper.type);
					type = cOMObjectWrapper.type;
					flag = true;
				}
				else
				{
					type = obj.GetType();
				}
			}
			if (flag2 || (flag3 && type.Name == "__ComObject"))
			{
				Type type2 = (Type)Marshal.GetComObjectData(obj, "NETLinkInterface");
				if (type2 != null)
				{
					type = type2;
					flag = true;
				}
				else
				{
					text = COMUtilities.GetDefaultCOMInterfaceName(obj);
				}
			}
			if (obj is Pointer)
			{
				obj = new IntPtr(Pointer.Unbox(obj));
				type = typeof(IntPtr);
			}
			string text2 = (flag ? fullTypeNameForMathematica(type) : ((text != null) ? text : ""));
			ulong num = instanceCollection.keyOf(obj);
			bool flag4 = num == 0;
			bool flag5;
			if (flag4)
			{
				num = instanceCollection.put(obj, text2);
				flag5 = false;
			}
			else
			{
				flag5 = instanceCollection.addAlias(num, text2);
			}
			string s = mmaSymbolFromKey(num, text2);
			if (flag4 || flag5)
			{
				string assemblyQualifiedName = type.AssemblyQualifiedName;
				string obj2 = fullTypeNameForMathematica(type);
				bool flag6 = typesTable[assemblyQualifiedName] == null;
				int argCount = ((flag2 || flag3) ? 7 : 5);
				ml.PutFunction("NETLink`Package`createInstanceDefs", argCount);
				ml.Put(obj2);
				ml.Put(assemblyQualifiedName);
				ml.PutSymbol(s);
				ml.Put(flag6);
				ml.Put(!flag4);
				if (flag2 || flag3)
				{
					ml.Put(true);
					ml.Put((text == null) ? "" : text);
				}
			}
			else
			{
				ml.PutSymbol(s);
			}
		}

		internal object lookupObject(string objSymbol)
		{
			object result = null;
			if (objSymbol != "Null")
			{
				ulong key = keyFromMmaSymbol(objSymbol);
				result = instanceCollection.get(key);
			}
			return result;
		}

		internal void peekObjects(IKernelLink ml)
		{
			ml.PutFunction("List", instanceCollection.size());
			InstanceCollection.InstanceCollectionEnumerator enumerator = instanceCollection.GetEnumerator();
			try
			{
				while (enumerator.MoveNext())
				{
					string s = (string)enumerator.Current;
					ml.PutSymbol(s);
				}
			}
			finally
			{
				(enumerator as IDisposable)?.Dispose();
			}
		}

		internal void peekTypes(IKernelLink ml)
		{
			ml.PutFunction("List", typesTable.Count);
			foreach (DictionaryEntry item in typesTable)
			{
				ml.PutFunction("List", 2);
				ml.Put(((TypeRecord)item.Value).fullName);
				ml.Put(((TypeRecord)item.Value).type.AssemblyQualifiedName);
			}
		}

		internal void peekAssemblies(IKernelLink ml)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			ml.PutFunction("List", assemblies.Length);
			Assembly[] array = assemblies;
			foreach (Assembly assembly in array)
			{
				ml.PutFunction("List", 2);
				ml.Put(assembly.GetName().Name);
				ml.Put(assembly.FullName);
			}
		}

		internal static string fullTypeNameForMathematica(Type t)
		{
			string text = t.FullName;
			if (text == null)
			{
				text = t.ToString();
			}
			if (t.IsGenericType)
			{
				Type[] genericArguments = t.GetGenericArguments();
				int num = text.IndexOf("[");
				text = ((num < 0) ? (text + "[") : text.Substring(0, num + 1));
				for (int i = 0; i < genericArguments.Length; i++)
				{
					text += fullTypeNameForMathematica(genericArguments[i]);
					if (i < genericArguments.Length - 1)
					{
						text += ",";
					}
				}
				text += "]";
			}
			return text;
		}

		internal Type getType(string aqTypeName)
		{
			TypeRecord typeRecord = (TypeRecord)typesTable[aqTypeName];
			if (typeRecord == null)
			{
				throw new CallNETException("badtype", aqTypeName, null);
			}
			return typeRecord.type;
		}

		internal ConstructorInfo[] getConstructors(string aqTypeName)
		{
			TypeRecord typeRecord = (TypeRecord)typesTable[aqTypeName];
			if (typeRecord == null)
			{
				throw new CallNETException("badtype", aqTypeName, null);
			}
			return typeRecord.constructors;
		}

		internal FieldInfo[] getFields(string aqTypeName)
		{
			TypeRecord typeRecord = (TypeRecord)typesTable[aqTypeName];
			if (typeRecord == null)
			{
				throw new CallNETException("badtype", aqTypeName, null);
			}
			return typeRecord.fields;
		}

		internal PropertyInfo[] getProperties(string aqTypeName)
		{
			TypeRecord typeRecord = (TypeRecord)typesTable[aqTypeName];
			if (typeRecord == null)
			{
				throw new CallNETException("badtype", aqTypeName, null);
			}
			return typeRecord.properties;
		}

		internal MethodInfo[] getMethods(string aqTypeName)
		{
			TypeRecord typeRecord = (TypeRecord)typesTable[aqTypeName];
			if (typeRecord == null)
			{
				throw new CallNETException("badtype", aqTypeName, null);
			}
			return typeRecord.methods;
		}

		internal EventInfo[] getEvents(string aqTypeName)
		{
			TypeRecord typeRecord = (TypeRecord)typesTable[aqTypeName];
			if (typeRecord == null)
			{
				throw new CallNETException("badtype", aqTypeName, null);
			}
			return typeRecord.events;
		}

		private object callMethod(IKernelLink ml, int callType, TypeRecord t, object obj, string memberName, int[] argTypes, out OutParamRecord[] outParams)
		{
			bool flag = callType == 0;
			bool flag2 = memberName == "" && !flag;
			if (flag && argTypes.Length == 0 && t.type.IsValueType)
			{
				outParams = null;
				return Activator.CreateInstance(t.type);
			}
			if (flag2)
			{
				if (t.indexerName != null)
				{
					memberName = t.indexerName;
				}
				else
				{
					if (obj == null || Utils.IsMono || !Marshal.IsComObject(obj) || (!(t.name == "__ComObject") && !t.type.IsInterface))
					{
						throw new CallNETException("noindexer", t.fullName, null);
					}
					memberName = "Item";
				}
			}
			string methName = memberName;
			switch (callType)
			{
			case 3:
				methName = "get_" + memberName;
				break;
			case 4:
				methName = "set_" + memberName;
				break;
			}
			MethodBase[] methods = (flag ? ((MethodBase[])t.constructors) : ((MethodBase[])t.methods));
			bool flag3 = callType == 3 || callType == 4;
			matchingMethods.Clear();
			findMethodMatches(methName, flag, argTypes, methods, t.methodNames, matchingMethods, ml.ComplexType, out var err);
			if (callType == 5 && err == 1)
			{
				findMethodMatches("get_" + memberName, flag, argTypes, methods, t.methodNames, matchingMethods, ml.ComplexType, out err);
				flag3 = true;
			}
			if (err == 1 && obj != null && !Utils.IsMono && Marshal.IsComObject(obj) && (t.name == "__ComObject" || t.type.IsInterface))
			{
				return comDispatchHandler.callDispatch(ml, obj, memberName, callType, argTypes, out outParams);
			}
			if (err != 0)
			{
				string text = "";
				throw new CallNETException(err switch
				{
					2 => (!flag) ? ((!flag2) ? ((!flag3) ? "methargc" : "parampropargc") : "indxrargc") : "ctorargc", 
					3 => (!flag) ? ((!flag2) ? ((!flag3) ? "methodargs" : "parampropargs") : "indxrargs") : "ctorargs", 
					_ => (!flag) ? ((!flag2) ? "nomethod" : ((callType == 3) ? "indxrnoget" : "indxrnoset")) : "noctor", 
				}, t.fullName, memberName);
			}
			MethodBase methodBase = null;
			object[] args;
			try
			{
				if (matchingMethods.Count == 1)
				{
					methodBase = (MethodBase)matchingMethods[0];
					ParameterInfo[] parameters = methodBase.GetParameters();
					args = new object[argTypes.Length];
					for (int i = 0; i < args.Length; i++)
					{
						if (Utils.IsOutOnlyParam(parameters[i]))
						{
							Utils.discardNext(ml);
							args[i] = null;
						}
						else
						{
							args[i] = Utils.readArgAs(ml, argTypes[i], parameters[i].ParameterType);
						}
					}
				}
				else
				{
					methodBase = pickBestMethod(ml, matchingMethods, argTypes, out args);
				}
			}
			catch (Exception)
			{
				if (!flag3)
				{
					_ = methodBase?.Name;
				}
				throw new CallNETException(flag ? "ctorargs" : "methodargs", t.fullName, (methodBase != null) ? methodBase.Name : memberName);
			}
			try
			{
				ParameterInfo[] parameters2 = methodBase.GetParameters();
				if (args.Length < parameters2.Length)
				{
					object[] array = new object[parameters2.Length];
					args.CopyTo(array, 0);
					for (int j = args.Length; j < parameters2.Length; j++)
					{
						array[j] = Missing.Value;
					}
					args = array;
				}
				PointerArgumentManager pointerArgumentManager = null;
				for (int k = 0; k < args.Length; k++)
				{
					ParameterInfo parameterInfo = parameters2[k];
					if (Utils.IsOutOnlyParam(parameterInfo))
					{
						args[k] = null;
					}
					else if (parameterInfo.ParameterType.IsPointer && Utils.IsTrulyPrimitive(parameterInfo.ParameterType.GetElementType()) && (Utils.IsTrulyPrimitive(args[k].GetType()) || args[k].GetType().IsArray))
					{
						if (pointerArgumentManager == null)
						{
							pointerArgumentManager = new PointerArgumentManager(args.Length);
						}
						args[k] = pointerArgumentManager.add(k, args[k], parameterInfo);
					}
				}
				object obj2;
				if (pointerArgumentManager == null)
				{
					obj2 = (flag ? ((ConstructorInfo)methodBase).Invoke(args) : methodBase.Invoke(obj, args));
				}
				else
				{
					try
					{
						obj2 = (flag ? ((ConstructorInfo)methodBase).Invoke(args) : methodBase.Invoke(obj, args));
					}
					finally
					{
						pointerArgumentManager.release();
					}
				}
				if (!flag && obj2 != null && !Utils.IsMono && Marshal.IsComObject(obj2) && obj2.GetType().Name == "__ComObject" && ((MethodInfo)methodBase).ReturnType.IsInterface)
				{
					obj2 = new COMObjectWrapper(obj2, ((MethodInfo)methodBase).ReturnType);
				}
				outParams = null;
				for (int l = 0; l < parameters2.Length; l++)
				{
					bool isByRef = parameters2[l].ParameterType.IsByRef;
					bool flag4 = pointerArgumentManager?.wasPointerArg(l) ?? false;
					if (!isByRef && !flag4)
					{
						continue;
					}
					if (outParams == null)
					{
						outParams = new OutParamRecord[parameters2.Length];
					}
					object obj3 = (flag4 ? pointerArgumentManager.getValue(l) : args[l]);
					if (obj3 != null && !Utils.IsMono && Marshal.IsComObject(obj3) && obj3.GetType().Name == "__ComObject")
					{
						Type paramType = GetParamType(methodBase.GetParameters()[l]);
						if (paramType.IsInterface)
						{
							obj3 = new COMObjectWrapper(obj3, paramType);
						}
					}
					outParams[l] = new OutParamRecord(l, obj3);
				}
				return obj2;
			}
			catch (Exception innerException)
			{
				throw new CallNETException(innerException, methodBase.Name);
			}
		}

		private object callFieldOrSimpleProperty(IKernelLink ml, int callType, TypeRecord t, object obj, string memberName, int[] argTypes)
		{
			bool flag = callType == 1;
			PropertyInfo propertyInfo = null;
			for (int i = 0; i < t.propertyNames.Length; i++)
			{
				if (Utils.memberNamesMatch(t.propertyNames[i], memberName))
				{
					propertyInfo = t.properties[i];
					break;
				}
			}
			if (propertyInfo != null)
			{
				if (flag)
				{
					if (propertyInfo.CanRead)
					{
						try
						{
							object value = propertyInfo.GetValue(obj, null);
							if (value == null)
							{
								return value;
							}
							if (Utils.IsMono)
							{
								return value;
							}
							if (!Marshal.IsComObject(value))
							{
								return value;
							}
							if (!(value.GetType().Name == "__ComObject"))
							{
								return value;
							}
							if (propertyInfo.PropertyType.IsInterface)
							{
								return new COMObjectWrapper(value, propertyInfo.PropertyType);
							}
							return value;
						}
						catch (Exception innerException)
						{
							throw new CallNETException(innerException, propertyInfo.Name);
						}
					}
					throw new CallNETException("propnoget", t.fullName, propertyInfo.Name);
				}
				if (propertyInfo.CanWrite)
				{
					object value2;
					try
					{
						value2 = Utils.readArgAs(ml, argTypes[0], propertyInfo.PropertyType);
					}
					catch (Exception)
					{
						throw new CallNETException("proptype", t.fullName, propertyInfo.Name);
					}
					try
					{
						propertyInfo.SetValue(obj, value2, null);
					}
					catch (Exception innerException2)
					{
						throw new CallNETException(innerException2, propertyInfo.Name);
					}
					return null;
				}
				throw new CallNETException("propnoset", t.fullName, propertyInfo.Name);
			}
			FieldInfo fieldInfo = null;
			for (int j = 0; j < t.fieldNames.Length; j++)
			{
				if (Utils.memberNamesMatch(t.fieldNames[j], memberName))
				{
					fieldInfo = t.fields[j];
					break;
				}
			}
			if (fieldInfo == null)
			{
				for (int k = 0; k < t.eventNames.Length; k++)
				{
					if (Utils.memberNamesMatch(t.eventNames[k], memberName))
					{
						throw new CallNETException("event", t.fullName, memberName);
					}
				}
				OutParamRecord[] outParams;
				if (obj != null && !Utils.IsMono && Marshal.IsComObject(obj) && (t.name == "__ComObject" || t.type.IsInterface))
				{
					return comDispatchHandler.callDispatch(ml, obj, memberName, callType, argTypes, out outParams);
				}
				throw new CallNETException("nofield", t.fullName, memberName);
			}
			if (flag)
			{
				try
				{
					object value = fieldInfo.GetValue(obj);
					if (value == null)
					{
						return value;
					}
					if (Utils.IsMono)
					{
						return value;
					}
					if (!Marshal.IsComObject(value))
					{
						return value;
					}
					if (!(value.GetType().Name == "__ComObject"))
					{
						return value;
					}
					if (fieldInfo.FieldType.IsInterface)
					{
						return new COMObjectWrapper(value, fieldInfo.FieldType);
					}
					return value;
				}
				catch (Exception innerException3)
				{
					throw new CallNETException(innerException3, fieldInfo.Name);
				}
			}
			if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
			{
				throw new CallNETException("fieldnoset", t.fullName, fieldInfo.Name);
			}
			object value3;
			try
			{
				value3 = Utils.readArgAs(ml, argTypes[0], fieldInfo.FieldType);
			}
			catch (Exception)
			{
				throw new CallNETException("fieldtype", t.fullName, fieldInfo.Name);
			}
			try
			{
				fieldInfo.SetValue(obj, value3);
			}
			catch (Exception innerException4)
			{
				throw new CallNETException(innerException4, fieldInfo.Name);
			}
			return null;
		}

		private unsafe MethodBase pickBestMethod(IKernelLink ml, IList matchingMethods, int[] argTypes, out object[] args)
		{
			args = new object[argTypes.Length];
			for (int i = 0; i < args.Length; i++)
			{
				args[i] = canOnlyMatchOutParam;
			}
			methodRecs.Clear();
			foreach (MethodBase matchingMethod in matchingMethods)
			{
				methodRecs.Add(new MethodRec(matchingMethod, matchingMethod.GetParameters()));
			}
			for (int j = 0; j < argTypes.Length; j++)
			{
				int num = argTypes[j];
				if (methodRecs.Count == 1)
				{
					ParameterInfo parameterInfo = ((MethodRec)methodRecs[0]).pia[j];
					if (Utils.IsOutOnlyParam(parameterInfo))
					{
						Utils.discardNext(ml);
						args[j] = null;
					}
					else
					{
						args[j] = Utils.readArgAs(ml, num, parameterInfo.ParameterType);
					}
					continue;
				}
				bool flag = false;
				bool flag2 = false;
				bool flag3 = true;
				bool flag4 = true;
				Type type = null;
				foreach (MethodRec methodRec29 in methodRecs)
				{
					ParameterInfo parameterInfo2 = methodRec29.pia[j];
					Type parameterType = parameterInfo2.ParameterType;
					if (!Utils.IsOutOnlyParam(parameterInfo2))
					{
						flag4 = false;
						if (type == null)
						{
							type = parameterType;
						}
						else if (type != parameterType)
						{
							flag3 = false;
						}
					}
					if (parameterType == typeof(Expr))
					{
						flag = true;
					}
					else if (parameterType == typeof(Expr[]))
					{
						flag2 = true;
					}
				}
				if (flag4)
				{
					Utils.discardNext(ml);
					args[j] = null;
					continue;
				}
				if (flag3)
				{
					args[j] = Utils.readArgAs(ml, num, type);
					continue;
				}
				if (flag)
				{
					args[j] = Utils.readArgAs(ml, num, typeof(Expr));
					for (int num2 = methodRecs.Count - 1; num2 >= 0; num2--)
					{
						MethodRec methodRec2 = (MethodRec)methodRecs[num2];
						ParameterInfo parameterInfo3 = methodRec2.pia[j];
						if (parameterInfo3.ParameterType != typeof(Expr) && !Utils.IsOutOnlyParam(parameterInfo3))
						{
							methodRecs.RemoveAt(num2);
						}
					}
					continue;
				}
				if (flag2 && (num == 8 || num == 9 || num == 10 || num == 11))
				{
					args[j] = Utils.readArgAs(ml, num, typeof(Expr[]));
					cullOutIncompatibleArrayMethods(methodRecs, j, typeof(Expr));
					continue;
				}
				switch (num)
				{
				case 1:
				{
					bool flag6 = false;
					foreach (MethodRec methodRec30 in methodRecs)
					{
						ParameterInfo pi2 = methodRec30.pia[j];
						Type paramType2 = GetParamType(pi2);
						if (!Utils.IsOutOnlyParam(pi2) && (paramType2 == typeof(long) || paramType2 == typeof(ulong) || paramType2 == typeof(decimal)))
						{
							flag6 = true;
							break;
						}
					}
					if (flag6)
					{
						args[j] = Utils.readArgAs(ml, num, typeof(decimal));
					}
					else
					{
						args[j] = Utils.readArgAs(ml, num, typeof(int));
					}
					break;
				}
				case 2:
				{
					bool flag5 = false;
					foreach (MethodRec methodRec31 in methodRecs)
					{
						ParameterInfo pi = methodRec31.pia[j];
						Type paramType = GetParamType(pi);
						if (!Utils.IsOutOnlyParam(pi) && paramType == typeof(decimal))
						{
							flag5 = true;
							break;
						}
					}
					if (flag5)
					{
						args[j] = Utils.readArgAs(ml, num, typeof(decimal));
					}
					else
					{
						args[j] = Utils.readArgAs(ml, num, typeof(double));
					}
					break;
				}
				case 3:
					args[j] = Utils.readArgAs(ml, num, typeof(string));
					break;
				case 4:
					args[j] = Utils.readArgAs(ml, num, typeof(bool));
					break;
				case 7:
				{
					object obj = Utils.readArgAs(ml, num, typeof(object));
					Type type15 = obj.GetType();
					for (int num7 = methodRecs.Count - 1; num7 >= 0; num7--)
					{
						MethodRec methodRec24 = (MethodRec)methodRecs[num7];
						ParameterInfo pi18 = methodRec24.pia[j];
						if (!Utils.IsOutOnlyParam(pi18) && !isWideningConversion(type15, pi18))
						{
							methodRecs.RemoveAt(num7);
						}
					}
					args[j] = obj;
					break;
				}
				case 5:
					ml.GetSymbol();
					args[j] = null;
					break;
				case 6:
				{
					ml.GetSymbol();
					args[j] = Missing.Value;
					for (int num6 = methodRecs.Count - 1; num6 >= 0; num6--)
					{
						MethodRec methodRec18 = (MethodRec)methodRecs[num6];
						if (!methodRec18.pia[j].IsOptional)
						{
							methodRecs.RemoveAt(num6);
						}
					}
					break;
				}
				case 12:
					args[j] = Utils.readArgAs(ml, num, ml.ComplexType);
					break;
				case 8:
				{
					bool flag59 = false;
					Type type16 = null;
					foreach (MethodRec methodRec32 in methodRecs)
					{
						ParameterInfo pi19 = methodRec32.pia[j];
						type16 = GetParamType(pi19);
						if (!Utils.IsOutOnlyParam(pi19) && type16 == typeof(Expr[]))
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Expr[]));
							flag59 = true;
							break;
						}
					}
					if (flag59)
					{
						cullOutIncompatibleArrayMethods(methodRecs, j, typeof(Expr));
						break;
					}
					Type leafObjectType4;
					switch (getLeafExprType(ml, out leafObjectType4))
					{
					case ExpressionType.String:
						args[j] = Utils.readArgAs(ml, num, typeof(string[]));
						cullOutIncompatibleArrayMethods(methodRecs, j, typeof(string));
						break;
					case ExpressionType.Object:
						foreach (MethodRec methodRec33 in methodRecs)
						{
							ParameterInfo pi20 = methodRec33.pia[j];
							type16 = GetParamType(pi20);
							if (!Utils.IsOutOnlyParam(pi20))
							{
								if (!type16.IsArray)
								{
									args[j] = Utils.readArgAs(ml, num, type16);
									break;
								}
								Type elementType6 = type16.GetElementType();
								if (leafObjectType4 == null || elementType6.IsAssignableFrom(leafObjectType4))
								{
									args[j] = Utils.readArgAs(ml, num, type16);
									cullOutIncompatibleArrayMethods(methodRecs, j, elementType6);
									break;
								}
							}
						}
						if (args[j] == canOnlyMatchOutParam)
						{
							Utils.discardNext(ml);
						}
						break;
					case ExpressionType.Boolean:
						args[j] = Utils.readArgAs(ml, num, typeof(bool[]));
						cullOutIncompatibleArrayMethods(methodRecs, j, typeof(bool));
						break;
					case ExpressionType.Real:
					{
						bool flag72 = false;
						bool flag73 = false;
						foreach (MethodRec methodRec34 in methodRecs)
						{
							ParameterInfo pi22 = methodRec34.pia[j];
							type16 = GetParamType(pi22);
							if (!Utils.IsOutOnlyParam(pi22))
							{
								if (type16 == typeof(double[]) || type16 == typeof(double*))
								{
									flag72 = true;
								}
								else if (type16 == typeof(decimal[]) || type16 == typeof(decimal*))
								{
									flag73 = true;
								}
							}
						}
						if (flag73)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(decimal[]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(decimal));
						}
						else if (flag72)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(double[]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(double));
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(float[]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(float));
						}
						break;
					}
					case ExpressionType.Integer:
					{
						bool flag60 = false;
						bool flag61 = false;
						bool flag62 = false;
						bool flag63 = false;
						bool flag64 = false;
						bool flag65 = false;
						bool flag66 = false;
						bool flag67 = false;
						bool flag68 = false;
						bool flag69 = false;
						bool flag70 = false;
						bool flag71 = false;
						foreach (MethodRec methodRec35 in methodRecs)
						{
							ParameterInfo pi21 = methodRec35.pia[j];
							type16 = GetParamType(pi21);
							if (!Utils.IsOutOnlyParam(pi21) && !type16.IsEnum)
							{
								Type type17 = (type16.IsArray ? type16.GetElementType() : null);
								switch (Type.GetTypeCode(type17))
								{
								case TypeCode.Int32:
									flag65 = true;
									break;
								case TypeCode.Double:
									flag71 = true;
									break;
								case TypeCode.Byte:
									flag60 = true;
									break;
								case TypeCode.SByte:
									flag61 = true;
									break;
								case TypeCode.Char:
									flag62 = true;
									break;
								case TypeCode.Int16:
									flag63 = true;
									break;
								case TypeCode.UInt16:
									flag64 = true;
									break;
								case TypeCode.UInt32:
									flag66 = true;
									break;
								case TypeCode.Int64:
									flag67 = true;
									break;
								case TypeCode.UInt64:
									flag68 = true;
									break;
								case TypeCode.Decimal:
									flag69 = true;
									break;
								case TypeCode.Single:
									flag70 = true;
									break;
								}
							}
						}
						Type type18 = null;
						type18 = (flag69 ? typeof(decimal[]) : (flag67 ? typeof(long[]) : (flag68 ? typeof(ulong[]) : (flag65 ? typeof(int[]) : (flag66 ? typeof(uint[]) : (flag63 ? typeof(short[]) : (flag64 ? typeof(ushort[]) : (flag62 ? typeof(char[]) : (flag60 ? typeof(byte[]) : (flag61 ? typeof(sbyte[]) : (flag71 ? typeof(double[]) : ((!flag70) ? typeof(int[]) : typeof(float[])))))))))))));
						args[j] = Utils.readArgAs(ml, num, type18);
						cullOutIncompatibleArrayMethods(methodRecs, j, type18.GetElementType());
						break;
					}
					case ExpressionType.Complex:
						if (ml.ComplexType == null)
						{
							throw new MathLinkException(1010);
						}
						args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(ml.ComplexType, 0).GetType());
						cullOutIncompatibleArrayMethods(methodRecs, j, ml.ComplexType);
						break;
					default:
						throw new ArgumentException();
					}
					break;
				}
				case 9:
				{
					bool flag7 = false;
					bool flag8 = false;
					Type type2 = null;
					foreach (MethodRec methodRec36 in methodRecs)
					{
						ParameterInfo pi3 = methodRec36.pia[j];
						if (!Utils.IsOutOnlyParam(pi3))
						{
							type2 = GetParamType(pi3);
							if (type2 == typeof(Expr[,]))
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Expr[,]));
								flag7 = true;
								break;
							}
							if (type2 == typeof(Expr[][]))
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Expr[][]));
								flag8 = true;
								break;
							}
						}
					}
					if (flag7)
					{
						cullOutIncompatibleArrayMethods(methodRecs, j, typeof(Expr));
						break;
					}
					if (flag8)
					{
						cullOutIncompatibleArrayMethods(methodRecs, j, typeof(Expr[]));
						break;
					}
					Type leafObjectType;
					ExpressionType leafExprType = getLeafExprType(ml, out leafObjectType);
					bool flag9 = false;
					bool flag10 = false;
					foreach (MethodRec methodRec37 in methodRecs)
					{
						ParameterInfo pi4 = methodRec37.pia[j];
						type2 = GetParamType(methodRec37.pia[j]);
						if (Utils.IsOutOnlyParam(pi4))
						{
							continue;
						}
						Type elementType = type2.GetElementType();
						switch (type2.GetArrayRank())
						{
						case 2:
							flag9 = true;
							break;
						case 1:
							if (elementType.IsArray)
							{
								flag10 = true;
							}
							break;
						}
					}
					switch (leafExprType)
					{
					case ExpressionType.String:
						if (flag9)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(string[,]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(string));
						}
						else if (flag10)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(string[][]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(string[]));
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					case ExpressionType.Object:
						foreach (MethodRec methodRec38 in methodRecs)
						{
							ParameterInfo pi5 = methodRec38.pia[j];
							type2 = GetParamType(pi5);
							if (!Utils.IsOutOnlyParam(pi5))
							{
								if (!type2.IsArray)
								{
									args[j] = Utils.readArgAs(ml, num, type2);
									break;
								}
								int arrayRank = type2.GetArrayRank();
								Type elementType = type2.GetElementType();
								if (arrayRank == 1)
								{
									elementType = elementType.GetElementType();
								}
								if (leafObjectType == null || elementType.IsAssignableFrom(leafObjectType))
								{
									args[j] = Utils.readArgAs(ml, num, type2);
									cullOutIncompatibleArrayMethods(methodRecs, j, type2.GetElementType());
									break;
								}
							}
						}
						if (args[j] == canOnlyMatchOutParam)
						{
							Utils.discardNext(ml);
						}
						break;
					case ExpressionType.Boolean:
						if (flag9)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(bool[,]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(bool));
						}
						else if (flag10)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(bool[][]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(bool[]));
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					case ExpressionType.Real:
					{
						bool flag23 = false;
						bool flag24 = false;
						foreach (MethodRec methodRec39 in methodRecs)
						{
							ParameterInfo pi7 = methodRec39.pia[j];
							if (!Utils.IsOutOnlyParam(pi7))
							{
								type2 = GetParamType(pi7);
								if (type2 == typeof(double[,]) || type2 == typeof(double[][]))
								{
									flag23 = true;
								}
								else if (type2 == typeof(decimal[,]) || type2 == typeof(decimal[][]))
								{
									flag24 = true;
								}
							}
						}
						if (flag9)
						{
							if (flag24)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(decimal[,]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(decimal));
							}
							else if (flag23)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(double[,]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(double));
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(float[,]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(float));
							}
						}
						else if (flag10)
						{
							if (flag24)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(decimal[][]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(decimal[]));
							}
							else if (flag23)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(double[][]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(double[]));
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(float[][]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(float[]));
							}
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					}
					case ExpressionType.Integer:
					{
						bool flag11 = false;
						bool flag12 = false;
						bool flag13 = false;
						bool flag14 = false;
						bool flag15 = false;
						bool flag16 = false;
						bool flag17 = false;
						bool flag18 = false;
						bool flag19 = false;
						bool flag20 = false;
						bool flag21 = false;
						bool flag22 = false;
						foreach (MethodRec methodRec40 in methodRecs)
						{
							ParameterInfo pi6 = methodRec40.pia[j];
							type2 = GetParamType(pi6);
							if (!Utils.IsOutOnlyParam(pi6) && !type2.IsEnum && type2 != typeof(Array))
							{
								Type elementType = type2.GetElementType();
								if (elementType.IsArray)
								{
									elementType = elementType.GetElementType();
								}
								switch (Type.GetTypeCode(elementType))
								{
								case TypeCode.Int32:
									flag16 = true;
									break;
								case TypeCode.Double:
									flag22 = true;
									break;
								case TypeCode.Byte:
									flag11 = true;
									break;
								case TypeCode.SByte:
									flag12 = true;
									break;
								case TypeCode.Char:
									flag13 = true;
									break;
								case TypeCode.Int16:
									flag14 = true;
									break;
								case TypeCode.UInt16:
									flag15 = true;
									break;
								case TypeCode.UInt32:
									flag17 = true;
									break;
								case TypeCode.Int64:
									flag18 = true;
									break;
								case TypeCode.UInt64:
									flag19 = true;
									break;
								case TypeCode.Decimal:
									flag20 = true;
									break;
								case TypeCode.Single:
									flag21 = true;
									break;
								}
							}
						}
						Type type4 = null;
						type4 = (flag20 ? (flag9 ? typeof(decimal[,]) : typeof(decimal[])) : (flag18 ? (flag9 ? typeof(long[,]) : typeof(long[])) : (flag19 ? (flag9 ? typeof(ulong[,]) : typeof(ulong[])) : (flag16 ? (flag9 ? typeof(int[,]) : typeof(int[])) : (flag17 ? (flag9 ? typeof(uint[,]) : typeof(uint[])) : (flag14 ? (flag9 ? typeof(short[,]) : typeof(short[])) : (flag15 ? (flag9 ? typeof(ushort[,]) : typeof(ushort[])) : (flag13 ? (flag9 ? typeof(char[,]) : typeof(char[])) : (flag11 ? (flag9 ? typeof(byte[,]) : typeof(byte[])) : (flag12 ? (flag9 ? typeof(sbyte[,]) : typeof(sbyte[])) : (flag22 ? (flag9 ? typeof(double[,]) : typeof(double[])) : ((!flag21) ? (flag9 ? typeof(int[,]) : typeof(int[])) : (flag9 ? typeof(float[,]) : typeof(float[]))))))))))))));
						if (flag9)
						{
							args[j] = Utils.readArgAs(ml, num, type4);
							cullOutIncompatibleArrayMethods(methodRecs, j, type4.GetElementType());
						}
						else if (flag10)
						{
							Type type5 = Array.CreateInstance(type4, 0).GetType();
							args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(type5, 1).GetType());
							cullOutIncompatibleArrayMethods(methodRecs, j, type5);
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					}
					case ExpressionType.Complex:
						if (ml.ComplexType == null)
						{
							throw new MathLinkException(1010);
						}
						if (flag9)
						{
							args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(ml.ComplexType, 1, 0).GetType());
							cullOutIncompatibleArrayMethods(methodRecs, j, ml.ComplexType);
						}
						else if (flag10)
						{
							Type type3 = Array.CreateInstance(ml.ComplexType, 0).GetType();
							args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(type3, 1).GetType());
							cullOutIncompatibleArrayMethods(methodRecs, j, type3);
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					default:
						throw new ArgumentException();
					}
					break;
				}
				case 10:
				{
					bool flag41 = false;
					bool flag42 = false;
					Type type10 = null;
					foreach (MethodRec methodRec41 in methodRecs)
					{
						ParameterInfo pi13 = methodRec41.pia[j];
						if (!Utils.IsOutOnlyParam(pi13))
						{
							type10 = GetParamType(pi13);
							if (type10 == typeof(Expr[,,]))
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Expr[,,]));
								flag41 = true;
								break;
							}
							if (type10 == typeof(Expr[][][]))
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Expr[][][]));
								flag42 = true;
								break;
							}
						}
					}
					if (flag41)
					{
						cullOutIncompatibleArrayMethods(methodRecs, j, typeof(Expr));
						break;
					}
					if (flag42)
					{
						cullOutIncompatibleArrayMethods(methodRecs, j, typeof(Expr[][]));
						break;
					}
					Type leafObjectType3;
					ExpressionType leafExprType3 = getLeafExprType(ml, out leafObjectType3);
					bool flag43 = false;
					bool flag44 = false;
					foreach (MethodRec methodRec42 in methodRecs)
					{
						ParameterInfo pi14 = methodRec42.pia[j];
						if (Utils.IsOutOnlyParam(pi14))
						{
							continue;
						}
						type10 = GetParamType(pi14);
						Type elementType5 = type10.GetElementType();
						switch (type10.GetArrayRank())
						{
						case 3:
							flag43 = true;
							break;
						case 1:
							if (elementType5.IsArray && elementType5.GetArrayRank() == 1 && elementType5.GetElementType().IsArray)
							{
								flag44 = true;
							}
							break;
						}
					}
					switch (leafExprType3)
					{
					case ExpressionType.String:
						if (flag43)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(string[,,]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(string));
						}
						else if (flag44)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(string[][][]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(string[][]));
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					case ExpressionType.Object:
						foreach (MethodRec methodRec43 in methodRecs)
						{
							ParameterInfo pi15 = methodRec43.pia[j];
							if (!Utils.IsOutOnlyParam(pi15))
							{
								type10 = GetParamType(pi15);
								if (!type10.IsArray)
								{
									args[j] = Utils.readArgAs(ml, num, type10);
									break;
								}
								int arrayRank5 = type10.GetArrayRank();
								Type elementType5 = null;
								switch (arrayRank5)
								{
								case 3:
									elementType5 = type10.GetElementType();
									break;
								case 1:
									elementType5 = type10.GetElementType().GetElementType().GetElementType();
									break;
								}
								if (leafObjectType3 == null || elementType5.IsAssignableFrom(leafObjectType3))
								{
									args[j] = Utils.readArgAs(ml, num, type10);
									cullOutIncompatibleArrayMethods(methodRecs, j, type10.GetElementType());
									break;
								}
							}
						}
						if (args[j] == canOnlyMatchOutParam)
						{
							Utils.discardNext(ml);
						}
						break;
					case ExpressionType.Boolean:
						if (flag43)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(bool[,,]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(bool));
						}
						else if (flag44)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(bool[][][]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(bool[][]));
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					case ExpressionType.Real:
					{
						bool flag57 = false;
						bool flag58 = false;
						foreach (MethodRec methodRec44 in methodRecs)
						{
							ParameterInfo pi17 = methodRec44.pia[j];
							if (!Utils.IsOutOnlyParam(pi17))
							{
								type10 = GetParamType(pi17);
								if (type10 == typeof(double[,,]) || type10 == typeof(double[][][]))
								{
									flag57 = true;
								}
								else if (type10 == typeof(decimal[,,]) || type10 == typeof(decimal[][][]))
								{
									flag58 = true;
								}
							}
						}
						if (flag43)
						{
							if (flag58)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(decimal[,,]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(decimal));
							}
							else if (flag57)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(double[,,]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(double));
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(float[,,]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(float));
							}
						}
						else if (flag44)
						{
							if (flag58)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(decimal[][][]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(decimal[]));
							}
							else if (flag57)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(double[][][]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(double[]));
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(float[][][]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(float[]));
							}
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					}
					case ExpressionType.Integer:
					{
						bool flag45 = false;
						bool flag46 = false;
						bool flag47 = false;
						bool flag48 = false;
						bool flag49 = false;
						bool flag50 = false;
						bool flag51 = false;
						bool flag52 = false;
						bool flag53 = false;
						bool flag54 = false;
						bool flag55 = false;
						bool flag56 = false;
						foreach (MethodRec methodRec45 in methodRecs)
						{
							ParameterInfo pi16 = methodRec45.pia[j];
							type10 = GetParamType(pi16);
							if (!Utils.IsOutOnlyParam(pi16) && !type10.IsEnum && type10 != typeof(Array))
							{
								Type elementType5 = type10.GetElementType();
								if (elementType5.IsArray)
								{
									elementType5 = elementType5.GetElementType().GetElementType();
								}
								switch (Type.GetTypeCode(elementType5))
								{
								case TypeCode.Int32:
									flag50 = true;
									break;
								case TypeCode.Double:
									flag56 = true;
									break;
								case TypeCode.Byte:
									flag45 = true;
									break;
								case TypeCode.SByte:
									flag46 = true;
									break;
								case TypeCode.Char:
									flag47 = true;
									break;
								case TypeCode.Int16:
									flag48 = true;
									break;
								case TypeCode.UInt16:
									flag49 = true;
									break;
								case TypeCode.UInt32:
									flag51 = true;
									break;
								case TypeCode.Int64:
									flag52 = true;
									break;
								case TypeCode.UInt64:
									flag53 = true;
									break;
								case TypeCode.Decimal:
									flag54 = true;
									break;
								case TypeCode.Single:
									flag55 = true;
									break;
								}
							}
						}
						Type type13 = null;
						type13 = (flag54 ? (flag43 ? typeof(decimal[,,]) : typeof(decimal[][])) : (flag52 ? (flag43 ? typeof(long[,,]) : typeof(long[][])) : (flag53 ? (flag43 ? typeof(ulong[,,]) : typeof(ulong[][])) : (flag50 ? (flag43 ? typeof(int[,,]) : typeof(int[][])) : (flag51 ? (flag43 ? typeof(uint[,,]) : typeof(uint[][])) : (flag48 ? (flag43 ? typeof(short[,,]) : typeof(short[][])) : (flag49 ? (flag43 ? typeof(ushort[,,]) : typeof(ushort[][])) : (flag47 ? (flag43 ? typeof(char[,,]) : typeof(char[][])) : (flag45 ? (flag43 ? typeof(byte[,,]) : typeof(byte[][])) : (flag46 ? (flag43 ? typeof(sbyte[,,]) : typeof(sbyte[][])) : (flag56 ? (flag43 ? typeof(double[,,]) : typeof(double[][])) : ((!flag55) ? (flag43 ? typeof(int[,,]) : typeof(int[][])) : (flag43 ? typeof(float[,,]) : typeof(float[][]))))))))))))));
						if (flag43)
						{
							args[j] = Utils.readArgAs(ml, num, type13);
							cullOutIncompatibleArrayMethods(methodRecs, j, type13.GetElementType());
						}
						else if (flag44)
						{
							Type type14 = Array.CreateInstance(type13, 0).GetType();
							args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(type14, 1).GetType());
							cullOutIncompatibleArrayMethods(methodRecs, j, type14);
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					}
					case ExpressionType.Complex:
						if (ml.ComplexType == null)
						{
							throw new MathLinkException(1010);
						}
						if (flag43)
						{
							args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(ml.ComplexType, 1, 1, 0).GetType());
							cullOutIncompatibleArrayMethods(methodRecs, j, ml.ComplexType);
						}
						else if (flag44)
						{
							Type type11 = Array.CreateInstance(ml.ComplexType, 0).GetType();
							Type type12 = Array.CreateInstance(type11, 1).GetType();
							args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(type12, 1).GetType());
							cullOutIncompatibleArrayMethods(methodRecs, j, type12);
						}
						else
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Array));
						}
						break;
					default:
						throw new ArgumentException();
					}
					break;
				}
				case 11:
				{
					int num4 = Utils.determineIncomingArrayDepth(ml);
					Type leafObjectType2;
					ExpressionType leafExprType2 = getLeafExprType(ml, out leafObjectType2);
					if (leafExprType2 == ExpressionType.Function)
					{
						throw new ArgumentException();
					}
					switch (num4)
					{
					case 1:
						throw new ArgumentException();
					case 2:
					{
						bool flag25 = false;
						bool flag26 = false;
						Type type6 = null;
						foreach (MethodRec methodRec46 in methodRecs)
						{
							ParameterInfo pi9 = methodRec46.pia[j];
							if (Utils.IsOutOnlyParam(pi9))
							{
								continue;
							}
							type6 = GetParamType(pi9);
							if (type6 == typeof(Expr[][]))
							{
								flag25 = true;
								break;
							}
							if (type6.IsArray)
							{
								Type elementType3 = type6.GetElementType();
								int arrayRank3 = type6.GetArrayRank();
								if (arrayRank3 == 1 && elementType3.IsArray)
								{
									flag26 = true;
									break;
								}
							}
						}
						if (flag25)
						{
							args[j] = Utils.readArgAs(ml, num, typeof(Expr[][]));
							cullOutIncompatibleArrayMethods(methodRecs, j, typeof(Expr[]));
							break;
						}
						switch (leafExprType2)
						{
						case ExpressionType.String:
							if (flag26)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(string[][]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(string[]));
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Array));
							}
							break;
						case ExpressionType.Object:
							foreach (MethodRec methodRec47 in methodRecs)
							{
								ParameterInfo pi11 = methodRec47.pia[j];
								if (Utils.IsOutOnlyParam(pi11))
								{
									continue;
								}
								type6 = GetParamType(pi11);
								if (type6.IsArray)
								{
									int arrayRank4 = type6.GetArrayRank();
									Type elementType4 = type6.GetElementType();
									if (arrayRank4 == 1)
									{
										elementType4 = elementType4.GetElementType();
										if (!elementType4.IsArray && (leafObjectType2 == null || elementType4.IsAssignableFrom(leafObjectType2)))
										{
											args[j] = Utils.readArgAs(ml, num, type6);
											cullOutIncompatibleArrayMethods(methodRecs, j, type6.GetElementType());
											break;
										}
									}
									continue;
								}
								args[j] = Utils.readArgAs(ml, num, type6);
								break;
							}
							if (args[j] == canOnlyMatchOutParam)
							{
								Utils.discardNext(ml);
							}
							break;
						case ExpressionType.Boolean:
							if (flag26)
							{
								args[j] = Utils.readArgAs(ml, num, typeof(bool[][]));
								cullOutIncompatibleArrayMethods(methodRecs, j, typeof(bool[]));
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Array));
							}
							break;
						case ExpressionType.Real:
						{
							bool flag27 = false;
							bool flag28 = false;
							foreach (MethodRec methodRec48 in methodRecs)
							{
								ParameterInfo pi10 = methodRec48.pia[j];
								if (!Utils.IsOutOnlyParam(pi10))
								{
									type6 = GetParamType(pi10);
									if (type6 == typeof(double[][]))
									{
										flag27 = true;
									}
									else if (type6 == typeof(decimal[][]))
									{
										flag28 = true;
									}
								}
							}
							if (flag26)
							{
								if (flag28)
								{
									args[j] = Utils.readArgAs(ml, num, typeof(decimal[][]));
									cullOutIncompatibleArrayMethods(methodRecs, j, typeof(decimal[]));
								}
								else if (flag27)
								{
									args[j] = Utils.readArgAs(ml, num, typeof(double[][]));
									cullOutIncompatibleArrayMethods(methodRecs, j, typeof(double[]));
								}
								else
								{
									args[j] = Utils.readArgAs(ml, num, typeof(float[][]));
									cullOutIncompatibleArrayMethods(methodRecs, j, typeof(float[]));
								}
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Array));
							}
							break;
						}
						case ExpressionType.Integer:
						{
							bool flag29 = false;
							bool flag30 = false;
							bool flag31 = false;
							bool flag32 = false;
							bool flag33 = false;
							bool flag34 = false;
							bool flag35 = false;
							bool flag36 = false;
							bool flag37 = false;
							bool flag38 = false;
							bool flag39 = false;
							bool flag40 = false;
							foreach (MethodRec methodRec49 in methodRecs)
							{
								ParameterInfo pi12 = methodRec49.pia[j];
								type6 = GetParamType(pi12);
								if (!Utils.IsOutOnlyParam(pi12) && !type6.IsEnum)
								{
									Type type8 = (type6.IsArray ? type6.GetElementType() : null);
									while (type8 != null && type8.IsArray)
									{
										type8 = type8.GetElementType();
									}
									switch (Type.GetTypeCode(type8))
									{
									case TypeCode.Int32:
										flag34 = true;
										break;
									case TypeCode.Double:
										flag40 = true;
										break;
									case TypeCode.Byte:
										flag29 = true;
										break;
									case TypeCode.SByte:
										flag30 = true;
										break;
									case TypeCode.Char:
										flag31 = true;
										break;
									case TypeCode.Int16:
										flag32 = true;
										break;
									case TypeCode.UInt16:
										flag33 = true;
										break;
									case TypeCode.UInt32:
										flag35 = true;
										break;
									case TypeCode.Int64:
										flag36 = true;
										break;
									case TypeCode.UInt64:
										flag37 = true;
										break;
									case TypeCode.Decimal:
										flag38 = true;
										break;
									case TypeCode.Single:
										flag39 = true;
										break;
									}
								}
							}
							if (flag26)
							{
								Type type9 = null;
								type9 = (flag38 ? typeof(decimal[]) : (flag36 ? typeof(long[]) : (flag37 ? typeof(ulong[]) : (flag34 ? typeof(int[]) : (flag35 ? typeof(uint[]) : (flag32 ? typeof(short[]) : (flag33 ? typeof(ushort[]) : (flag31 ? typeof(char[]) : (flag29 ? typeof(byte[]) : (flag30 ? typeof(sbyte[]) : (flag40 ? typeof(double[]) : ((!flag39) ? typeof(int[]) : typeof(float[])))))))))))));
								args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(type9, 1).GetType());
								cullOutIncompatibleArrayMethods(methodRecs, j, type9);
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Array));
							}
							break;
						}
						case ExpressionType.Complex:
							if (ml.ComplexType == null)
							{
								throw new MathLinkException(1010);
							}
							if (flag26)
							{
								Type type7 = Array.CreateInstance(ml.ComplexType, 0).GetType();
								args[j] = Utils.readArgAs(ml, num, Array.CreateInstance(type7, 1).GetType());
								cullOutIncompatibleArrayMethods(methodRecs, j, type7);
							}
							else
							{
								args[j] = Utils.readArgAs(ml, num, typeof(Array));
							}
							break;
						default:
							throw new ArgumentException();
						}
						break;
					}
					default:
					{
						MethodRec methodRec11 = null;
						MethodRec methodRec12 = null;
						Type complexType = ml.ComplexType;
						foreach (MethodRec methodRec50 in methodRecs)
						{
							ParameterInfo pi8 = methodRec50.pia[j];
							if (Utils.IsOutOnlyParam(pi8))
							{
								continue;
							}
							Type paramType3 = GetParamType(pi8);
							if (!paramType3.IsArray)
							{
								continue;
							}
							Type elementType2 = paramType3.GetElementType();
							int arrayRank2 = paramType3.GetArrayRank();
							if (elementType2.IsArray)
							{
								if (arrayRank2 > 1)
								{
									continue;
								}
								int num5 = 1;
								while (elementType2.IsArray && elementType2.GetArrayRank() == 1)
								{
									elementType2 = elementType2.GetElementType();
									num5++;
								}
								if (num5 == num4)
								{
									if ((leafExprType2 == ExpressionType.Integer && elementType2 == typeof(int)) || (leafExprType2 == ExpressionType.Real && elementType2 == typeof(double)) || (leafExprType2 == ExpressionType.String && elementType2 == typeof(string)) || (leafExprType2 == ExpressionType.Boolean && elementType2 == typeof(bool)) || (leafExprType2 == ExpressionType.Object && (leafObjectType2 == null || elementType2.IsAssignableFrom(leafObjectType2))) || (leafExprType2 == ExpressionType.Complex && elementType2 == ml.ComplexType))
									{
										methodRec11 = methodRec50;
										break;
									}
									if ((leafExprType2 == ExpressionType.Integer && possibleMatch(1, elementType2, isOptional: false, wasPointer: false, complexType)) || (leafExprType2 == ExpressionType.Real && possibleMatch(2, elementType2, isOptional: false, wasPointer: false, complexType)) || (leafExprType2 == ExpressionType.String && possibleMatch(3, elementType2, isOptional: false, wasPointer: false, complexType)) || (leafExprType2 == ExpressionType.Boolean && possibleMatch(4, elementType2, isOptional: false, wasPointer: false, complexType)) || (leafExprType2 == ExpressionType.Object && (leafObjectType2 == null || elementType2.IsAssignableFrom(leafObjectType2))) || (leafExprType2 == ExpressionType.Complex && elementType2 == ml.ComplexType))
									{
										methodRec12 = methodRec50;
									}
								}
							}
							else if (num4 > 3 && arrayRank2 == num4)
							{
								if ((leafExprType2 == ExpressionType.Integer && elementType2 == typeof(int)) || (leafExprType2 == ExpressionType.Real && elementType2 == typeof(double)) || (leafExprType2 == ExpressionType.String && elementType2 == typeof(string)) || (leafExprType2 == ExpressionType.Boolean && elementType2 == typeof(bool)) || (leafExprType2 == ExpressionType.Object && (leafObjectType2 == null || elementType2.IsAssignableFrom(leafObjectType2))) || (leafExprType2 == ExpressionType.Complex && elementType2 == ml.ComplexType))
								{
									methodRec11 = methodRec50;
									break;
								}
								if ((leafExprType2 == ExpressionType.Integer && possibleMatch(1, elementType2, isOptional: false, wasPointer: false, complexType)) || (leafExprType2 == ExpressionType.Real && possibleMatch(2, elementType2, isOptional: false, wasPointer: false, complexType)) || (leafExprType2 == ExpressionType.String && possibleMatch(3, elementType2, isOptional: false, wasPointer: false, complexType)) || (leafExprType2 == ExpressionType.Boolean && possibleMatch(4, elementType2, isOptional: false, wasPointer: false, complexType)) || (leafExprType2 == ExpressionType.Object && (leafObjectType2 == null || elementType2.IsAssignableFrom(leafObjectType2))) || (leafExprType2 == ExpressionType.Complex && elementType2 == ml.ComplexType))
								{
									methodRec12 = methodRec50;
								}
							}
						}
						if (methodRec11 != null)
						{
							args[j] = Utils.readArgAs(ml, num, GetParamType(methodRec11.pia[j]));
						}
						else if (methodRec12 != null)
						{
							args[j] = Utils.readArgAs(ml, num, GetParamType(methodRec12.pia[j]));
						}
						if (args[j] == canOnlyMatchOutParam)
						{
							Utils.discardNext(ml);
						}
						break;
					}
					}
					break;
				}
				case 13:
				{
					for (int num3 = methodRecs.Count - 1; num3 >= 0; num3--)
					{
						MethodRec methodRec3 = (MethodRec)methodRecs[num3];
						if (!Utils.IsOutOnlyParam(methodRec3.pia[j]))
						{
							methodRecs.RemoveAt(j);
						}
					}
					if (methodRecs.Count == 0)
					{
						throw new ArgumentException();
					}
					Utils.discardNext(ml);
					args[j] = null;
					break;
				}
				}
			}
			int count = methodRecs.Count;
			if (count == 0)
			{
				throw new ArgumentException();
			}
			bool[] array = new bool[count];
			bool[] array2 = new bool[count];
			bool flag74 = false;
			for (int k = 0; k < count; k++)
			{
				ParameterInfo[] pia = ((MethodRec)methodRecs[k]).pia;
				int objsMatch = 0;
				int primsMatch = 0;
				checkTypeMatches(args, pia, out objsMatch, out primsMatch);
				if (objsMatch == 2 && primsMatch == 2)
				{
					return ((MethodRec)methodRecs[k]).m;
				}
				if (objsMatch == 2 || objsMatch == 1)
				{
					array[k] = true;
					flag74 = true;
				}
				if (primsMatch == 2)
				{
					array2[k] = true;
				}
			}
			if (!flag74)
			{
				throw new ArgumentException();
			}
			for (int l = 0; l < count; l++)
			{
				if (array[l] && array2[l])
				{
					return ((MethodRec)methodRecs[l]).m;
				}
			}
			object[] array3 = new object[argTypes.Length];
			for (int m = 0; m < count && array[m]; m++)
			{
				args.CopyTo(array3, 0);
				ParameterInfo[] pia2 = ((MethodRec)methodRecs[m]).pia;
				if (narrowPrimitives(args, pia2, array3))
				{
					array3.CopyTo(args, 0);
					return ((MethodRec)methodRecs[m]).m;
				}
			}
			throw new ArgumentException();
		}

		private static void findMethodMatches(string methName, bool isCtor, int[] argTypes, MethodBase[] methods, string[] methodNames, IList matchingMethods, Type complexClass, out int err)
		{
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			for (int i = 0; i < methods.Length; i++)
			{
				if (!isCtor && !Utils.memberNamesMatch(methodNames[i], methName))
				{
					continue;
				}
				flag = true;
				MethodBase methodBase = methods[i];
				ParameterInfo[] parameters = methodBase.GetParameters();
				int num = 0;
				for (int j = 0; j < parameters.Length; j++)
				{
					num = (parameters[j].IsOptional ? (num + 1) : 0);
				}
				if (argTypes.Length > parameters.Length || argTypes.Length + num < parameters.Length)
				{
					continue;
				}
				flag2 = true;
				bool flag4 = true;
				for (int k = 0; k < argTypes.Length; k++)
				{
					ParameterInfo parameterInfo = parameters[k];
					if (!Utils.IsOutOnlyParam(parameterInfo) && !possibleMatch(argTypes[k], GetParamType(parameterInfo), parameterInfo.IsOptional, parameterInfo.ParameterType.IsPointer, complexClass))
					{
						flag4 = false;
						break;
					}
				}
				if (flag4)
				{
					flag3 = true;
					matchingMethods.Add(methodBase);
				}
			}
			if (!flag)
			{
				err = 1;
			}
			else if (!flag2)
			{
				err = 2;
			}
			else if (!flag3)
			{
				err = 3;
			}
			else
			{
				err = 0;
			}
		}

		private static bool possibleMatch(int argType, Type paramType, bool isOptional, bool wasPointer, Type complexClass)
		{
			TypeCode typeCode = Type.GetTypeCode(paramType);
			if (paramType == typeof(Expr))
			{
				return true;
			}
			switch (argType)
			{
			case 1:
				if ((!Utils.IsTrulyPrimitive(paramType) || typeCode == TypeCode.Boolean) && !paramType.IsEnum && typeCode != TypeCode.Decimal && paramType != typeof(object))
				{
					return paramType == complexClass;
				}
				return true;
			case 2:
				if (typeCode != TypeCode.Double && typeCode != TypeCode.Single && typeCode != TypeCode.Decimal && paramType != typeof(object))
				{
					return paramType == complexClass;
				}
				return true;
			case 3:
				if (typeCode != TypeCode.String)
				{
					return paramType == typeof(object);
				}
				return true;
			case 4:
				if (typeCode != TypeCode.Boolean)
				{
					return paramType == typeof(object);
				}
				return true;
			case 7:
				return true;
			case 5:
				if (typeCode != TypeCode.Object)
				{
					return typeCode == TypeCode.String;
				}
				return true;
			case 6:
				return isOptional;
			case 8:
				if ((!paramType.IsArray || paramType.GetArrayRank() != 1) && paramType != typeof(Array) && paramType != typeof(object))
				{
					return wasPointer;
				}
				return true;
			case 9:
			{
				if (paramType == typeof(Array) || paramType == typeof(object) || paramType == typeof(object[]))
				{
					return true;
				}
				if (!paramType.IsArray)
				{
					return false;
				}
				int arrayRank3 = paramType.GetArrayRank();
				Type elementType3 = paramType.GetElementType();
				if (arrayRank3 != 2)
				{
					if (arrayRank3 == 1 && elementType3.IsArray)
					{
						return elementType3.GetArrayRank() == 1;
					}
					return false;
				}
				return true;
			}
			case 10:
			{
				if (paramType == typeof(Array) || paramType == typeof(object) || paramType == typeof(object[]) || paramType == typeof(object[,]) || paramType == typeof(object[][]))
				{
					return true;
				}
				if (!paramType.IsArray)
				{
					return false;
				}
				int arrayRank = paramType.GetArrayRank();
				Type elementType = paramType.GetElementType();
				if (arrayRank != 3)
				{
					if (arrayRank == 1 && elementType.IsArray && elementType.GetArrayRank() == 1 && elementType.GetElementType().IsArray)
					{
						return elementType.GetElementType().GetArrayRank() == 1;
					}
					return false;
				}
				return true;
			}
			case 11:
			{
				if (paramType == typeof(Array) || paramType == typeof(object) || paramType == typeof(object[]))
				{
					return true;
				}
				if (!paramType.IsArray)
				{
					return false;
				}
				int arrayRank2 = paramType.GetArrayRank();
				Type elementType2 = paramType.GetElementType();
				if (arrayRank2 <= 3)
				{
					if (arrayRank2 == 1)
					{
						return elementType2.IsArray;
					}
					return false;
				}
				return true;
			}
			case 12:
				if (paramType != complexClass)
				{
					return paramType == typeof(object);
				}
				return true;
			case 13:
				return false;
			default:
				return false;
			}
		}

		private static void checkTypeMatches(object[] args, ParameterInfo[] paramInfos, out int objsMatch, out int primsMatch)
		{
			objsMatch = 2;
			primsMatch = 2;
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] == null)
				{
					continue;
				}
				Type type = ((args[i] != null) ? args[i].GetType() : null);
				ParameterInfo parameterInfo = paramInfos[i];
				Type paramType = GetParamType(parameterInfo);
				if (Utils.IsOutOnlyParam(parameterInfo) || paramType == type || (parameterInfo.IsOptional && args[i] == Missing.Value) || (parameterInfo.ParameterType.IsPointer && (type == typeof(Pointer) || type == typeof(IntPtr) || type.IsArray || type == typeof(Array))))
				{
					continue;
				}
				if (Utils.IsTrulyPrimitive(paramType))
				{
					primsMatch = 0;
				}
				else if (paramType.IsEnum && Utils.IsTrulyPrimitive(type))
				{
					primsMatch = 0;
				}
				else if (paramType.IsAssignableFrom(type))
				{
					if (objsMatch != 0)
					{
						objsMatch = 1;
					}
				}
				else
				{
					objsMatch = 0;
				}
			}
		}

		private static bool narrowPrimitives(object[] args, ParameterInfo[] paramInfos, object[] narrowedArgs)
		{
			try
			{
				for (int i = 0; i < args.Length; i++)
				{
					if (args[i] == null)
					{
						continue;
					}
					Type paramType = GetParamType(paramInfos[i]);
					if (paramType.IsEnum)
					{
						narrowedArgs[i] = Enum.ToObject(paramType, narrowedArgs[i]);
					}
					else if (Utils.IsTrulyPrimitive(paramType) && !Utils.IsOutOnlyParam(paramInfos[i]) && args[i] != Missing.Value)
					{
						switch (Type.GetTypeCode(paramType))
						{
						case TypeCode.Byte:
							narrowedArgs[i] = Convert.ToByte(narrowedArgs[i]);
							break;
						case TypeCode.SByte:
							narrowedArgs[i] = Convert.ToSByte(narrowedArgs[i]);
							break;
						case TypeCode.Char:
							narrowedArgs[i] = Convert.ToChar(narrowedArgs[i]);
							break;
						case TypeCode.UInt16:
							narrowedArgs[i] = Convert.ToUInt16(narrowedArgs[i]);
							break;
						case TypeCode.Int16:
							narrowedArgs[i] = Convert.ToInt16(narrowedArgs[i]);
							break;
						case TypeCode.Int32:
							narrowedArgs[i] = Convert.ToInt32(narrowedArgs[i]);
							break;
						case TypeCode.UInt32:
							narrowedArgs[i] = Convert.ToUInt32(narrowedArgs[i]);
							break;
						case TypeCode.Int64:
							narrowedArgs[i] = Convert.ToInt64(narrowedArgs[i]);
							break;
						case TypeCode.UInt64:
							narrowedArgs[i] = Convert.ToUInt64(narrowedArgs[i]);
							break;
						case TypeCode.Single:
							narrowedArgs[i] = Convert.ToSingle(narrowedArgs[i]);
							break;
						case TypeCode.Double:
							narrowedArgs[i] = Convert.ToDouble(narrowedArgs[i]);
							break;
						}
					}
				}
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		private bool isWideningConversion(Type from, ParameterInfo pi)
		{
			Type paramType = GetParamType(pi);
			if (from == paramType)
			{
				return true;
			}
			if (Utils.IsTrulyPrimitive(from) && paramType == typeof(object))
			{
				return true;
			}
			if (paramType.IsByRef && paramType.GetElementType() == from)
			{
				return true;
			}
			if ((from == typeof(Pointer) || from == typeof(IntPtr)) && pi.ParameterType.IsPointer)
			{
				return true;
			}
			if (!Utils.IsTrulyPrimitive(from))
			{
				return paramType.IsAssignableFrom(from);
			}
			switch (Type.GetTypeCode(from))
			{
			case TypeCode.SByte:
			case TypeCode.Byte:
				if (paramType != typeof(short) && paramType != typeof(ushort) && paramType != typeof(int) && paramType != typeof(uint) && paramType != typeof(long) && paramType != typeof(ulong) && paramType != typeof(float) && paramType != typeof(double))
				{
					return paramType == typeof(decimal);
				}
				return true;
			case TypeCode.Char:
			case TypeCode.Int16:
			case TypeCode.UInt16:
				if (paramType != typeof(int) && paramType != typeof(uint) && paramType != typeof(long) && paramType != typeof(ulong) && paramType != typeof(float) && paramType != typeof(double))
				{
					return paramType == typeof(decimal);
				}
				return true;
			case TypeCode.Int32:
			case TypeCode.UInt32:
				if (paramType != typeof(long) && paramType != typeof(ulong) && paramType != typeof(float) && paramType != typeof(double))
				{
					return paramType == typeof(decimal);
				}
				return true;
			case TypeCode.Int64:
			case TypeCode.UInt64:
				if (paramType != typeof(float) && paramType != typeof(double))
				{
					return paramType == typeof(decimal);
				}
				return true;
			case TypeCode.Single:
				if (paramType != typeof(double))
				{
					return paramType == typeof(decimal);
				}
				return true;
			case TypeCode.Double:
				return paramType == typeof(decimal);
			default:
				return paramType.IsAssignableFrom(from);
			}
		}

		private static void cullOutIncompatibleArrayMethods(ArrayList methodRecs, int argPos, Type leafTypeToKeep)
		{
			for (int num = methodRecs.Count - 1; num >= 0; num--)
			{
				MethodRec methodRec = (MethodRec)methodRecs[num];
				ParameterInfo parameterInfo = methodRec.pia[argPos];
				Type type = (parameterInfo.ParameterType.IsPointer ? parameterInfo.ParameterType : GetParamType(parameterInfo));
				if (type != typeof(Array) && !Utils.IsOutOnlyParam(parameterInfo))
				{
					Type elementType = type.GetElementType();
					bool flag = false;
					if ((!Utils.IsTrulyPrimitive(elementType)) ? (!elementType.IsAssignableFrom(leafTypeToKeep)) : (elementType != leafTypeToKeep))
					{
						methodRecs.RemoveAt(num);
					}
				}
			}
		}

		private ExpressionType getLeafExprType(IKernelLink ml, out Type leafObjectType)
		{
			ILinkMark mark = ml.CreateMark();
			try
			{
				ExpressionType leafExprType = getLeafExprType0(ml, out leafObjectType);
				ml.SeekMark(mark);
				return leafExprType;
			}
			finally
			{
				ml.DestroyMark(mark);
			}
		}

		private ExpressionType getLeafExprType0(IKernelLink ml, out Type leafObjectType)
		{
			leafObjectType = null;
			bool flag = false;
			ExpressionType expressionType = ExpressionType.Function;
			int argCount;
			string function = ml.GetFunction(out argCount);
			if (function == "List")
			{
				for (int i = 0; i < argCount; i++)
				{
					expressionType = ml.GetNextExpressionType();
					switch (expressionType)
					{
					case ExpressionType.Function:
					{
						ExpressionType leafExprType = getLeafExprType0(ml, out leafObjectType);
						if (leafExprType != ExpressionType.Function && (leafExprType != ExpressionType.Object || leafObjectType != null))
						{
							return leafExprType;
						}
						break;
					}
					case ExpressionType.Object:
					{
						object @object = ml.GetObject();
						if (@object == null)
						{
							flag = true;
							break;
						}
						leafObjectType = @object.GetType();
						return expressionType;
					}
					default:
						return expressionType;
					}
				}
			}
			if (expressionType != ExpressionType.Function || !flag)
			{
				return expressionType;
			}
			return ExpressionType.Object;
		}

		private static Type GetParamType(ParameterInfo pi)
		{
			Type parameterType = pi.ParameterType;
			if (!parameterType.IsByRef && !parameterType.IsPointer)
			{
				return parameterType;
			}
			return parameterType.GetElementType();
		}

		private static ulong keyFromMmaSymbol(string sym)
		{
			if (!sym.StartsWith("NETLink`Objects`NETObject$"))
			{
				return 0uL;
			}
			string text = sym.Substring(objectSymbolPrefixLength);
			int num = text.IndexOf('$');
			if (num > 0)
			{
				text = text.Substring(num + 1);
			}
			return ulong.Parse(text);
		}

		private static string mmaSymbolFromKey(ulong key, string typeAlias)
		{
			if (typeAlias == "")
			{
				return "NETLink`Objects`NETObject$" + key;
			}
			return "NETLink`Objects`NETObject$" + (uint)typeAlias.GetHashCode() + "$" + key;
		}
	}
}
