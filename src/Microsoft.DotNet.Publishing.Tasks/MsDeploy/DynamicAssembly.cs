﻿using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.DotNet.Publishing.Tasks.MsDeploy
{
    internal class DynamicAssembly
    {
        public DynamicAssembly(string assemblyName, System.Version verToLoad, string publicKeyToken)
        {
            AssemblyFullName = string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0}, Version={1}.{2}.0.0, Culture=neutral, PublicKeyToken={3}", assemblyName, verToLoad.Major, verToLoad.Minor, publicKeyToken);
            Assembly = Assembly.Load(AssemblyFullName);
            Version = verToLoad;
        }

        public DynamicAssembly() { }

        public string AssemblyFullName { get; set; }
        public System.Version Version { get; set; }
        public Assembly Assembly { get; set; }

        public System.Type GetType(string typeName)
        {
            System.Type type = Assembly.GetType(typeName);
            Debug.Assert(type != null);
            return type;
        }

        public virtual System.Type TryGetType(string typeName)
        {
            System.Type type = Assembly.GetType(typeName);
            return type;
        }

        public object GetEnumValue(string enumName, string enumValue)
        {
            System.Type enumType = Assembly.GetType(enumName);
            FieldInfo enumItem = enumType.GetField(enumValue);
            object ret = enumItem.GetValue(enumType);
            Debug.Assert(ret != null);
            return ret;
        }

        public object GetEnumValueIgnoreCase(string enumName, string enumValue)
        {
            System.Type enumType = Assembly.GetType(enumName);
            FieldInfo enumItem = enumType.GetField(enumValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            object ret = enumItem.GetValue(enumType);
            Debug.Assert(ret != null);
            return ret;
        }

        public bool TryGetEnumValue(string enumTypeName, string enumStrValue, out object retValue)
        {
            bool fGetValue = false;
            retValue = System.Enum.ToObject(GetType(enumTypeName), 0);
            try
            {
                retValue = GetEnumValueIgnoreCase(enumTypeName, enumStrValue);
                fGetValue = true;
            }
            catch
            {
            }
            return fGetValue;
        }


        public object CreateObject(string typeName)
        {
            return CreateObject(typeName, null);
        }

        public object CreateObject(string typeName, object[] arguments)
        {
            object createdObject = null;
            System.Type[] argumentTypes = null;
            if (arguments == null || arguments.GetLength(0) == 0)
            {
                argumentTypes = System.Type.EmptyTypes;
            }
            else
            {
                argumentTypes = arguments.Select(p => p.GetType()).ToArray();
            }
            System.Type typeToConstruct = Assembly.GetType(typeName);
            System.Reflection.ConstructorInfo constructorInfoObj = typeToConstruct.GetConstructor(argumentTypes);

            if (constructorInfoObj == null)
            {
                Debug.Assert(false, "DynamicAssembly.CreateObject Failed to get the constructorInfoObject");
            }
            else
            {
                createdObject = constructorInfoObj.Invoke(arguments);
            }
            Debug.Assert(createdObject != null);
            return createdObject;
        }

        public object CallStaticMethod(string typeName, string methodName, object[] arguments)
        {
            System.Type t = GetType(typeName);
            return t.InvokeMember(methodName, BindingFlags.InvokeMethod, null, t, arguments, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Support late bind delegate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void EventHandlerDynamicDelegate(object sender, dynamic e);
        public delegate void EventHandlerEventArgsDelegate(object sender, System.EventArgs e);
        internal static System.Delegate CreateEventHandlerDelegate<TDelegate>(System.Reflection.EventInfo evt, TDelegate d)
        {
            var handlerType = evt.EventHandlerType;
            var eventParams = handlerType.GetMethod("Invoke").GetParameters();

            ParameterExpression[] parameters = eventParams.Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToArray();
            MethodCallExpression body = Expression.Call(Expression.Constant(d), d.GetType().GetMethod("Invoke"), parameters);
            var lambda = Expression.Lambda(body, parameters);
            // Diagnostics.Debug.Assert(false, lambda.ToString());
            return System.Delegate.CreateDelegate(handlerType, lambda.Compile(), "Invoke", false);
        }

        static public System.Delegate AddEventDeferHandler(dynamic obj, string eventName, System.Delegate deferEventHandler)
        {
            EventInfo eventinfo = obj.GetType().GetEvent(eventName);
            System.Delegate eventHandler = DynamicAssembly.CreateEventHandlerDelegate(eventinfo, deferEventHandler);
            eventinfo.AddEventHandler(obj, eventHandler);
            return eventHandler;
        }

        static public void AddEventHandler(dynamic obj, string eventName, System.Delegate eventHandler)
        {
            EventInfo eventinfo = obj.GetType().GetEvent(eventName);
            eventinfo.AddEventHandler(obj, eventHandler);
        }

        static public void RemoveEventHandler(dynamic obj, string eventName, System.Delegate eventHandler)
        {
            EventInfo eventinfo = obj.GetType().GetEvent(eventName);
            eventinfo.RemoveEventHandler(obj, eventHandler);
        }
    }
}
