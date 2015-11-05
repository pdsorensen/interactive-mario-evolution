package own;

import java.nio.DoubleBuffer;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;

import org.apache.log4j.Logger;
import org.jgap.BulkFitnessFunction;
import org.jgap.Chromosome;

import com.anji.integration.Activator;
import com.anji.integration.ActivatorTranscriber;
import com.anji.integration.TargetFitnessFunction;
import com.anji.integration.TranscriberException;
import com.anji.util.Arrays;
import com.anji.util.Configurable;
import com.anji.util.Properties;

import ch.idsia.agents.Agent;
import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;
import ch.idsia.benchmark.tasks.BasicTask;
import ch.idsia.tools.MarioAIOptions;

public class MarioFitnessFunction implements BulkFitnessFunction, Configurable {

	private static Logger logger = Logger.getLogger( TargetFitnessFunction.class );
	private ActivatorTranscriber factory;
	private int numTrials = 10; 
	
	
	//MARIO VARIABLES
	static final MarioAIOptions marioAIOptions = new MarioAIOptions();
    static final BasicTask basicTask = new BasicTask(marioAIOptions);
    static Environment environment = MarioEnvironment.getInstance();
    static Agent agent = new NEATController();
    
    //Info on stage
    double[] state; 
    int zLevelScene = 0;
    protected byte[][] levelScene;
	
	@Override
	public void init(Properties props) throws Exception {
		// TODO Auto-generated method stub
		System.out.println("INITTING");
		factory = (ActivatorTranscriber) props.singletonObjectProperty( ActivatorTranscriber.class );		
	}

	@Override
	public void evaluate(List genotypes) {
		// TODO Auto-generated method stub

		Iterator it = genotypes.iterator();
		while ( it.hasNext() ) {
			Chromosome genotype = (Chromosome) it.next();
			evaluate(genotype);
			//GET MARIO TO PLAY...
		}
		System.out.println("EVALUATING LIST END");
	}
	
	public void evaluate( Chromosome c ) {
		System.out.println("EVALUATE CHROMOSOME");
		try {
			Activator activator = factory.newActivator( c );
			//System.out.println("ACTIVATOR: " + activator.getClass());

			// calculate fitness, sum of multiple trials
			int fitness = 0;
			for ( int i = 0; i < numTrials; i++ )
				fitness += singleTrial( activator );
			c.setFitnessValue( fitness );
		}
		catch ( Throwable e ) {
			logger.warn( "error evaluating chromosome " + c.toString(), e );
			c.setFitnessValue( 0 );
		}
	}
	
	private double[] newState() {
		double[] state = new double[ 2 ];
		state[ 0 ] = state[ 1 ] = 0;
		return state;
	}
	
	private int singleTrial( Activator activator ) {
		state = newState();
		int fitness = 0;
		logger.debug( "state = " + Arrays.toString( state ) );
		double[] networkInput;
	    levelScene = environment.getLevelSceneObservationZ(zLevelScene);
		

	    System.out.println("RUNNING SIMULATION");
		while(!environment.isLevelFinished()){
			
//			#For each tick do
//				getState 
//				setInputs
				networkInput = new double[ 2 ];
				state = getStateFromStage();
				networkInput[ 0 ] = state[ 0 ]; 
				networkInput[ 1 ] = state[ 1 ]; 
				
				double networkOutput = activator.next(networkInput)[0];
				System.out.println("NetworkOutput: " + networkOutput);
				//performAction(networkOutput, state);
				makeTick();
	    }
		System.out.println("END OF SIMULATION");
		//double networkOutput = activator.next( networkInput )[ 0 ];
		//logger.debug( "trial took " + currentTimestep + " steps" );
		return fitness;
	}
			
	public double[] getStateFromStage(){
		
		double[] inputs =  { levelScene[9][10], levelScene[9][11]};// = new double[2];
		
	    //GET INPUTS FROM STAGE
//	    for(int i = 9; i<levelScene.length; i++){
//			for(int j = 9; j<levelScene[i].length; j++){
//			
//			}
//		}
		
		return inputs;
		
	}
	
	public void performAction(){
		
		environment.performAction(agent.getAction());	
		
	}
	
	public void makeTick(){
		environment.tick();
    	agent.integrateObservation(environment);
        
	}
	
	@Override
	public int getMaxFitnessValue() {
		// TODO Auto-generated method stub
		return 0;
	}
	
	
	
}
