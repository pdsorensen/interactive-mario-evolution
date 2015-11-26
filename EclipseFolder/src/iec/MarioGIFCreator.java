package iec;

import own.NEATController;
import ch.idsia.agents.Agent;
import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;
import ch.idsia.benchmark.tasks.BasicTask;
import ch.idsia.tools.MarioAIOptions;

public class MarioGIFCreator {
	/* 
	 * Creates a mario GIF from a simulation run
	 * 
	 * 
	 */
	
	
	// For debugging purposes: 
	public static void main(String[] args)
	{
	    final MarioAIOptions marioAIOptions = new MarioAIOptions(args);
	    final BasicTask basicTask = new BasicTask(marioAIOptions);
	    
	    // Uncomment to play with keyboard
	    //basicTask.setOptionsAndReset(marioAIOptions);
	    //basicTask.runSingleEpisode(1);
	    
	    Environment environment = MarioEnvironment.getInstance();
	    Agent agent = new NEATController();
	    
	    String options = "-lf on -zs 1 -ls 16 -vis on";
	    environment.reset(options);
	    environment.recordMario(false);
	    //environment.drawNEATInputsString(100, 100);
	    
	    marioAIOptions.setLevelDifficulty(0);
	    marioAIOptions.setLevelRandSeed(20);
	    basicTask.setOptionsAndReset(marioAIOptions);
	    
	    while(!environment.isLevelFinished()){
	    	environment.tick();
	    	agent.integrateObservation(environment);
	        environment.performAction(agent.getAction());
	    }
	    
	    System.out.println(environment.getEvaluationInfo());
	    System.exit(0);
	}
}
