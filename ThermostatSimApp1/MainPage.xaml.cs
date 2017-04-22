using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ThermostatSimApp1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Initialize the IoT Device as Thermostat.
        private IoTDevice iotDevice = new IoTDevice()
        {
            deviceId = "th01",
            deviceType = "thermostat",
            deviceDescription = "An IoT Thermostat",
            deviceValue = 5, deviceRoom = "Bedroom",
            deviceHubAccessKey = ""
        };

        public MainPage()
        {
            this.InitializeComponent();

            // Display the IoT device values in GUI.
            textBlockDeviceIdChange.Text = iotDevice.deviceId;
            textBlockDeviceTypeChange.Text = iotDevice.deviceType;
            textBlockDeviceDescriptionChange.Text = iotDevice.deviceDescription;
            textBlockDeviceRoomChange.Text = iotDevice.deviceRoom;
            //sliderTempValue.Value = iotDevice.deviceValue;

            // Register device to Azure IoT Hub and local SQL Database.
            registerDevice();

            // Retrieve latest known value.
            sliderTempValue.Value = MySqlDB.deviceLastValueFromDB(iotDevice.deviceId);

            // Send device value to SQL DB every second.
            ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
            {
                //do some work \ dispatch to UI thread as needed
                deviceToDB();
            }, TimeSpan.FromSeconds(1));

            //// Send messages from device to cloud.
            //deviceToCloud(iotDevice.deviceHubAccessKey);

            //background();
        }

        //protected override void OnNavigatedTo(NavigationEventArgs e)
        //{
        //    if (e.NavigationMode == NavigationMode.New)
        //    {
        //        test();
        //    }
        //}

        //private async void background()
        //{
        //    var extendedSession = new ExtendedExecutionSession();
        //    extendedSession.Reason = ExtendedExecutionReason.LocationTracking;
        //    extendedSession.Description = "Location tracking";

        //    ExtendedExecutionResult result = await extendedSession.RequestExtensionAsync();
        //    if (result == ExtendedExecutionResult.Allowed)
        //    {
        //        // Send device value to SQL DB every second.
        //        ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer((t) =>
        //        {
        //            //do some work \ dispatch to UI thread as needed
        //            deviceToDB();
        //        }, TimeSpan.FromSeconds(1));

        //        Debug.WriteLine("Background execution approved");
        //    }
        //    else
        //    {
        //        Debug.WriteLine("Background execution denied");
        //    }
        //}

        // Get the value of the slider when it changes.
        private void sliderTempValue_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                // Convert slider value from double to string.
                iotDevice.deviceValue = slider.Value;
                // Display the slider value to GUI.
                textBlockDeviceValueChange.Text = iotDevice.deviceValue + " °C"; // ALT + 0176 -> °
            }
        }

        // Register device to Azure IoT Hub and local SQL Database.
        private async void registerDevice()
        {
            // Register the device to the cloud and receive the access key.
            iotDevice.deviceHubAccessKey = await AzureIoTHub.AddDeviceAsync(iotDevice.deviceId);

            // Register the device to the database.
            MySqlDB.registerDeviceToDB(iotDevice.deviceId, iotDevice.deviceType, iotDevice.deviceDescription, iotDevice.deviceRoom);

            cloudToDevice(iotDevice.deviceHubAccessKey);
        }

        // .
        private void deviceToDB()
        {
            // Get current date and time.
            DateTime dTime = DateTime.Now;

            // Send device value to the database.
            MySqlDB.deviceToDB(iotDevice.deviceId, iotDevice.deviceValue, dTime.ToString("yyyy-MM-ddTHH:mm:ss"));
        }

        // Device to Cloud Communication.
        //private async void deviceToCloud(string deviceSharedAccessKey)
        //{
        //    // Get current date and time.
        //    DateTime dt;

        //    // 
        //    var registerTelemetryMessage = new AzureIoTHub.Telemetry
        //    {
        //        deviceStatus = 0,
        //        deviceId = iotDevice.deviceId,
        //        deviceType = iotDevice.deviceType,
        //        deviceDescription = iotDevice.deviceDescription,
        //        deviceValue = iotDevice.deviceValue,
        //        deviceRoom = iotDevice.deviceRoom,
        //    };
        //    await AzureIoTHub.SendDeviceToCloudMessageAsync(registerTelemetryMessage, iotDevice.deviceId, deviceSharedAccessKey);

        //    while (true)
        //    {
        //        // Get current date and time
        //        dt = DateTime.Now;
        //        // Get seconds, convert them to string and pass them in the GUI
        //        textBlockSecondsValue.Text = System.Convert.ToString(dt.Second);

        //        // If a minute is passed send date, time 
        //        // and temperature value to the cloud
        //        if (dt.Second == 00)
        //        {
        //            var telemetryMessage = new AzureIoTHub.Telemetry
        //            {
        //                deviceStatus = 1,
        //                deviceId = iotDevice.deviceId,
        //                deviceType = "",
        //                deviceDescription = "",
        //                deviceValue = iotDevice.deviceValue,
        //                deviceRoom = "",
        //                dTime = dt.ToString("yyyy-MM-ddTHH:mm:ss"),
        //            };
        //            await AzureIoTHub.SendDeviceToCloudMessageAsync(telemetryMessage, iotDevice.deviceId, deviceSharedAccessKey);
        //        }
        //        await Task.Delay(TimeSpan.FromSeconds(1));
        //    }
        //}

        // Listen for messages from the cloud
        // Set temperature value according to the cloud message
        private async void cloudToDevice(string deviceSharedAccessKey)
        {
            while (true)
            {
                string receivedMessage = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync(iotDevice.deviceId, deviceSharedAccessKey);
                iotDevice.deviceValue = Convert.ToDouble(receivedMessage);

                if (iotDevice.deviceValue >= 5 && iotDevice.deviceValue <= 30)
                    sliderTempValue.Value = iotDevice.deviceValue;
            }
        }
    }
}
