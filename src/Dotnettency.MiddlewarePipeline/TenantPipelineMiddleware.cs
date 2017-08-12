﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Dotnettency.Container;

namespace Dotnettency.MiddlewarePipeline
{

    public class TenantPipelineMiddleware<TTenant>
        where TTenant : class
    {

        private readonly RequestDelegate _next;
        private readonly IApplicationBuilder _rootApp;
        private readonly ILogger<TenantPipelineMiddleware<TTenant>> _logger;
        private readonly ITenantMiddlewarePipelineFactory<TTenant> _factory;


        public TenantPipelineMiddleware(
            RequestDelegate next,
            IApplicationBuilder rootApp,
            ILogger<TenantPipelineMiddleware<TTenant>> logger,
            ITenantMiddlewarePipelineFactory<TTenant> factory)

        {
            _next = next;
            _rootApp = rootApp;
            _logger = logger;
            _factory = factory;
        }


        public async Task Invoke(HttpContext context, ITenantContainerAccessor<TTenant> tenantContainerAccessor, ITenantShellAccessor<TTenant> tenantShellAccessor)
        {
            var tenantShell = await tenantShellAccessor.CurrentTenantShell.Value;
            if (tenantShell != null)
            {
                var tenant = tenantShell?.Tenant;
                var tenantPipeline = tenantShell.GetOrAddMiddlewarePipeline<TTenant>(new Lazy<Task<RequestDelegate>>(() =>
                {
                    //await tenantContainerAccessor.
                    return _factory.Get(_rootApp, tenant, tenantContainerAccessor, _next);
                }));
                var requestDelegate = await tenantPipeline.Value;
                await requestDelegate(context);
            }
            else
            {
                _logger.LogDebug("Null tenant shell - No Tenant Middleware Pipeline to execute.");
                await _next(context);
            }

        }
    }
}
