package own;

import java.nio.DoubleBuffer;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;

import org.apache.log4j.Logger;
import org.jgap.BulkFitnessFunction;
import org.jgap.Chromosome;
import org.jgap.Configuration;

import com.anji.integration.Activator;
import com.anji.integration.ActivatorTranscriber;
import com.anji.integration.TargetFitnessFunction;
import com.anji.integration.TranscriberException;
import com.anji.persistence.Persistence;
import com.anji.util.Arrays;
import com.anji.util.Configurable;
import com.anji.util.DummyConfiguration;
import com.anji.util.Properties;

import ch.idsia.agents.Agent;
import ch.idsia.benchmark.mario.engine.sprites.Mario;
import ch.idsia.benchmark.mario.environments.Environment;
import ch.idsia.benchmark.mario.environments.MarioEnvironment;
import ch.idsia.benchmark.tasks.BasicTask;
import ch.idsia.tools.MarioAIOptions;

public class MarioFitnessFunction implements BulkFitnessFunction, Configurable {

	private static Logger logger = Logger.getLogger( TargetFitnessFunction.class );
	private ActivatorTranscriber factory;
	private int numTrials = 3;
	
	
	//MARIO VARIABLES
	static final MarioAIOptions marioAIOptions = new MarioAIOptions();
    static final BasicTask basicTask = new BasicTask(marioAIOptions);
    static Environment environment = MarioEnvironment.getInstance();
    static Agent agent = new NEATController();
    
    //Info on stage
    double[] state; 
    double[][] fullState;
    double[][] limitedState;
    int detectionRadius = 1;
    int radNorth, radEast, radSouth, radWest;
    int radCenter = 9;
    int zLevelScene = 0;
    int zLevelEnemies = 0;
    protected byte[][] levelScene;
    protected byte[][] mergedObservation;
    //Control buttons
    boolean[] actions = new boolean[Environment.numberOfKeys]; 
	
	
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
		environment.reset(marioAIOptions);
	}
	
	public void evaluate( Chromosome c ) {
		System.out.println("EVALUATE CHROMOSOME");
		try {
			Activator activator = factory.newActivator( c );
			//System.out.println("ACTIVATOR: " + activator.getClass());

			marioAIOptions.setVisualization(false);
			
			// calculate fitness, sum of multiple trials
			int fitness = 0;
			environment.reset(marioAIOptions);
			for ( int i = 0; i < numTrials; i++ )
				fitness += singleTrial( activator );
			System.out.println("EVALUATE: fitness score,  " + fitness);
			c.setFitnessValue( fitness );
		}
		catch ( Throwable e ) {
			logger.warn( "error evaluating chromosome " + c.toString(), e );
			c.setFitnessValue( 0 );
		}
	}
	

	
	private int singleTrial( Activator activator ) {
		
		
		int fitness = 0;
		//logger.debug( "state = " + Arrays.toString( state ) );
		double[] networkInput;
		//levelScene = environment.getLevelSceneObservationZ(zLevelScene);
	    levelScene = environment.getMergedObservationZZ(zLevelScene, zLevelEnemies);
	    
	    //state = newState();
	    setRadius(2, 2, 2, 2);
		
		while(!environment.isLevelFinished()){
			//Set all actions to false
			resetActions();
			
//			#For each tick do
				
				//GET STATE
//				state = getStateFromStage();
				fullState = getFullStateFromStage();
				
				
				limitedState = getLimitedStateFromStage();
				
				//networkInput = new double[ levelScene.length * levelScene[0].length ];
				networkInput = new double[ getXdimensionLength() * getYdimensionLength() ];
				
				//SET INPUTS
//				networkInput = state;
				//networkInput = getTwoDimToOneDimArray(fullState);
				networkInput = getTwoDimToOneDimArray(limitedState);
				//System.out.println("Xdim: " + getXdimensionLength() +  " Ydim: " + getYdimensionLength());
				//System.out.println("networkInput: " + networkInput.length);
				//Give the network some inputs
				double[] networkOutput = activator.next(networkInput);

				//Perform some action based on networkOutput
				environment.performAction(getAction(networkOutput));
				makeTick();
				
	    }
		fitness = environment.getEvaluationInfo().distancePassedCells;
		//System.out.println("Fitness: " + fitness);
		//double networkOutput = activator.next( networkInput )[ 0 ];
		//logger.debug( "trial took " + currentTimestep + " steps" );
		return fitness;
	}
	
	
	private double[] newState() {
		double[] state = new double[ 2 ];
		state[ 0 ] = state[ 1 ] =  0;
		return state;
	}
	
	/*
	 * @return The state of the Mario World. Atm only the two by three blocks in front of Mario
	 */
	public double[] getStateFromStage(){
		
		double[] inputs =  { levelScene[ 9 ][ 10 ], levelScene[9][11]};

		return inputs;	
	}
	
	private double[][] newFullState(){
		
		double[][] state = new double[ levelScene.length ][ levelScene[0].length ];
		
		for(int i = 0; i < levelScene.length; i++)
			for(int j = 0; j < levelScene[i].length; j++)
				state[ i ][ j ] = 0;
		
		return state;
	}
	
	/*
	 * @return The FULL state of the Mario World
	 */
	private double[][] getFullStateFromStage(){

		double[][] inputs = newFullState();
		
		for(int i = 0; i < levelScene.length; i++)
			for(int j = 0; j< levelScene[i].length; j++)
				inputs[ i ][ j ] = levelScene[ i ][ j ];
			
		return inputs;
	}
	
	private double[] getTwoDimToOneDimArray(double[][] state){
		
		double[] newArray = new double[state.length * state[0].length];
		
		for(int i = 0; i < state.length; i++){
			double[] row = state[i];
			
			for(int j = 0; i < row.length; i++){
				newArray[i * row.length + j] = state[i][j];
			}
		}
		
		return newArray;
	}
	
	private double[][] getBlankLimitedState(){
		
		//Calculate dimension lengths
		int xDimension = getXdimensionLength();
		int yDimension = getYdimensionLength();
		
		//Create array
		double[][] state = new double[ xDimension ][ yDimension ];
		
		//Reset array - is it necessary?
		for(int i = 0; i < xDimension; i++)
			for(int j = 0; j < yDimension; j++)
				state[ i ][ j ] = 0;
		
		
		return state;
	}
	
	private double[][] getLimitedStateFromStage(){
		
		double[][] inputs = getBlankLimitedState();
		
		//Calculate dimension lengths
		int xDimension = getXdimensionLength();
		int yDimension = getYdimensionLength();
		
		//Create array
		double[][] state = new double[ xDimension ][ yDimension ];
		
		//Put values in the array
		for(int i = getStartX(); i < xDimension; i++)
			for(int j = getStartY(); j< yDimension; j++)
				inputs[ i ][ j ] = levelScene[ i ][ j ];
		
		return inputs;
	}
	
	/*
	 * Set radius for all 4 directions;
	 */
	private void setRadius(int north, int east, int south, int west){	
		radNorth = north;
		radEast = east;
		radSouth = south;
		radWest = west;	
	}
	
	private int getXdimensionLength(){
		int xDimension = ( radCenter + radEast ) - ( radCenter - radWest ) + 1;
		return xDimension;
	}
	
	private int getYdimensionLength(){
		int yDimension = ( radCenter + radEast ) - ( radCenter - radWest ) + 1;
		return yDimension;
	}
	
	private int getStartX(){
		return radCenter - radWest;
	}
	private int getStartY(){
		return radCenter - radSouth;
	}
	
	
	
	
	
	
	public boolean[] getAction(double[] networkOutput){
		
		if(networkOutput[0] < 0.5)
			actions[Mario.KEY_LEFT] = false;
		else
			actions[Mario.KEY_LEFT] = true;
		
		if(networkOutput[1] < 0.5)
			actions[Mario.KEY_RIGHT] = false;
		else
			actions[Mario.KEY_RIGHT] = true;
		
		if(networkOutput[2] < 0.5)
			actions[Mario.KEY_DOWN] = false;
		else
			actions[Mario.KEY_DOWN] = true;
		
		if(networkOutput[3] < 0.5)
			actions[Mario.KEY_UP] = false;
		else
			actions[Mario.KEY_UP] = true;
		
		if(networkOutput[4] < 0.5)
			actions[Mario.KEY_JUMP] = false;
		else
			actions[Mario.KEY_JUMP] = true;
		
		if(networkOutput[5] < 0.5)
			actions[Mario.KEY_SPEED] = false;
		else
			actions[Mario.KEY_SPEED] = true;
		
		return actions;
	}
	
	public void resetActions(){
	    actions[Mario.KEY_LEFT] = false;
	    actions[Mario.KEY_RIGHT] = false;
	    actions[Mario.KEY_DOWN] = false;
	    actions[Mario.KEY_UP] = false;
	    actions[Mario.KEY_JUMP] = false;
	    actions[Mario.KEY_SPEED] = false;
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
		return 10;
	}
	
	public void showBestMario(Chromosome c){
//		Persistence db = (Persistence) props.newObjectProperty( Persistence.PERSISTENCE_CLASS_KEY );
//		Configuration config = new DummyConfiguration();
//		Chromosome chrom = db.loadChromosome( , config );
//		if ( chrom == null )
//			throw new IllegalArgumentException( "no chromosome found: " + args[ 1 ] );
//		ff.enableDisplay();
//		ff.evaluate( chrom );
	}
	
	
}
