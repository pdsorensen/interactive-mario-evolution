using System;
using SharpNeatLib.NeuralNetwork;

namespace SharpNeatLib.Experiments
{
	public class ThreeParityNetworkEvaluator : INetworkEvaluator
	{
		#region Public Methods

        public string evaluatorMessage = string.Empty;

        public double EvaluateNetwork(INetwork network)
        {
            double fitness = 0.0;
            bool pass = true;
            string tempMessage = string.Empty;
            for (int i = 0; i < 2; i++){
                for (int j = 0; j < 2; j++){
                    for (int k = 0; k < 2; k++){
                        network.ClearSignals();
                        network.SetInputSignals(new double[] { (double)i, (double)j, (double)k });
                        if (!network.RelaxNetwork(10, 0.01)) // Any networks that don't relax 
                            return 0.0;                      // are unlikely to be any good to us
                        double output = network.GetOutputSignal(0);
                        double addedFitness = (i + j + k) % 2 == 0 ? (1 - output) : output;
                        if (addedFitness <= 0.5) // does not get a bonus if the output is incorrect
                            pass = false;
                        fitness += addedFitness*addedFitness; // squared fitness function
                        tempMessage += "(" + i.ToString() + j.ToString() + k.ToString() + "|" + 
                            output.ToString().Substring(0, Math.Min(output.ToString().Length, 6)) +") ";
                    }
                }
            }
            if (pass){
                fitness += 10.0;
                evaluatorMessage = tempMessage;
            }
            return fitness;
        }

		public string EvaluatorStateMessage
		{
			get
			{	
				return evaluatorMessage;
			}
		}

		#endregion
	}
}
