﻿using System;
using System.Threading;
using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using Meadow.Foundation.ICs.IOExpanders;
using Meadow.Foundation.Leds;
using Meadow.Foundation.Sensors.Rotary;
using Meadow.Hardware;

namespace EngineTest
{
    /// <summary>
    /// Simple code to test DC motor controlled with IBT_2 H briidge based on two BTS7960Bs, with PWM generated by PCA9685
    /// </summary>
    public class MeadowApp : App<F7Micro, MeadowApp>
    {
        RgbPwmLed onboardLed;
        Pca9685 pca;
        II2cBus bus;
        IPwmPort lPwm;
        IPwmPort rPwm;
        RotaryEncoder encoder;
        float speed = 0;                    //Start speed
        IPwmPort currentPort;               //So I'm perfectly sure that I'm changing only one PWM port at a time
        float dSpeed = 0.01f;               //Delta speed

        public MeadowApp()
        {
            Initialize();
            onboardLed.SetColor(Color.Red); //Just to know it's ready
        }

        void Initialize()
        {
            Console.WriteLine("Initialize hardware...");

            onboardLed = new RgbPwmLed(device: Device,
                redPwmPin: Device.Pins.OnboardLedRed,
                greenPwmPin: Device.Pins.OnboardLedGreen,
                bluePwmPin: Device.Pins.OnboardLedBlue,
                3.3f, 3.3f, 3.3f,
                Meadow.Peripherals.Leds.IRgbLed.CommonType.CommonAnode);

            bus = Device.CreateI2cBus(400000);

            pca = new Pca9685(bus, 64, 500);
            pca.Initialize();

            lPwm = pca.CreatePwmPort(14, 0);
            rPwm = pca.CreatePwmPort(15, 0);

            currentPort = rPwm;

            encoder = new RotaryEncoder(Device, Device.Pins.D15, Device.Pins.D11);
            encoder.Rotated += Encoder_Rotated;
        }

        private void Encoder_Rotated(object sender, Meadow.Peripherals.Sensors.Rotary.RotaryTurnedEventArgs e)
        {
            if(e.Direction == Meadow.Peripherals.Sensors.Rotary.RotationDirection.Clockwise)
            {
                if (speed < 0 && speed + dSpeed >= 0) ChangeDirection();    //In final project software will decide about direction,
                speed += dSpeed;                                            //...But here I need to be able to reverse it with encoder
                if (speed >= 1) speed = 0.9999f;                            //When DutyCycle == 1f, it is abruptly stopped, it is 4096 levels
                currentPort.DutyCycle = Math.Abs(speed);                    //...and 4095/4096 = 0.99976 so tihs should give highest possible value smaller than 1
                Console.WriteLine("R: " + rPwm.DutyCycle + " L: " + lPwm.DutyCycle + " speed: " + speed);
            }
            else
            {
                if (speed > 0 && speed - dSpeed <= 0) ChangeDirection();
                speed -= dSpeed;
                if (speed <= -1) speed = -0.9999f;
                currentPort.DutyCycle = Math.Abs(speed);
                Console.WriteLine("R: " + rPwm.DutyCycle + " L: " +lPwm.DutyCycle + " speed: " + speed);
            }
        }

        private void ChangeDirection()
        {
            if (currentPort == rPwm)
            {
                rPwm.DutyCycle = 0;
                Thread.Sleep(50);           //Tihs is most important, as I've read IBT_2 can be easily fried with double PWM signal
                currentPort = lPwm;         //...and 50ms is small enough to be invisible with such powerfull motors, but allows me to be sure
            }                               //...that previous line defiinetelu executed and electronics had enough time to execute it
            else
            {
                lPwm.DutyCycle = 0;
                Thread.Sleep(50);
                currentPort = rPwm;
            }
        }
    }
}
