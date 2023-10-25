﻿using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RosyCrow.Extensions;
using RosyCrow.Models.Serialization;
using RosyCrow.Views;
using Tab = RosyCrow.Models.Tab;

// ReSharper disable AsyncVoidLambda

namespace RosyCrow;

public partial class App : Application
{
    private readonly ILogger<App> _logger;

    public App()
    {
        InitializeComponent();

        _logger = MauiProgram.Services.GetRequiredService<ILogger<App>>();
        MainPage = new NavigationPage(MauiProgram.Services.GetRequiredService<MainPage>());
    }

    public async Task HandleImportTabsIntent(Stream stream)
    {
        try
        {
            if ((MainPage as NavigationPage)?.RootPage is not MainPage page)
                return;

            using var reader = new StreamReader(stream);

            try
            {
                var tabs = JsonConvert.DeserializeObject<SerializedTab[]>(await reader.ReadToEndAsync());

                if (tabs?.Any() ?? false)
                {
                    if (!await page.DisplayAlertOnMainThread("Import Tabs", $"Do you want to import {tabs.Length} tabs?", "Yes", "No"))
                        return;

                    await page.TabCollection.ImportTabs(tabs.Select(t => new Tab
                    {
                        Url = t.Url,
                        Label = t.Icon
                    }));
                }
                else
                {
                    page.ShowToast("The file contains no tabs", ToastDuration.Short);
                }
            }
            catch (JsonException)
            {
                page.ShowToast("The file is formatted incorrectly", ToastDuration.Long);
                throw;
            }
            catch (Exception)
            {
                page.ShowToast("No tabs were imported because something went wrong", ToastDuration.Long);
                throw;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not handle tab import intent");
        }
        finally
        {
            stream?.Close();
        }
    }

    public void HandleNavigationIntent(string uri)
    {
        try
        {
            _logger.LogInformation("Handling intent to navigate to {URL}", uri);

            if (!string.IsNullOrWhiteSpace(uri) && (MainPage as NavigationPage)?.RootPage is MainPage page)
            {
                try
                {
                    var alreadyOpenTab = page.TabCollection.Tabs
                        .FirstOrDefault(t => uri.AreGeminiUrlsEqual(t.Url));

                    // if this URL is already open in some tab, just select that tab.
                    // otherwise, add a new tab
                    if (alreadyOpenTab != null)
                        page.TabCollection.SelectTab(alreadyOpenTab);
                    else
                        Dispatcher.Dispatch(async () => await page.TabCollection.AddTab(uri.ToGeminiUri()));
                }
                catch (Exception)
                {
                    page.ShowToast("Something went wrong, so the link could not be opened", ToastDuration.Long);
                    throw;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Could not handle navigation intent for {URL}", uri);
        }
    }
}