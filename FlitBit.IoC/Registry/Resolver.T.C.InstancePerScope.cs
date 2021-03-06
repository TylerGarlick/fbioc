﻿#region COPYRIGHT© 2009-2014 Phillip Clark. All rights reserved.

// For licensing information see License.txt (MIT style licensing).

#endregion

using System;
using System.Collections.Concurrent;
using FlitBit.IoC.Constructors;

namespace FlitBit.IoC.Registry
{
	internal class InstancePerScopeResolver<T, TConcrete> : Resolver<T, TConcrete>
		where TConcrete : class, T
	{
		readonly ConcurrentDictionary<Guid, T> _containerInstances = new ConcurrentDictionary<Guid, T>();

		public InstancePerScopeResolver(ConstructorSet<T, TConcrete> constructors)
			: base(constructors) { }

		public override bool TryResolve(IContainer container, LifespanTracking tracking, string name, out T instance,
			params Param[] parameters)
		{
			var kind = CreationEventKind.Reissued;

			CommandBinding<T> command = null;
			var key = container.Key;
			var tempIssued = false;
			var temp = default(T);
			while (true)
			{
				if (_containerInstances.TryGetValue(key, out instance))
				{
					if (tempIssued && IsDisposable)
					{
						((IDisposable) temp).Dispose();
					}
					break;
				}
				if (command == null && !Constructors.TryMatchAndBind(parameters, out command))
				{
					instance = default(T);
					return false;
				}
				if (!tempIssued)
				{
					temp = command.Execute(container, name);
					tempIssued = true;
				}
				if (_containerInstances.TryAdd(key, temp))
				{
					container.Scope.AddAction(() =>
					{
						T value;
						if (_containerInstances.TryRemove(key, out value))
						{
							if (IsDisposable)
							{
								((IDisposable) value).Dispose();
							}
						}
					});
					instance = temp;
					kind = CreationEventKind.Created;
					break;
				}
			}
			container.NotifyObserversOfCreationEvent(typeof(T), instance, name, kind);
			return true;
		}
	}
}