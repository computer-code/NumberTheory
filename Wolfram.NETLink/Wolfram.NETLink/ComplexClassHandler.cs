using System;
using System.Reflection;

namespace Wolfram.NETLink
{
	public class ComplexClassHandler
	{
		private Type complexClass;

		protected ConstructorInfo complexCtor;

		protected MethodInfo complexCreateMethod;

		protected MethodInfo complexReMethod;

		protected MethodInfo complexImMethod;

		protected FieldInfo complexReField;

		protected FieldInfo complexImField;

		protected bool ctorUsesFloat;

		public Type ComplexType
		{
			get
			{
				return complexClass;
			}
			set
			{
				ctorUsesFloat = false;
				ConstructorInfo constructorInfo = null;
				MethodInfo methodInfo = null;
				MethodInfo methodInfo2 = null;
				MethodInfo methodInfo3 = null;
				FieldInfo fieldInfo = null;
				FieldInfo fieldInfo2 = null;
				Type[] types = new Type[0];
				if (value != null)
				{
					constructorInfo = value.GetConstructor(new Type[2]
					{
						typeof(double),
						typeof(double)
					});
					if (constructorInfo == null)
					{
						constructorInfo = value.GetConstructor(new Type[2]
						{
							typeof(float),
							typeof(float)
						});
						if (constructorInfo != null)
						{
							ctorUsesFloat = true;
						}
					}
					if (constructorInfo == null)
					{
						methodInfo = value.GetMethod("Create", new Type[2]
						{
							typeof(double),
							typeof(double)
						});
					}
					methodInfo2 = value.GetMethod("Re", types);
					if (methodInfo2 == null)
					{
						methodInfo2 = value.GetMethod("Real", types);
					}
					if (methodInfo2 == null)
					{
						methodInfo2 = value.GetMethod("get_Re", types);
					}
					if (methodInfo2 == null)
					{
						methodInfo2 = value.GetMethod("get_Real", types);
					}
					if (methodInfo2 == null)
					{
						methodInfo2 = value.GetMethod("get_r", types);
					}
					if (methodInfo2 == null)
					{
						fieldInfo = value.GetField("Re");
					}
					if (methodInfo2 == null && fieldInfo == null)
					{
						fieldInfo = value.GetField("Real");
					}
					methodInfo3 = value.GetMethod("Im", types);
					if (methodInfo3 == null)
					{
						methodInfo3 = value.GetMethod("Imag", types);
					}
					if (methodInfo3 == null)
					{
						methodInfo3 = value.GetMethod("Imaginary", types);
					}
					if (methodInfo3 == null)
					{
						methodInfo3 = value.GetMethod("get_Im", types);
					}
					if (methodInfo3 == null)
					{
						methodInfo3 = value.GetMethod("get_Imag", types);
					}
					if (methodInfo3 == null)
					{
						methodInfo3 = value.GetMethod("get_Imaginary", types);
					}
					if (methodInfo3 == null)
					{
						methodInfo3 = value.GetMethod("get_i", types);
					}
					if (methodInfo3 == null)
					{
						fieldInfo2 = value.GetField("Im");
					}
					if (methodInfo3 == null && fieldInfo2 == null)
					{
						fieldInfo2 = value.GetField("Imag");
					}
					if (methodInfo3 == null && fieldInfo2 == null)
					{
						fieldInfo2 = value.GetField("Imaginary");
					}
					if ((constructorInfo == null && methodInfo == null) || (methodInfo2 == null && fieldInfo == null) || (methodInfo3 == null && fieldInfo2 == null))
					{
						throw new ArgumentException("The specified Type does not have the necessary members to represent complex numbers in .NET/Link.");
					}
				}
				complexClass = value;
				complexCtor = constructorInfo;
				complexCreateMethod = methodInfo;
				complexReMethod = methodInfo2;
				complexImMethod = methodInfo3;
				complexReField = fieldInfo;
				complexImField = fieldInfo2;
			}
		}

		public object GetComplex(IMathLink ml)
		{
			double num = 0.0;
			double im = 0.0;
			if (ComplexType == null)
			{
				throw new MathLinkException(1010);
			}
			switch (ml.GetNextExpressionType())
			{
			case ExpressionType.Real:
			case ExpressionType.Integer:
				num = ml.GetDouble();
				break;
			case ExpressionType.Complex:
				ml.CheckFunctionWithArgCount("Complex", 2);
				num = ml.GetDouble();
				im = ml.GetDouble();
				break;
			default:
				throw new MathLinkException(1009);
			}
			return constructComplex(num, im);
		}

		public void PutComplex(IMathLink ml, object obj)
		{
			if (ComplexType == null)
			{
				throw new MathLinkException(1010);
			}
			double num = 0.0;
			double num2 = 0.0;
			try
			{
				num = getRealPart(obj);
				num2 = getImaginaryPart(obj);
			}
			catch (Exception ex)
			{
				ml.PutSymbol("$Failed");
				throw ex;
			}
			ml.PutFunction("Complex", 2);
			ml.Put(num);
			ml.Put(num2);
		}

		private object constructComplex(double re, double im)
		{
			try
			{
				if (complexCreateMethod == null)
				{
					return ctorUsesFloat ? complexCtor.Invoke(new object[2]
					{
						(float)re,
						(float)im
					}) : complexCtor.Invoke(new object[2]
					{
						re,
						im
					});
				}
				return complexCreateMethod.Invoke(null, new object[2]
				{
					re,
					im
				});
			}
			catch (Exception)
			{
				return null;
			}
		}

		private double getRealPart(object complex)
		{
			if (complex.GetType() == complexClass)
			{
				object value = ((complexReMethod == null) ? complexReField.GetValue(complex) : complexReMethod.Invoke(complex, null));
				return Convert.ToDouble(value);
			}
			throw new ArgumentException("Object passed to PutComplex is not of the type set with SetComplexType().");
		}

		private double getImaginaryPart(object complex)
		{
			if (complex.GetType() == complexClass)
			{
				object value = ((complexImMethod == null) ? complexImField.GetValue(complex) : complexImMethod.Invoke(complex, null));
				return Convert.ToDouble(value);
			}
			throw new ArgumentException("Object passed to PutComplex is not of the type set with SetComplexType().");
		}
	}
}
