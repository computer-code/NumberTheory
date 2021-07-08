using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Wolfram.NETLink.Internal.COM;

namespace Wolfram.NETLink.Internal
{
	internal class CallPacketHandler
	{
		private IMathLink feServerLink;

		private ObjectHandler objectHandler;

		public IMathLink FEServerLink
		{
			get
			{
				return feServerLink;
			}
			set
			{
				feServerLink = value;
			}
		}

		internal CallPacketHandler(ObjectHandler objectHandler)
		{
			this.objectHandler = objectHandler;
		}

		internal void handleCallPacket(KernelLinkImpl ml)
		{
			int num = 0;
			try
			{
				num = ml.GetInteger();
				ml.CheckFunction("List");
			}
			catch (MathLinkException e)
			{
				handleCleanException(ml, e);
				return;
			}
			if (num != 70)
			{
				ml.LastExceptionDuringCallPacketHandling = null;
			}
			try
			{
				StdLink.setup(ml);
				ml.WasInterrupted = false;
				switch (num)
				{
				case 1:
					call(ml);
					break;
				case 5:
					loadAssembly(ml);
					break;
				case 6:
					loadAssemblyFromDir(ml);
					break;
				case 2:
					loadType1(ml);
					break;
				case 3:
					loadType2(ml);
					break;
				case 4:
					loadExistingType(ml);
					break;
				case 7:
					getAssemblyObject(ml);
					break;
				case 8:
					getTypeObject(ml);
					break;
				case 9:
					releaseInstance(ml);
					break;
				case 10:
					makeObject(ml);
					break;
				case 12:
					val(ml);
					break;
				case 15:
					setComplex(ml);
					break;
				case 16:
					sameObjectQ(ml);
					break;
				case 17:
					instanceOf(ml);
					break;
				case 18:
					cast(ml);
					break;
				case 20:
					peekTypes(ml);
					break;
				case 21:
					peekObjects(ml);
					break;
				case 22:
					peekAssemblies(ml);
					break;
				case 13:
					reflectType(ml);
					break;
				case 14:
					reflectAssembly(ml);
					break;
				case 11:
					createDelegate(ml);
					break;
				case 50:
					defineDelegate(ml);
					break;
				case 51:
					delegateTypeName(ml);
					break;
				case 52:
					addEventHandler(ml);
					break;
				case 53:
					removeEventHandler(ml);
					break;
				case 60:
					createDLL1(ml);
					break;
				case 61:
					createDLL2(ml);
					break;
				case 31:
					doModal(ml);
					break;
				case 32:
					showForm(ml);
					break;
				case 33:
					doShareKernel(ml);
					break;
				case 34:
					allowUIComps(ml);
					break;
				case 35:
					uiLink(ml);
					break;
				case 40:
					isCOMProp(ml);
					break;
				case 41:
					createCOM(ml);
					break;
				case 42:
					getActiveCOM(ml);
					break;
				case 43:
					releaseCOM(ml);
					break;
				case 44:
					loadTypeLibrary(ml);
					break;
				case 70:
					getException(ml);
					break;
				case 80:
					connectToFEServer(ml);
					break;
				case 81:
					disconnectToFEServer(ml);
					break;
				case 90:
					ml.Put(43);
					break;
				case 91:
					noop2(ml);
					break;
				}
			}
			catch (Exception ex)
			{
				Exception ex3 = (ml.LastExceptionDuringCallPacketHandling = ex);
			}
			finally
			{
				StdLink.remove();
				ml.ClearError();
				ml.NewPacket();
				ml.EndPacket();
				ml.Flush();
			}
		}

		private void loadAssembly(KernelLinkImpl ml)
		{
			Assembly assembly = null;
			bool flag = false;
			try
			{
				string @string = ml.GetString();
				flag = ml.GetBoolean();
				ml.NewPacket();
				assembly = TypeLoader.LoadAssembly(@string);
			}
			catch (Exception ex)
			{
				if ((!(ex is BadImageFormatException) && !(ex is FileLoadException)) || !flag)
				{
					handleCleanException(ml, ex);
					return;
				}
			}
			if (assembly != null)
			{
				ml.PutFunction("List", 2);
				ml.Put(assembly.GetName().Name);
				ml.Put(assembly.FullName);
			}
			else
			{
				ml.PutSymbol("$Failed");
			}
		}

		private void loadAssemblyFromDir(KernelLinkImpl ml)
		{
			Assembly assembly = null;
			try
			{
				string @string = ml.GetString();
				string string2 = ml.GetString();
				ml.NewPacket();
				assembly = TypeLoader.LoadAssembly(@string, string2);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			if (assembly != null)
			{
				ml.PutFunction("List", 2);
				ml.Put(assembly.GetName().Name);
				ml.Put(assembly.FullName);
			}
			else
			{
				ml.PutSymbol("$Failed");
			}
		}

		private void loadType1(KernelLinkImpl ml)
		{
			try
			{
				string @string = ml.GetString();
				string string2 = ml.GetString();
				ml.NewPacket();
				Type type = TypeLoader.GetType(@string, string2, throwOnError: true);
				objectHandler.loadType(ml, type);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
			}
		}

		private void loadType2(KernelLinkImpl ml)
		{
			try
			{
				string @string = ml.GetString();
				Assembly assembly = (Assembly)ml.GetObject();
				ml.NewPacket();
				Type type = TypeLoader.GetType(@string, assembly, throwOnError: true);
				objectHandler.loadType(ml, type);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
			}
		}

		private void loadExistingType(KernelLinkImpl ml)
		{
			try
			{
				Type t = (Type)ml.GetObject();
				ml.NewPacket();
				objectHandler.loadType(ml, t);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
			}
		}

		private void call(KernelLinkImpl ml)
		{
			object obj = null;
			string @string;
			string symbol;
			int integer;
			bool boolean;
			string string2;
			int[] array;
			try
			{
				@string = ml.GetString();
				symbol = ml.GetSymbol();
				integer = ml.GetInteger();
				boolean = ml.GetBoolean();
				string2 = ml.GetString();
				int integer2 = ml.GetInteger();
				array = new int[integer2];
				for (int i = 0; i < integer2; i++)
				{
					array[i] = ml.GetInteger();
				}
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			bool isManual = ml.IsManual;
			try
			{
				ml.IsManual = false;
				obj = objectHandler.call(ml, @string, symbol, integer, string2, array, out var outParams);
				ml.NewPacket();
				if (ml.IsManual)
				{
					ml.EndPacket();
					sendOutParams(ml, outParams);
					ml.PutSymbol("Null");
				}
				else if (ml.WasInterrupted)
				{
					ml.PutFunction("Abort", 0);
				}
				else if (boolean)
				{
					sendOutParams(ml, outParams);
					ml.PutReference(obj);
				}
				else
				{
					sendOutParams(ml, outParams);
					ml.Put(obj);
				}
			}
			catch (Exception ex)
			{
				if (ml.IsManual)
				{
					ml.LastExceptionDuringCallPacketHandling = ex;
					ml.ClearError();
					ml.EndPacket();
					ml.Flush();
					ml.PutFunction("NETLink`Package`manualException", 1);
					if (ex is CallNETException)
					{
						((CallNETException)ex).writeToLink(ml);
					}
					else
					{
						ml.Put(ex.ToString());
					}
				}
				else
				{
					handleCleanException(ml, ex);
				}
			}
			finally
			{
				ml.IsManual = isManual;
			}
		}

		private void getAssemblyObject(KernelLinkImpl ml)
		{
			Assembly assembly = null;
			try
			{
				string @string = ml.GetString();
				ml.NewPacket();
				assembly = TypeLoader.LoadAssembly(@string);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(assembly);
		}

		private void getTypeObject(KernelLinkImpl ml)
		{
			Type type = null;
			try
			{
				string @string = ml.GetString();
				ml.NewPacket();
				type = TypeLoader.GetType(@string, throwOnError: true);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(type);
		}

		private void releaseInstance(KernelLinkImpl ml)
		{
			try
			{
				string[] stringArray = ml.GetStringArray();
				ml.NewPacket();
				objectHandler.releaseInstance(stringArray);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutSymbol("Null");
		}

		private void makeObject(KernelLinkImpl ml)
		{
			object obj = null;
			try
			{
				string @string = ml.GetString();
				int integer = ml.GetInteger();
				Type type = null;
				type = ((@string.IndexOf('.') != -1) ? TypeLoader.GetType(@string, throwOnError: true) : TypeLoader.GetType("System." + @string, throwOnError: true));
				try
				{
					obj = Utils.readArgAs(ml, integer, type);
				}
				catch (Exception)
				{
					ml.ClearError();
					throw new ArgumentException("Expression cannot be read as the requested type.");
				}
				ml.NewPacket();
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(obj);
		}

		private void createDelegate(KernelLinkImpl ml)
		{
			object obj;
			try
			{
				string @string = ml.GetString();
				string string2 = ml.GetString();
				int integer = ml.GetInteger();
				bool boolean = ml.GetBoolean();
				bool boolean2 = ml.GetBoolean();
				ml.NewPacket();
				Type type = TypeLoader.GetType(@string, throwOnError: true);
				obj = Delegate.CreateDelegate(type, DelegateHelper.createDynamicMethod(null, type, string2, integer, boolean, boolean2));
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(obj);
		}

		private void val(KernelLinkImpl ml)
		{
			object obj;
			try
			{
				object @object = ml.GetObject();
				ml.NewPacket();
				if (@object.GetType().IsEnum)
				{
					obj = Convert.ChangeType(@object, Enum.GetUnderlyingType(@object.GetType()));
				}
				else if (@object is ICollection && !(@object is Array))
				{
					object[] array = new object[((ICollection)@object).Count];
					((ICollection)@object).CopyTo(array, 0);
					obj = array;
				}
				else
				{
					obj = @object;
				}
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(obj);
		}

		private void setComplex(KernelLinkImpl ml)
		{
			bool b = true;
			try
			{
				string @string = ml.GetString();
				ml.NewPacket();
				Type type2 = (ml.ComplexType = TypeLoader.GetType(@string, throwOnError: true));
			}
			catch (ArgumentException)
			{
				b = false;
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(b);
		}

		private void sameObjectQ(KernelLinkImpl ml)
		{
			bool b;
			try
			{
				object @object = ml.GetObject();
				object object2 = ml.GetObject();
				ml.NewPacket();
				b = object.ReferenceEquals(@object, object2);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(b);
		}

		private void instanceOf(KernelLinkImpl ml)
		{
			bool b;
			try
			{
				object @object = ml.GetObject();
				string @string = ml.GetString();
				ml.NewPacket();
				Type type = TypeLoader.GetType(@string, throwOnError: true);
				b = type.IsInstanceOfType(@object);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(b);
		}

		private void cast(KernelLinkImpl ml)
		{
			object obj;
			Type type;
			try
			{
				obj = ml.GetObject();
				string @string = ml.GetString();
				ml.NewPacket();
				type = TypeLoader.GetType(@string, throwOnError: true);
				if (!Utils.IsMono && Marshal.IsComObject(obj))
				{
					obj = COMUtilities.Cast(obj, type);
					type = null;
				}
				else if (!type.IsInstanceOfType(obj))
				{
					throw new InvalidCastException();
				}
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(obj, type);
		}

		private void peekTypes(KernelLinkImpl ml)
		{
			objectHandler.peekTypes(ml);
		}

		private void peekObjects(KernelLinkImpl ml)
		{
			objectHandler.peekObjects(ml);
		}

		private void peekAssemblies(KernelLinkImpl ml)
		{
			objectHandler.peekAssemblies(ml);
		}

		private void reflectType(KernelLinkImpl ml)
		{
			ILoopbackLink loopbackLink = MathLinkFactory.CreateLoopbackLink();
			try
			{
				string @string = ml.GetString();
				ml.NewPacket();
				Type type = objectHandler.getType(@string);
				loopbackLink.PutFunction("List", 6);
				loopbackLink.PutFunction("List", 10);
				string text = ObjectHandler.fullTypeNameForMathematica(type);
				string obj = ((type.IsGenericType && text.IndexOf(type.Name) > 0) ? text.Substring(text.IndexOf(type.Name)) : type.Name);
				loopbackLink.Put(obj);
				loopbackLink.Put(text);
				ArrayList arrayList = new ArrayList();
				for (Type baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
				{
					arrayList.Add(ObjectHandler.fullTypeNameForMathematica(baseType));
				}
				loopbackLink.Put(arrayList.ToArray());
				Type[] interfaces = type.GetInterfaces();
				loopbackLink.PutFunction("List", interfaces.Length);
				Type[] array = interfaces;
				foreach (Type t in array)
				{
					loopbackLink.Put(ObjectHandler.fullTypeNameForMathematica(t));
				}
				loopbackLink.Put(type.IsValueType);
				loopbackLink.Put(type.IsEnum);
				loopbackLink.Put(type.IsSubclassOf(typeof(Delegate)));
				loopbackLink.Put(type.IsInterface);
				loopbackLink.Put(type.AssemblyQualifiedName);
				try
				{
					loopbackLink.Put(type.Assembly.Location);
				}
				catch (Exception)
				{
					loopbackLink.Put("");
				}
				ConstructorInfo[] constructors = objectHandler.getConstructors(@string);
				loopbackLink.PutFunction("List", constructors.Length);
				ConstructorInfo[] array2 = constructors;
				foreach (ConstructorInfo constructorInfo in array2)
				{
					putParameterInfo(loopbackLink, constructorInfo.GetParameters());
				}
				FieldInfo[] fields = objectHandler.getFields(@string);
				loopbackLink.PutFunction("List", fields.Length);
				FieldInfo[] array3 = fields;
				foreach (FieldInfo fieldInfo in array3)
				{
					if (type.IsEnum && fieldInfo.IsSpecialName)
					{
						loopbackLink.PutFunction("Sequence", 0);
						continue;
					}
					loopbackLink.PutFunction("List", 6);
					loopbackLink.Put(fieldInfo.DeclaringType != type);
					loopbackLink.Put(fieldInfo.IsStatic);
					loopbackLink.Put(fieldInfo.IsLiteral);
					loopbackLink.Put(fieldInfo.IsInitOnly);
					loopbackLink.Put(ObjectHandler.fullTypeNameForMathematica(fieldInfo.FieldType));
					loopbackLink.Put(fieldInfo.Name);
				}
				PropertyInfo[] properties = objectHandler.getProperties(@string);
				loopbackLink.PutFunction("List", properties.Length);
				PropertyInfo[] array4 = properties;
				foreach (PropertyInfo propertyInfo in array4)
				{
					loopbackLink.PutFunction("List", 10);
					bool b = false;
					bool b2 = false;
					bool b3 = false;
					bool b4 = false;
					bool b5 = false;
					bool b6 = false;
					MethodInfo getMethod = propertyInfo.GetGetMethod();
					MethodInfo setMethod = propertyInfo.GetSetMethod();
					if (getMethod != null)
					{
						b5 = true;
						if (getMethod.IsStatic)
						{
							b = true;
						}
						if (getMethod.IsVirtual && !getMethod.IsFinal)
						{
							b2 = true;
						}
						if (getMethod.IsVirtual && getMethod.DeclaringType == type && getMethod.GetBaseDefinition().DeclaringType != type && !getMethod.GetBaseDefinition().DeclaringType.IsInterface)
						{
							b3 = true;
						}
						if (getMethod.IsAbstract)
						{
							b4 = true;
						}
					}
					if (setMethod != null)
					{
						b6 = true;
						if (setMethod.IsStatic)
						{
							b = true;
						}
						if (setMethod.IsVirtual && !setMethod.IsFinal)
						{
							b2 = true;
						}
						if (setMethod.IsVirtual && setMethod.DeclaringType == type && setMethod.GetBaseDefinition().DeclaringType != type && !setMethod.GetBaseDefinition().DeclaringType.IsInterface)
						{
							b3 = true;
						}
						if (setMethod.IsAbstract)
						{
							b4 = true;
						}
					}
					loopbackLink.Put(propertyInfo.DeclaringType != type);
					loopbackLink.Put(b);
					loopbackLink.Put(b2);
					loopbackLink.Put(b3);
					loopbackLink.Put(b4);
					loopbackLink.Put(b5);
					loopbackLink.Put(b6);
					loopbackLink.Put(ObjectHandler.fullTypeNameForMathematica(propertyInfo.PropertyType));
					loopbackLink.Put(propertyInfo.Name);
					ParameterInfo[] array5 = null;
					bool flag = false;
					if (getMethod != null)
					{
						array5 = getMethod.GetParameters();
					}
					else if (setMethod != null)
					{
						array5 = setMethod.GetParameters();
						flag = true;
					}
					if (array5 == null || (flag && array5.Length == 1))
					{
						loopbackLink.PutFunction("List", 0);
						continue;
					}
					if (flag)
					{
						ParameterInfo[] array6 = new ParameterInfo[array5.Length - 1];
						Array.Copy(array5, 0, array6, 0, array6.Length);
						array5 = array6;
					}
					putParameterInfo(loopbackLink, array5);
				}
				MethodInfo[] methods = objectHandler.getMethods(@string);
				loopbackLink.PutFunction("List", methods.Length);
				MethodInfo[] array7 = methods;
				foreach (MethodInfo methodInfo in array7)
				{
					loopbackLink.PutFunction("List", 8);
					loopbackLink.Put(methodInfo.DeclaringType != type);
					loopbackLink.Put(methodInfo.IsStatic);
					loopbackLink.Put(methodInfo.IsVirtual && !methodInfo.IsFinal);
					loopbackLink.Put(methodInfo.IsVirtual && methodInfo.DeclaringType == type && methodInfo.GetBaseDefinition().DeclaringType != type && !methodInfo.GetBaseDefinition().DeclaringType.IsInterface);
					loopbackLink.Put(methodInfo.IsAbstract);
					loopbackLink.Put(ObjectHandler.fullTypeNameForMathematica(methodInfo.ReturnType));
					loopbackLink.Put(methodInfo.Name);
					putParameterInfo(loopbackLink, methodInfo.GetParameters());
				}
				EventInfo[] events = objectHandler.getEvents(@string);
				loopbackLink.PutFunction("List", events.Length);
				EventInfo[] array8 = events;
				foreach (EventInfo eventInfo in array8)
				{
					loopbackLink.PutFunction("List", 9);
					MethodInfo addMethod = eventInfo.GetAddMethod();
					loopbackLink.Put(eventInfo.DeclaringType != type);
					loopbackLink.Put(addMethod.IsStatic);
					loopbackLink.Put(addMethod.IsVirtual && !addMethod.IsFinal);
					loopbackLink.Put(addMethod.IsVirtual && addMethod.DeclaringType == type && addMethod.GetBaseDefinition().DeclaringType != type && !addMethod.GetBaseDefinition().DeclaringType.IsInterface);
					loopbackLink.Put(addMethod.IsAbstract);
					loopbackLink.Put(ObjectHandler.fullTypeNameForMathematica(eventInfo.EventHandlerType));
					loopbackLink.Put(eventInfo.Name);
					MethodInfo method = eventInfo.EventHandlerType.GetMethod("Invoke");
					loopbackLink.Put(ObjectHandler.fullTypeNameForMathematica(method.ReturnType));
					putParameterInfo(loopbackLink, method.GetParameters());
				}
				ml.TransferExpression(loopbackLink);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
			}
			finally
			{
				loopbackLink.Close();
			}
		}

		private void reflectAssembly(KernelLinkImpl ml)
		{
			ILoopbackLink loopbackLink = MathLinkFactory.CreateLoopbackLink();
			try
			{
				string @string = ml.GetString();
				ml.NewPacket();
				Assembly assembly = TypeLoader.LoadAssembly(@string);
				Type[] array;
				try
				{
					array = assembly.GetExportedTypes();
				}
				catch (NotSupportedException)
				{
					Type[] types = assembly.GetTypes();
					ArrayList arrayList = new ArrayList(types.Length);
					Type[] array2 = types;
					foreach (Type type in array2)
					{
						if (type.IsPublic)
						{
							arrayList.Add(type);
						}
					}
					array = (Type[])arrayList.ToArray(typeof(Type));
				}
				loopbackLink.PutFunction("List", array.Length + 3);
				loopbackLink.Put(assembly.GetName().Name);
				loopbackLink.Put(assembly.FullName);
				try
				{
					loopbackLink.Put(assembly.Location);
				}
				catch (Exception)
				{
					loopbackLink.Put("");
				}
				Type[] array3 = array;
				foreach (Type type2 in array3)
				{
					loopbackLink.PutFunction("List", 6);
					loopbackLink.Put(ObjectHandler.fullTypeNameForMathematica(type2));
					loopbackLink.Put((type2.Namespace != null) ? type2.Namespace : "");
					loopbackLink.Put(type2.IsValueType);
					loopbackLink.Put(type2.IsEnum);
					loopbackLink.Put(type2.IsSubclassOf(typeof(Delegate)));
					loopbackLink.Put(type2.IsInterface);
				}
				ml.TransferExpression(loopbackLink);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
			}
			finally
			{
				loopbackLink.Close();
			}
		}

		private void defineDelegate(KernelLinkImpl ml)
		{
			string obj;
			try
			{
				string @string = ml.GetString();
				string string2 = ml.GetString();
				string[] stringArray = ml.GetStringArray();
				ml.NewPacket();
				obj = DelegateHelper.defineDelegate(@string, string2, stringArray);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(obj);
		}

		private void delegateTypeName(KernelLinkImpl ml)
		{
			string delegateTypeName;
			try
			{
				object @object = ml.GetObject();
				string @string = ml.GetString();
				string string2 = ml.GetString();
				ml.NewPacket();
				delegateTypeName = EventHelper.getDelegateTypeName(@object, @string, string2);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(delegateTypeName);
		}

		private void addEventHandler(KernelLinkImpl ml)
		{
			Delegate dlg;
			try
			{
				object @object = ml.GetObject();
				string @string = ml.GetString();
				string string2 = ml.GetString();
				dlg = (Delegate)ml.GetObject();
				ml.NewPacket();
				dlg = EventHelper.addHandler(@object, @string, string2, dlg);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(dlg);
		}

		private void removeEventHandler(KernelLinkImpl ml)
		{
			try
			{
				object @object = ml.GetObject();
				string @string = ml.GetString();
				string string2 = ml.GetString();
				Delegate dlg = (Delegate)ml.GetObject();
				ml.NewPacket();
				EventHelper.removeHandler(@object, @string, string2, dlg);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutSymbol("Null");
		}

		private void createDLL1(KernelLinkImpl ml)
		{
			string obj;
			try
			{
				string @string = ml.GetString();
				string string2 = ml.GetString();
				string string3 = ml.GetString();
				string string4 = ml.GetString();
				string[] stringArray = ml.GetStringArray();
				bool[] booleanArray = ml.GetBooleanArray();
				string string5 = ml.GetString();
				ml.NewPacket();
				obj = DLLHelper.CreateDLLCall(@string, string2, string3, string4, stringArray, booleanArray, string5);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(obj);
		}

		private void createDLL2(KernelLinkImpl ml)
		{
			string[] obj;
			try
			{
				string @string = ml.GetString();
				string[] stringArray = ml.GetStringArray();
				string string2 = ml.GetString();
				ml.NewPacket();
				obj = DLLHelper.CreateDLLCall(@string, stringArray, string2);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(obj);
		}

		private void doModal(KernelLinkImpl ml)
		{
			try
			{
				bool boolean = ml.GetBoolean();
				Form form = ml.GetObject() as Form;
				ml.NewPacket();
				Reader.isInModalState = boolean;
				if (boolean && form != null)
				{
					activateWindow(form);
				}
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutSymbol("Null");
		}

		private void showForm(KernelLinkImpl ml)
		{
			try
			{
				Form form = ml.GetObject() as Form;
				ml.NewPacket();
				activateWindow(form);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutSymbol("Null");
		}

		private void doShareKernel(KernelLinkImpl ml)
		{
			bool boolean;
			try
			{
				boolean = ml.GetBoolean();
				ml.NewPacket();
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			Reader.shareKernel(boolean);
			ml.PutSymbol("Null");
		}

		private void allowUIComps(KernelLinkImpl ml)
		{
			bool boolean;
			try
			{
				boolean = ml.GetBoolean();
				ml.NewPacket();
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			Reader.allowUIComputations = boolean;
			ml.PutSymbol("Null");
		}

		private void uiLink(KernelLinkImpl ml)
		{
			string @string;
			string string2;
			try
			{
				@string = ml.GetString();
				string2 = ml.GetString();
				ml.NewPacket();
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			bool b = true;
			IKernelLink kernelLink = null;
			try
			{
				kernelLink = (StdLink.UILink = MathLinkFactory.CreateKernelLink("-linkname " + @string + " -linkconnect -linkprotocol " + string2));
				((KernelLinkImpl)kernelLink).copyStateFrom(ml);
			}
			catch (Exception)
			{
				kernelLink?.Close();
				b = false;
			}
			ml.Put(b);
			ml.Flush();
			kernelLink.Connect();
		}

		private void connectToFEServer(KernelLinkImpl ml)
		{
			bool b = false;
			try
			{
				string @string = ml.GetString();
				ml.NewPacket();
				string cmdLine = "-linkmode connect -linkname " + @string;
				FEServerLink = MathLinkFactory.CreateMathLink(cmdLine);
				if (FEServerLink != null)
				{
					try
					{
						FEServerLink.Connect();
						FEServerLink.PutFunction("InputNamePacket", 1);
						FEServerLink.Put("In[1]:=");
						FEServerLink.Flush();
						while (true)
						{
							int argCount;
							string function = FEServerLink.GetFunction(out argCount);
							FEServerLink.NewPacket();
							switch (function)
							{
							case "EnterTextPacket":
							case "EnterExpressionPacket":
								b = true;
								goto end_IL_0032;
							case "EvaluatePacket":
								goto IL_00b1;
							}
							continue;
							IL_00b1:
							FEServerLink.PutFunction("ReturnPacket", 1);
							FEServerLink.PutSymbol("Null");
						}
						end_IL_0032:;
					}
					catch (MathLinkException)
					{
						FEServerLink.Close();
						FEServerLink = null;
					}
				}
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutFunction("ReturnPacket", 1);
			ml.Put(b);
			ml.EndPacket();
		}

		private void disconnectToFEServer(KernelLinkImpl ml)
		{
			FEServerLink.Close();
			FEServerLink = null;
			ml.PutFunction("ReturnPacket", 1);
			ml.PutSymbol("Null");
			ml.EndPacket();
		}

		private void isCOMProp(KernelLinkImpl ml)
		{
			bool b;
			try
			{
				object @object = ml.GetObject();
				string @string = ml.GetString();
				ml.NewPacket();
				b = COMUtilities.IsCOMProp(@object, @string);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(b);
		}

		private void createCOM(KernelLinkImpl ml)
		{
			object obj;
			try
			{
				string @string = ml.GetString();
				ml.NewPacket();
				obj = COMUtilities.createCOMObject(@string);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(obj);
		}

		private void getActiveCOM(KernelLinkImpl ml)
		{
			object activeCOMObject;
			try
			{
				string @string = ml.GetString();
				ml.NewPacket();
				activeCOMObject = COMUtilities.getActiveCOMObject(@string);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(activeCOMObject);
		}

		private void releaseCOM(KernelLinkImpl ml)
		{
			int i;
			try
			{
				object @object = ml.GetObject();
				ml.NewPacket();
				i = COMUtilities.releaseCOMObject(@object);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(i);
		}

		private void loadTypeLibrary(KernelLinkImpl ml)
		{
			Assembly obj;
			bool foundPIAInstead;
			try
			{
				string @string = ml.GetString();
				bool boolean = ml.GetBoolean();
				string string2 = ml.GetString();
				ml.NewPacket();
				obj = COMTypeLibraryLoader.loadTypeLibrary(@string, boolean, string2, out foundPIAInstead);
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutFunction("List", 2);
			ml.PutReference(obj);
			ml.Put(foundPIAInstead);
		}

		private void getException(KernelLinkImpl ml)
		{
			try
			{
				ml.NewPacket();
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.PutReference(ml.LastExceptionDuringCallPacketHandling);
		}

		private void noop2(KernelLinkImpl ml)
		{
			Console.Error.WriteLine("in noop2, ");
			int num = 0;
			try
			{
				num = ml.GetInteger();
				for (int i = 0; i < num; i++)
				{
					Utils.discardNext(ml);
				}
				ml.NewPacket();
			}
			catch (Exception e)
			{
				handleCleanException(ml, e);
				return;
			}
			ml.Put(num);
		}

		private void handleCleanException(KernelLinkImpl ml, Exception e)
		{
			ml.LastExceptionDuringCallPacketHandling = e;
			ml.ClearError();
			ml.NewPacket();
			if (ml.WasInterrupted)
			{
				ml.PutFunction("Abort", 0);
			}
			else
			{
				ml.PutFunction("NETLink`Package`handleException", 1);
				if (e is CallNETException)
				{
					((CallNETException)e).writeToLink(ml);
				}
				else
				{
					string text = e.ToString().Replace("\r\n", "\n");
					int num = text.IndexOf("Fusion log");
					if (num > 0)
					{
						text = text.Substring(0, num - 1);
					}
					int num2 = text.IndexOf("=== Pre-bind state information");
					if (num2 > 0)
					{
						text = text.Substring(0, num2 - 1);
					}
					int num3 = text.IndexOf("--- End of inner exception stack trace");
					if (num3 > 0)
					{
						text = text.Substring(0, num3 - 1);
					}
					ml.Put(text);
				}
			}
			ml.EndPacket();
		}

		private void sendOutParams(IKernelLink ml, OutParamRecord[] outParams)
		{
			if (outParams == null)
			{
				return;
			}
			foreach (OutParamRecord outParamRecord in outParams)
			{
				if (outParamRecord != null)
				{
					ml.PutFunction("EvaluatePacket", 1);
					ml.PutFunction("NETLink`Package`outParam", 2);
					ml.Put(outParamRecord.argPosition + 1);
					ml.Put(outParamRecord.val);
					ml.WaitAndDiscardAnswer();
				}
			}
		}

		private static void putParameterInfo(IMathLink ml, ParameterInfo[] pis)
		{
			ml.PutFunction("List", pis.Length);
			foreach (ParameterInfo parameterInfo in pis)
			{
				ml.PutFunction("List", 6);
				ml.Put(parameterInfo.IsOptional);
				if (parameterInfo.DefaultValue == DBNull.Value)
				{
					ml.PutSymbol("Default");
				}
				else if (parameterInfo.DefaultValue != null)
				{
					ml.Put(parameterInfo.DefaultValue.ToString());
				}
				else
				{
					ml.Put("null");
				}
				ml.Put(Utils.IsOutOnlyParam(parameterInfo));
				Type parameterType = parameterInfo.ParameterType;
				ml.Put(parameterType.IsByRef);
				ml.Put(ObjectHandler.fullTypeNameForMathematica(parameterType));
				ml.Put((parameterInfo.Name != null) ? parameterInfo.Name : "noname");
			}
		}

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		private static extern int GetWindowThreadProcessId(IntPtr hwnd, IntPtr procIDPtr);

		[DllImport("kernel32.dll")]
		private static extern int GetCurrentThreadId();

		[DllImport("user32.dll")]
		private static extern bool AttachThreadInput(int idAttachedThread, int idAttachingThread, bool attachOrDetach);

		private void activateWindow(Form form)
		{
			int num = 0;
			int num2 = 0;
			if (Utils.IsWindows)
			{
				num = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
				num2 = GetCurrentThreadId();
				if (num != num2)
				{
					AttachThreadInput(num, num2, attachOrDetach: true);
				}
			}
			form.Show();
			form.Activate();
			if (Utils.IsWindows && num != num2)
			{
				AttachThreadInput(num, num2, attachOrDetach: false);
			}
			if (form.WindowState == FormWindowState.Minimized)
			{
				form.WindowState = FormWindowState.Normal;
			}
		}
	}
}
