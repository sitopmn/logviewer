using log4net;
using logviewer.Controls;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

namespace logviewer
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Logger for the class
        /// </summary>
        private readonly log4net.ILog _logger = LogManager.GetLogger(typeof(App));

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            Startup += OnStartup;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// Starts up the application
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Argument of the event</param>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            // configure the logger
            log4net.Config.XmlConfigurator.Configure();
            _logger.Fatal("Startup");

            // upgrade the application settings
            logviewer.Properties.Settings.Default.Upgrade();

            // build the composition container
            var container = new CompositionContainer(new ApplicationCatalog());

            // show the main window
            MainWindow = container.GetExportedValue<MainWindow>();
            MainWindow.Show();
        }

        /// <summary>
        /// Catches and logs unhandled exceptions
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Argument of the event</param>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.Fatal("Unhandled exception", (Exception)e.ExceptionObject);
        }
    }

    [Export(typeof(ParametrizedFactory<>))]
    public class ParametrizedFactory<T>
    {
        private readonly CompositionContainer _container;

        private readonly Type _metadata;

        [ImportingConstructor]
        public ParametrizedFactory(CompositionContainer container)
            : this(null, container)
        { }

        protected ParametrizedFactory(Type metadata, CompositionContainer container)
        {
            _metadata = metadata;
            _container = container;
        }

        public ExportLifetimeContext<T> CreateExport(params object[] parameters)
        {
            var type = typeof(T);
            var constructor = type.GetConstructors().FirstOrDefault(c => c.GetCustomAttribute<ImportingConstructorAttribute>() != null);
            var argTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
            var argValues = new object[argTypes.Length];

            for (var i = 0; i < argValues.Length; i++)
            {
                // try to resolve from given parameters first
                argValues[i] = parameters.SingleOrDefault(p => p.GetType() == argTypes[i]);

                // try to resolve using the container
                if (argValues[i] == null)
                {
                    var export = _container.GetExports(argTypes[i], _metadata, null).FirstOrDefault();
                    if (export != null)
                    {
                        argValues[i] = export.Value;
                    }
                }

                // error out if no value resolved yet
                if (argValues[i] == null)
                {
                    throw new ArgumentException("Cannot resolve argument");
                }
            }

            return new ExportLifetimeContext<T>((T)constructor.Invoke(argValues), () => { });
        }
    }

    [Export(typeof(ParametrizedFactory<,>))]
    public class ParametrizedFactory<T, TMetadata> : ParametrizedFactory<T>
    {
        [ImportingConstructor]
        public ParametrizedFactory(CompositionContainer container)
            : base(typeof(TMetadata), container)
        {
            var export = container.GetExports(typeof(T), typeof(TMetadata), null).FirstOrDefault();
            Metadata = (TMetadata)export.Metadata;
        }

        public TMetadata Metadata
        {
            get;
            set;
        }
    }

}
