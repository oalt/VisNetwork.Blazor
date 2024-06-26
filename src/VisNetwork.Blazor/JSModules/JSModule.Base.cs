﻿using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace VisNetwork.Blazor;

public partial class JSModule : IAsyncDisposable
{
    private readonly IJSRuntime jsRuntime;
    private readonly IVersionProvider versionProvider;

    private Task<IJSObjectReference>? moduleTask;
    private bool isAsyncDisposed;

    public JSModule(IJSRuntime jsRuntime, IVersionProvider versionProvider)
    {
        this.jsRuntime = jsRuntime;
        this.versionProvider = versionProvider;
    }

    private Task<IJSObjectReference> Module => moduleTask ??= jsRuntime.InvokeAsync<IJSObjectReference>("import", ModuleFileName).AsTask();

    public string ModuleFileName => $"./_content/MDD4All.VisNetwork.Blazor/BlazorVisNetwork.js?v={versionProvider.Version}";

    private async ValueTask InvokeVoidAsync(string identifier, params object[] args)
    {
        try
        {
            var module = await Module;

            if (isAsyncDisposed)
            {
                return;
            }

            await module.InvokeVoidAsync(identifier, args);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
        }
    }

    private async ValueTask<TValue> InvokeAsync<TValue>(string identifier, params object[] args)
    {
        try
        {
            var module = await Module;

            if (isAsyncDisposed)
            {
                return default!;
            }

            return await module.InvokeAsync<TValue>(identifier, args);
        }
        catch (Exception exception) when (exception is JSDisconnectedException or ObjectDisposedException or TaskCanceledException)
        {
            return default!;
        }
    }

    public async ValueTask DisposeAsync() => await DisposeAsync(true);

    private async ValueTask DisposeAsync(bool disposing)
    {
        try
        {
            if (!isAsyncDisposed)
            {
                isAsyncDisposed = true;

                if (disposing)
                {
                    if (moduleTask != null)
                    {
                        var moduleInstance = await moduleTask;
                        var disposableTask = moduleInstance.DisposeAsync();

                        try
                        {
                            await disposableTask;
                        }
                        catch when (disposableTask.IsCanceled)
                        {
                        }
                        catch (JSDisconnectedException)
                        {
                        }

                        moduleTask = null;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            await Task.FromException(exception);
        }
    }
}
