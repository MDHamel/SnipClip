﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Gaming.XboxGameBar;

namespace SnipClip
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App : Application
	{
		private XboxGameBarWidget widget1 = null;

		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			this.InitializeComponent();
			this.Suspending += OnSuspending;
		}

		protected override void OnActivated(IActivatedEventArgs args)
		{
			XboxGameBarWidgetActivatedEventArgs widgetArgs = null;
			if (args.Kind == ActivationKind.Protocol)
			{
				var protocolArgs = args as IProtocolActivatedEventArgs;
				string scheme = protocolArgs.Uri.Scheme;
				if (scheme.Equals("ms-gamebarwidget"))
				{
					widgetArgs = args as XboxGameBarWidgetActivatedEventArgs;
				}
			}
			if (widgetArgs != null)
			{
				//
				// Activation Notes:
				//
				//    If IsLaunchActivation is true, this is Game Bar launching a new instance
				// of our widget. This means we have a NEW CoreWindow with corresponding UI
				// dispatcher, and we MUST create and hold onto a new XboxGameBarWidget.
				//
				// Otherwise this is a subsequent activation coming from Game Bar. We MUST
				// continue to hold the XboxGameBarWidget created during initial activation
				// and ignore this repeat activation, or just observe the URI command here and act 
				// accordingly.  It is ok to perform a navigate on the root frame to switch 
				// views/pages if needed.  Game Bar lets us control the URI for sending widget to
				// widget commands or receiving a command from another non-widget process. 
				//
				// Important Cleanup Notes:
				//    When our widget is closed--by Game Bar or us calling XboxGameBarWidget.Close()-,
				// the CoreWindow will get a closed event.  We can register for Window.Closed
				// event to know when our particular widget has shutdown, and cleanup accordingly.
				//
				// NOTE: If a widget's CoreWindow is the LAST CoreWindow being closed for the process
				// then we won't get the Window.Closed event.  However, we will get the OnSuspending
				// call and can use that for cleanup.
				//
				if (widgetArgs.IsLaunchActivation)
				{
					var rootFrame = new Frame();
					rootFrame.NavigationFailed += OnNavigationFailed;
					Window.Current.Content = rootFrame;

					// Create Game Bar widget object which bootstraps the connection with Game Bar
					widget1 = new XboxGameBarWidget(
						widgetArgs,
						Window.Current.CoreWindow,
						rootFrame);
					rootFrame.Navigate(typeof(Widget1));

					Window.Current.Closed += Widget1Window_Closed;

					Window.Current.Activate();
				}
				else
				{
					// You can perform whatever behavior you need based on the URI payload.
				}
			}
		}

		private void Widget1Window_Closed(object sender, Windows.UI.Core.CoreWindowEventArgs e)
		{
			widget1 = null;
			Window.Current.Closed -= Widget1Window_Closed;
		}

		
		void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
		}

		/// <summary>
		/// Invoked when application execution is being suspended.  Normally we
		/// wouldn't know if the app was being terminated or just suspended at this
		/// point. However, the app will never be suspended if Game Bar has an
		/// active widget connection to it, so if you see this call it's safe to
		/// cleanup any widget related objects. Keep in mind if all widgets are closed
		/// and you have a foreground window for your app, this could still result in 
		/// suspend or terminate. Regardless, it should always be safe to cleanup
		/// your widget related objects.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();

			widget1 = null;

			deferral.Complete();
		}
	}
}
