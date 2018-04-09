////////////////////////////////////////////////////////////////////////////////
//
//    WPF Example
//
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Impinj.OctaneSdk;

namespace OctaneSdkExamples
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Create an instance of the ImpinjReader class.
        private ImpinjReader reader = new ImpinjReader();

        public MainWindow(string[] args)
        {
            InitializeComponent();

            try
            {
                // Connect to the reader.
                // Pass in a reader hostname or IP address as a 
                // command line argument when running the example
                if (args.Length != 1)
                {
                    System.Diagnostics.Trace.
                        WriteLine("Error: No hostname specified.  Pass in the reader hostname as a command line argument when running the Sdk Example.");
                    return;
                }
                string hostname = args[0];
                reader.Connect(hostname);

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Tell the reader to include the antenna number
                // in all tag reports. Other fields can be added
                // to the reports in the same way by setting the 
                // appropriate Report.IncludeXXXXXXX property.
                settings.Report.IncludeAntennaPortNumber = true;

                // Send a tag report for every tag read.
                settings.Report.Mode = ReportMode.Individual;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                // This method will in turn call a delegate 
                // to update the UI (Listbox).
                reader.TagsReported += OnTagsReported;
            }
            catch (OctaneSdkException ex)
            {
                // An Octane SDK exception occurred. Handle it here.
                System.Diagnostics.Trace.
                    WriteLine("An Octane SDK exception has occurred : {0}", ex.Message);
            }
            catch (Exception ex)
            {
                // A general exception occurred. Handle it here.
                System.Diagnostics.Trace.
                    WriteLine("An exception has occurred : {0}", ex.Message);
            }
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Don't call the Start method if the
                // reader is already running.
                if (!reader.QueryStatus().IsSingulating)
                {
                    // Start reading.
                    reader.Start();
                }
            }
            catch (OctaneSdkException ex)
            {
                // An Octane SDK exception occurred. Handle it here.
                System.Diagnostics.Trace.
                    WriteLine("An Octane SDK exception has occurred : {0}", ex.Message);
            }
            catch (Exception ex)
            {
                // A general exception occurred. Handle it here.
                System.Diagnostics.Trace.
                    WriteLine("An exception has occurred : {0}", ex.Message);
            }
        }

        private void UpdateListbox(List<Tag> list)
        {
            // Loop through each tag is the list and add it to the Listbox.
            foreach (var tag in list)
            {
                listTags.Items.Add(tag.Epc + ", " + tag.AntennaPortNumber);
            }
        }

        private void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // This event handler gets called when a tag report is available.
            // Since it is executed in a different thread, we cannot operate
            // directly on UI elements (the Listbox) in this method.
            // We must execute another method (UpdateListbox) on the main thread
            // using BeginInvoke. We will pass updateListbox a List of tags.
            Action action = delegate()
            {
                UpdateListbox(report.Tags);
            };

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Don't call the Stop method if the
                // reader is already stopped.
                if (reader.QueryStatus().IsSingulating)
                {
                    reader.Stop();
                }
            }
            catch (OctaneSdkException ex)
            {
                // An Octane SDK exception occurred. Handle it here.
                System.Diagnostics.Trace.
                    WriteLine("An Octane SDK exception has occurred : {0}", ex.Message);
            }
            catch (Exception ex)
            {
                // A general exception occurred. Handle it here.
                System.Diagnostics.Trace.
                    WriteLine("An exception has occurred : {0}", ex.Message);
            }
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            // Clear all the readings from the Listbox.
            listTags.Items.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // The application is closing.
            // Stop the reader and disconnect.
            try
            {
                // Don't call the Stop method if the
                // reader is already stopped.
                if (reader.QueryStatus().IsSingulating)
                {
                    reader.Stop();
                }
                // Disconnect from the reader.
                reader.Disconnect();
            }
            catch (OctaneSdkException ex)
            {
                // An Octane SDK exception occurred. Handle it here.
                System.Diagnostics.Trace.
                    WriteLine("An Octane SDK exception has occurred : {0}", ex.Message);
            }
            catch (Exception ex)
            {
                // A general exception occurred. Handle it here.
                System.Diagnostics.Trace.
                    WriteLine("An exception has occurred : {0}", ex.Message);
            }
        }
    }
}
