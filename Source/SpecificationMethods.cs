using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Aksio.Specifications
{
    /// <summary>
    /// Represents the lifecycle methods for a <see cref="Specification"/>.
    /// </summary>
    /// <typeparam name="T">Target type it represents.</typeparam>
    /// <remarks>
    /// It will recursively execute lifecycle methods in the inheritance hierarchy.
    /// This enables one to encapsulate reusable contexts. The order it executes them in
    /// is reversed; meaning that it will start at the lowest level in the inheritance chain
    /// and move towards the specific type.
    /// Example:
    /// Context class:
    /// public class a_specific_context : Specification
    /// {
    ///     void Establish() => ....
    /// }
    /// _
    /// Specification class:
    /// _
    /// public class when_doing_something : a_specific_context
    /// {
    ///     void Establish() => ....
    /// }
    /// -
    /// It will run the Establish first for the `a_specific_context` and then the `when_doing_something`
    /// class.
    /// </remarks>
    public static class SpecificationMethods<T>
    {
        static SpecificationMethods()
        {
            _establish = GetMethodsFor("Establish");
            _destroy = GetMethodsFor("Destroy");
            _because = GetMethodsFor("Because");
        }

        static IEnumerable<MethodInfo> _establish { get; }

        static IEnumerable<MethodInfo> _because { get; }

        static IEnumerable<MethodInfo> _destroy { get; }

        /// <summary>
        /// Invoke all Establish methods.
        /// </summary>
        /// <param name="unit">Unit to invoke them on.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task Establish(object unit) => InvokeMethods(_establish, unit);

        /// <summary>
        /// Invoke all Destroy methods.
        /// </summary>
        /// <param name="unit">Unit to invoke them on.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task Destroy(object unit) => InvokeMethods(_destroy, unit);

        /// <summary>
        /// Invoke all Because methods.
        /// </summary>
        /// <param name="unit">Unit to invoke them on.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task Because(object unit) => InvokeMethods(_because, unit);

        static Task InvokeMethods(IEnumerable<MethodInfo> methods, object unit)
        {
            var tasks = new List<Task>();
            var asyncMethodsInChain = false;

            foreach (var method in methods)
            {
                var result = method.Invoke(unit, Array.Empty<object>());
                if (result is Task taskResult)
                {
                    tasks.Add(taskResult);
                    asyncMethodsInChain = true;
                }
                else
                {
                    tasks.Add(Task.CompletedTask);
                }
            }

            if (!asyncMethodsInChain) return Task.CompletedTask;
            return Task.WhenAll(tasks.ToArray());
        }

        static IEnumerable<MethodInfo> GetMethodsFor(string name)
        {
            var type = typeof(T);
            var methods = new List<MethodInfo>();

            while (type != typeof(Specification))
            {
                var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null) methods.Insert(0, method);
                type = type.BaseType;
            }

            return methods;
        }
    }
}
