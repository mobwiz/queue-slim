// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using DotnetCore.QueueSlim.Internal;
using DotnetCore.QueueSlim.Options;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetCore.QueueSlim
{

    public static class EventBusExtensions
    {
        public static IServiceCollection AddQueueSlim(this IServiceCollection services, Action<QueueSlimOptions> configure)
        {
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var options = new QueueSlimOptions();
            configure(options);

            services.Configure(configure);

            services.AddTransient<CloudEventFormatter, JsonEventFormatter>();
            services.AddTransient<IQueuePublisher, DefaultEventPublisher>();
            services.AddTransient<InternalQueueSubscriber>();

            foreach (var extension in options.Extensions)
            {
                extension.AddServices(services);
            }

            services.AddHostedService<BaseSubscriberService>();

            RegisterAllQueueSubscribers(services);

            return services;
        }


        private static void RegisterAllQueueSubscribers(IServiceCollection services)
        {
            // 获取当前应用程序域中的所有程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // 查找所有实现了 IQueueSubscriber 接口的类
            var subscriberTypes = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IQueueSubscriber).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);

            // 将这些类注册为单例服务            
            foreach (var subscriberType in subscriberTypes)
            {
                Console.WriteLine($"Registering subscriber: {subscriberType.FullName}");
                services.AddSingleton(typeof(IQueueSubscriber), subscriberType);
            }
        }
    }

}