﻿//Edward Li
//Angela Chen
//Darius Kianersi
//Anish Gorentala
//Aneesh Boreda
//Shreepa Parthaje
//Suhas Nandiraju

using System;
using System.Drawing;
using System.Windows.Forms;
using XInput.Wrapper;


namespace GUI
{
    public partial class MainForm : Form
    {
        private ROV rov;
        private SerialCommunication comms;

        private AttitudeIndicator attitudeIndicator;
        private HeadingIndicator headingIndicator;
        private DepthIndicator depthIndicator;
        public X.Gamepad pilot, copilot;

        public MainForm()
        {
            //controller
            pilot = X.Gamepad_1;
            pilot.Enable = true;
            pilot.Update(); //must call update right after setting enable to true in order for it to connect
            //copilot = X.Gamepad_2;
            //copilot.Enable = false; //change later on

            //copilot = X.Gamepad_2;
            //copilot.Enable = false;

            //setup window
            this.KeyPreview = true;
            InitializeComponent();

            depthIndicator = new DepthIndicator() { Location = new Point(0, 100) };
            attitudeIndicator = new AttitudeIndicator() { Location = new Point(100, 100) };
            headingIndicator = new HeadingIndicator() { Location = new Point(600, 100) };
            Controls.Add(depthIndicator);
            Controls.Add(attitudeIndicator);
            Controls.Add(headingIndicator);

            //setup devices
            BetterSerialPort port = new BetterSerialPort("COM6", 115200);
            portLabel.Text = string.Format("{0}@{1}baud", port.PortName, port.BaudRate);
            comms = new SerialCommunication(port);
            comms.Stopped += comms_Stopped;
            comms.Started += comms_Started;
            //comms.Connect();

            rov = new ROV(comms);

            //update displays when sensors polled
            rov.OrientationSensor.Updated += OrientationSensor_Updated;
            rov.DepthSensor.Updated += DepthSensor_Updated;
        }

        private void DepthSensor_Updated(object sender, DepthData e)
        {
            depthIndicator.Depth = rov.DepthSensor.Data.DepthValue;
        }

        private void OrientationSensor_Updated(object sender, OrientationData e)
        {
            attitudeIndicator.PitchAngle = rov.OrientationSensor.Data.Pitch;
            attitudeIndicator.RollAngle = rov.OrientationSensor.Data.Roll;
            attitudeIndicator.YawAngle = rov.OrientationSensor.Data.Yaw;
            headingIndicator.Heading = rov.OrientationSensor.Data.Yaw;
        }

        private void comms_Started(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                connectButton.Text = "Comms Started";
            }));
        }

        private void comms_Stopped(object sender, EventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                connectButton.Text = "Comms Stopped";
            }));
        }

        private void timer500_Tick(object sender, EventArgs e)
        {
            queueLabel.Text = "Queue length: " + comms.Queue.Count;
            armButton.Text = rov.StatusSensor.Data.Status == ROVStatus.ARMED ? "Armed" : "Disarmed";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            comms.LinkActive = !comms.LinkActive;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(rov.StatusSensor.Data.Status == ROVStatus.ARMED)
            {
                rov.StatusActuator.Data.DesiredStatus = ROVStatus.DISARMED;
            } else if(rov.StatusSensor.Data.Status == ROVStatus.DISARMED)
            {
                rov.StatusActuator.Data.DesiredStatus = ROVStatus.ARMED;
            }
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            rov.StatusActuator.Data.DesiredStatus = ROVStatus.REBOOT;
        }


        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Environment.Exit(1);
        }

        private void controllerUpdateTimer_Tick(object sender, EventArgs e)
        {
            pilot.Update();
            if (pilot.IsConnected)
            {
                ConnectionB.BackColor = Color.Green;
                ConnectionLabel.Text = "" + pilot.LTrigger;
                button0.Text = "LStick.X" + pilot.LStick.X;
                button1.Text = "LStick.Y" + pilot.LStick.Y;
                button2.Text = "LStick" + pilot.LStick_down;
                button3.Text = "RStick.X" + pilot.RStick.X;
                button4.Text = "RStick.Y" + pilot.RStick.Y;
                button5.Text = "DPad up" + pilot.Dpad_Up_down;
                button6.Text = "DPad down" + pilot.Dpad_Down_down;
                button7.Text = "DPad left" + pilot.Dpad_Left_down;
                button8.Text = "DPad right" + pilot.Dpad_Right_down;
                button9.Text = "A" + pilot.A_down;
                button10.Text = "B" + pilot.B_down;
                button11.Text = "X" + pilot.X_down;
                button12.Text = "Y" + pilot.Y_down;
                button13.Text = "LBumper" + pilot.LBumper_down;
                button14.Text = "RBumper" + pilot.RBumper_down;
                button15.Text = "LTrigger" + pilot.LTrigger;
                button16.Text = "RTrigger" + pilot.RTrigger;
                button17.Text = "Start" + pilot.Start_down;

                //Lstick controls horizontal translations
                rov.ForeAftMotion = ConvertUtils.Map(pilot.LStick.Y, -32768, 32767, -100, 100);
                rov.StrafeMotion = ConvertUtils.Map(pilot.LStick.X, -32768, 32767, -100, 100);
                if (rov.EnableHeadingLock)
                {
                    //RStick controls desired heading
                    rov.TurnMotion = 0;
                    rov.DesiredHeading += ConvertUtils.Map(pilot.RStick.X, -32768, 32767, -100, 100) / 100;
                }
                else
                {
                    //Rstick controls yaw (turning about vertical axis)
                    rov.TurnMotion = ConvertUtils.Map(pilot.RStick.X, -32768, 32767, -100, 100);
                }
                    
                //left bumper moves downward, right bumper moves upward
                rov.VerticalMotion = ConvertUtils.Map(pilot.LTrigger, 0, 255, 0, -100) + ConvertUtils.Map(pilot.RTrigger, 0, 255, 0, 100);
            }
            else
            {
                ConnectionB.BackColor = Color.DarkRed;
                ConnectionLabel.Text = "no" + pilot.LTrigger;
            }

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < 6; i++)
            {
                //rov.PropulsionActuator.Data.Speeds[i] = Int32.Parse(textBox1.Text);
            }
        }
    }   
}
