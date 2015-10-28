using System;
using System.Diagnostics;
using System.Collections.Generic;
using SharpNeatLib.NeuralNetwork;
using System.Net.Sockets;
using System.Text;
using SharpNeatExperiments.Luigi;

namespace SharpNeatLib.Experiments
{
	public class LuigiNetworkEvaluator : INetworkEvaluator
	{
        private LuigiParameters luigiParameters;

        private TcpClient mTcpClient = new TcpClient();
        private NetworkStream mStream = null;
        private ASCIIEncoding mASCIIEnc = new ASCIIEncoding();
        private LuigiEnvironment mEnvironment;

        private Process mJavaProcess = null;

        private int numJumpCommands = 0;
        private bool jumpedLastFrame = false;
        private int fireballCounter = 0;

        private bool beatLevel = false;
        private int beatLevelCount = 0;
        System.Media.SoundPlayer beatLevelSound = new System.Media.SoundPlayer("..\\..\\..\\..\\..\\..\\luigiYahoo.wav");

        #region Public Methods

        public LuigiNetworkEvaluator(LuigiParameters lp, LuigiEnvironment env)
        {
            mEnvironment = env;
            luigiParameters = lp;

            mJavaProcess = Process.Start("cmd", "/K cd ..\\..\\..\\..\\..\\..\\marioai\\classes\\ && java ch.idsia.scenarios.PlayNeuro " + luigiParameters.parametersAsString);

            try
            {
                mTcpClient.Connect("localhost", luigiParameters.portNumber);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Console.WriteLine(e.Message);
            }

            mStream = mTcpClient.GetStream();
            //mStream.ReadTimeout = 5;

            SendData("JDub Studios");

            // discard welcome message
            TaggedData ret = ReceiveData();
        }

        ~LuigiNetworkEvaluator()
        {
            if (mJavaProcess != null && !mJavaProcess.HasExited)
            {
                mJavaProcess.Kill();
            }
        }
        
        public double EvaluateNetwork(INetwork network)
        {
            double fitness = 0.0;
            int numRuns = 0;
            beatLevelCount = 0;
            int maxRuns = (luigiParameters.useRandomSeed ? luigiParameters.numberOfRuns : 1);
            for (int i = 0; i < maxRuns; i++)
            {
                numRuns++;
                beatLevel = false;
                luigiParameters.UpdateParametersWithRandomSeed(i);
                SendData(luigiParameters.parametersAsString);

                bool gotScore = false;
                double score = 0;

                while (!gotScore)
                {
                    TaggedData ret = ReceiveData();

                    if (ret.Tag == "Score")
                    {
                        gotScore = true;
                        score = GetScore(mASCIIEnc.GetString(ret.Data));
                    }
                    else if (ret.Tag == "Env")
                    {
                        sbyte[] data = new sbyte[ret.Data.Length];
                        for (int j = 0; j < ret.Data.Length; j++)
                            data[j] = (sbyte)ret.Data[j];

                        mEnvironment.SetNetworkInputs(network, data,luigiParameters);

                        if (network.RelaxNetwork(10, 0.1))
                            SendControllerCommand(network);
                        else
                            SendControllerCommand();
                    }
                }

                Console.Write("Fitness for run {0} = {1} ", i, score);
                Console.Write(beatLevel ? " LEVEL COMPLETE!\n" : "\n");
                fitness += score;

                network.ClearSignals();
            }
            fitness /= (double)numRuns;
            if(luigiParameters.numberOfRuns > 1)
                Console.Write("Average Fitness = {0}", fitness);
            if (beatLevelCount == numRuns)
            {
                if(numRuns > 1)
                    Console.Write(" COMPLETED ALL SEEDS!");
                if (luigiParameters.playSoundOnWin)
                    beatLevelSound.Play();
                luigiParameters.BeatLevel();
            }
            Console.Write("\n");
            return fitness;
        }

        public double GetScore(string scoreString)
        {
            double score = 0;
            double scoreMult = 1;
            string[] individualScores = scoreString.Split(new char[] { ';' });
            foreach (string str in individualScores)
            {
                string[] parts = str.Split(new char[] { '=' });
                string scoreName = parts[0];
                string scoreValue = parts[1];

                if (scoreName == "Distance")
                    score += Double.Parse(scoreValue) * luigiParameters.fitnessDistanceCoefficient;
                else if (scoreName == "Time")
                    score += Double.Parse(scoreValue) * luigiParameters.fitnessTimeCoefficient;
                else if (scoreName == "Coin")
                    score += Double.Parse(scoreValue) * luigiParameters.fitnessCoinCoefficient;
                else if (scoreName == "LuigiSize")
                    score += Double.Parse(scoreValue) * luigiParameters.fitnessLuigiSizeCoefficient;
                else if (scoreName == "Kills")
                    score += Double.Parse(scoreValue) * luigiParameters.fitnessKillsCoefficient;
                else if (scoreName == "Powerups")
                    score += Double.Parse(scoreValue) * luigiParameters.fitnessPowerupsCoefficient;
                else if (scoreName == "Bricks")
                    score += Double.Parse(scoreValue) * luigiParameters.fitnessBricksCoefficient;
                else if (scoreName == "Jumps")
                    score -= Double.Parse(scoreValue) * luigiParameters.fitnessJumpPenalty;
                else if (scoreName == "Success")
                {
                    scoreMult *= 1 + Int32.Parse(scoreValue) * luigiParameters.fitnessVictoryBonus / 100;
                    if (scoreValue == "1")
                    {
                        beatLevelCount++;
                        beatLevel = true;
                    }
                }
                else if (scoreName == "Suicide")
                    scoreMult *= 1 - Int32.Parse(scoreValue) * luigiParameters.fitnessSuicidePenalty / 100;
                else
                    System.Windows.Forms.MessageBox.Show("Illegal score name receieved: " + scoreName);
            }

            if (score < 0)
                score = 0;

            score *= scoreMult;

            return score;
        }

        public string EvaluatorStateMessage
		{
			get
			{
                string message = "Difficulty" + luigiParameters.levelDifficulty.ToString() + "_Length" + luigiParameters.levelLength.ToString() +
                                 "_Time" + luigiParameters.timeLimit.ToString() + "_Seeds";
                if (luigiParameters.numberOfRuns == 1)
                    message += luigiParameters.levelRandomizationSeed.ToString();
                else if (luigiParameters.numberOfRuns > 1)
                {
                    foreach (int i in luigiParameters.randomSeeds)
                    {
                        message += i.ToString() + ",";
                    }
                }
                return message;
			}
		}

        public TaggedData ReceiveData()
        {
            TaggedData ret = new TaggedData();
            int buffSize = 1024;                        
            byte[] buffer = new byte[buffSize];

            DateTime start = DateTime.Now;
            while (mTcpClient.Connected && !mStream.DataAvailable && (mStream.ReadTimeout == -1 || DateTime.Now.Subtract(start).TotalSeconds < mStream.ReadTimeout))
            {
                System.Threading.Thread.Sleep(10);
            }

            int bytesRead = 0;
            bool tagRead = false;

            while (mStream.DataAvailable && bytesRead < buffSize)
            {
                int b = mStream.ReadByte();

                if (b == -1)
                    break;

                if (!tagRead && (char)b == ':')
                {
                    ret.Tag = mASCIIEnc.GetString(buffer, 0, bytesRead);
                    tagRead = true;
                    bytesRead = 0;
                }
                else
                {
                    buffer[bytesRead++] = (byte)b;
                }                
            }

            if (!tagRead)
                ret.Tag = "Undefined";

            ret.Data = new byte[bytesRead];
            Array.Copy(buffer, ret.Data, bytesRead);

            return ret;
        }

        public struct TaggedData
        {
            public string Tag;
            public byte[] Data;
        }

        public void SendData(string str)
        {
            if (mStream == null)
                return;

            byte[] data = mASCIIEnc.GetBytes(str + "\r\n");

            mStream.Write(data, 0, data.Length);
        }

        public void SendControllerCommand(INetwork network)
        {
            // Controls
            //
            // Left  = move left
            // Right = move right
            // Down  = duck - not used, always false
            // A     = jump. only jumps if A was not already pressed. Length of time A is held = height of jump
            // B     = tap to shoot fireball, hold to run. 

            bool cLeft  = network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_DIRECTION) <= 1.0 / 3.0;
            bool cRight = network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_DIRECTION) >= 2.0 / 3.0;
            bool cDown  = false;
            bool cA     = false;
            bool cB     = false;

            if (luigiParameters.jumpScript == JumpScript.FULL)
            {
                if (numJumpCommands > 0)
                {
                    cA = true;
                    numJumpCommands--;
                }
                else if (network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_JUMP) >= 0.5)
                {
                    if (mEnvironment.CanJump)
                    {
                        numJumpCommands = 2 * (int)Math.Floor((network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_JUMP) - 0.5) * 10);
                        cA = true;
                    }
                    else
                    {
                        cA = false;
                        numJumpCommands = 0;
                    }
                }
            }
            else if (luigiParameters.jumpScript == JumpScript.HYBRID)
            {
                cA = false;
                if (numJumpCommands > 0)
                {
                    cA = true;
                    numJumpCommands--;
                }
                else if (network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_JUMP) > 0.5)
                {
                    if (mEnvironment.CanJump && !jumpedLastFrame)
                    {
                        numJumpCommands = 2 * (int)Math.Floor((network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_JUMP) - 0.5) * 10);
                        cA = true;
                    }
                    else if (jumpedLastFrame)
                    {
                        cA = network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_JUMP) > 0.5;
                    }
                }
            }
            else
            {
                cA = network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_JUMP) > 0.5;
            }

            if (fireballCounter == 0)
            {
                if (network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_SHOOT) >= 0.5)
                {
                    // if we want to shoot, then check if we can shoot
                    // if we can, then press the shoot button during this frame
                    // otherwise we need to release it and then press it on the next frame
                    if (mEnvironment.CanShoot)
                    {
                        fireballCounter = 1;
                    }
                    else
                    {
                        fireballCounter = 2;
                    }
                }
            }

            if (fireballCounter > 0)
            {
                // if counter is equal to 2, release shoot button (then press it next frame)
                // if counter is equal to 1, press shoot button
                cB = fireballCounter == 1;
                fireballCounter--;
            }
            else
            {
                // if not shooting a fireball, then check if we want to run
                cB = network.GetOutputSignal(LuigiEnvironment.OUTPUT_INDEX_SPEED) >= 0.5;
            }

            jumpedLastFrame = cA;
            SendControllerCommand(cLeft, cRight, cDown, cA, cB);
        }

        public void SendControllerCommand()
        {
            SendData("Input:00000");
        }

        public void SendControllerCommand(bool left, bool right, bool down, bool A, bool B)
        {
            string inputToSend = "Input:" + (left ? '1' : '0') + (right ? '1' : '0') + (down ? '1' : '0') + (A ? '1' : '0') + (B ? '1' : '0');
            SendData(inputToSend);
        }

		#endregion
	}

}
