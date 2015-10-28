using System;
using System.Collections.Generic;
using System.Text;

namespace SharpNeatExperiments.Luigi
{
    public enum LuigiMode
    {
        LUIGI_SMALL,
        LUIGI_BIG,
        LUIGI_FIRE
    }
    public enum LevelMode
    {
        OVERGROUND,
        UNDERGROUND,
        CASTLE,
        RANDOM
    }
    public enum ZMode
    {
        Z0,
        Z1,
        Z2
    }
    public enum JumpScript
    {
        NONE,
        HYBRID,
        FULL
    }
    public class LuigiParameters
    {
        #region Constants
        public const bool MAXIMIZE_FPS = true;
        public const bool ENABLE_GAME_VIEWER = true;
        public const int PORT_NUMBER = 4242;
        public const int RANDOM_PORT_MIN = 4000;
        public const int RANDOM_PORT_MAX = 5000;
        public const bool RANDOM_PORT = false;
        public const bool ENABLE_VISUALIZATION = true;
        public const bool STOP_SIM_AFTER_FIRST_WIN = true;
        public const LuigiMode LUIGI_MODE = LuigiMode.LUIGI_FIRE;
        public const LevelMode LEVEL_MODE = LevelMode.RANDOM;
        public const int LEVEL_DIFFICULTY = 0;
        public const int LEVEL_LENGTH = 320;
        public const int TIME_LIMIT = 200;
        public const int NUMBER_OF_RUNS = 1;
        public const int LEVEL_RANDOMIZATION_SEED = 1;
        public const bool USE_RANDOM_SEED = false;
        public const int LEVEL_RANDOM_SEED_MIN = 1;
        public const int LEVEL_RANDOM_SEED_MAX = 1000;
        public const double FITNESS_DISTANCE_COEFFICIENT = 1;
        public const double FITNESS_TIME_COEFFICIENT = 0;
        public const double FITNESS_COIN_COEFFICIENT = 0;
        public const double FITNESS_LUIGI_SIZE_COEFFICIENT = 0;
        public const double FITNESS_KILLS_COEFFICIENT = 0;
        public const double FITNESS_POWERUPS_COEFFICIENT = 0;
        public const double FITNESS_BRICKS_COEFFICIENT = 0;
        public const double FITNESS_VICTORY_BONUS = 50;
        public const double FITNESS_SUICIDE_PENALTY = 50;
        public const double FITNESS_STOP_THRESHOLD = 1000;
        public const double FITNESS_JUMP_PENALTY = 0;
        public const bool FITNESS_USE_STOP_THRESHOLD = false;
        public const int GENERATION_STOP_THRESHOLD = 100;
        public const bool USE_GENERATION_STOP_THRESHOLD = false;
        public const ZMode Z_MAP = ZMode.Z0;
        public const ZMode Z_ENEMY = ZMode.Z0;
        //public const bool USE_JUMP_SCRIPT = false;
        public const JumpScript JUMP_SCRIPT = JumpScript.FULL;
        public const bool USE_INVERSE_DISTANCES = false;
        public const bool PLAY_SOUND_ON_WIN = true;
        public const bool INCREASE_DIFFICULTY = false;
        public const bool INCREASE_LENGTH = false;
        public const int INCREASE_DIFFICULTY_AMOUNT = 1;
        public const int INCREASE_LENGTH_AMOUNT = 50;
        #endregion
        #region Fields
        public Random rand;
        public bool maximizeFps;
        public bool enableGameViewer;
        public int portNumber;
        public bool randomPort;
        public int randomPortMin;
        public int randomPortMax;
        public bool enableVisualization;
        public bool stopSimAfterFirstWin;
        public LuigiMode luigiMode;
        public LevelMode levelMode;
        public int levelDifficulty;
        public int levelLength;
        public int timeLimit;
        public int numberOfRuns;
        public int levelRandomizationSeed;
        public bool useRandomSeed;
        public int levelRandomizationSeedMin;
        public int levelRandomizationSeedMax;
        public double fitnessDistanceCoefficient;
        public double fitnessTimeCoefficient;
        public double fitnessCoinCoefficient;
        public double fitnessLuigiSizeCoefficient;
        public double fitnessKillsCoefficient;
        public double fitnessPowerupsCoefficient;
        public double fitnessBricksCoefficient;
        public double fitnessStopThreshold;
        public bool fitnessUseStopThreshold;
        public double fitnessVictoryBonus;
        public double fitnessSuicidePenalty;
        public double fitnessJumpPenalty;
        public int generationStopThreshold;
        public bool useGenerationStopThreshold;
        public ZMode zMap;
        public ZMode zEnemy;
        //public bool useJumpScript;
        public JumpScript jumpScript;
        public bool useInverseDistances;
        public bool playSoundOnWin;
        public string parametersAsString;
        public List<int> randomSeeds = new List<int>();
        public bool increaseDifficulty;
        public bool increaseLength;
        public int increaseDifficultyAmount;
        public int increaseLengthAmount;
        public bool shouldIncreaseDifficultyOrLength;
        #endregion
        #region Constructor
        public LuigiParameters()
        {
            rand = new Random((int)(DateTime.Now.Ticks));
            maximizeFps = MAXIMIZE_FPS;
            enableGameViewer = ENABLE_GAME_VIEWER;
            portNumber = PORT_NUMBER;
            randomPort = RANDOM_PORT;
            randomPortMin = RANDOM_PORT_MIN;
            randomPortMax = RANDOM_PORT_MAX;
            enableVisualization = ENABLE_VISUALIZATION;
            stopSimAfterFirstWin = STOP_SIM_AFTER_FIRST_WIN;
            luigiMode = LUIGI_MODE;
            levelMode = LEVEL_MODE;
            levelDifficulty = LEVEL_DIFFICULTY;
            levelLength = LEVEL_LENGTH;
            timeLimit = TIME_LIMIT;
            numberOfRuns = NUMBER_OF_RUNS;
            levelRandomizationSeed = LEVEL_RANDOMIZATION_SEED;
            useRandomSeed = USE_RANDOM_SEED;
            levelRandomizationSeedMin = LEVEL_RANDOM_SEED_MIN;
            levelRandomizationSeedMax = LEVEL_RANDOM_SEED_MAX;
            fitnessDistanceCoefficient = FITNESS_DISTANCE_COEFFICIENT;
            fitnessTimeCoefficient = FITNESS_TIME_COEFFICIENT;
            fitnessCoinCoefficient = FITNESS_COIN_COEFFICIENT;
            fitnessLuigiSizeCoefficient = FITNESS_LUIGI_SIZE_COEFFICIENT;
            fitnessKillsCoefficient = FITNESS_KILLS_COEFFICIENT;
            fitnessPowerupsCoefficient = FITNESS_POWERUPS_COEFFICIENT;
            fitnessBricksCoefficient = FITNESS_BRICKS_COEFFICIENT;
            fitnessVictoryBonus = FITNESS_VICTORY_BONUS;
            fitnessSuicidePenalty = FITNESS_SUICIDE_PENALTY;
            fitnessJumpPenalty = FITNESS_JUMP_PENALTY;
            fitnessStopThreshold = FITNESS_STOP_THRESHOLD;
            fitnessUseStopThreshold = FITNESS_USE_STOP_THRESHOLD;
            generationStopThreshold = GENERATION_STOP_THRESHOLD;
            useGenerationStopThreshold = USE_GENERATION_STOP_THRESHOLD;
            zMap = Z_MAP;
            zEnemy = Z_ENEMY;
            //useJumpScript = USE_JUMP_SCRIPT;
            jumpScript = JUMP_SCRIPT;
            useInverseDistances = USE_INVERSE_DISTANCES;
            playSoundOnWin = PLAY_SOUND_ON_WIN;
            increaseDifficulty = INCREASE_DIFFICULTY;
            increaseLength = INCREASE_LENGTH;
            increaseDifficultyAmount = INCREASE_DIFFICULTY_AMOUNT;
            increaseLengthAmount = INCREASE_LENGTH_AMOUNT;
            shouldIncreaseDifficultyOrLength = false;
            UpdateString(true);
        }

        public void UpdateString(bool forcedUpdate)
        {
            if(!forcedUpdate)
                IncreaseDifficultyAndLengthIfDesired();
            parametersAsString = ToString();
        }

        public override string ToString()
        {
            string toReturn = string.Empty;
            toReturn += "-maxFPS:" + (maximizeFps ? "on" : "off");
            toReturn += " -gvc:" + (enableGameViewer ? "on" : "off");
            toReturn += " -vis:" + (enableVisualization ? "on" : "off");
            toReturn += " -ssiw:" + (stopSimAfterFirstWin ? "on" : "off");
            
            toReturn += " -mm:";
            if (luigiMode == LuigiMode.LUIGI_SMALL) toReturn += "0";
            else if (luigiMode == LuigiMode.LUIGI_BIG) toReturn += "1";
            else if (luigiMode == LuigiMode.LUIGI_FIRE) toReturn += "2";
            else
                System.Windows.Forms.MessageBox.Show("Error in LuigiParameters.ToString(), invalid LuigiMode!");

            toReturn += " -lt:";
            if (levelMode == LevelMode.OVERGROUND) toReturn += "0";
            else if (levelMode == LevelMode.UNDERGROUND) toReturn += "1";
            else if (levelMode == LevelMode.CASTLE) toReturn += "2";
            else if (levelMode == LevelMode.RANDOM) toReturn += rand.Next(0, 2).ToString();
            else
                System.Windows.Forms.MessageBox.Show("Error in LuigiParameters.ToString(), invalid LevelMode!");

            toReturn += " -ld:" + levelDifficulty.ToString();
            toReturn += " -ll:" + levelLength.ToString();
            toReturn += " -tl:" + timeLimit.ToString();

            toReturn += " -zm:";
            if (zMap == ZMode.Z0) toReturn += "0";
            else if (zMap == ZMode.Z1) toReturn += "1";
            else if (zMap == ZMode.Z2) toReturn += "2";
            else
                System.Windows.Forms.MessageBox.Show("Error in LuigiParameters.ToString(), invalid zMap!");

            toReturn += " -ze:";
            if (zEnemy == ZMode.Z0) toReturn += "0";
            else if (zEnemy == ZMode.Z1) toReturn += "1";
            else if (zEnemy == ZMode.Z2) toReturn += "2";
            else
                System.Windows.Forms.MessageBox.Show("Error in LuigiParameters.ToString(), invalid zEnemy!");

            toReturn += " -port:" + portNumber;

            // ALWAYS HAVE RANDOM SEED LAST
            if (useRandomSeed)
                levelRandomizationSeed = rand.Next(levelRandomizationSeedMin, levelRandomizationSeedMax+1);
            toReturn += " -ls:" + levelRandomizationSeed.ToString();
            return toReturn;
        }

        public void UpdateRandomSeed()
        {
            // JAT TODO URGENT - DO NOT ASK FOR MORE RUNS THAN RANDOM SEED RANGE!!!!
            randomSeeds.Clear();
            if (useRandomSeed && numberOfRuns > 1)
            {
                for (int i = 0; i < numberOfRuns; i++)
                {
                    if (levelRandomizationSeedMax - levelRandomizationSeedMin + 1 < numberOfRuns)
                        randomSeeds.Add(rand.Next(levelRandomizationSeedMin, levelRandomizationSeedMax+1));
                    else
                    {
                        int seed = rand.Next(levelRandomizationSeedMin, levelRandomizationSeedMax+1);
                        while (randomSeeds.Contains(seed))
                            seed = rand.Next(levelRandomizationSeedMin, levelRandomizationSeedMax+1);
                        randomSeeds.Add(seed);
                    }
                }
            }
        }

        public void UpdateParametersWithRandomSeed(int runNumber)
        {
            if (randomSeeds.Count == 0)
                return;
            if (runNumber >= randomSeeds.Count)
                System.Windows.Forms.MessageBox.Show("Error! runNumber exceeds random seed array bounds! diaf");
            else if(numberOfRuns > 1)
            {
                int index = parametersAsString.IndexOf(" -ls");
                if(index != -1)
                    parametersAsString = parametersAsString.Remove(index);
                parametersAsString += " -ls:" + randomSeeds[runNumber];
            }
        }

        public void IncreaseDifficultyAndLengthIfDesired()
        {
            if (increaseLength && shouldIncreaseDifficultyOrLength)
                levelLength += increaseLengthAmount;
            if (increaseDifficulty && shouldIncreaseDifficultyOrLength)
                levelDifficulty += increaseDifficultyAmount;
            shouldIncreaseDifficultyOrLength = false;
        }

        public void BeatLevel()
        {
            shouldIncreaseDifficultyOrLength = true;
        }

        #endregion
    }
}
