using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpNeatLib.NeuralNetwork;
using System.Net.Sockets;
using System.Text;


namespace SharpNeatExperiments.Luigi
{
    public class LuigiEnvironment
    {
        #region Constants

        // Sensor sizes and counts (in nodes)
        public const int SENSOR_SIZE_PIT = 2;
        public const int SENSOR_COUNT_PIT = 1;

        public const int SENSOR_SIZE_ENEMY = 2;
        public const int SENSOR_COUNT_ENEMY = 3;

        public const int SENSOR_SIZE_POWERUP = 2;
        public const int SENSOR_COUNT_POWERUP = 2;

        public const int SENSOR_SIZE_ENVIRO = 2;
        public const int SENSOR_COUNT_ENVIRO = 1;

        public const int SENSOR_COUNT_LUIGI = 3; // Player specific information

        // Output nodes
        public const int OUTPUT_NODE_COUNT = 4;

        public const int OUTPUT_INDEX_DIRECTION = 0;
        public const int OUTPUT_INDEX_JUMP  = 1;
        public const int OUTPUT_INDEX_SHOOT = 2;
        public const int OUTPUT_INDEX_SPEED = 3;

        #endregion

        #region Private Members

        private int mLuigiType;
        private bool mCanJump;
        private bool mCanShoot;

        #endregion

        #region Public Functions

        public int LuigiType
        {
            get { return mLuigiType; }
            set { mLuigiType = value; }
        }

        public bool CanJump
        {
            get { return mCanJump; }
            set { mCanJump = value; }
        }

        public bool CanShoot
        {
            get { return mCanShoot; }
            set { mCanShoot = value; }
        }

        public int PitSensorNodeCount
        {
            get { return SENSOR_COUNT_PIT * SENSOR_SIZE_PIT; }
        }

        public int EnemySensorNodeCount
        {
            get { return SENSOR_COUNT_ENEMY * SENSOR_SIZE_ENEMY; }
        }

        public int EnviroSensorNodeCount
        {
            get { return SENSOR_COUNT_ENVIRO * SENSOR_SIZE_ENVIRO; } 
        }

        public int PowerupSensorNodeCount
        {
            get { return SENSOR_COUNT_POWERUP * SENSOR_SIZE_POWERUP; }
        }

        public int TotalInputNodeCount
        {
            get { return PitSensorNodeCount + EnemySensorNodeCount + PowerupSensorNodeCount + EnviroSensorNodeCount + LuigiInputNodeCount; }
        }

        public int LuigiInputNodeCount
        {
            get { return SENSOR_COUNT_LUIGI; }
        }

        public void SetNetworkInputs(INetwork network, sbyte[] data, LuigiParameters param)
        {
            int node = 0;
            double maxDistance = 11 * 16 / 2;

            mLuigiType = data[0];
            mCanJump = data[1] != 0;
            mCanShoot = data[2] != 0;

            // Luigi specific inputs
            ScaleAndSetInput(network, node, data[node], 0, 2, 0, 1);
            ScaleAndSetInput(network, ++node, data[node], 0, 1, 0, 1);
            ScaleAndSetInput(network, ++node, data[node], 0, 1, 0, 1);

            // Wall sensor data
            ScaleAndSetInput(network, ++node, data[node], 0, maxDistance, 0, 1, param.useInverseDistances);
            ScaleAndSetInput(network, ++node, data[node], 0, 11, 0, 1);

            // Pit sensor data
            ScaleAndSetInput(network, ++node, data[node], 0, maxDistance, 0, 1, param.useInverseDistances);
            ScaleAndSetInput(network, ++node, data[node], 0, 11, 0, 1);

            // Enemy sensor data
            for (int i = 0; i < this.EnemySensorNodeCount; i++)
                ScaleAndSetInput(network, ++node, data[node], -maxDistance, maxDistance, -1, 1, param.useInverseDistances);

            // Powerup sensor data
            for (int i = 0; i < this.PowerupSensorNodeCount; i++)
                ScaleAndSetInput(network, ++node, data[node], -maxDistance, maxDistance, -1, 1, param.useInverseDistances);
        }

        public void ScaleAndSetInput(INetwork network, int node, double val, double minIn, double maxIn, double minOut, double maxOut)
        {
            ScaleAndSetInput(network, node, val, minIn, maxIn, minOut, maxOut, false);
        }

        public void ScaleAndSetInput(INetwork network, int node, double val, double minIn, double maxIn, double minOut, double maxOut, bool invertVal)
        {
            if (val < minIn)
                val = minIn;
            if (val > maxIn)
                val = maxIn;

            if (invertVal)
            {
                if (minIn < 0)
                {
                    if (val < 0)
                    {
                        val = minIn - val;
                    }
                    else
                    {
                        val = maxIn - val;
                    }
                }
                else
                {
                    val = maxIn - val + minIn;
                }
            }

            double inputRange = maxIn - minIn;
            double outputRange = maxOut - minOut;

            double normalized = (val - minIn) / inputRange;
            double scaled = normalized  * outputRange + minOut;

            network.SetInputSignal(node, scaled);
        }

        #endregion
    }
}
