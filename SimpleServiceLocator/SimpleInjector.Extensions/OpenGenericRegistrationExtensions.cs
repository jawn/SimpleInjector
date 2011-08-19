﻿#region Copyright (c) 2010 S. van Deursen
/* The Simple Injector is an easy-to-use Inversion of Control library for .NET
 * 
 * Copyright (C) 2010 S. van Deursen
 * 
 * To contact me, please visit my blog at http://www.cuttingedge.it/blogs/steven/ or mail to steven at 
 * cuttingedge.it.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
 * associated documentation files (the "Software"), to deal in the Software without restriction, including 
 * without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the 
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial 
 * portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO 
 * EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE 
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

namespace SimpleInjector.Extensions
{
    using System;
    using System.Collections.Generic;

    using SimpleInjector;

    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods for registration of open generic service
    /// types in the <see cref="Container"/>.
    /// </summary>
    public static class OpenGenericRegistrationExtensions
    {
        /// <summary>
        /// Registers that a new instance of <paramref name="openGenericImplementation"/> will be returned 
        /// every time a <paramref name="openGenericServiceType"/> is requested.
        /// </summary>
        /// <example>
        /// The following example shows the definition of a generic <code>IValidator&lt;T&gt;</code> interface
        /// and, a <code>NullValidator&lt;T&gt;</code> implementation and a specific validator for Orders.
        /// The registration ensures a <code>OrderValidator</code> is returned when a 
        /// <code>IValidator&lt;Order&gt;</code> is requested. For all requests for a 
        /// <code>IValidator&lt;T&gt;</code> other than a <code>IValidator&lt;Order&gt;</code>, an 
        /// implementation of <code>NullValidator&lt;T&gt;</code> will be returned.
        /// <code lang="cs"><![CDATA[
        /// public interface IValidator<T>
        /// { 
        ///     void Validate(T instance);
        /// }
        /// 
        /// public class NullValidator<T> : IValidator<T>
        /// {
        ///     public void Validate(T instance)
        ///     {
        ///     }
        /// }
        /// 
        /// public class OrderValidator : IValidator<Order>
        /// {
        ///     public void Validate(Order instance)
        ///     {
        ///         if (instance.Total < 0)
        ///         {
        ///             throw new ValidationException("Total can not be negative.");
        ///         }
        ///     }
        /// }
        /// 
        /// [TestMethod]
        /// public static void TestRegisterInitializer()
        /// {
        ///     // Arrange
        ///     var container = new Container();
        ///     
        ///     container.Register<IValidator<Order>, OrderValidator>();
        ///     container.RegisterOpenGeneric(typeof(IValidator<>), typeof(NullValidator<>));
        ///     
        ///     // Act
        ///     var orderValidator = container.GetInstance<IValidator<Order>>();
        ///     var customerValidator = container.GetInstance<IValidator<Customer>>();
        ///     var productValidator = container.GetInstance<IValidator<Product>>();
        /// 
        ///     // Assert
        ///     Assert.IsInstanceOfType(orderValidator, typeof(OrderValidator));
        ///     Assert.IsInstanceOfType(customerValidator, typeof(NullValidator<Customer>));
        ///     Assert.IsInstanceOfType(productValidator, typeof(NullValidator<Product>));
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="container">The container to make the registrations in.</param>
        /// <param name="openGenericServiceType">The definition of the open generic service type that can be 
        /// used to retrieve instances..</param>
        /// <param name="openGenericImplementation">The definition of the open generic implementation type
        /// that will be returned when a <typeparamref name="openGenericServiceType"/> is requested.</param>
        public static void RegisterOpenGeneric(this Container container,
            Type openGenericServiceType, Type openGenericImplementation)
        {
            Requires.TypeIsOpenGeneric(openGenericServiceType, "openGenericServiceType");
            Requires.TypeIsOpenGeneric(openGenericImplementation, "openGenericImplementation");
            Requires.ServiceIsAssignableFromImplementation(openGenericServiceType, openGenericImplementation,
                "openGenericServiceType");

            var transientResolver = new TransientOpenGenericResolver
            {
                OpenGenericServiceType = openGenericServiceType,
                OpenGenericImplementation = openGenericImplementation,
                Container = container
            };

            container.ResolveUnregisteredType += transientResolver.ResolveOpenGeneric;
        }

        /// <summary>
        /// Registers that the same instance of <paramref name="openGenericImplementation"/> will be returned 
        /// every time a <paramref name="openGenericServiceType"/> is requested.
        /// </summary>
        /// <example>
        /// The following example shows the definition of a generic <code>IValidator&lt;T&gt;</code> interface
        /// and, a <code>NullValidator&lt;T&gt;</code> implementation and a specific validator for Orders.
        /// The registration ensures a <code>OrderValidator</code> is returned when a 
        /// <code>IValidator&lt;Order&gt;</code> is requested. For all requests for a 
        /// <code>IValidator&lt;T&gt;</code> other than a <code>IValidator&lt;Order&gt;</code>, an 
        /// implementation of <code>NullValidator&lt;T&gt;</code> will be returned.
        /// <code lang="cs"><![CDATA[
        /// public interface IValidator<T>
        /// { 
        ///     void Validate(T instance);
        /// }
        /// 
        /// public class NullValidator<T> : IValidator<T>
        /// {
        ///     public void Validate(T instance)
        ///     {
        ///     }
        /// }
        /// 
        /// public class OrderValidator : IValidator<Order>
        /// {
        ///     public void Validate(Order instance)
        ///     {
        ///         if (instance.Total < 0)
        ///         {
        ///             throw new ValidationException("Total can not be negative.");
        ///         }
        ///     }
        /// }
        /// 
        /// [TestMethod]
        /// public static void TestRegisterInitializer()
        /// {
        ///     // Arrange
        ///     var container = new Container();
        ///     
        ///     container.RegisterSingle<IValidator<Order>, OrderValidator>();
        ///     container.RegisterSingleOpenGeneric(typeof(IValidator<>), typeof(NullValidator<>));
        ///     
        ///     // Act
        ///     var orderValidator = container.GetInstance<IValidator<Order>>();
        ///     var customerValidator = container.GetInstance<IValidator<Customer>>();
        ///     var productValidator = container.GetInstance<IValidator<Product>>();
        /// 
        ///     // Assert
        ///     Assert.IsInstanceOfType(orderValidator, typeof(OrderValidator));
        ///     Assert.IsInstanceOfType(customerValidator, typeof(NullValidator<Customer>));
        ///     Assert.IsInstanceOfType(productValidator, typeof(NullValidator<Product>));
        /// }
        /// ]]></code>
        /// </example>
        /// <param name="container">The container to make the registrations in.</param>
        /// <param name="openGenericServiceType">The definition of the open generic service type that can be 
        /// used to retrieve instances..</param>
        /// <param name="openGenericImplementation">The definition of the open generic implementation type
        /// that will be returned when a <typeparamref name="openGenericServiceType"/> is requested.</param>
        public static void RegisterSingleOpenGeneric(this Container container,
            Type openGenericServiceType, Type openGenericImplementation)
        {
            Requires.TypeIsOpenGeneric(openGenericServiceType, "openGenericServiceType");
            Requires.TypeIsOpenGeneric(openGenericImplementation, "openGenericImplementation");
            Requires.ServiceIsAssignableFromImplementation(openGenericServiceType, openGenericImplementation,
                "openGenericServiceType");

            var singletonResolver = new SingleOpenGenericResolver
            {
                OpenGenericServiceType = openGenericServiceType,
                OpenGenericImplementation = openGenericImplementation,
                Container = container
            };

            container.ResolveUnregisteredType += singletonResolver.ResolveOpenGeneric;
        }

        /// <summary>Resolves a given open generic type as transient.</summary>
        private sealed class TransientOpenGenericResolver : OpenGenericResolver
        {
            internal override void Register(Type closedGenericImplementation, UnregisteredTypeEventArgs e)
            {
                IInstanceProducer producter = this.Container.GetRegistration(closedGenericImplementation);

                e.Register(producter.GetInstance);
            }
        }

        /// <summary>Resolves a given open generic type as singleton.</summary>
        private sealed class SingleOpenGenericResolver : OpenGenericResolver
        {
            private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

            internal override void Register(Type closedGenericImplementation, UnregisteredTypeEventArgs e)
            {
                object singleton = this.GetSingleton(closedGenericImplementation);

                e.Register(() => singleton);
            }

            private object GetSingleton(Type closedGenericImplementation)
            {
                object singleton;

                lock (this)
                {
                    if (!this.singletons.TryGetValue(closedGenericImplementation, out singleton))
                    {
                        singleton = this.Container.GetInstance(closedGenericImplementation);
                        this.singletons[closedGenericImplementation] = singleton;
                    }
                }

                return singleton;
            }
        }

        /// <summary>Resolves a given open generic type.</summary>
        private abstract class OpenGenericResolver
        {
            internal Type OpenGenericServiceType { get; set; }

            internal Type OpenGenericImplementation { get; set; }

            internal Container Container { get; set; }

            internal void ResolveOpenGeneric(object sender, UnregisteredTypeEventArgs e)
            {
                if (!this.OpenGenericServiceType.IsGenericTypeDefinitionOf(e.UnregisteredServiceType))
                {
                    return;
                }

                var builder = new GenericTypeBuilder
                {
                    ClosedGenericBaseType = e.UnregisteredServiceType,
                    OpenGenericImplementation = this.OpenGenericImplementation
                };

                var results = builder.BuildClosedGenericImplementation();

                if (results.ClosedServiceTypeSatisfiesAllTypeConstraints)
                {
                    this.Register(results.ClosedGenericImplementation, e);
                }
            }

            internal abstract void Register(Type closedGenericImplementation, UnregisteredTypeEventArgs e);
        }
    }
}