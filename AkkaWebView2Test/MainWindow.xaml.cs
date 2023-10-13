using Akka.Actor;
using Akka.Configuration;
using Akka.Event;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.WebUI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AkkaWebView2Test
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            var configPath = System.IO.Path.Combine(Package.Current.InstalledPath, "Akka.conf");
            var configText = File.ReadAllText(configPath);
            var akkaConfig = ConfigurationFactory.ParseString(configText);
            var system = ActorSystem.Create("test", akkaConfig);

            this.InitializeComponent();

            var webView = new WebView2();

            var actorRef = system.ActorOf(Props.Create(() => new WebView2Actor()).WithDispatcher("synchronized-dispatcher"));
            actorRef.Tell(new Work() { WebView = webView, Window = this });
        }

    }

    public class Work
    {
        public WebView2 WebView { get; init; }
        public MainWindow Window { get; init; }
    }

    public class WebView2Actor : ReceiveActor
    {
        override protected void PreStart()
        {
            Become(Working); // makes no difference - could be declared in WebView2Actor().
        }

        public WebView2Actor() : base()
        {            
        }

        private void Working()
        {
            ReceiveAsync<Work>(async message =>
            {
                try
                {
                    Sender.Tell(message);
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"before: {ex}");
                }

                await message.WebView.EnsureCoreWebView2Async();
                message.Window.Content = message.WebView;
                message.WebView.CoreWebView2.Navigate("https://www.microsoft.com/surface");

                try
                {
                    Sender.Tell(message);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"after: {ex}");
                }
            });
        }
    }
}
