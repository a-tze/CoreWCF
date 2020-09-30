﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CoreWCF.Channels;
using CoreWCF.Dispatcher;
using System;
using System.Collections.Generic;
using System.Text;
using CoreWCF.Description;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;

namespace CoreWCF.Configuration
{
    public static class ServiceModelServiceCollectionExtensions
    {
        public static IServiceCollection AddServiceModelServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            services.AddSingleton<WrappingIServer>();
            for (int i=0; i<services.Count; i++)
            {
                if (services[i].ServiceType == typeof(IServer))
                {
                    Type implType = services[i].ImplementationType;
                    if (!services.Any(d => d.ServiceType == implType))
                    {
                        services.AddSingleton(implType);
                    }
                    services[i] = ServiceDescriptor.Singleton<IServer>((provider) =>
                        {
                            var originalIServer = (IServer)provider.GetRequiredService(implType);
                            var wrappingServer = provider.GetRequiredService<WrappingIServer>();
                            wrappingServer.InnerServer = originalIServer;
                            return wrappingServer;
                        });
                }
            }
            services.AddSingleton<ServiceBuilder>();
            services.AddSingleton<IServiceBuilder>(provider => provider.GetRequiredService<ServiceBuilder>());
            services.AddSingleton<IServiceBehavior>(provider => provider.GetRequiredService<ServiceAuthorizationBehavior>());
            services.AddSingleton<ServiceAuthorizationBehavior>(provider =>
            {
                var behavior = new ServiceAuthorizationBehavior();
                var manager = provider.GetService<ServiceAuthorizationManager>();
                if (manager != null)
                {
                    behavior.ServiceAuthorizationManager = manager;
                }
                return behavior;
            });
            services.TryAddSingleton(typeof(IServiceConfiguration<>), typeof(ServiceConfiguration<>));
            services.TryAddSingleton<IDispatcherBuilder, DispatcherBuilderImpl>();
            services.AddSingleton(typeof(ServiceConfigurationDelegateHolder<>));
            services.AddScoped<ReplyChannelBinder>();
            services.AddScoped<DuplexChannelBinder>();
            services.AddScoped<InputChannelBinder>();
            services.AddScoped<ServiceChannel.SessionIdleManager>();
            services.AddSingleton(typeof(ServiceHostObjectModel<>));
            services.AddSingleton(typeof(TransportCompressionSupportHelper));
            return services;
        }
    }
}
