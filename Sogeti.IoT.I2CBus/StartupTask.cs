// ----------------------------------------------------------------------------
// <copyright file="StartupTask.cs" company="SOGETI Spain">
//     Copyright © 2016 SOGETI Spain. All rights reserved.
//     Basic example of I2C communication between Raspberry & Arduino by Osc@rNET.
// </copyright>
// ----------------------------------------------------------------------------
namespace Sogeti.IoT.I2CBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.IoT.Lightning.Providers;
    using Windows.ApplicationModel.Background;
    using Windows.Devices;
    using Windows.Devices.I2c;

    /// <summary>
    /// Represents the startup task.
    /// </summary>
    public sealed class StartupTask : IBackgroundTask
    {
        #region Fields

        /// <summary>
        /// Defines the I2C address of the Arduino with red LED.
        /// </summary>
        private const byte ArduinoI2CAddressWithRedLED = 0x0A;

        /// <summary>
        /// Defines the I2C address of the Arduino with yellow LED.
        /// </summary>
        private const byte ArduinoI2CAddressWithYellowLED = 0x0B;

        /// <summary>
        /// Defines the I2C address of the Arduino with blue LED.
        /// </summary>
        private const byte ArduinoI2CAddressWithBlueLED = 0x0C;

        /// <summary>
        /// Defines the I2C address of the Arduino with temperature sensor.
        /// </summary>
        private const byte ArduinoI2CAddressWithTemperatureSensor = 0x0D;

        /// <summary>
        /// Defines the I2C controller.
        /// </summary>
        private I2cController controller;

        #endregion Fields

        #region Enums

        /// <summary>
        /// Defines the I2C commands.
        /// </summary>
        private enum I2CCommands
        {
            /// <summary>
            /// Represents the command for get the current temperature.
            /// </summary>
            GetTemperature,

            /// <summary>
            /// Represents the command for turn on the red LED.
            /// </summary>
            TurnOnRedLED,

            /// <summary>
            /// Represents the command for turn off the red LED.
            /// </summary>
            TurnOffRedLED,

            /// <summary>
            /// Represents the command for turn on the yellow LED.
            /// </summary>
            TurnOnYellowLED,

            /// <summary>
            /// Represents the command for turn off the yellow LED.
            /// </summary>
            TurnOffYellowLED,

            /// <summary>
            /// Represents the command for turn on the blue LED.
            /// </summary>
            TurnOnBlueLED,

            /// <summary>
            /// Represents the command for turn off the blue LED.
            /// </summary>
            TurnOffBlueLED
        }

        #endregion Enums

        #region Methods

        /// <summary>
        /// Performs the work of a background task. The system calls this method when the associated background task has been triggered.
        /// </summary>
        /// <param name="taskInstance">An interface to an instance of the background task. The system creates this instance when the task has been triggered to run.</param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            if (LightningProvider.IsLightningEnabled)
            {
                LowLevelDevicesController.DefaultProvider = LightningProvider.GetAggregateProvider();
            }

            controller = await I2cController.GetDefaultAsync();

            if (!RunLEDsTest())
            {
                deferral.Complete();
                return;
            }

            byte currentTemperature;
            while ((currentTemperature = PerformCommand(I2CCommands.GetTemperature)) > 0)
            {
                Debug.Write(string.Format("Temperature: {0} C. ", currentTemperature));

                if ((currentTemperature > 0) && (currentTemperature <= 15))
                {
                    Debug.WriteLine(" It's cold!");
                    PerformCommand(I2CCommands.TurnOnBlueLED);
                    PerformCommand(I2CCommands.TurnOffYellowLED);
                    PerformCommand(I2CCommands.TurnOffRedLED);
                }
                else if ((currentTemperature > 15) && (currentTemperature <= 25))
                {
                    Debug.WriteLine("It's tempered!");
                    PerformCommand(I2CCommands.TurnOffBlueLED);
                    PerformCommand(I2CCommands.TurnOnYellowLED);
                    PerformCommand(I2CCommands.TurnOffRedLED);
                }
                else
                {
                    Debug.WriteLine(" It's hot!");
                    PerformCommand(I2CCommands.TurnOffBlueLED);
                    PerformCommand(I2CCommands.TurnOffYellowLED);
                    PerformCommand(I2CCommands.TurnOnRedLED);
                }

                await Task.Delay(5000);
            }

            deferral.Complete();
        }

        /// <summary>
        /// Performs the given command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>
        /// The command result; otherwise, a zero value.
        /// </returns>
        private byte PerformCommand(I2CCommands command)
        {
            bool result = false;

            switch (command)
            {
                case I2CCommands.GetTemperature:
                    return ReceiveByte(ArduinoI2CAddressWithTemperatureSensor);

                case I2CCommands.TurnOnRedLED:
                    result = SendData(ArduinoI2CAddressWithRedLED, new byte[] { 0x01 });
                    break;

                case I2CCommands.TurnOffRedLED:
                    result = SendData(ArduinoI2CAddressWithRedLED, new byte[] { 0x00 });
                    break;

                case I2CCommands.TurnOnYellowLED:
                    result = SendData(ArduinoI2CAddressWithYellowLED, new byte[] { 0x01 });
                    break;

                case I2CCommands.TurnOffYellowLED:
                    result = SendData(ArduinoI2CAddressWithYellowLED, new byte[] { 0x00 });
                    break;

                case I2CCommands.TurnOnBlueLED:
                    result = SendData(ArduinoI2CAddressWithBlueLED, new byte[] { 0x01 });
                    break;

                case I2CCommands.TurnOffBlueLED:
                    result = SendData(ArduinoI2CAddressWithBlueLED, new byte[] { 0x00 });
                    break;
            }

            return (result) ? (byte)1 : (byte)0;
        }

        /// <summary>
        /// Receives a byte from the given address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>
        /// The received byte; otherwise, a zero value.
        /// </returns>
        private byte ReceiveByte(byte address)
        {
            byte[] data = new byte[1];

            I2cConnectionSettings connection =
                new I2cConnectionSettings(address)
                {
                    BusSpeed = I2cBusSpeed.FastMode,
                    SharingMode = I2cSharingMode.Exclusive
                };

            using (I2cDevice device = controller.GetDevice(connection))
            {
                I2cTransferResult transferResult = device.ReadPartial(data);
                if (transferResult.Status == I2cTransferStatus.FullTransfer)
                {
                    return data[0];
                }
            }

            return 0;
        }

        /// <summary>
        /// Runs the LEDs test.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the LEDs test was successfully; otherwise, <c>false</c>.
        /// </returns>
        private bool RunLEDsTest()
        {
            bool result = true;

            for (int count = 0; count < 5; count++)
            {
                result = result && (PerformCommand(I2CCommands.TurnOnBlueLED) != 0);
                result = result && (PerformCommand(I2CCommands.TurnOnYellowLED) != 0);
                result = result && (PerformCommand(I2CCommands.TurnOnRedLED) != 0);

                result = result && (PerformCommand(I2CCommands.TurnOffBlueLED) != 0);
                result = result && (PerformCommand(I2CCommands.TurnOffYellowLED) != 0);
                result = result && (PerformCommand(I2CCommands.TurnOffRedLED) != 0);
            }

            return result;
        }

        /// <summary>
        /// Sends data to the given address.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <param name="data">The data.</param>
        /// <returns>
        /// <c>true</c> if the data were sent sucessfully; otherwise, <c>false</c>.
        /// </returns>
        private bool SendData(byte address, byte[] data)
        {
            I2cConnectionSettings connection =
                new I2cConnectionSettings(address)
                {
                    BusSpeed = I2cBusSpeed.FastMode,
                    SharingMode = I2cSharingMode.Exclusive
                };

            using (I2cDevice device = controller.GetDevice(connection))
            {
                I2cTransferResult transferResult = device.WritePartial(data);
                if (transferResult.Status == I2cTransferStatus.FullTransfer)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion Methods
    }
}
