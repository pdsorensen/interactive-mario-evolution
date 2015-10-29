using System;
using System.Collections;
using System.Collections.Generic;

using SharpNeatLib.Evolution;
using SharpNeatLib.NeuralNetwork;
using SharpNeatExperiments.Luigi;

namespace SharpNeatLib.Experiments
{
	public class LuigiExperiment : IExperiment
	{
		IPopulationEvaluator populationEvaluator;
		IActivationFunction activationFunction = new PlainSigmoid();
        private LuigiEnvironment mEnvironment = new LuigiEnvironment();

		#region Constructor

        public LuigiExperiment()
		{
		}

		#endregion

		#region IExperiment Members
		
		/// <summary>
		/// This method is called immediately following instantiation of an experiment. It is used
		/// to pass in a hashtable of string key-value pairs from the 'experimentParameters' 
		/// block of the experiment configuration block within the application config file.
		/// 
		/// If no parameters where specified then an empty Hashtable is used.
		/// </summary>
		/// <param name="parameterTable"></param>
		public void LoadExperimentParameters(Hashtable parameterTable)
		{
		}

		public IPopulationEvaluator PopulationEvaluator
		{
			get
			{
				if(populationEvaluator==null)
					ResetEvaluator(activationFunction);
				return populationEvaluator;
			}
		}

        public void ResetEvaluator(IActivationFunction activationFn)
        {
            populationEvaluator = new SingleFilePopulationEvaluator(new LuigiNetworkEvaluator(new LuigiParameters(),mEnvironment), activationFn);
        }

		public void ResetEvaluator(IActivationFunction activationFn, LuigiParameters mp)
		{
            populationEvaluator = new SingleFilePopulationEvaluator(new LuigiNetworkEvaluator(mp,mEnvironment), activationFn);
		}

		public int InputNeuronCount
		{
			get
			{
				return mEnvironment.TotalInputNodeCount;
			}
		}

		public int OutputNeuronCount
		{
			get
			{
				return LuigiEnvironment.OUTPUT_NODE_COUNT;
			}
		}

		public NeatParameters DefaultNeatParameters
		{
			get
			{
				NeatParameters np = new NeatParameters();
				np.pOffspringAsexual = 0.8;
				np.pOffspringSexual = 0.2;

				np.pMutateConnectionWeights  = 0.6;
				np.pMutateAddNode            = 0.20;
				np.pMutateAddConnection      = 0.19;
                np.pMutateDeleteConnection   = 0.005;
                np.pMutateDeleteSimpleNeuron = 0.005;
                np.populationSize = 20;
                np.targetSpeciesCountMin = 4;
                np.targetSpeciesCountMin = 6;
                np.allowRecurrence = true;

				return np;
			}
		}

        public LuigiParameters DefaultLuigiParameters
        {
            get
            {
                LuigiParameters lp = new LuigiParameters();
                lp.timeLimit = 60;
                lp.enableGameViewer = false;
                lp.enableVisualization = false;
                lp.useRandomSeed = false;
                lp.levelMode = LevelMode.OVERGROUND;
                lp.zMap = ZMode.Z1;
                lp.zEnemy = ZMode.Z1;
                lp.jumpScript = JumpScript.NONE;
                lp.useInverseDistances = true;

                // for powerups only!
                //lp.fitnessDistanceCoefficient = 0;
                //lp.fitnessBricksCoefficient = 5;
                //lp.fitnessPowerupsCoefficient = 20;
                lp.luigiMode = LuigiMode.LUIGI_SMALL;
                return lp;
            }
        }

		public IActivationFunction SuggestedActivationFunction
		{
			get
			{
				return activationFunction;
			}
		}

		public AbstractExperimentView CreateExperimentView()
		{
			return null;
		}

		public string ExplanatoryText
		{
			get
			{
                return @"LUIGI!";
			}
		}

		#endregion
	}
}
