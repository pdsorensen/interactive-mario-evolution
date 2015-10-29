using System;
using System.Collections;

using SharpNeatLib.Evolution;
using SharpNeatLib.NeuralNetwork;

namespace SharpNeatLib.Experiments
{
	public class ThreeParityExperiment : IExperiment
	{
		IPopulationEvaluator populationEvaluator;
		IActivationFunction activationFunction = new PlainSigmoid();

		#region Constructor

		public ThreeParityExperiment()
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
			populationEvaluator = new SingleFilePopulationEvaluator(new ThreeParityNetworkEvaluator(), activationFn);
		}

		public int InputNeuronCount
		{
			get
			{
				return 3;
			}
		}

		public int OutputNeuronCount
		{
			get
			{
				return 1;
			}
		}

		public NeatParameters DefaultNeatParameters
		{
			get
			{
				NeatParameters np = new NeatParameters();
				np.pOffspringAsexual = 0.8;
				np.pOffspringSexual = 0.2;

                np.connectionWeightRange = 20;

				np.pMutateConnectionWeights  = 0.78997;
				np.pMutateAddNode            = 0.00001;
				np.pMutateAddConnection      = 0.2;
                np.pMutateDeleteConnection   = 0.00001;
                np.pMutateDeleteSimpleNeuron = 0.00001;

                np.allowRecurrence = false;

				return np;
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
				return @"Three Bit Parity. The goal is to reproduce the following logic truth table:
ABC|O
-----
000|0
001|1
010|1
011|0
100|1
101|0
110|0
111|1

Each test case is tested in turn. An output less than 0.5 is interpreted as a 0(false) response, >= is interprested as a 1(true) response.
Evaluation terminates early if a network fails to relax(settle on an output value) within 10 timesteps.
A fitness of 1.0 is assigned for each correct test case, this is on a linear sliding scale that assigns 0 for a completey wrong response, e.g. 1.0 when 0.0 was expected.

An additional fitness of 10 is assigned if all four test cases are passed. Thus the maximum fitness is 18, but any score >=10 indicates
a sucessful network.";
			}
		}

		#endregion
	}
}
