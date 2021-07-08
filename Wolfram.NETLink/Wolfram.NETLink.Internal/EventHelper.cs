using System;
using System.Reflection;

namespace Wolfram.NETLink.Internal
{
	internal class EventHelper
	{
		internal static string getDelegateTypeName(object eventsObject, string aqTypeName, string evtName)
		{
			EventInfo eventInfo = getEventInfo(eventsObject, aqTypeName, evtName);
			return eventInfo.EventHandlerType.AssemblyQualifiedName;
		}

		internal static Delegate addHandler(object eventsObject, string aqTypeName, string evtName, Delegate dlg)
		{
			EventInfo eventInfo = getEventInfo(eventsObject, aqTypeName, evtName);
			eventInfo.AddEventHandler(eventsObject, dlg);
			return dlg;
		}

		internal static void removeHandler(object eventsObject, string aqTypeName, string evtName, Delegate dlg)
		{
			EventInfo eventInfo = getEventInfo(eventsObject, aqTypeName, evtName);
			eventInfo.RemoveEventHandler(eventsObject, dlg);
		}

		private static EventInfo getEventInfo(object eventsObject, string aqTypeName, string evtName)
		{
			EventInfo eventInfo = null;
			if (eventsObject != null)
			{
				Type type = eventsObject.GetType();
				EventInfo[] events = type.GetEvents(BindingFlags.Instance | BindingFlags.Public);
				EventInfo[] array = events;
				foreach (EventInfo eventInfo2 in array)
				{
					if (Utils.memberNamesMatch(eventInfo2.Name, evtName))
					{
						eventInfo = eventInfo2;
						break;
					}
				}
				if (eventInfo == null)
				{
					throw new ArgumentException("No public instance event named " + evtName + " exists for the given object.");
				}
			}
			else
			{
				Type type2 = TypeLoader.GetType(aqTypeName, throwOnError: true);
				EventInfo[] events2 = type2.GetEvents(BindingFlags.Static | BindingFlags.Public);
				EventInfo[] array2 = events2;
				foreach (EventInfo eventInfo3 in array2)
				{
					if (Utils.memberNamesMatch(eventInfo3.Name, evtName))
					{
						eventInfo = eventInfo3;
						break;
					}
				}
				if (eventInfo == null)
				{
					throw new ArgumentException("No public static event named " + evtName + " exists for the type " + aqTypeName.Split(',')[0] + ".");
				}
			}
			return eventInfo;
		}
	}
}
