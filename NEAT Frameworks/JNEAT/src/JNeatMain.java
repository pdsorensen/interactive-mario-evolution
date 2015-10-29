import java.util.Vector;

import jneat.*;

public class JNeatMain {
	
	
	public static void main(String[] args)
	{
	   System.out.println("Hello");

	   //Population neatPop = new Population(); 
	   Population neatPop = new Population(
			   10 /* population size */, 
			   2 /* network inputs */,
			   2 /* network outputs */,
			   5 /* max index of nodes */, 
			   true /* recurrent */, 
			   0.5 /* probability of connecting two nodes */ );
	   //Population neatPop2 = new 
	   //Population neatPop = new Population("Name");
	   int numInputs = 10; 
	   
	   Vector neatOrgs = neatPop.getOrganisms();
	   
	   for(int i=0;i<neatOrgs.size();i++)
	   {
		  System.out.println("For loop");
	     // Extract the neural network from the jNEAT organism.
	     Network brain = ((Organism)neatOrgs.get(i)).getNet();
	    
	     double inputs[] = new double[numInputs+1];
	     inputs[numInputs] = -1.0; // Bias
	     
	     // Populate the rest of "inputs" from this organism's status in the simulation.
	     //
	     //
	    
	     // Load these inputs into the neural network.
	     brain.load_sensors(inputs);
	    
	     int net_depth = brain.max_depth();
	     // first activate from sensor to next layer....
	     brain.activate();
	    
	     // next activate each layer until the last level is reached
	     for (int relax = 0; relax <= net_depth; relax++)
	     {
	         brain.activate();
	     }
	           
	     // Retrieve outputs from the final layer.
	     double output1 = ((NNode) brain.getOutputs().elementAt(0)).getActivation(); 
	     double output2 = ((NNode) brain.getOutputs().elementAt(1)).getActivation();
	    
	     // Use the outputs to modify the associated member of the population.
	     //
	     //
	    
	   }
	}
}
