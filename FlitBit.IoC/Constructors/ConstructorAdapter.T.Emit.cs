﻿#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using FlitBit.Core;
using FlitBit.Emit;

namespace FlitBit.IoC.Constructors
{
	/// <summary>
	///   Adapter for constructors defined on type T
	/// </summary>
	public partial class ConstructorAdapter<T>
	{
		/// <summary>
		///   Compiles a constructor adapter for the given constructor.
		/// </summary>
		/// <param name="ordinal">the ordinal position of the constructor among constructors defined on type T</param>
		/// <param name="ci">constructor info</param>
		/// <returns>the compiled constructor adapter type</returns>
		public static Type GetConstructorAdapterByOrdinal(int ordinal, ConstructorInfo ci)
		{
			Contract.Requires(ci != null);
			Contract.Ensures(Contract.Result<Type>() != null);

			var lck = typeof(ConstructorAdapter<T>).GetLockForType();
			lock (lck)
			{
				var targetType = typeof(T);
				var typeName = RuntimeAssemblies.PrepareTypeName(targetType, String.Concat("Ctor#", ordinal));

				var module = ConstructorAdapter.Module;
				var type = module.Builder.GetType(typeName, false, false) ?? BuildConstructorAdapter(module, typeName, ci);
				return type;
			}
		}

		internal static Type BuildConstructorAdapter(EmittedModule module, string typeName, ConstructorInfo ci)
		{
			Contract.Requires(module != null);
			Contract.Requires(typeName != null);
			Contract.Requires(typeName.Length > 0);
			Contract.Ensures(Contract.Result<Type>() != null);

			var builder = module.DefineClass(typeName, EmittedClass.DefaultTypeAttributes, typeof(ConstructorAdapter<T>), null);
			builder.Attributes = TypeAttributes.Sealed | TypeAttributes.Public | TypeAttributes.BeforeFieldInit;
			builder.DefineDefaultCtor();

			var method = builder.DefineOverrideMethod(typeof(ConstructorAdapter<T>).GetMethod("Execute"));

			method.ContributeInstructions((m, il) =>
			{
				var result = il.DeclareLocal(typeof(T));
				il.Nop();
				foreach (var param in ci.GetParameters())
				{
					il.LoadArg_3();
					il.LoadValue(param.Position);
					il.LoadElementRef();
					if (param.ParameterType.IsPrimitive || param.ParameterType.IsValueType)
					{
						il.UnboxAny(param.ParameterType);
					}
					else if (typeof(Object) != param.ParameterType)
					{
						il.CastClass(param.ParameterType);
					}
				}
				il.NewObj(ci);
				il.StoreLocal(result);
				var br = il.DefineLabel();
				il.Branch(br);
				il.MarkLabel(br);
				il.LoadLocal(result);
			});

			builder.Compile();
			return builder.Ref.Target;
		}
	}
}