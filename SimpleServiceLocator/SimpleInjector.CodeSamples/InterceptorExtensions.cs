﻿namespace SimpleInjector.CodeSamples
{
    /*
    // Example usage
    var container = new Container();

    // Ensures that IUserRepository gets intercepted using a MonitoringInterceptor.
    container.InterceptWith<MonitoringInterceptor>(type => type == typeof(IUserRepository));
    
    // Use multiple interceptors (YetAnotherInterceptor wraps MonitoringInterceptor wraps IUserRepository).
    container.InterceptWith<MonitoringInterceptor>(type => type == typeof(IUserRepository));
    container.InterceptWith<YetAnotherInterceptor>(type => type == typeof(IUserRepository));
    
    // Ensures that all registered (interface) service that who's name end with 'CommandHandler' get 
    // intercepted using with a MonitoringInterceptor.
    container.InterceptWith<MonitoringInterceptor>(type => type.Name.EndsWith("CommandHandler"));

    // Reuse the same interceptor instance.
    container.RegisterSingle<MonitoringInterceptor>();
    container.InterceptWith<MonitoringInterceptor>(type => type == typeof(IUserRepository));
    
    // Manually: returns a SqlUserRepository decorated by a MonitoringInterceptor.
    container.Register<IUserRepository>(() =>
        Interceptor.CreateProxy<IUserRepository>(
            container.GetInstance<MonitoringInterceptor>(),
            container.GetInstance<SqlUserRepository>()
        )
    );
    */

    // http://simpleinjector.codeplex.com/wikipage?title=InterceptionExtensions
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Remoting.Messaging;
    using System.Runtime.Remoting.Proxies;

    using SimpleInjector;

    public interface IInterceptor
    {
        void Intercept(IInvocation invocation);
    }

    public interface IInvocation
    {
        object InvocationTarget { get; }

        object ReturnValue { get; set; }

        void Proceed();

        MethodBase GetConcreteMethod();
    }

    // Extension methods for interceptor registration
    // NOTE: These extension methods can only intercept interfaces, not abstract types.
    public static class InterceptorExtensions
    {
        public static void InterceptWith<TInterceptor>(this Container container, 
            Func<Type, bool> predicate)
            where TInterceptor : class, IInterceptor
        {
            RequiresIsNotNull(container, "container");
            RequiresIsNotNull(predicate, "predicate");

            var interceptWith = new InterceptionHelper(container)
            {
                BuildInterceptorExpression = 
                    () => BuildInterceptorExpression<TInterceptor>(container),
                Predicate = type => type.IsInterface && predicate(type)
            };

            container.ExpressionBuilt += interceptWith.OnExpressionBuilt;
        }

        public static void InterceptWith(this Container container, 
            Func<IInterceptor> interceptorCreator,
            Func<Type, bool> predicate)
        {
            RequiresIsNotNull(container, "container");
            RequiresIsNotNull(interceptorCreator, "interceptorCreator");
            RequiresIsNotNull(predicate, "predicate");

            var interceptWith = new InterceptionHelper(container)
            {
                BuildInterceptorExpression = 
                    () => Expression.Invoke(Expression.Constant(interceptorCreator)),
                Predicate = type => type.IsInterface && predicate(type)
            };

            container.ExpressionBuilt += interceptWith.OnExpressionBuilt;
        }

        public static void InterceptWith(this Container container, 
            IInterceptor interceptor,
            Func<Type, bool> predicate)
        {
            RequiresIsNotNull(container, "container");
            RequiresIsNotNull(interceptor, "interceptor");
            RequiresIsNotNull(predicate, "predicate");

            var interceptWith = new InterceptionHelper(container)
            {
                BuildInterceptorExpression = () => Expression.Constant(interceptor),
                Predicate = type => type.IsInterface && predicate(type)
            };

            container.ExpressionBuilt += interceptWith.OnExpressionBuilt;
        }

        [DebuggerStepThrough]
        private static Expression BuildInterceptorExpression<TInterceptor>(Container container)
        {
            var interceptorRegistration = container.GetRegistration(typeof(TInterceptor));

            if (interceptorRegistration == null)
            {
                throw new ActivationException(string.Format(
                    "No registration for interceptor type {0} " +
                    "could be found and an implicit registration could not be made.", 
                    typeof(TInterceptor)));
            }

            return interceptorRegistration.BuildExpression();
        }

        private static void RequiresIsNotNull(object instance, string paramName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        private class InterceptionHelper
        {
            private static readonly MethodInfo NonGenericInterceptorCreateProxyMethod = (
                from method in typeof(Interceptor).GetMethods()
                where method.Name == "CreateProxy"
                where method.GetParameters().Length == 3
                select method)
                .Single();

            public InterceptionHelper(Container container)
            {
                this.Container = container;
            }

            internal Container Container { get; private set; }

            internal Func<Expression> BuildInterceptorExpression { get; set; }

            internal Func<Type, bool> Predicate { get; set; }

            [DebuggerStepThrough]
            public void OnExpressionBuilt(object sender, ExpressionBuiltEventArgs e)
            {
                // NOTE: We can only handle interfaces, because 
                // System.Runtime.Remoting.Proxies.RealProxy only supports interfaces.
                bool interceptType = e.RegisteredServiceType.IsInterface && 
                    this.Predicate(e.RegisteredServiceType);

                if (interceptType)
                {
                    e.Expression = this.BuildProxyExpression(e);
                }
            }

            [DebuggerStepThrough]
            private Expression BuildProxyExpression(ExpressionBuiltEventArgs e)
            {
                var interceptor = this.BuildInterceptorExpression();

                // Create call to 
                // (ServiceType)Interceptor.CreateProxy(Type, IInterceptor, object)
                var proxyExpression =
                    Expression.Convert(
                        Expression.Call(NonGenericInterceptorCreateProxyMethod,
                            Expression.Constant(e.RegisteredServiceType),
                            interceptor,
                            e.Expression),
                        e.RegisteredServiceType);

                // Optimization for singletons.
                if (e.Expression is ConstantExpression && interceptor is ConstantExpression)
                {
                    return Expression.Constant(CreateInstance(proxyExpression),
                        e.RegisteredServiceType);
                }

                return proxyExpression;
            }

            [DebuggerStepThrough]
            private static object CreateInstance(Expression expression)
            {
                var instanceCreator = Expression.Lambda<Func<object>>(expression, 
                    new ParameterExpression[0])
                    .Compile();

                return instanceCreator();
            }
        }
    }

    public static class Interceptor
    {
        public static T CreateProxy<T>(IInterceptor interceptor, T realInstance)
        {
            return (T)CreateProxy(typeof(T), interceptor, realInstance);
        }

        public static object CreateProxy(Type serviceType, IInterceptor interceptor, 
            object realInstance)
        {
            var proxy = new InterceptorProxy(serviceType, realInstance, interceptor);

            return proxy.GetTransparentProxy();
        }

        private sealed class InterceptorProxy : RealProxy
        {
            private object realInstance;
            private IInterceptor interceptor;

            public InterceptorProxy(Type classToProxy, object realInstance, 
                IInterceptor interceptor)
                : base(classToProxy)
            {
                this.realInstance = realInstance;
                this.interceptor = interceptor;
            }

            public override IMessage Invoke(IMessage msg)
            {
                if (msg is IMethodCallMessage)
                {
                    return this.InvokeMethodCall((IMethodCallMessage)msg);
                }

                return msg;
            }

            private IMessage InvokeMethodCall(IMethodCallMessage message)
            {
                var invocation = new Invocation { Proxy = this, Message = message };

                invocation.Proceeding += (s, e) =>
                {
                    invocation.ReturnValue = message.MethodBase.Invoke(
                        this.realInstance, message.Args);
                };

                this.interceptor.Intercept(invocation);

                return new ReturnMessage(invocation.ReturnValue, null, 0, null, message);
            }

            private class Invocation : IInvocation
            {
                public event EventHandler Proceeding;

                public InterceptorProxy Proxy { get; set; }

                public IMethodCallMessage Message { get; set; }

                public object ReturnValue { get; set; }

                public object InvocationTarget
                {
                    get { return this.Proxy.realInstance; }
                }

                public void Proceed()
                {
                    if (this.Proceeding != null)
                    {
                        this.Proceeding(this, EventArgs.Empty);
                    }
                }

                public MethodBase GetConcreteMethod()
                {
                    return this.Message.MethodBase;
                }
            }
        }
    }
}