package own.util;

import ch.idsia.agents.Agent;
import ch.idsia.benchmark.mario.engine.GlobalOptions;
import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;
import ch.idsia.benchmark.tasks.BasicTask;
import ch.idsia.tools.MarioAIOptions;
import own.MarioFitnessFunction;
import own.MarioInputs;

public class DeceptiveMarioLevel {
	public static void main(String[] args)
	{
		String options = "-mm 0 -mix 16 -miy 220";
	    final MarioAIOptions marioAIOptions = new MarioAIOptions(options);
	    final BasicTask basicTask = new BasicTask(marioAIOptions);
	    
	    Environment environment = MarioEnvironment.getInstance();
	    
	    // FOR DEBUGGING DRAWING FUNCTIONS
	    Agent agent = marioAIOptions.getAgent();
	    
	    marioAIOptions.setLevelDifficulty(0);
	    marioAIOptions.setLevelType(0);
	    marioAIOptions.setLevelRandSeed(0);

	    while(!environment.isLevelFinished()){
	    	environment.tick();
	    	
	    	boolean[] action = agent.getAction();
            environment.performAction(action);
	    }

	    System.exit(0);
	}
}
