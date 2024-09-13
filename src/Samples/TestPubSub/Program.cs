// Copyright (c) .NET Core Community. All rights reserved. 
//  Licensed under the MIT License. See License.txt in the project root for license information.

using DotnetCore.QueueSlim;
using Microsoft.Extensions.Hosting;

namespace TestPubSub
{
    internal partial class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var builder = Host.CreateApplicationBuilder(args);

            builder.Logging.AddConsole();
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            builder.Logging.SetMinimumLevel(LogLevel.Information);

            builder.Services.AddQueueSlim(options =>
            {
                options.UseRabbitMQ(options =>
                {
                    options.Port = 5672;
                    options.HostName = "192.168.0.111";
                    options.UserName = "admin";
                    options.Password = "123456";
                    options.VirtualHost = "queueslim";
                });
            });

            //builder.Services.AddSingleton<IQueueSubscriber, SubscirberTest>();
            //builder.Services.AddSingleton<IQueueSubscriber, SubscirberTestAsync>();

            builder.Services.AddHostedService<PublisherTestBackgroundService>();

            var app = builder.Build();
            await app.RunAsync();
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
                services.AddSingleton(typeof(IQueueSubscriber), subscriberType);
            }
        }
    }
}
