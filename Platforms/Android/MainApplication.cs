﻿using Android.App;
using Android.Runtime;
using RosyCrow.Platforms.Android;

namespace RosyCrow;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
        TransparentTrustProvider.Register();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
